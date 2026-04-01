using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections;

public class Map : MonoBehaviour
{
    public static Map Instantce;
    TilesManager tileManager;
    CityGenerator cityGenerator;

    [SerializeField] BiomePreset[] biomes;
    [SerializeField] Tilemap[] tilemaps;

    [Header("Dimensions")]
    [SerializeField] int seed;
    [SerializeField] float scale = 0.5f;
    [SerializeField] Vector3Int offset;
    [SerializeField] Transform player;

    [Header("Chunk Generation")]
    public int chunkLoadDistance;
    public int chunkSize;
    public readonly HashSet<Vector3Int> loadedChunks = new();
    int tileChunkLoadDistance;

    public HashSet<Vector3Int> needToSaveChunks = new();

    Vector3Int currentChunk;
    Vector3Int currentChunkProperty
    {
        set
        {
            if (currentChunk != value)
            {
                MovedOnChunk(value);
                currentChunk = value;
            }
        }
        get
        {
            return currentChunk;
        }
    }

    List<Vector3Int> needToLoadChunks = new();

    [Header("Shadows")]
    [SerializeField] Tile shadowTile;

    [Header("Structure Generation")]
    public List<Structure> structures;
    [SerializeField] int structureLoadDistance;
    [SerializeField] float structuresDensity;

    int tileStructureLoadDistance;
    Vector3Int structureOrigin;

    [Header("Ore Generation")]
    public LootTable oresLootTable;

    [Header("Lakes")]
    [SerializeField] LootTable lakeGrownsTiles;

    [Header("Entities")]
    [SerializeField] LootTable standartEntitiesTable;

    [Header("Height Map")]
    [SerializeField] Wave[] heightWaves;
    [SerializeField] float[,] heightMap;

    [Header("Moisture Map")]
    [SerializeField] Wave[] moistureWaves;
    private float[,] moistureMap;

    [Header("Heat Map")]
    [SerializeField] Wave[] heatWaves;
    private float[,] heatMap;

    [Header("Trees Map")]
    [SerializeField] Wave[] treesWaves;
    private float[,] treesMap;

    [Header("Structures Map")]
    [SerializeField] Wave[] structuresWaves;
    private float[,] structuresMap;

    [Header("Ores Map")]
    [SerializeField] Wave[] oresWaves;
    private float[,] oresMap;

    private void Awake()
    {
        Instantce = this;
    }
    private void Start()
    {
        tileManager = FindObjectOfType<TilesManager>();
        cityGenerator = FindObjectOfType<CityGenerator>();

        tileChunkLoadDistance = chunkLoadDistance * chunkSize;
        tileStructureLoadDistance = structureLoadDistance * chunkSize;

        seed = UnityEngine.Random.Range(-999999, 999999);
        RandomizeSeeds();
        offset = new Vector3Int(UnityEngine.Random.Range(-999999, 999999), UnityEngine.Random.Range(-999999, 999999), 0);

        currentChunk = new Vector3Int(Mathf.RoundToInt(player.position.x / chunkSize) * chunkSize, Mathf.RoundToInt(player.position.y / chunkSize) * chunkSize, 0);

        GenerateMapAroundPos(currentChunkProperty, true);

        StartCoroutine(CheckForChunkGeneration());
    }
    private void Update()
    {
        Vector3Int calculatedChunk = new Vector3Int(Mathf.RoundToInt(player.position.x / chunkSize) * chunkSize, Mathf.RoundToInt(player.position.y / chunkSize) * chunkSize, 0);
        if (currentChunk != calculatedChunk)
        {
            currentChunkProperty = calculatedChunk;
        }
    }

    void RandomizeSeeds()
    {
        for (int i = 0; i < heightWaves.Length; i++)
        {
            heightWaves[i].seed = seed;
            moistureWaves[i].seed = seed;
            heatWaves[i].seed = seed;
            treesWaves[i].seed = seed;
            structuresWaves[i].seed = seed;
            oresWaves[i].seed = seed;
        }
    }
    void MovedOnChunk(Vector3Int newChunkPos)
    {
        //there we using currentChunk variable as old chunk because i dont set it now (it will set after this method)
        DestroyCornerChunksAroundPos(newChunkPos);
        GenerateMapAroundPos(newChunkPos);
    }

    IEnumerator CheckForChunkGeneration()
    {
        while (true)
        {
            if (needToLoadChunks.Count > 0)
            {
                Vector3Int currentLoadChunkPosition = needToLoadChunks[0];
                needToLoadChunks.RemoveAt(0);

                if (!loadedChunks.Contains(currentLoadChunkPosition))
                {
                    //if (PlayerPrefs.GetInt("ChunkSaved" + currentLoadChunkPosition) == 1)
                    //{
                    //    LoadChunk(currentLoadChunkPosition);
                    //}
                    //else
                    //{
                    //    GenerateChunk(currentLoadChunkPosition);
                    //}
                    GenerateChunk(currentLoadChunkPosition);
                }
            }
            yield return new WaitForSeconds(0.02f);
        }
    }
    void GenerateMapAroundPos(Vector3Int originChunk, bool inMoment = false)
    {
        for (int x = -chunkLoadDistance; x <= chunkLoadDistance; x++)
        {
            for (int y = -chunkLoadDistance; y <= chunkLoadDistance; y++)
            {
                int samplePosX = x * chunkSize + originChunk.x;
                int samplePosY = y * chunkSize + originChunk.y;
                Vector3Int chunkPos = new(samplePosX, samplePosY, 0);

                if (!loadedChunks.Contains(chunkPos) && !needToLoadChunks.Contains(chunkPos))
                {
                    if (inMoment)
                    {
                        //if (PlayerPrefs.GetInt("ChunkSaved" + chunkPos) == 1)
                        //{
                        //    LoadChunk(chunkPos);
                        //} else
                        //{
                        //    GenerateChunk(chunkPos);
                        //}
                        GenerateChunk(chunkPos);
                    } else
                    {
                        needToLoadChunks.Add(chunkPos);
                    }
                }
            }
        }
    }

