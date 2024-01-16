using System.Diagnostics;

/// <summary>
/// Stores a 16x16x16 grid of ints in a very memory
/// efficient way. The storage method also allows
/// for very quickly generating vertices.
/// 
/// 1000 VoxelChunks with an average of 25 unique blocks
/// per chunk, and reasonably split up geometry should
/// take up ~10MB of memory. Getting and setting a single
/// block should take ~0.05ms
/// </summary>
class VoxelChunk
{
    /// <summary>
    /// block id to block spans of that block type
    /// </summary>
    private readonly Dictionary<int, List<CuboidSpan>> Blocks;
    private List<int> BlockIds
    {
        get
        {
            return Blocks.Keys.ToList();
        }
    }

    public VoxelChunk()
    {
        Blocks = new Dictionary<int, List<CuboidSpan>>();
        return;
    }

    public int GetBlock(Point3D position)
    {
        foreach (KeyValuePair<int, List<CuboidSpan>> keyValuePair in Blocks)
        {
            foreach (CuboidSpan span in keyValuePair.Value)
            {
                if (span.Contains(position)) return keyValuePair.Key;
            }
        }
        return 0;
    }

    public void SetBlock(Point3D position, int blockId)
    {
        SetBlockSpan(position, position, blockId);
        return;
    }

    /// <summary>
    /// Sets the block of the given position to 0.
    /// </summary>
    public void RemoveBlock(Point3D position)
    {
        SetBlockSpan(position, position, 0);
        return;
    }

    public void SetBlockSpan(
        Point3D startPosition,
        Point3D endPosition, // inclusive
        int blockId
        )
    {
        Split(new(startPosition, endPosition));
        if (blockId == 0)
        {
            return;
        }

        if (!BlockIds.Contains(blockId))
        {
            Blocks[blockId] = new List<CuboidSpan>() { new(startPosition, endPosition) };
            return;
        }

        CuboidSpan span = new(startPosition, endPosition);
        if (MergeAll(blockId, span))
        {
            return;
        }

        Blocks[blockId].Add(span);
        return;
    }

    /// <summary>
    /// Merges all spans of a given block type together,
    /// if they can be merged. This function runs recursively
    /// to merge all eligible spans for a given block type.
    /// </summary>
    /// <returns>true if any merges completed. Otherwise
    /// returns false</returns>
    private bool MergeAll(int blockId, CuboidSpan span)
    {
        // span does not need to be removed from the Blocks
        // since it is the newly created span coming in.
        // however, all other spans that can be merged recursively
        // must be removed from Blocks.
        CuboidSpan? mergedSpan = MergeIn(blockId, span);
        if (mergedSpan == null)
        {
            return false;
        }
        // mergedSpan needs to be recursively merged
        while (mergedSpan != null)
        {
            mergedSpan = MergeIn(
                blockId,
                (CuboidSpan)mergedSpan,
                true
            );
        }
        return true;
    }

    /// <summary>
    /// Attempts to merge the given span with all spans
    /// of the given block type so that a span that
    /// already exists, will now include the given span.
    /// 
    /// removeOldSpan will remove span from Blocks if a
    /// merge takes place.
    /// </summary>
    /// <returns>The CuboidSpan that was just updated
    /// from the merge or null if no merge occurred.</returns>
    private CuboidSpan? MergeIn(
        int blockId,
        CuboidSpan span,
        bool removeOldSpan = false
    )
    {
        // TODO: may be possible to optimize this search with a
        // cache or something?
        for (int x = 0; x < Blocks[blockId].Count; x++)
        {
            if (span.CanMerge(Blocks[blockId][x]))
            {
                var mergedSpan = Blocks[blockId][x];
                mergedSpan.Merge(span);
                Blocks[blockId][x] = mergedSpan;
                if (removeOldSpan)
                {
                    Blocks[blockId].Remove(span);
                }
                return mergedSpan;
            }
        }
        return null;
    }

    public void RemoveBlockSpan(
        Point3D startPosition,
        Point3D endPosition // inclusive
        )
    {
        SetBlockSpan(startPosition, endPosition, 0);
        return;
    }

    /// <summary>
    /// Loops through all cuboid spans within this chunk,
    /// and any spans that intersect the given span will be
    /// broken up, so that they no longer intersect.
    /// 
    /// This is used when a new block is set, or a block
    /// is removed within this chunk.
    /// </summary>
    private void Split(CuboidSpan splitter, int exclude = -1)
    {
        // to copy the list
        List<int> blockIds = BlockIds.ToList();
        // does nothing if exclude is not found in the list
        // so this is safe
        blockIds.Remove(exclude);
        foreach (int blockId in blockIds)
        {
            for (int x = 0; x < Blocks[blockId].Count; x++)
            {
                if (Blocks[blockId][x].Intersects(splitter))
                {
                    CuboidSpan span = Blocks[blockId][x];
                    Blocks[blockId].Remove(span);
                    Blocks[blockId].AddRange(span.Split(splitter));
                }
            }
        }
        return;
    }

