using System.Diagnostics;

/*
TODO:
increase read/write speed by implementing a
sort that goes x,z,y on spans

implement visibility of faces

implement generating of meshes
*/

/// <summary>
/// <para>
/// Stores a 16x16x16 grid of ints in a very memory
/// efficient way. The storage method also allows
/// for quick generation meshes.
/// </para>
/// 
/// <para>
/// 8000 VoxelChunks will take up 264MB of memory in the worst
/// case which is 4096 unique blocks within a VoxelChunk. This
/// is very unlikely.
/// 8000 VoxelChunks loaded results in 160 cubes in any direction
/// from the center.
/// </para>
/// 
/// <para>
/// 8000 VoxelChunks with the average case of 25 unique
/// blocks with reasonably split geometry should take up
/// less than 5MB.
/// </para>
/// 
/// <para>
/// 8000 non-empty VoxelChunks best case will take up
/// less than 1MB of memory.
/// </para>
/// 
/// <para>
/// The trade off for this efficiency is that read / 
/// write speed is slow. Setting a block worst case
/// is 2-3 ms. And reading a block from a position
/// worst case is 2-3 ms.
/// </para>
/// 
/// <para>
/// High memory fragmentation can be avoid if all sets,
/// are called on a per VoxelChunk basis, before moving on
/// to the next VoxelChunk. After all data is inserted, it
/// should not change very often, so the higher memory fragmentation
/// vs bit arrays does not come into play.
/// </para>
/// </summary>
class VoxelChunk
{
    /// <summary>
    /// block id to block spans of that block type
    /// </summary>
    private readonly List<CuboidSpan> Spans;

    public VoxelChunk()
    {
        Spans = new();
        return;
    }

    public int GetBlock(Point3D position)
    {
        // TODO: sorting
        foreach (CuboidSpan span in Spans)
        {
            if (span.Contains(position)) return span.Id;
        }

        return 0;
    }