    void DestroyCornerChunksAroundPos(Vector3Int currentChunkPos)
    {
        List<Vector3Int> chunksToDestroy = new List<Vector3Int>();

        foreach (var chunkPos in loadedChunks)
        {
            if (Mathf.Abs(chunkPos.x - currentChunkPos.x) > tileChunkLoadDistance ||
                Mathf.Abs(chunkPos.y - currentChunkPos.y) > tileChunkLoadDistance)
            {
                chunksToDestroy.Add(chunkPos);
            }
        }

        foreach (var chunkPos in chunksToDestroy)
        {
            DestroyChunk(chunkPos);
        }
    }

    void GenerateChunk(Vector3Int origin)
    {
        loadedChunks.Add(origin);

        Vector3 perlinPos = origin + offset;
        // height map
        heightMap = NoiseGenerator.GenerateChunk(chunkSize, scale, heightWaves, perlinPos, origin);

        // moisture map
        moistureMap = NoiseGenerator.GenerateChunk(chunkSize, scale, moistureWaves, perlinPos);

        // heat map
        heatMap = NoiseGenerator.GenerateChunk(chunkSize, scale, heatWaves, perlinPos);

        // trees map
        treesMap = NoiseGenerator.GenerateChunk(chunkSize, scale, treesWaves, perlinPos);

        // ores map
        oresMap = NoiseGenerator.GenerateChunk(chunkSize, scale, oresWaves, perlinPos);

        Vector3Int[] positions = new Vector3Int[chunkSize * chunkSize];
        Tile[] tileArrayGround = new Tile[positions.Length];
        Tile[] tileArrayWall = new Tile[positions.Length];
        Tile[] tileArrayWalkable = new Tile[positions.Length];
        Tile[] tileArrayShadows = new Tile[positions.Length];

        List<Vector3Int> walkableCheckPositions = new();

        //Making Structure
        //int structureIndex = CheckForStructures(origin);

        for (int index = 0; index < positions.Length; index++)
        {
            Vector3Int localPos = new(index % chunkSize, index / chunkSize, 0);
            int localPosX = localPos.x;
            int localPosY = localPos.y;

            Vector3Int globalPos = localPos + origin;
            positions[index] = globalPos;

            Vector3 cellCenterPos = tilemaps[0].GetCellCenterWorld(globalPos);

            float heightValue = heightMap[localPosX, localPosY];
            float heatValue = heatMap[localPosX, localPosY];
            float moistureValue = moistureMap[localPosX, localPosY];
            BiomePreset biome = GetBiome(heightValue, moistureValue, heatValue, biomes);
            Tile tile = biome.GetTile(heightValue);

            bool isWallOrWater = biome.IsWallOrWater(heightValue);
            bool isWallOrSandOrWater = biome.IsWallOrSandOrWater(heightValue);
            bool isWall = biome.IsWall(heightValue);

            //Checking on which tilemap put block
            if (isWall)
            {
                //Checking if can put ore
                if (UnityEngine.Random.Range(0, 100) < biome.oreDensity)
                {
                    RewardItem item = oresLootTable.GetRandomItem();
                    tileArrayWall[index] = item.oreTile;
                }
                else
                {
                    tileArrayWall[index] = tile;
                }

                if (CheckShadow(tileArrayWall[index])) { tileArrayShadows[index] = shadowTile; }
                tileArrayGround[index] = biome.GetTileByIndex(2);
            }
            else
            {
                tileArrayGround[index] = tile;
            }

            //Checking if can put tree
            if (CheckForTree(treesMap[localPosX, localPosY], biome) && !isWallOrSandOrWater)
            {
                Tile treeTile = biome.tree;
                tileArrayWall[index] = treeTile;
                if (CheckShadow(treeTile)) { tileArrayShadows[index] = shadowTile; }
                continue;
            }

            if (!isWallOrWater & biome.tileFoliageChance > 0 & UnityEngine.Random.Range(0, 100) < biome.tileFoliageChance)
            {
                Tile folTile = biome.foliageLootTable.GetRandomItem().oreTile;
                tileArrayWalkable[index] = folTile;
                if (CheckShadow(folTile)) { tileArrayShadows[index] = shadowTile; }
                walkableCheckPositions.Add(globalPos);
                continue;
            }

            if (!isWallOrWater & biome.overgrownLakes & biome.IsSand(heightValue) & UnityEngine.Random.Range(0, 100) < 50)
            {
                Tile lakeGrowTile = lakeGrownsTiles.GetRandomItem().oreTile;
                tileArrayWalkable[index] = lakeGrowTile;
                if (CheckShadow(lakeGrowTile)) { tileArrayShadows[index] = shadowTile; }
                walkableCheckPositions.Add(globalPos);
                continue;
            }

            //Checking if can put foliage or/and entity
            if (biome.isThereFoliage() && !isWallOrWater)
            {
                ItemData folItem = biome.RandomFoliage();
                Main.instance.SpawnItem(folItem, 1, cellCenterPos);
            }
            if (!isWallOrWater && biome.entityDensity != 0 && UnityEngine.Random.Range(0f, 100f) < biome.entityDensity)
            {
                LootTable table = biome.entityLootTable.CanBeUsed() ? biome.entityLootTable : standartEntitiesTable;
                RewardItem item = table.GetRandomItem();
                Instantiate(item.entity, cellCenterPos, Quaternion.identity);
                continue;
            }
        }
        //========Adding Structure========
        //for (int x = 0; x < chunkSize; x++)
        //{
        //    if (structureIndex == -1) { break; }
        //    for (int y = 0; y < chunkSize; y++)
        //    {
        //        Vector2Int tileLocalPos = new Vector2Int(x, y) - structureOrigin;
        //        StructureTileInfo[] tilesArray = Array.FindAll(structures[structureIndex].tiles, t => t.position == (Vector3Int)tileLocalPos);

        //        if (tilesArray.Length == 0) { continue; }

        //        foreach (var strTile in tilesArray)
        //        {
        //            int tilemapIndex = strTile.tilemapIndex;
        //            int index = x + y * chunkSize;
        //            if (tilemapIndex == 0)
        //            {
        //                tileArrayGround[index] = strTile.tile;
        //            }
        //            else if (tilemapIndex == 1)
        //            {
        //                tileArrayWall[index] = strTile.tile;
        //            }
        //        }
        //    }
        //}

        //Adding tiles to tilemap
        tilemaps[0].SetTiles(positions, tileArrayGround);
        tilemaps[1].SetTiles(positions, tileArrayWall);
        tilemaps[2].SetTiles(positions, tileArrayWalkable);
        tilemaps[3].SetTiles(positions, tileArrayShadows);

        foreach (var tilePos in walkableCheckPositions)
        {
            tileManager.CheckForTileAdditionalObjects(tilePos);
        }
    }

