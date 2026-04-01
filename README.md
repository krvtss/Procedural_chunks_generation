# Procedural_chunks_generation

**Project Archive Notice:** This system was built several years ago for a personal 2D game. It is provided here to demonstrate a foundational approach to chunk streaming, noise layering, and performance optimization in Unity.

![World Generation Streaming](0401-_2_.gif)

## Core Features
* **Biome Generation:** Layers multiple Perlin noise maps (Height, Moisture, Heat) to calculate biomes and structures.
* **Gradual Streaming:** Uses a time-sliced Coroutine queue to load chunks asynchronously, spreading hardware load to maintain frame rate.
* **Spatial Tracking:** Utilizes `HashSet` collections for O(1) lookups to track loaded areas and minimize garbage collection.
* **Persistent Data:** World rules and entities are driven by `ScriptableObjects`. Map modifications are saved at runtime as chunks unload.

## Technical Overview

### Chunk Management
The world is divided into a localized grid. As the player moves, the `Map.cs` manager calculates spatial distance. Out-of-bounds chunks are destroyed to free memory. New coordinates are pushed into a queue and processed sequentially across multiple frames, preventing main thread lockups during tile placement.

### Noise Logic
The generator evaluates multiple data points per tile:
1. **Height Map:** Determines base terrain boundaries (Water, Sand, Ground, Walls).
2. **Moisture & Heat Maps:** Blended with height data to define the specific `BiomePreset`.
3. **Foliage & Ore Maps:** Independent noise layers governing resource density based on the active biome's parameters.
* `Scripts/NoiseGenerator.cs` - Handles the Perlin noise math and array generation.
* `Scripts/ScriptableObjects/` - Contains the data containers for Biome Presets, Loot Tables, and Items.
