using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Biome Preset", menuName = "New Biome Preset")]
public class BiomePreset : ScriptableObject
{
    [Tooltip("0: water; 1: near water ground; 2: ground; 3: wall")] [SerializeField] Tile[] tiles;
    public Tile tree;
    //public Tile ore;
    public int structure;

    [Header("Effect")]
    public bool hasEffect;
    public int effectIndex;

    [Header("Entity Spawn")]
    public LootTable entityLootTable;

    [Header("Foliages")]
    [SerializeField] int foliageDensity;
    [SerializeField] Foliage[] foliages;

    [Header("Tile Foliage")]
    public bool overgrownLakes;
    public float tileFoliageChance;
    public LootTable foliageLootTable;

    [Header("Biome Settings")]
    public float minHeight;
    public float minMoisture;
    public float minHeat;

    [Header("Density")]
    public float treeDensity;
    public float oreDensity;
    public float entityDensity;

    public bool isThereFoliage()
    {
        if (foliageDensity == 0) { return false; }

        if (Random.Range(0, 100) < foliageDensity)
        {
            return true;
        } else { return false; }
    }
    public ItemData RandomFoliage()
    {
        int randomChance = Random.Range(0, 100);

        int chanceAdder = 0;
        for (int i = 0; i < foliages.Length; i++)
        {
            int folChance = foliages[i].chance;
            if (randomChance <= folChance + chanceAdder)
            {
                return foliages[i].foliageItemData;
            } else
            {
                chanceAdder += folChance;
            }
        }
        return foliages[0].foliageItemData;
    }

    // Returns a Tile relatively to Height
    public Tile GetTile (float height)
    {
        int returnedTile;
        if (height > 0.64f)
        {
            //Wall
            returnedTile = 3;
        } else if (height > 0.35f)
        {
            //Ground
            returnedTile = 2;
        } else if (height > 0.32f)
        {
            //Sand
            returnedTile = 1;
        } else
        {
            //Water
            returnedTile = 0;
        }
        return tiles[returnedTile];
    }

    public Tile GetTileByIndex(int index)
    {
        return tiles[index];
    }

    public bool IsWallOrSandOrWater(float height)
    {
        return height > 0.64f || height < 0.35f;
    }
    public bool IsWallOrWater(float height)
    {
        return height > 0.64f || height < 0.32f;
    }
    public bool IsWall(float height)
    {
        return height > 0.64f;
    }
    public bool IsSand(float height)
    {
        return height > 0.32f & height < 0.35f;
    }

    public bool MatchCondition (float height, float moisture, float heat)
    {
        return height >= minHeight && moisture >= minMoisture && heat >= minHeat;
    }
}
[System.Serializable]
public class Foliage
{
    public ItemData foliageItemData;
    [Range(0, 100)] public int chance;
}