    void SaveChunk(Vector3Int chunkOrigin)
    {
        if (loadedChunks.Contains(chunkOrigin) == false) { return; }

        PlayerPrefs.SetInt("ChunkSaved" + chunkOrigin, 1);

        for (int t = 0; t < tilemaps.Length; t++)
        {
            Tilemap tilemap = tilemaps[t];
            for (int index = 0; index < chunkSize * chunkSize; index++)
            {
                Vector3Int localPos = new(index % chunkSize, index / chunkSize, 0);
                Vector3Int globalPos = localPos + chunkOrigin;

                if (PlayerPrefs.HasKey(tilemap.GetTile(globalPos).name) == false) { continue; }

                int tileIndex = PlayerPrefs.GetInt(tilemap.GetTile(globalPos).name);
                PlayerPrefs.SetInt("tileIndex" + globalPos.x + globalPos.y + t, tileIndex);
            }
        }
    }
    void LoadChunk(Vector3Int chunkOrigin)
    {
        if (PlayerPrefs.GetInt("ChunkSaved" + chunkOrigin) == 0) { return; }

        Vector3Int[] positions = new Vector3Int[chunkSize * chunkSize];
        Tile[] tileArrayGround = new Tile[positions.Length];
        Tile[] tileArrayWall = new Tile[positions.Length];
        Tile[] tileArrayWalkable = new Tile[positions.Length];
        Tile[] tileArrayShadows = new Tile[positions.Length];

        List<Vector3Int> walkableCheckPositions = new();

        for (int t = 0; t < tilemaps.Length; t++)
        {
            for (int index = 0; index < chunkSize * chunkSize; index++)
            {
                Vector3Int localPos = new(index % chunkSize, index / chunkSize, 0);
                Vector3Int globalPos = localPos + chunkOrigin;

                int savedTileIndex = PlayerPrefs.GetInt("tileIndex" + globalPos.x + globalPos.y + t);
                Tile savedTile = tileManager.dataBaseTiles[savedTileIndex].tileBase;

                switch (t)
                {
                    case 0: tileArrayGround[index] = savedTile;
                        break;
                    case 1: tileArrayWall[index] = savedTile;
                        break;
                    case 2: tileArrayWalkable[index] = savedTile;
                        break;
                }

                if (CheckShadow(savedTile)) { tileArrayShadows[index] = shadowTile; }
            }
        }

        //Adding tiles to tilemap
        tilemaps[0].SetTiles(positions, tileArrayGround);
        tilemaps[1].SetTiles(positions, tileArrayWall);
        tilemaps[2].SetTiles(positions, tileArrayWalkable);
        tilemaps[3].SetTiles(positions, tileArrayShadows);

        foreach (var tilePos in walkableCheckPositions)
        {
            tileManager.CheckForTileAdditionalObjects(tilePos);
        }
    }

    int CheckForStructures(Vector3Int origin)
    {
        // structures map
        structuresMap = NoiseGenerator.GenerateChunk(tileStructureLoadDistance * 2, scale, structuresWaves, origin + offset);
        int structureIndex = -1;

        for (int x = -tileStructureLoadDistance; x < tileStructureLoadDistance; x++)
        {
            for (int y = -tileStructureLoadDistance; y < tileStructureLoadDistance; y++)
            {
                if (structuresMap[x + tileStructureLoadDistance, y + tileStructureLoadDistance] > structuresDensity)
                {
                    Vector2 structurePerlinPos = origin + new Vector3(x, y) + offset;

                    BiomePreset biome = GetTileBiome(structurePerlinPos);
                    if (biome.structure == -1) { continue; }

                    structureIndex = biome.structure;
                    structureOrigin = new Vector3Int(x, y);

                    return structureIndex;
                }
            }
        }

        return structureIndex;
    }

