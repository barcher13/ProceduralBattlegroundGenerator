using System.Collections;
using UnityEngine;

public class terrainGen : MonoBehaviour
{

	public int mapChunkSizeHeight = 1024;
	public int mapChunkSizeWidth = 718;

	[Range(0, 6)]
	public int levelOfDetail;
	public float noiseScale;

	public int octaves;
	[Range(0, 1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;
	public int heightMultiplier;

	public bool autoUpdate;


	public void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSizeHeight, mapChunkSizeWidth, seed, noiseScale, octaves, persistance, lacunarity, offset);
		terrain.terrainData.size = new Vector3(mapChunkSizeHeight, heightMultiplier, mapChunkSizeWidth);
        terrain.terrainData.SetHeights(0, 0, noiseMap);
    }

}
