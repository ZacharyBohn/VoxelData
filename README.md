## Voxel Data Tests

This repo is used to test out a data structure that would be able to both store boxy voxel data efficiently, access / update it quickly, and use a storage method that would make vertex generation trivial.

-   A read/write operation to a single block average: 0.3ms
-   Best case chunk memory footprint: 344 bytes
-   Best case chunk memory footprint for 20^3 chunks loaded: 3MB
-   Worst case chunk memory footprint: 22KB
-   Worst case chunk memory footprint for 20^3 chunks loaded: 176MB

Overall the performance for this data structure is a success. It is fast enough and small enough that it would be able to store boxy voxel data in a real world scenario.

The data structures are stored entirley in ./CSharpTests/VoxelChunk.cs