    void DestroyChunk(Vector3Int origin)
    {
        if (!loadedChunks.Contains(origin) && needToLoadChunks.Contains(origin)) { needToLoadChunks.Remove(origin); return; }

        //Saving Chunk if it is changed
        if (needToSaveChunks.Contains(origin))
        {
            SaveChunk(origin);
            needToSaveChunks.Remove(origin);
        }

        loadedChunks.Remove(origin);

        Vector3Int[] positions = new Vector3Int[chunkSize * chunkSize];
        Tile[] tiles = new Tile[positions.Length];

        for (int index = 0; index < positions.Length; index++)
        {
            positions[index] = new Vector3Int(index % chunkSize + origin.x, index / chunkSize + origin.y, 0);

            //WorldTile hereTile = tileManager.GetWorldTile(positions[index]);
            //if (hereTile == null || hereTile != null && hereTile.inventory.Slots.Count == 0) { continue; }
            tileManager.RemoveTile(positions[index], false);
        }

        foreach (var tilemap in tilemaps)
        {
            tilemap.SetTiles(positions, tiles);
        }

        //Deleting all Entities on this chunk
        Collider2D[] entities = Physics2D.OverlapBoxAll(origin + new Vector3(chunkSize / 2, chunkSize / 2), new Vector3(chunkSize, chunkSize), 0, Main.instance.deleteWithGeneration_LayerMask);
        foreach (var entity in entities)
        {
            Destroy(entity.gameObject);
        }
    }

    public BiomePreset GetTileBiome(Vector3 position)
    {
        //Getting Biome
        Vector3 noisePosition = position + offset;
        float[,] heightMap = NoiseGenerator.GenerateChunk(1, scale, heightWaves, noisePosition);
        float[,] moistureMap = NoiseGenerator.GenerateChunk(1, scale, moistureWaves, noisePosition);
        float[,] heatMap = NoiseGenerator.GenerateChunk(1, scale, heatWaves, noisePosition);
        return GetBiome(heightMap[0, 0], moistureMap[0, 0], heatMap[0, 0], biomes);
    }

    bool CheckForTree(float treeStrenght, BiomePreset biome)
    {
        return biome.treeDensity > 0 && treeStrenght <= biome.treeDensity;
    }
    bool CheckForOre(float oreStrenght, BiomePreset biome)
    {
        return biome.oreDensity > 0 && oreStrenght >= biome.oreDensity;
    }

    BiomePreset GetBiome(float height, float moisture, float heat, BiomePreset[] biomesList)
    {
        BiomePreset bestBiome = biomesList[0];
        float smallestDifference = 999999f;

        foreach (BiomePreset biome in biomesList)
        {
            if (biome.MatchCondition(height, moisture, heat))
            {
                float difference = (height - biome.minHeight) + (moisture - biome.minMoisture) + (heat - biome.minHeat);

                if (difference < smallestDifference)
                {
                    bestBiome = biome;
                    smallestDifference = difference;
                }
            }
        }

        return bestBiome;
    }

    public void SetChunkToSaved(Vector3Int changedTilePosition)
    {
        //if (needToSaveChunks.ContainsKey(changedTilePosition)) { return; }

        needToSaveChunks.Add(GlobalToChunkPosition(changedTilePosition));
    }

    bool CheckShadow(Tile tileUnder)
    {
        if (tileUnder == null) { return false; }
        return tileManager.dataBaseTiles[PlayerPrefs.GetInt(tileUnder.name)].haveAmbientOcclusion;
    }
    public bool IsStandingOnLoadedChunk(Vector3Int chunkPos, bool andPlanned = false)
    {
        if (andPlanned)
        {
            return loadedChunks.Contains(chunkPos) || needToLoadChunks.Contains(chunkPos);
        } else
        {
            return loadedChunks.Contains(chunkPos);
        }
    }
    public bool IsChunkLoaded(Vector3 globalPos)
    {
        return loadedChunks.Contains(GlobalToChunkPosition(globalPos));
    }
    public Vector3Int GlobalToChunkPosition(Vector3 globalPos)
    {
        return new Vector3Int(Mathf.RoundToInt(globalPos.x / chunkSize) * chunkSize, Mathf.RoundToInt(globalPos.y / chunkSize) * chunkSize, 0);
    }
}

/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Collections;

public class Map : MonoBehaviour
{
    public static Map Instantce;
    TilesManager tileManager;
    CityGenerator cityGenerator;

    [SerializeField] BiomePreset[] biomes;
    [SerializeField] Tilemap[] tilemaps;

    [Header("Dimensions")]
    [SerializeField] int seed;
    [SerializeField] float scale = 0.5f;
    [SerializeField] Vector3Int offset;
    [SerializeField] Transform player;

    [Header("Chunk Generation")]
    public int chunkLoadDistance;
    public int chunkSize;
    public readonly Dictionary<Vector3Int, bool> loadedChunks = new();
    int tileChunkLoadDistance;

    public Dictionary<Vector3Int, bool> needToSaveChunks = new();

    Vector3Int currentChunk;
    Vector3Int currentChunkProperty
    {
        set
        {
            if (currentChunk != value)
            {
                MovedOnChunk(value);
                currentChunk = value;
            }
        }
        get
        {
            return currentChunk;
        }
    }