    public override string ToString()
    {
        string value = "";
        foreach (KeyValuePair<int, List<CuboidSpan>> valuePair in Blocks)
        {
            value += $"blockId: {valuePair.Key}\n";
            foreach (CuboidSpan span in valuePair.Value)
            {
                value += $"{span}\n";
            }
            value += "\n\n";
        }
        return value;
    }

    public int DebugGetTotalCuboidSpans
    {
        get
        {
            int total = 0;
            foreach (KeyValuePair<int, List<CuboidSpan>> valuePair in Blocks)
            {
                total += valuePair.Value.Count;
            }
            return total;
        }
    }

    public List<Quad> GenerateQuads()
    {
        // inserting a new block
        // how to hide faces of other spans?
        //
        // assume that everything is visible until
        // proven invisible.
        // 
        // when are faces invisible?
        return new();
    }
}

// Lots of optimizations can be put in place to perform
// comparisons between CuboidSpans just using masks
// ie use "data" directly
/// <summary>
/// Presumes that north = z+
/// </summary>
struct CuboidSpan
{
    // int must be 32bit
    private int data;
    // 15 = 0b1111 so that can be used to mask off
    // exactly 4 bits within the int
    // first bit is reserved by int to allow for negatives
    // this will be ignored
    // next 12 bits represent start Point3D
    // first 4 = x, second 4 = y, third 4 = z
    // second 12 bits represent end Point3D
    // next 6 bits represent which faces are visible
    // last bit does nothing
    private const int startXMask = 15 << 27;
    private const int startYMask = 15 << 23;
    private const int startZMask = 15 << 19;
    private const int endXMask = 15 << 15;
    private const int endYMask = 15 << 11;
    private const int endZMask = 15 << 7;
    private const int upVisibleMask = 1 << 6;
    private const int downVisibleMask = 1 << 5;
    private const int northVisibleMask = 1 << 4;
    private const int southVisibleMask = 1 << 3;
    private const int westVisibleMask = 1 << 2;
    private const int eastVisibleMask = 1 << 1;
    private const int allFacesVisibleMask = (1 << 7) - 1;

    public readonly Point3D Start
    {
        get
        {
            return new Point3D(
                (data & startXMask) >> 27,
                (data & startYMask) >> 23,
                (data & startZMask) >> 19
                );
        }
    }
    public readonly Point3D End
    {
        get
        {
            return new Point3D(
                (data & endXMask) >> 15,
                (data & endYMask) >> 11,
                (data & endZMask) >> 7
                );
        }
    }
    public bool UpVisible
    {
        readonly get
        {
            return (data & upVisibleMask) == upVisibleMask;
        }
        set
        {
            if (value)
                data |= upVisibleMask;
            else
                data &= ~upVisibleMask;
        }
    }
    public bool DownVisible
    {
        readonly get
        {
            return (data & downVisibleMask) == downVisibleMask;
        }
        set
        {
            if (value)
                data |= downVisibleMask;
            else
                data &= ~downVisibleMask;
        }
    }
    public bool NorthVisible
    {
        readonly get
        {
            return (data & northVisibleMask) == northVisibleMask;
        }
        set
        {
            if (value)
                data |= northVisibleMask;
            else
                data &= ~northVisibleMask;
        }
    }
    public bool SouthVisible
    {
        readonly get
        {
            return (data & southVisibleMask) == southVisibleMask;
        }
        set
        {
            if (value)
                data |= southVisibleMask;
            else
                data &= ~southVisibleMask;
        }
    }
    public bool WestVisible
    {
        readonly get
        {
            return (data & westVisibleMask) == westVisibleMask;
        }
        set
        {
            if (value)
                data |= westVisibleMask;
            else
                data &= ~westVisibleMask;
        }
    }
    public bool EastVisible
    {
        readonly get
        {
            return (data & eastVisibleMask) == eastVisibleMask;
        }
        set
        {
            if (value)
                data |= eastVisibleMask;
            else
                data &= ~eastVisibleMask;
        }
    }

    public CuboidSpan()
    {
        data = 0;
        SetAllFacesVisible();
        return;
    }

