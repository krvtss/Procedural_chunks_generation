# Procedural_chunks_generation

Wrote this a couple years back for a 2D Unity project. Open-sourcing it to show my implementation of chunk streaming and noise-based biomes.

![World Gen Demo](0401-_2_.gif)

**Chunk Streaming & Memory**
The map is split into a localized grid. As the player moves, `Map.cs` calculates spatial distance, destroying out-of-bounds chunks to free memory and pushing new coordinates to a load queue. A Coroutine processes this queue incrementally to distribute the instantiation load and prevent main thread spikes. Loaded chunk coordinates are tracked using a `HashSet<Vector3Int>` to ensure O(1) lookups and avoid unnecessary memory allocation overhead.

**Noise and Biome Logic**
Biomes are calculated by cross-referencing multiple Perlin noise maps (Height, Moisture, Heat). The terrain type is evaluated per-tile by checking the combined float values against `BiomePreset` ScriptableObjects. Additional independent noise layers dictate the density of foliage, entities, and ores based on the active biome's parameters. Any runtime modifications to the tiles are saved when the chunk unloads.