    List<Vector3Int> needToLoadChunks = new();

    [Header("Shadows")]
    [SerializeField] Tile shadowTile;

    [Header("Structure Generation")]
    public List<Structure> structures;
    [SerializeField] int structureLoadDistance;
    [SerializeField] float structuresDensity;

    int tileStructureLoadDistance;
    Vector3Int structureOrigin;

    [Header("Ore Generation")]
    public LootTable oresLootTable;

    [Header("Lakes")]
    [SerializeField] LootTable lakeGrownsTiles;

    [Header("Entities")]
    [SerializeField] LootTable standartEntitiesTable;

    [Header("Height Map")]
    [SerializeField] Wave[] heightWaves;
    [SerializeField] float[,] heightMap;

    [Header("Moisture Map")]
    [SerializeField] Wave[] moistureWaves;
    private float[,] moistureMap;

    [Header("Heat Map")]
    [SerializeField] Wave[] heatWaves;
    private float[,] heatMap;

    [Header("Trees Map")]
    [SerializeField] Wave[] treesWaves;
    private float[,] treesMap;

    [Header("Structures Map")]
    [SerializeField] Wave[] structuresWaves;
    private float[,] structuresMap;

    [Header("Ores Map")]
    [SerializeField] Wave[] oresWaves;
    private float[,] oresMap;

    private void Awake()
    {
        Instantce = this;
    }
    private void Start()
    {
        tileManager = FindObjectOfType<TilesManager>();
        cityGenerator = FindObjectOfType<CityGenerator>();

        tileChunkLoadDistance = chunkLoadDistance * chunkSize;
        tileStructureLoadDistance = structureLoadDistance * chunkSize;

        seed = UnityEngine.Random.Range(-999999, 999999);
        RandomizeSeeds();
        offset = new Vector3Int(UnityEngine.Random.Range(-999999, 999999), UnityEngine.Random.Range(-999999, 999999));

        currentChunk = new Vector3Int(Mathf.RoundToInt(player.position.x / chunkSize) * chunkSize, Mathf.RoundToInt(player.position.y / chunkSize) * chunkSize);

        GenerateMapAroundPos(currentChunkProperty, true);

        InvokeRepeating(nameof(CheckForChunkGeneration), 0, 0.08f);
    }
    private void Update()
    {
        currentChunkProperty = new Vector3Int(Mathf.RoundToInt(player.position.x / chunkSize) * chunkSize, Mathf.RoundToInt(player.position.y / chunkSize) * chunkSize);
    }

    void RandomizeSeeds()
    {
        for (int i = 0; i < heightWaves.Length; i++)
        {
            heightWaves[i].seed = seed;
            moistureWaves[i].seed = seed;
            heatWaves[i].seed = seed;
            treesWaves[i].seed = seed;
            structuresWaves[i].seed = seed;
            oresWaves[i].seed = seed;
        }
    }
    void MovedOnChunk(Vector3Int newChunkPos)
    {
        //there we using currentChunk variable as old chunk because i dont set it now (it will set after this method)
        DestroyCornerChunksAroundPos(newChunkPos, currentChunkProperty, -1);
        GenerateMapAroundPos(newChunkPos);
    }

    void CheckForChunkGeneration()
    {
        if (needToLoadChunks.Count == 0) { return; }
        Vector3Int currentLoadChunkPosition = needToLoadChunks[0];
        if (loadedChunks.ContainsKey(currentLoadChunkPosition)) { return; }

        //if (PlayerPrefs.GetInt("ChunkSaved" + currentLoadChunkPosition) == 1)
        //{
        //    LoadChunk(currentLoadChunkPosition);
        //}
        //else
        //{
        //    GenerateChunk(currentLoadChunkPosition);
        //}
        GenerateChunk(currentLoadChunkPosition);
        needToLoadChunks.RemoveAt(0);
    }
    void GenerateMapAroundPos(Vector3Int originChunk, bool inMoment = false)
    {
        for (int x = -chunkLoadDistance; x < chunkLoadDistance; x++)
        {
            for (int y = -chunkLoadDistance; y < chunkLoadDistance; y++)
            {
                int samplePosX = x * chunkSize + originChunk.x;
                int samplePosY = y * chunkSize + originChunk.y;
                Vector3Int chunkPos = new(samplePosX, samplePosY);

                if (!loadedChunks.ContainsKey(chunkPos) && !needToLoadChunks.Contains(chunkPos))
                {
                    if (inMoment)
                    {
                        //if (PlayerPrefs.GetInt("ChunkSaved" + chunkPos) == 1)
                        //{
                        //    LoadChunk(chunkPos);
                        //} else
                        //{
                        //    GenerateChunk(chunkPos);
                        //}
                        GenerateChunk(chunkPos);
                    } else
                    {
                        needToLoadChunks.Add(chunkPos);
                    }
                }
            }
        }
    }