    public CuboidSpan(Point3D start, Point3D end)
    {
        Debug.Assert(start.X <= end.X);
        Debug.Assert(start.Y <= end.Y);
        Debug.Assert(start.Z <= end.Z);
        SetStart(start);
        SetEnd(end);
        SetAllFacesVisible();
        return;
    }

    private void SetStart(Point3D start)
    {
        // creates 12 1's
        int startMask = (1 << 12) - 1;
        // sets start to 0,0,0
        data &= ~(startMask << 19);
        data |= start.ToInt() << 19;
        return;
    }
    private void SetEnd(Point3D end)
    {
        // creates 12 1's
        int endMask = (1 << 12) - 1;
        // sets start to 0,0,0
        data &= ~(endMask << 7);
        data |= end.ToInt() << 7;
        return;
    }

    private void SetAllFacesVisible()
    {
        data |= allFacesVisibleMask;
        return;
    }

    /// <summary>
    /// Checks if the position given is contained within
    /// this span.
    /// </summary>
    public readonly bool Contains(Point3D position)
    {
        // TODO: can be optimized by not using Start and End
        // but instead using data directly
        return Start.X <= position.X && End.X >= position.X &&
        Start.Y <= position.Y && End.Y >= position.Y &&
        Start.Z <= position.Z && End.Z >= position.Z;
    }

    /// <summary>
    /// Checks if this span intersects with the given span.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    public readonly bool Intersects(CuboidSpan span)
    {
        // TODO: can be optimized by not using Start and End
        // but instead using data directly
        return Start.X <= span.End.X && span.Start.X <= End.X &&
        Start.Y <= span.End.Y && span.Start.Y <= End.Y &&
        Start.Z <= span.End.Z && span.Start.Z <= End.Z;
    }

    /// <summary>
    /// Generates a list of CuboidSpans that cover
    /// the same space as this cuboid span
    /// </summary>
    public readonly List<CuboidSpan> Split(CuboidSpan exclude)
    {
        List<CuboidSpan> cuboids = new();
        // TODO: can be optimized by not using Start and End
        // but instead using data directly

        // There will be six potential cuboids that this
        // span could be broken into. Each cuboid (if created)
        // will share a face with exclude.

        // X axis
        // west
        if (Start.X < exclude.Start.X)
        {
            cuboids.Add(
                new CuboidSpan(
                    Start,
                    new Point3D(exclude.Start.X - 1, End.Y, End.Z)
                )
            );
        }
        // east
        if (End.X > exclude.End.X)
        {
            cuboids.Add(
                new CuboidSpan(
                    new Point3D(exclude.End.X + 1, Start.Y, Start.Z),
                    End
                )
            );
        }

        // Z axis
        // north
        if (End.Z > exclude.End.Z)
        {
            cuboids.Add(
                new CuboidSpan(
                    new Point3D(exclude.Start.X, Start.Y, exclude.End.Z + 1),
                    new Point3D(exclude.End.X, End.Y, End.Z)
                )
            );
        }
        // south
        if (Start.Z < exclude.Start.Z)
        {
            cuboids.Add(
                new CuboidSpan(
                    new Point3D(exclude.Start.X, Start.Y, Start.Z),
                    new Point3D(exclude.End.X, End.Y, exclude.Start.Z - 1)
                )
            );
        }

        // Y axis
        // up
        if (End.Y > exclude.End.Y)
        {
            cuboids.Add(
                new CuboidSpan(
                    new Point3D(exclude.Start.X, exclude.End.Y + 1, exclude.Start.Z),
                    new Point3D(exclude.End.X, End.Y, exclude.End.Z)
                )
            );
        }
        // down
        if (Start.Y < exclude.Start.Y)
        {
            cuboids.Add(
                new CuboidSpan(
                    new Point3D(exclude.Start.X, Start.Y, exclude.Start.Z),
                    new Point3D(exclude.End.X, exclude.Start.Y - 1, exclude.End.Z)
                )
            );
        }
        return cuboids;
    }

