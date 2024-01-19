using System.Diagnostics;

Console.WriteLine("Starting tests");
VoxelChunkTests.Test0();
VoxelChunkTests.Test1();
VoxelChunkTests.Test2();
VoxelChunkTests.Test3();
VoxelChunkTests.Test4();
VoxelChunkTests.Test5();
VoxelChunkTests.Test6();
VoxelChunkTests.Test7();
Console.WriteLine("All tests completed");

class VoxelChunkTests
{
    static public void Test0()
    {

        float performanceCount = 200.0f;
        Stopwatch watch = new();
        watch.Start();

        for (int _ = 0; _ < performanceCount; _++)
        {
            VoxelChunk chunk = new();
            chunk.SetBlockSpan(new(0, 0, 0), new(15, 15, 15), 1);
            // this will split the chunk into 6 spans
            chunk.SetBlock(new(7, 7, 7), 0);

            /*
            chunk at this point should have the following spans:

            west
            Start(0, 0, 0), End(6, 15, 15)

            east
            Start(8, 0?, 0?), End(15, 15, 15)

            north
            Start(7, 0, 8), End(7, 15, 15)

            south
            Start(7, 0, 0), End(7, 15, 6)

            up
            Start(7, 0, 7), End(7, 6, 7)

            down
            Start(7, 8, 7), End(7, 15, 7)

            
            */

            // this should merge them back into 1 span
            chunk.SetBlock(new(7, 7, 7), 1);
            if (chunk.DebugGetTotalCuboidSpans != 1)
            {
                Console.WriteLine("Wrong number of spans.");
                Console.WriteLine("Test failed");
                return;
            }
        }

        watch.Stop();
        Console.WriteLine($"Function completed in average of " +
        $"{watch.ElapsedMilliseconds / performanceCount} milliseconds");
        Console.WriteLine("to complete 4096 set operations");
        Console.WriteLine("Test0 passed");
        return;
    }
    static public void Test1()
    {
        VoxelChunk chunk = new();
        List<Point3D> positions = new() {
            new(0, 0, 0),
            new(1, 0, 0),
            new(15, 15, 15),
            new(15, 15, 14),
            new(15, 14, 15),
            new(15, 14, 14),
            new(4, 4, 4),
            new(7, 7, 7),
        };

        foreach (Point3D point in positions)
        {
            chunk.SetBlock(point, 1);
        }
        foreach (Point3D point in positions)
        {
            bool passed = chunk.GetBlock(point) == 1;
            if (!passed)
            {
                Console.WriteLine($"failed on point {point}");
                Console.WriteLine("chunk: " + chunk.ToString());
                Console.WriteLine("test failed");
                return;
            }
        }
        Console.WriteLine("Test1 passed");
        return;
    }
    static public void Test2()
    {
        VoxelChunk chunk = new();
        chunk.SetBlockSpan(new(0, 0, 0), new(15, 15, 15), 5);
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    bool passed = chunk.GetBlock(new(x, y, z)) == 5;
                    if (!passed)
                    {
                        Console.WriteLine("chunk: " + chunk.ToString());
                        Console.WriteLine("test failed");
                        return;
                    }
                }

            }

        }
        Console.WriteLine("Test2 passed");
        return;
    }
    static public void Test3()
    {
        VoxelChunk chunk = new();
        chunk.SetBlockSpan(
            new(0, 0, 0),
            new(15, 15, 15),
            9
        );
        chunk.RemoveBlockSpan(
            new(0, 5, 5),
            new(15, 5, 5)
        );
        bool passed = chunk.GetBlock(new(0, 0, 0)) == 9;
        if (!passed)
        {
            Console.WriteLine("0, 0, 0 was not nine");
            Console.WriteLine("test failed");
            return;
        }
        passed = chunk.GetBlock(new(15, 15, 15)) == 9;
        if (!passed)
        {
            Console.WriteLine("15, 15, 15 was not nine");
            Console.WriteLine("test failed");
            return;
        }
        foreach (int x in Enumerable.Range(0, 16))
        {

            passed = chunk.GetBlock(new(x, 5, 5)) == 0;
            if (!passed)
            {
                Console.WriteLine($"{x}, 5, 5 was not zero");
                Console.WriteLine("test failed");
                return;
            }
        }
        Console.WriteLine("Test3 passed");
        return;
    }
    static public void Test4()
    {
        int count = 1000;
        long memory = GC.GetTotalMemory(true);
        List<VoxelChunk> chunks = new();
        for (int x = 0; x < count; x++)
        {
            VoxelChunk chunk = new();
            chunk.SetBlockSpan(new(0, 0, 0), new(15, 15, 15), 1);
            // this will split the chunk into 6 spans
            chunk.SetBlock(new(7, 7, 7), 0);
            chunk.SetBlock(new(9, 0, 1), 0);
            chunk.SetBlock(new(4, 7, 2), 0);
            chunk.SetBlock(new(6, 0, 0), 0);
            chunk.SetBlock(new(15, 15, 15), 0);
            chunk.SetBlock(new(2, 0, 1), 3);
            chunk.SetBlock(new(2, 7, 2), 3);
            chunk.SetBlock(new(2, 1, 1), 3);
            chunk.SetBlock(new(8, 15, 15), 8);
            chunk.SetBlock(new(8, 14, 15), 9);
            chunk.SetBlock(new(8, 13, 15), 10);
            chunk.SetBlock(new(8, 12, 15), 11);
            chunk.SetBlock(new(8, 11, 15), 12);
            chunk.SetBlock(new(8, 10, 15), 13);
            chunk.SetBlock(new(8, 9, 15), 14);
            chunk.SetBlock(new(8, 8, 15), 15);
            chunk.SetBlock(new(8, 7, 15), 16);
            chunk.SetBlock(new(8, 6, 15), 17);
            chunk.SetBlock(new(8, 5, 15), 18);
            chunk.SetBlock(new(8, 4, 15), 19);
            chunk.SetBlock(new(8, 3, 15), 20);
            chunk.SetBlock(new(8, 2, 15), 21);
            chunk.SetBlock(new(8, 1, 15), 22);
            chunk.SetBlock(new(8, 0, 15), 23);
            chunk.SetBlock(new(11, 0, 15), 24);
            chunk.SetBlock(new(11, 1, 15), 25);
            chunk.SetBlock(new(11, 2, 15), 26);
            chunk.SetBlock(new(11, 3, 15), 27);
            chunk.SetBlock(new(11, 4, 15), 28);
            chunk.SetBlock(new(11, 5, 15), 29);
            chunk.SetBlock(new(11, 6, 15), 30);
            chunk.SetBlock(new(11, 7, 15), 31);
            chunks.Add(chunk);
        }
        Console.WriteLine($"total memory used: {(GC.GetTotalMemory(true) - memory) / 1000} KB " +
        $"for {count} chunks");
        Console.WriteLine($"for 25 unique blocks with split geometry.");
        Console.WriteLine("Test4 passed");
        return;
    }
    static public void Test5()
    {
        float performanceCount = 12;
        Stopwatch watch = new();
        long memory = GC.GetTotalMemory(true);
        watch.Start();
        VoxelChunk chunk = new();
        for (int _ = 0; _ < performanceCount; _++)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        chunk.SetBlock(new(x, y, z), (ushort)(x + (y * 16) + (z * 16 * 16)));
                    }
                }
            }

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        chunk.GetBlock(new(x, y, z));
                    }
                }
            }
            chunk.RemoveAllBlocks();
        }
        watch.Stop();
        Console.WriteLine($"Worst case of a read and write operation, runs at " +
        $"{watch.ElapsedMilliseconds / (performanceCount * 4096)}ms " +
        "per operation on average.");
        long memoryTotalKB = (GC.GetTotalMemory(true) - memory) / 1000;
        Console.WriteLine($"total memory used: {memoryTotalKB} KB " +
        $"for {performanceCount} chunks");
        Console.WriteLine($"@ {memoryTotalKB / performanceCount} KB per chunk (worst case)");
        return;
    }

    public static void Test6()
    {
        float performanceCount = 1000;
        long memory = GC.GetTotalMemory(true);
        List<VoxelChunk> chunks = new();
        for (int _ = 0; _ < performanceCount; _++)
        {
            VoxelChunk chunk = new();
            chunk.SetBlockSpan(new(0, 0, 0), new(15, 15, 15), 1);
            chunks.Add(chunk);
        }

        long memoryTotalKB = (GC.GetTotalMemory(true) - memory) / 1000;
        Console.WriteLine($"total memory used: {memoryTotalKB} KB " +
        $"for {performanceCount} chunks");
        Console.WriteLine($"@ {memoryTotalKB / performanceCount} KB per chunk (best case)");
        chunks.Clear();
        return;
    }

    public static void Test7()
    {
        VoxelChunk chunk = new();
        chunk.SetBlockSpan(new(0, 0, 0), new(15, 15, 15), 1);
        List<Quad> quads = chunk.GenerateQuads();
        if (quads.Count != 6)
        {
            Console.WriteLine("Test7 failed, wrong number of quads generated.");
            Console.WriteLine($"Expected 6 but got {quads.Count}");
            return;
        }
        chunk.SetBlock(new(7, 7, 7), 0);
        List<Quad> splitQuads = chunk.GenerateQuads();
        if (splitQuads.Count != 24)
        {
            Console.WriteLine("Test7 failed, wrong number of quads generated.");
            Console.WriteLine($"Expected 24 but got {quads.Count}");
            return;
        }
        Console.WriteLine($"Test7 passed");
        return;
    }
}