    void DestroyCornerChunksAroundPos(Vector3Int newChunk, Vector3Int oldChunk, int side)
    {
        if (side == -1)
        {
            if (newChunk.x > oldChunk.x)
            {
                side = 3;
            }
            else if (newChunk.x < oldChunk.x)
            {
                side = 2;
            }
            if (newChunk.y > oldChunk.y)
            {
                if (side != -1)
                {
                    DestroyCornerChunksAroundPos(newChunk, oldChunk, side);
                }
                side = 1;
            }
            else if (newChunk.y < oldChunk.y)
            {
                if (side != -1)
                {
                    DestroyCornerChunksAroundPos(newChunk, oldChunk, side);
                }
                side = 0;
            }
        }

        int samplePosX = 0;
        int samplePosY = 0;
        if (side == 0)
        {
            //UP
            samplePosY = tileChunkLoadDistance + oldChunk.y;
        }
        if (side == 1)
        {
            //DOWN
            samplePosY = -tileChunkLoadDistance + oldChunk.y;
        }
        if (side == 2)
        {
            //RIGHT
            samplePosX = tileChunkLoadDistance + oldChunk.x;
        }
        if (side == 3)
        {
            //LEFT
            samplePosX = -tileChunkLoadDistance + oldChunk.x;
        }
        for (int x = -(tileChunkLoadDistance + 1); x < (tileChunkLoadDistance + 1); x++)
        {
            if (side == 0 || side == 1)
            {
                samplePosX = x + oldChunk.x;
            }
            if (side == 2 || side == 3)
            {
                samplePosY = x + oldChunk.y;
            }

            Vector3Int chunkPos = new(samplePosX, samplePosY);
            DestroyChunk(chunkPos);
        }
    }

    void GenerateChunk(Vector3Int origin)
    {
        loadedChunks.Add(origin, false);

        Vector3 perlinPos = origin + offset;
        // height map
        heightMap = NoiseGenerator.GenerateChunk(chunkSize, scale, heightWaves, perlinPos, origin);

        // moisture map
        moistureMap = NoiseGenerator.GenerateChunk(chunkSize, scale, moistureWaves, perlinPos);

        // heat map
        heatMap = NoiseGenerator.GenerateChunk(chunkSize, scale, heatWaves, perlinPos);

        // trees map
        treesMap = NoiseGenerator.GenerateChunk(chunkSize, scale, treesWaves, perlinPos);

        // ores map
        oresMap = NoiseGenerator.GenerateChunk(chunkSize, scale, oresWaves, perlinPos);

        Vector3Int[] positions = new Vector3Int[chunkSize * chunkSize];
        Tile[] tileArrayGround = new Tile[positions.Length];
        Tile[] tileArrayWall = new Tile[positions.Length];
        Tile[] tileArrayWalkable = new Tile[positions.Length];
        Tile[] tileArrayShadows = new Tile[positions.Length];

        List<Vector3Int> walkableCheckPositions = new();

        //Making Structure
        //int structureIndex = CheckForStructures(origin);

        for (int index = 0; index < positions.Length; index++)
        {
            Vector3Int localPos = new(index % chunkSize, index / chunkSize, 0);
            int localPosX = localPos.x;
            int localPosY = localPos.y;

            Vector3Int globalPos = localPos + origin;
            positions[index] = globalPos;

            Vector3 cellCenterPos = tilemaps[0].GetCellCenterWorld(globalPos);

            float heightValue = heightMap[localPosX, localPosY];
            float heatValue = heatMap[localPosX, localPosY];
            float moistureValue = moistureMap[localPosX, localPosY];
            BiomePreset biome = GetBiome(heightValue, moistureValue, heatValue, biomes);
            Tile tile = biome.GetTile(heightValue);

            bool isWallOrWater = biome.IsWallOrWater(heightValue);
            bool isWallOrSandOrWater = biome.IsWallOrSandOrWater(heightValue);
            bool isWall = biome.IsWall(heightValue);

            //Checking on which tilemap put block
            if (isWall)
            {
                tileArrayWall[index] = tile;
                if (CheckShadow(tile)) { tileArrayShadows[index] = shadowTile; }
                tileArrayGround[index] = biome.GetTileByIndex(2);
            }
            else
            {
                tileArrayGround[index] = tile;
            }

            //Checking if can put ore
            if (isWall)
            {
                if (UnityEngine.Random.Range(0, 100) < biome.oreDensity)
                {
                    RewardItem item = oresLootTable.GetRandomItem();
                    tileArrayWall[index] = item.oreTile;
                    if (CheckShadow(item.oreTile)) { tileArrayShadows[index] = shadowTile; }
                    continue;
                }
            }
            //============================================

            //Checking if can put tree
            if (CheckForTree(treesMap[localPosX, localPosY], biome) && !isWallOrSandOrWater)
            {
                Tile treeTile = biome.tree;
                tileArrayWall[index] = treeTile;
                if (CheckShadow(treeTile)) { tileArrayShadows[index] = shadowTile; }
                continue;
            }

            if (!isWallOrWater & biome.tileFoliageChance > 0 & UnityEngine.Random.Range(0, 100) < biome.tileFoliageChance)
            {
                Tile folTile = biome.foliageLootTable.GetRandomItem().oreTile;
                tileArrayWalkable[index] = folTile;
                if (CheckShadow(folTile)) { tileArrayShadows[index] = shadowTile; }
                walkableCheckPositions.Add(globalPos);
                continue;
            }

            if (!isWallOrWater & biome.overgrownLakes & biome.IsSand(heightValue) & UnityEngine.Random.Range(0, 100) < 50)
            {
                Tile lakeGrowTile = lakeGrownsTiles.GetRandomItem().oreTile;
                tileArrayWalkable[index] = lakeGrowTile;
                if (CheckShadow(lakeGrowTile)) { tileArrayShadows[index] = shadowTile; }
                walkableCheckPositions.Add(globalPos);
                continue;
            }

            //Checking if can put foliage or/and entity
            if (biome.isThereFoliage() && !isWallOrWater)
            {
                ItemData folItem = biome.RandomFoliage();
                Main.instance.SpawnItem(folItem, 1, cellCenterPos);
            }
            if (!isWallOrWater && biome.entityDensity != 0 && UnityEngine.Random.Range(0f, 100f) < biome.entityDensity)
            {
                LootTable table = biome.entityLootTable.CanBeUsed() ? biome.entityLootTable : standartEntitiesTable;
                RewardItem item = table.GetRandomItem();
                Instantiate(item.entity, cellCenterPos, Quaternion.identity);
                continue;
            }
        }
        //========Adding Structure========
        //for (int x = 0; x < chunkSize; x++)
        //{
        //    if (structureIndex == -1) { break; }
        //    for (int y = 0; y < chunkSize; y++)
        //    {
        //        Vector2Int tileLocalPos = new Vector2Int(x, y) - structureOrigin;
        //        StructureTileInfo[] tilesArray = Array.FindAll(structures[structureIndex].tiles, t => t.position == (Vector3Int)tileLocalPos);

        //        if (tilesArray.Length == 0) { continue; }

        //        foreach (var strTile in tilesArray)
        //        {
        //            int tilemapIndex = strTile.tilemapIndex;
        //            int index = x + y * chunkSize;
        //            if (tilemapIndex == 0)
        //            {
        //                tileArrayGround[index] = strTile.tile;
        //            }
        //            else if (tilemapIndex == 1)
        //            {
        //                tileArrayWall[index] = strTile.tile;
        //            }
        //        }
        //    }
        //}

        //Adding tiles to tilemap
        tilemaps[0].SetTiles(positions, tileArrayGround);
        tilemaps[1].SetTiles(positions, tileArrayWall);
        tilemaps[2].SetTiles(positions, tileArrayWalkable);
        tilemaps[3].SetTiles(positions, tileArrayShadows);

        foreach (var tilePos in walkableCheckPositions)
        {
            tileManager.CheckForTileAdditionalObjects(tilePos);
        }
    }