    public void SetBlock(Point3D position, ushort blockId)
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
        ushort blockId
        )
    {
        CuboidSpan span = new(blockId, startPosition, endPosition);
        Split(span);
        if (blockId == 0)
        {
            return;
        }

        // TODO: sorting
        Spans.Add(span);
        MergeRecursively(span);
        return;
    }

    /// <summary>
    /// Inserts the span into Spans while keeping ordering.
    /// </summary>
    private void InsertSpan(CuboidSpan span)
    {
        int low = 0;
        int high = Spans.Count - 1;
        int middle = high / 2;
        while (low < high)
        {
            middle = low + ((high - low) / 2);
            if (Spans[middle].Compare(span) == Positioning.before)
            {
                high = middle - 1;
            }
            if (Spans[middle].Compare(span) == Positioning.after)
            {
                low = middle + 1;
            }
            if (Spans[middle].Compare(span) == Positioning.overlap)
            {
                // shouldn't be possible?
            }
        }
        return;
    }

    /// <summary>
    /// Merges all spans of a given block type together,
    /// if they can be merged. This function runs recursively
    /// to merge all eligible spans for a given block type.
    /// </summary>
    /// <returns>true if any merges completed. Otherwise
    /// returns false</returns>
    private bool MergeRecursively(CuboidSpan span)
    {
        // TODO: sorting
        // span does not need to be removed from the Blocks
        // since it is the newly created span coming in.
        // however, all other spans that can be merged recursively
        // must be removed from Blocks.
        CuboidSpan? mergedSpan = MergeWithNeighbors(span);
        if (mergedSpan == null)
        {
            return false;
        }
        // mergedSpan needs to be recursively merged
        while (mergedSpan != null)
        {
            mergedSpan = MergeWithNeighbors(
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
    private CuboidSpan? MergeWithNeighbors(
        CuboidSpan span,
        bool removeOldSpan = false
    )
    {
        // TODO: may be possible to optimize this search with a
        // cache or something?
        for (int x = 0; x < Spans.Count; x++)
        {
            if (span.CanMerge(Spans[x]))
            {
                CuboidSpan mergedSpan = Spans[x];
                mergedSpan.Merge(span);
                Spans[x] = mergedSpan;
                if (removeOldSpan)
                {
                    Spans.Remove(span);
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

    public void RemoveAllBlocks()
    {
        Spans.Clear();
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
        // TODO: sorting
        // TODO: like 80% of the time for setting / getting
        // a block, is spent in this function (verified)
        // could sort spans by x,z,y.
        // need to implement and then time it.
        for (int x = 0; x < Spans.Count; x++)
        {
            if (Spans[x].Intersects(splitter))
            {
                CuboidSpan span = Spans[x];
                Spans.Remove(span);
                Spans.AddRange(span.Split(splitter));
            }
        }
        return;
    }

    public override string ToString()
    {
        string value = "";
        foreach (CuboidSpan span in Spans)
        {
            value += $"blockId: {span.Id}\n";
            value += $"{span}\n\n";
        }
        return value;
    }

    public int DebugGetTotalCuboidSpans
    {
        get
        {
            return Spans.Count;
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
        // - when a non zero block is set,
        // and a span must be split
        return new();
    }

    public VoxelChunk Clone()
    {
        var chunk = new VoxelChunk();
        foreach (CuboidSpan span in Spans)
        {

            chunk.SetBlockSpan(span.Start, span.End, (ushort)span.Id);
        }
        return chunk;
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
    private readonly ushort id;
    public readonly int Id
    {
        get
        {
            return id;
        }
    }

    // int must be 32bit
    // first two bits are not currently used
    private int data;


    // 4 bits to used encode either x, y, or z
    // for start and end
    private const int positionAxisMask = 0b1111;

    // just twelve bits set. in order to get the actual mask
    // need to left shift by z position of either start or end
    private const int startEndMask = (1 << 12) - 1;
    private const int startXMaskPosition = 26;
    private const int startXMask = positionAxisMask << startXMaskPosition;
    private const int startYMaskPosition = 22;
    private const int startYMask = positionAxisMask << startYMaskPosition;
    private const int startZMaskPosition = 18;
    private const int startZMask = positionAxisMask << startZMaskPosition;
    private const int endXMaskPosition = 14;
    private const int endXMask = positionAxisMask << endXMaskPosition;
    private const int endYMaskPosition = 10;
    private const int endYMask = positionAxisMask << endYMaskPosition;
    private const int endZMaskPosition = 6;
    private const int endZMask = positionAxisMask << endZMaskPosition;

    private const int upVisibleMask = 1 << 5;
    private const int downVisibleMask = 1 << 4;
    private const int northVisibleMask = 1 << 3;
    private const int southVisibleMask = 1 << 2;
    private const int westVisibleMask = 1 << 1;
    private const int eastVisibleMask = 1 << 0;

    private const int allFacesVisibleMask = 0b11_1111;

    public readonly Point3D Start
    {
        get
        {
            return new Point3D(
                (data & startXMask) >> startXMaskPosition,
                (data & startYMask) >> startYMaskPosition,
                (data & startZMask) >> startZMaskPosition
                );
        }
    }
    public readonly Point3D End
    {
        get
        {
            return new Point3D(
                (data & endXMask) >> endXMaskPosition,
                (data & endYMask) >> endYMaskPosition,
                (data & endZMask) >> endZMaskPosition
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
        id = 0;
        SetAllFacesVisible();
        return;
    }

    public CuboidSpan(Point3D start, Point3D end)
    {
        Debug.Assert(start.X <= end.X);
        Debug.Assert(start.Y <= end.Y);
        Debug.Assert(start.Z <= end.Z);
        id = 0;
        SetStart(start);
        SetEnd(end);
        SetAllFacesVisible();
        return;
    }

    public CuboidSpan(ushort id, Point3D start, Point3D end)
    {
        Debug.Assert(start.X <= end.X);
        Debug.Assert(start.Y <= end.Y);
        Debug.Assert(start.Z <= end.Z);
        this.id = id;
        SetStart(start);
        SetEnd(end);
        SetAllFacesVisible();
        return;
    }

    private void SetStart(Point3D start)
    {
        // sets start to 0,0,0
        data &= ~(startEndMask << startZMaskPosition);
        data |= start.ToInt() << startZMaskPosition;
        return;
    }
    private void SetEnd(Point3D end)
    {
        // sets end to 0,0,0
        data &= ~(startEndMask << endZMaskPosition);
        data |= end.ToInt() << endZMaskPosition;
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
                    id,
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
                    id,
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
                    id,
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
                    id,
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
                    id,
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
                    id,
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
        if (id != other.id)
        {
            return false;
        }
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
            id,
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

    /// <summary>
    /// Returns Position.before if other comes
    /// before this span.
    /// Returns Position.after if other comes
    /// after this span.
    /// Return Position.overlap if other overlaps
    /// with this span.
    /// </summary>
    public readonly Positioning Compare(CuboidSpan other)
    {
        if (other.Start.X < Start.X)
        {
            return Positioning.before;
        }
        if (other.Start.X > Start.X)
        {
            return Positioning.after;
        }
        // x is equal

        if (other.Start.Z < Start.Z)
        {
            return Positioning.before;
        }
        if (other.Start.Z > Start.Z)
        {
            return Positioning.after;
        }
        // z is equal

        if (other.Start.Y < Start.Y)
        {
            return Positioning.before;
        }
        if (other.Start.Y > Start.Y)
        {
            return Positioning.after;
        }
        return Positioning.overlap;
    }

    public override readonly string ToString()
    {
        return $"ID: {id}, Start: {Start}, End: {End}";
    }
}

enum Positioning
{
    before = -1,
    overlap = 0,
    after = 1,
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