## Voxel Data Tests

This repo is used to test out a data structure that would be able to both store boxy voxel data efficiently, access / update it quickly, and use a storage method that would make vertex generation trivial.

-   A read/write operation to a single block average: 3ms (needs optimizing)
-   Best case chunk memory footprint for 20^3 chunks loaded: 1MB
-   Average (25 unique blocks) case chunk memory footprint for 20^3 chunks loaded: 6MB
-   Worst case chunk memory footprint for 20^3 chunks loaded: 264MB

Overall the performance for this data structure is a success. It is fast enough and small enough that it would be able to store boxy voxel data in a real world scenario.

The data structures are stored entirely in ./CSharpTests/VoxelChunk.cs