    void SaveChunk(Vector3Int chunkOrigin)
    {
        if (IsChunkLoaded(chunkOrigin) == false) { return; }

        PlayerPrefs.SetInt("ChunkSaved" + chunkOrigin, 1);

        for (int t = 0; t < tilemaps.Length; t++)
        {
            Tilemap tilemap = tilemaps[t];
            for (int index = 0; index < chunkSize * chunkSize; index++)
            {
                Vector3Int localPos = new(index % chunkSize, index / chunkSize, 0);
                Vector3Int globalPos = localPos + chunkOrigin;

                if (PlayerPrefs.HasKey(tilemap.GetTile(globalPos).name) == false) { continue; }

                int tileIndex = PlayerPrefs.GetInt(tilemap.GetTile(globalPos).name);
                PlayerPrefs.SetInt("tileIndex" + globalPos.x + globalPos.y + t, tileIndex);
            }
        }
    }
    void LoadChunk(Vector3Int chunkOrigin)
    {
        if (PlayerPrefs.GetInt("ChunkSaved" + chunkOrigin) == 0) { return; }

        Vector3Int[] positions = new Vector3Int[chunkSize * chunkSize];
        Tile[] tileArrayGround = new Tile[positions.Length];
        Tile[] tileArrayWall = new Tile[positions.Length];
        Tile[] tileArrayWalkable = new Tile[positions.Length];
        Tile[] tileArrayShadows = new Tile[positions.Length];

        List<Vector3Int> walkableCheckPositions = new();

        for (int t = 0; t < tilemaps.Length; t++)
        {
            for (int index = 0; index < chunkSize * chunkSize; index++)
            {
                Vector3Int localPos = new(index % chunkSize, index / chunkSize, 0);
                Vector3Int globalPos = localPos + chunkOrigin;

                int savedTileIndex = PlayerPrefs.GetInt("tileIndex" + globalPos.x + globalPos.y + t);
                Tile savedTile = tileManager.dataBaseTiles[savedTileIndex].tileBase;

                switch (t)
                {
                    case 0: tileArrayGround[index] = savedTile;
                        break;
                    case 1: tileArrayWall[index] = savedTile;
                        break;
                    case 2: tileArrayWalkable[index] = savedTile;
                        break;
                }

                if (CheckShadow(savedTile)) { tileArrayShadows[index] = shadowTile; }
            }
        }

        //Adding tiles to tilemap
        tilemaps[0].SetTiles(positions, tileArrayGround);
        tilemaps[1].SetTiles(positions, tileArrayWall);
        tilemaps[2].SetTiles(positions, tileArrayWalkable);
        tilemaps[3].SetTiles(positions, tileArrayShadows);

        foreach (var tilePos in walkableCheckPositions)
        {
            tileManager.CheckForTileAdditionalObjects(tilePos);
        }
    }

    int CheckForStructures(Vector3Int origin)
    {
        // structures map
        structuresMap = NoiseGenerator.GenerateChunk(tileStructureLoadDistance * 2, scale, structuresWaves, origin + offset);
        int structureIndex = -1;

        for (int x = -tileStructureLoadDistance; x < tileStructureLoadDistance; x++)
        {
            for (int y = -tileStructureLoadDistance; y < tileStructureLoadDistance; y++)
            {
                if (structuresMap[x + tileStructureLoadDistance, y + tileStructureLoadDistance] > structuresDensity)
                {
                    Vector2 structurePerlinPos = origin + new Vector3(x, y) + offset;

                    BiomePreset biome = GetTileBiome(structurePerlinPos);
                    if (biome.structure == -1) { continue; }

                    structureIndex = biome.structure;
                    structureOrigin = new Vector3Int(x, y);

                    return structureIndex;
                }
            }
        }

        return structureIndex;
    }

