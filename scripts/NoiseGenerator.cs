using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{
    static float IslandBorders = 1000;
    static float IslandFadeStart = 800;

    public static float[,] GenerateChunk(int chunkSize, float scale, Wave[] waves, Vector3 offset, Vector3 worldPosition = default)
    {
        bool island = false;
        if (worldPosition != default)
        {
            island = true;
        }

        // create the noise map
        float[,] noiseMap = new float[chunkSize, chunkSize];
        float currentScale = scale;
        if (waves[0].scale != 0)
        {
            currentScale = waves[0].scale;
        }

        // loop through each element in the noise map
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                // calculate the sample positions
                float samplePosX = (x + offset.x) * currentScale;
                float samplePosY = (y + offset.y) * currentScale;

                float normalization = 0.0f;

                // loop through each wave
                foreach (Wave wave in waves)
                {
                    // sample the perlin noise taking into consideration amplitude and frequency
                    noiseMap[x, y] += wave.amplitude * Mathf.PerlinNoise(samplePosX * wave.frequency + wave.seed, samplePosY * wave.frequency + wave.seed);
                    normalization += wave.amplitude;
                }

                // normalize the value
                noiseMap[x, y] /= normalization;

                //Island Normalization
                if (island)
                {
                    float islandFade = Mathf.InverseLerp(IslandFadeStart, IslandBorders, Mathf.Abs(worldPosition.magnitude));
                    islandFade = Mathf.Clamp01(islandFade);
                    noiseMap[x, y] *= 1 - islandFade;
                }
            }
        }
        
        return noiseMap;
    }

    //public static float[,] Generate(int width, int height, float scale, Wave[] waves, Vector2 offset)
    //{
    //    // create the noise map
    //    float[,] noiseMap = new float[width, height];

    //    // loop through each element in the noise map
    //    for (int x = 0; x < width; x++)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            // calculate the sample positions
    //            float samplePosX = x * scale + offset.x;
    //            float samplePosY = y * scale + offset.y;

    //            float normalization = 0.0f;

    //            // loop through each wave
    //            foreach (Wave wave in waves)
    //            {
    //                // sample the perlin noise taking into consideration amplitude and frequency
    //                noiseMap[x, y] += wave.amplitude * Mathf.PerlinNoise(samplePosX * wave.frequency + wave.seed, samplePosY * wave.frequency + wave.seed);
    //                normalization += wave.amplitude;
    //            }

    //            // normalize the value
    //            noiseMap[x, y] /= normalization;
    //        }
    //    }

    //    return noiseMap;
    //}
}

[System.Serializable]
public class Wave
{
    public float scale;
    public float seed;
    public float frequency;
    public float amplitude;
}