    /// <summary>
    /// Checks if this span can be merged with the
    /// given span. Two spans are considered mergeable,
    /// if they could be represented by a single span
    /// without losing any block data.
    /// </summary>
    public readonly bool CanMerge(CuboidSpan other)
    {
        /*
        just failed on
        Start(1, 1, 1), End(1, 15, 1)
        
        other
        Start(1, 0, 1), End(1, 0, 1)

        if they are adjacent, then either
        a.end + 1 = b.start
        OR
        b.end + 1 = a.start

        translates to

        (End.X + 1 == other.Start.X) || (other.End.X + 1 == Start.X)


        */
        // x adjacent
        if ((End.X + 1 == other.Start.X) || (other.End.X + 1 == Start.X))
        {
            return Start.Y == other.Start.Y &&
            Start.Z == other.Start.Z &&
            End.Y == other.End.Y &&
            End.Z == other.End.Z;
        }
        // y adjacent
        // 
        if ((End.Y + 1 == other.Start.Y) || (other.End.Y + 1 == Start.Y))
        {
            return Start.X == other.Start.X &&
            Start.Z == other.Start.Z &&
            End.X == other.End.X &&
            End.Z == other.End.Z;
        }
        // z adjacent
        if ((End.Z + 1 == other.Start.Z) || (other.End.Z + 1 == Start.Z))
        {
            return Start.X == other.Start.X &&
            Start.Y == other.Start.Y &&
            End.X == other.End.X &&
            End.Y == other.End.Y;
        }
        return false;
    }

    /// <summary>
    /// Consumes the given span so that this span covers
    /// the original area of this span plus the area of
    /// the given span.
    /// This function will update this span to cover both.
    /// The other span can safely be discarded.
    /// </summary>
    public void Merge(CuboidSpan other)
    {
        // when a new span is added to a chunk
        // (voxel world is responsible for split spans
        // into chunks if they cross several chunks)
        // then that span has its start -1 to x,y,z
        // and end +1 to x,y,z
        // it is then bounded between 0-15
        // then, (only in the match block type array)
        // relevant spans are collected by calling
        // span.Intersects(span).
        // then the expanded span is discarded (its
        // purpose was served!)
        // and then CanMerge is called on the span and all
        // relevant spans, if it returns true, then this
        // function is called between them. the new span is
        // not added, but instead the merge between the new
        // span and mergeable span, replaces the mergeable span
        SetStart(
            new Point3D(
            Math.Min(Start.X, other.Start.X),
            Math.Min(Start.Y, other.Start.Y),
            Math.Min(Start.Z, other.Start.Z)
            )
        );
        var end = new Point3D(
            Math.Max(End.X, other.End.X),
            Math.Max(End.Y, other.End.Y),
            Math.Max(End.Z, other.End.Z)
        );
        SetEnd(
            new Point3D(
            Math.Max(End.X, other.End.X),
            Math.Max(End.Y, other.End.Y),
            Math.Max(End.Z, other.End.Z)
            )
        );
        return;
    }

    /// <summary>
    /// Returns a span identical to this one,
    /// except that it is expanded by 1 in each
    /// of the 6 directions.
    /// It will still be bounded between 0 - 15.
    /// </summary>
    public readonly CuboidSpan Expand()
    {
        return new CuboidSpan(
            new Point3D(
                Math.Max(Start.X - 1, 0),
                Math.Max(Start.Y - 1, 0),
                Math.Max(Start.Z - 1, 0)
            ),
            new Point3D(
                Math.Min(End.X + 1, 15),
                Math.Min(End.Y + 1, 15),
                Math.Min(End.Z + 1, 15)
            )
        );
    }

    public override readonly string ToString()
    {
        return $"Start: {Start}, End: {End}";
    }
}

/// <summary>
/// A point in distinct 3D space.
/// ToInt() encodes x,y,z into 0-15 range
/// (inclusive) into a single int
/// </summary>
readonly struct Point3D
{
    // TODO, could convert this to only use
    // 2 bytes instead of 12
    public int X
    {
        get;
    }
    public int Y
    {
        get;
    }
    public int Z
    {
        get;
    }

    public Point3D()
    {
        X = 0;
        Y = 0;
        Z = 0;
        return;
    }

    public Point3D(int x, int y, int z)
    {
        Debug.Assert(-1 < x && x < 16);
        Debug.Assert(-1 < y && y < 16);
        Debug.Assert(-1 < y && z < 16);
        X = x;
        Y = y;
        Z = z;
        return;
    }

    public readonly int ToInt()
    {
        return (X << 8) + (Y << 4) + Z;
    }

    public override string ToString()
    {
        return "(" + X + ", " + Y + ", " + Z + ")";
    }
}

readonly struct Quad
{

    public readonly Point3D BlockId;
    public readonly Point3D Point1;
    public readonly Point3D Point2;
    public readonly Point3D Point3;
    public readonly Point3D Point4;

    public Quad(
        Point3D blockId,
        Point3D point1,
        Point3D point2,
        Point3D point3,
        Point3D point4
    )
    {
        BlockId = blockId;
        Point1 = point1;
        Point2 = point2;
        Point3 = point3;
        Point4 = point4;
        return;
    }

}