    void DestroyChunk(Vector3Int origin)
    {
        if (!loadedChunks.ContainsKey(origin) && needToLoadChunks.Contains(origin)) { needToLoadChunks.Remove(origin); return; }

        //Saving Chunk if it is changed
        if (needToSaveChunks.ContainsKey(origin))
        {
            SaveChunk(origin);
            needToSaveChunks.Remove(origin);
        }

        loadedChunks.Remove(origin);

        Vector3Int[] positions = new Vector3Int[chunkSize * chunkSize];
        Tile[] tiles = new Tile[positions.Length];

        for (int index = 0; index < positions.Length; index++)
        {
            positions[index] = new Vector3Int(index % chunkSize + origin.x, index / chunkSize + origin.y, 0);

            //WorldTile hereTile = tileManager.GetWorldTile(positions[index]);
            //if (hereTile == null || hereTile != null && hereTile.inventory.Slots.Count == 0) { continue; }
            tileManager.RemoveTile(positions[index], false);
        }

        foreach (var tilemap in tilemaps)
        {
            tilemap.SetTiles(positions, tiles);
        }

        //Deleting all Entities on this chunk
        Collider2D[] entities = Physics2D.OverlapBoxAll(origin + new Vector3(chunkSize / 2, chunkSize / 2), new Vector3(chunkSize, chunkSize), 0, Main.instance.deleteWithGeneration_LayerMask);
        foreach (var entity in entities)
        {
            Destroy(entity.gameObject);
        }
    }

    public BiomePreset GetTileBiome(Vector3 position)
    {
        //Getting Biome
        Vector3 noisePosition = position + offset;
        float[,] heightMap = NoiseGenerator.GenerateChunk(1, scale, heightWaves, noisePosition);
        float[,] moistureMap = NoiseGenerator.GenerateChunk(1, scale, moistureWaves, noisePosition);
        float[,] heatMap = NoiseGenerator.GenerateChunk(1, scale, heatWaves, noisePosition);
        return GetBiome(heightMap[0, 0], moistureMap[0, 0], heatMap[0, 0], biomes);
    }

    public class BiomeTempData
    {
        public BiomePreset biome;

        public BiomeTempData(BiomePreset preset)
        {
            biome = preset;
        }

        public float GetDiffValue(float height, float moisture, float heat)
        {
            return (height - biome.minHeight) + (moisture - biome.minMoisture) + (heat - biome.minHeat);
        }
    }

    bool CheckForTree(float treeStrenght, BiomePreset biome)
    {
        return biome.treeDensity > 0 && treeStrenght <= biome.treeDensity;
    }
    bool CheckForOre(float oreStrenght, BiomePreset biome)
    {
        return biome.oreDensity > 0 && oreStrenght >= biome.oreDensity;
    }

    BiomePreset GetBiome(float height, float moisture, float heat, BiomePreset[] biomesList)
    {
        BiomePreset biomeToReturn = null;
        List<BiomeTempData> biomeTemp = new();

        foreach (BiomePreset biome in biomesList)
        {
            if (biome.MatchCondition(height, moisture, heat))
            {
                biomeTemp.Add(new BiomeTempData(biome));
            }
        }

        float curVal = 0.0f;

        foreach (BiomeTempData biome in biomeTemp)
        {
            if (biomeToReturn == null)
            {
                biomeToReturn = biome.biome;
                curVal = biome.GetDiffValue(height, moisture, heat);
            }
            else
            {
                if (biome.GetDiffValue(height, moisture, heat) < curVal)
                {
                    biomeToReturn = biome.biome;
                    curVal = biome.GetDiffValue(height, moisture, heat);
                }
            }
        }

        if (biomeToReturn == null)
            biomeToReturn = biomesList[0];

        return biomeToReturn;
    }

    public void SetChunkToSaved(Vector3Int changedTilePosition)
    {
        //if (needToSaveChunks.ContainsKey(changedTilePosition)) { return; }

        needToSaveChunks.TryAdd(GlobalToChunkPosition(changedTilePosition), false);
    }

    bool CheckShadow(Tile tileUnder)
    {
        return tileManager.dataBaseTiles[PlayerPrefs.GetInt(tileUnder.name)].haveAmbientOcclusion;
    }
    public bool IsStandingOnLoadedChunk(Vector3Int chunkPos, bool andPlanned = false)
    {
        if (andPlanned)
        {
            return loadedChunks.ContainsKey(chunkPos) || needToLoadChunks.Contains(chunkPos);
        } else
        {
            return loadedChunks.ContainsKey(chunkPos);
        }
    }
    public bool IsChunkLoaded(Vector3 globalPos)
    {
        return loadedChunks.ContainsKey(GlobalToChunkPosition(globalPos));
    }
    public Vector3Int GlobalToChunkPosition(Vector3 globalPos)
    {
        return new Vector3Int(Mathf.RoundToInt(globalPos.x / chunkSize) * chunkSize, Mathf.RoundToInt(globalPos.y / chunkSize) * chunkSize);
    }
}*/