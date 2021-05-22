using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, ColourMap, Mesh, Voronoi};
	public DrawMode drawMode;

	public static int mapChunkSize = 245;
	public static int xSize = 1470;
	public static int zSize = 735;
	[Range(0,6)]
	public int levelOfDetail;
	public float noiseScale; 

	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve focusFuncCurve;
	public AnimationCurve regionFocusCurve;
	public AnimationCurve meshHeightCurveBase;
	public AnimationCurve colorFader;

	public bool autoUpdate;
	public bool objectPlacement;
	public bool convolution;
	public bool heightConvolution;
	public bool AorB;
	public bool textureFade;
	public bool experimental;

	public int hconvNum;
	public int tconvNum;


	public WorldObject[] worldObjects;

	public GameObject GrassParent;
	public GameObject TreeParent;
	public GameObject ObstacleParent;

	public float[] regionMaxHeight;
	public float[] regionMinHeight;

	GameObject[,] globalMap = new GameObject[3,6];

	//int testX = 640;
	//int testZ = 640;

	public void DrawMapInEditor()
    {
		MapData mapData = GenerateMapData();
		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		}
		else if (drawMode == DrawMode.ColourMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap[0][0], mapChunkSize, mapChunkSize));
		}
		else if (drawMode == DrawMode.Mesh)
		{
			GameObject curMesh;
			for(int x = 0; x < 3; x++)
            {
				for(int z = 0; z < 6; z++)
                {
					curMesh = globalMap[x,z];
					curMesh.transform.position = new Vector3((z * (mapChunkSize-1))-mapChunkSize/2, 0, ((3-x) * (mapChunkSize-1))+mapChunkSize/2);
					MeshFilter meshFilter = curMesh.GetComponent<MeshFilter>();
					meshFilter.sharedMesh = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, levelOfDetail, z * (mapChunkSize-1), x * (mapChunkSize-1)).CreateMesh();
					MeshRenderer meshRenderer = curMesh.GetComponent<MeshRenderer>();
					meshRenderer.sharedMaterial.mainTexture = TextureGenerator.TextureFromColourMap(mapData.colourMap[0][0], mapChunkSize, mapChunkSize);
				}
			}
			
		}
		else if (drawMode == DrawMode.Voronoi)
        {
			Vector3[] regions = Voronoi.GenerateRegions();
			List<float[,]> heightMapLayers = new List<float[,]>();

			for(int i = 0; i < regions.Length; i++)
            {
				heightMapLayers.Add(GenerateRegionHeightMap(i));
            }

			double [,,] voronoiMap = Voronoi.DetermineClosestNeighbors(regions);

			float[,] finalHeightMap = CombineHeightMaps(heightMapLayers, mapData.heightMap, voronoiMap, regions);

			mapData = GenerateColourData(mapData, finalHeightMap);

			GameObject curMesh;

			if (objectPlacement)
			{
				initEnvironment(ref finalHeightMap, regions);
			}

			for (int x = 0; x < 3; x++)
			{
				for (int z = 0; z < 6; z++)
				{
					curMesh = globalMap[x, z];
					curMesh.transform.position = new Vector3((z * (mapChunkSize - 1))+mapChunkSize/2, 0, ((3 - x) * (mapChunkSize - 1))-mapChunkSize/2);
					MeshFilter meshFilter = curMesh.GetComponent<MeshFilter>();
					meshFilter.sharedMesh = MeshGenerator.GenerateTerrainMesh(finalHeightMap, meshHeightMultiplier, levelOfDetail, z * (mapChunkSize - 1), x * (mapChunkSize - 1)).CreateMesh();
					Material mat = curMesh.GetComponent<Material>();
					MeshRenderer meshRenderer = curMesh.GetComponent<MeshRenderer>();
					meshRenderer.sharedMaterial.mainTexture = TextureGenerator.TextureFromColourMap(mapData.colourMap[z][x], mapChunkSize, mapChunkSize);
					renderTexture(meshRenderer.sharedMaterial);
				}
			}
		}
	}


	public void renderTexture(Material material)
    {
		float[] baseRatioShader = new float[xSize * zSize];
		float[] regionShader = new float[xSize * zSize];

		for(int x = 0; x < xSize; x++)
        {
			for(int z = 0; z < zSize; z++)
            {
				baseRatioShader[z * zSize + x] = (float)Voronoi.baseRatio[x, z];
				regionShader[z * zSize + x] = (float)Voronoi.closestPoint[x, z, 0];
            }
        }

		// material.SetFloatArray("baseRatio", baseRatioShader);
		//material.SetFloatArray("regions", regionShader);
		//material.SetInt("xSize", xSize);
		//material.SetInt("zSize", zSize);

    }


	public void InitMap()
    {
		//Debug.Log("test");
        for(int x = 0; x < 3; x++)
        {
			for(int z = 0; z < 6; z++)
            {
				string temp = "(" + z + "," + x + ")";
                globalMap[x, z] = GameObject.Find(temp);
			}
        }
    }


    MapData GenerateMapData() {

		float[,] noiseMap = Noise.GenerateNoiseMap (xSize, zSize, seed + UnityEngine.Random.Range(0,1000), noiseScale, octaves, persistance, lacunarity, offset);



		//Color[,,] colourMap = new Color[xSize/mapChunkSize, zSize/mapChunkSize, mapChunkSize * mapChunkSize];
		List<List<Color[]>> colourMap = new List<List<Color[]>>();
		for(int i = 0; i < xSize/mapChunkSize; i++)
        {
			List<Color[]> temp = new List<Color[]>();
			for (int j = 0; j < zSize / mapChunkSize; j++)
            {
				temp.Add(new Color[mapChunkSize * mapChunkSize]);
            }
			colourMap.Add(temp);
        }
		

		return new MapData(noiseMap, colourMap);
	}


	MapData GenerateColourData(MapData cur, float[,] heightMap)
    {
		int r;

		for (int x = 0; x < xSize; x++)
		{
			for (int z = 0; z < zSize; z++)
			{

				int modX = x % mapChunkSize;
				int curChunkX = (x - modX) / mapChunkSize;
				int modZ = z % mapChunkSize;
				int curChunkZ = (z - modZ) / mapChunkSize;

				//Debug.Log(Voronoi.closest[x, z]);
				r = Voronoi.regionType[Voronoi.closestPoint[x, z, 0]];

				float heightVar = (heightMap[x, z] - regionMinHeight[r]) / (regionMaxHeight[r] - regionMinHeight[r]);

				if (textureFade)
				{
					if (Voronoi.baseRatio[x, z] == 1)
					{
						cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = getColor(heightVar, r);
					}
					else
					{
						//float rand = colorFader.Evaluate(UnityEngine.Random.Range(0f, 1f));
						float rand = 0f;
						if (rand <= Voronoi.baseRatio[x, z] / 2 + 0.5)
						{
							//cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = getColor(heightVar, r);
							Color a = getColor(0.1f, Voronoi.regionType[Voronoi.midlinePoints[x,z]]);
							Color b = getColor(heightVar, r);
							float ratio = (float)Voronoi.baseRatio[x, z];
							cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = a * (1 - (ratio / 2 + .5f)) + b * (ratio / 2 + .5f);
						}
						else
						{
							//cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = ;

							Color a = getColor(0.1f, Voronoi.regionType[Voronoi.midlinePoints[x,z]]);
							Color b = getColor(heightVar, r);
							float ratio = (float)Voronoi.baseRatio[x, z];
							cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = a * (ratio / 2 + .5f) + b * (1 - (ratio / 2 + .5f));
						}
					}
                }
                else
                {
					cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = getColor(heightVar, r);
				}

			}
		}
		if (convolution)
		{
			for (int i = 0; i < tconvNum; i++)
			{
				for (int x = 0; x < xSize - 1; x++)
				{
					for (int z = 0; z < zSize - 1; z++)
					{

						int modX = x % mapChunkSize;
						int curChunkX = (x - modX) / mapChunkSize;
						int modZ = z % mapChunkSize;
						int curChunkZ = (z - modZ) / mapChunkSize;

						Color b = new Color(0, 0, 0, 1);
						Color a = new Color(0, 0, 0, 1);

						if ((modZ * mapChunkSize + (modX + 1)) < mapChunkSize * mapChunkSize)
						{
							a = cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + (modX + 1)];
						}
						else if (curChunkX == 3)
						{
							a = cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX];
						}
						else
						{ 
							if(curChunkX == 3 && (curChunkZ % 10 == 0))
							{
								Debug.Log(modX);
							}
							a = cur.colourMap[curChunkX][curChunkZ+1][modZ * mapChunkSize + 0];
						}

						if (((modZ + 1) * mapChunkSize + modX) < mapChunkSize * mapChunkSize)
						{
							b = cur.colourMap[curChunkX][curChunkZ][(modZ + 1) * mapChunkSize + modX];
						}
						else if (curChunkX == 6)
						{
							b = cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX];
						}
						else
						{
							b = cur.colourMap[curChunkX][curChunkZ + 1][0 * mapChunkSize + modX];
						}


						if (experimental)
						{
							float aVal = UnityEngine.Random.Range(0.1f, 0.5f);
							float bVal = UnityEngine.Random.Range(0.1f, 0.5f);
							float remainder = 1 - (aVal + bVal);

							cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = (cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX]*remainder + a*aVal + b*bVal);
						}
						else
						{
							cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] = (cur.colourMap[curChunkX][curChunkZ][modZ * mapChunkSize + modX] + a + b) / 3;
						}

					}
				}
			}
		}


		return new MapData(cur.heightMap, cur.colourMap);

	}

	public Color getColor(float heightVar, int r)
    {
		int lowerB = (int)(1000 * heightVar);
		int upperB = (int)(-500 * (1-heightVar));
		if (r == 3) // grassland
		{
			int rand = UnityEngine.Random.Range(0, 1000);
			if (rand < 650)
			{
				return new Color32(31, 109, 4, 1);
			}
			else if(rand < 960)
            {
				return new Color32(69, 132, 17, 1);
            }else
			{
				return new Color32(107, 155, 30, 1);
			}
		}
		else if (r == 2) //mountains
		{
			int rand = UnityEngine.Random.Range(lowerB, 1000+upperB);
			if(rand < 200)
            {
				return getColor(heightVar, 3);
			}else if(rand < 500)
            {
				return new Color32(105, 101, 111, 1);
			}
            else if(rand < 800)
            {
				return new Color32(172, 161, 161, 1);
            }
            else
            {
				
				return getColor(heightVar/2, 4);
			}
		}
		else if (r == 1) // desert
		{
			int rand = UnityEngine.Random.Range(lowerB, 1000-(int)(500*heightVar));
			if (rand < 350)
			{
				return new Color32(246, 228, 173, 1);
			}
			else if(rand < 550)
            {
				return new Color32(247, 212, 153, 1);
			}else if(rand < 700)
            {
				return new Color32(249, 203, 138, 1);
			}
			else if(rand < 900)
            {
				return new Color32(238, 197, 138, 1);
			}
            else
            {
				return new Color32(235, 203, 143, 1);
			}
		}
		else if (r == 0) // forest
		{
			int rand = UnityEngine.Random.Range(0, 1000);
			if (rand < 650)
			{
				return new Color32(69, 132, 17, 1);
			}
			else if (rand < 960)
			{
				return new Color32(88, 144, 24, 1);
			}
			else
			{
				return new Color32(107, 155, 30, 1);
			}
		}
		else //tundra
		{
			int rand = UnityEngine.Random.Range(0, 1000);
			if(rand < 450)
            {
				return new Color32(220, 219, 219, 1);
            }else if(rand < 980)
            {
				return new Color32(220, 224, 225, 1);
            }
            else
            {
				return new Color32(200, 210, 212, 1);

            }
		}
	}


	public float[,] GenerateRegionHeightMap(int i)
    {
		int regionType = Voronoi.regionType[i];
		//Debug.Log(i);
		float NS = Voronoi.regionData[regionType, 0];
		int Octaves = (int)Voronoi.regionData[regionType, 1];
		float P = Voronoi.regionData[regionType, 2];
		float L = Voronoi.regionData[regionType, 3];
        //Debug.Log("NS: " + NS + " Octaves: " + Octaves + " P: " + P + " L: " + L);

		return Noise.GenerateNoiseMap(xSize, zSize, seed, NS, Octaves, P, L, offset);
    }

	public float[,] CombineHeightMaps(List<float[,]> regionHeightMaps, float[,] baseHeightMap, double[,,] voronoiMap, Vector3[] regions)
    {
		float[,] finalHeightMap = new float[xSize, zSize];
		float layerHeight;

		regionMaxHeight = new float[regions.Length];
		regionMinHeight = new float[regions.Length];
		for(int i = 0; i < regions.Length; i++)
        {
			regionMinHeight[i] = float.MaxValue;
        }

		for (int x = 0; x < xSize; x++)
		{
			//Debug.Log(x);
			for (int z = 0; z < zSize; z++)
			{

				finalHeightMap[x, z] = (float)meshHeightCurveBase.Evaluate(baseHeightMap[x,z]) * meshHeightMultiplier;
				layerHeight = regionHeightMaps[Voronoi.closestPoint[x, z, 0]][x, z];

				finalHeightMap[x, z] += layerHeight * regionFocusCurve.Evaluate((float)Voronoi.baseRatio[x,z]) * Voronoi.heightMultFunc(x, z);

				if(regionMaxHeight[Voronoi.closestPoint[x,z,0]] < finalHeightMap[x, z])
                {
					regionMaxHeight[Voronoi.closestPoint[x, z, 0]] = finalHeightMap[x, z];
                }else if(regionMinHeight[Voronoi.closestPoint[x,z,0]] > finalHeightMap[x, z])
                {
					regionMinHeight[Voronoi.closestPoint[x, z, 0]] = finalHeightMap[x, z];
                }

			}
		}


        if (heightConvolution)
        {
			for (int i = 0; i < hconvNum; i++)
			{
				for (int x = 1; x < xSize - 1; x++)
				{
					for (int z = 1; z < zSize - 1; z++)
					{
						if (AorB)
						{
							finalHeightMap[x, z] = (finalHeightMap[x, z] + finalHeightMap[x + 1, z] + finalHeightMap[x, z + 1]) / 3f;
							//finalHeightMap[x, z] = (finalHeightMap[x, z] + finalHeightMap[x - 1, z] + finalHeightMap[x, z - 1]) / 3f;
						}
						else
						{
							finalHeightMap[x, z] = (finalHeightMap[x, z] + finalHeightMap[x + 1, z] + finalHeightMap[x - 1, z]) / 3f;
							finalHeightMap[x, z] = (finalHeightMap[x, z] + finalHeightMap[x, z + 1] + finalHeightMap[x, z - 1]) / 3f;
						}


						if (regionMaxHeight[Voronoi.closestPoint[x, z, 0]] < finalHeightMap[x, z])
						{
							regionMaxHeight[Voronoi.closestPoint[x, z, 0]] = finalHeightMap[x, z];
						}
						else if (regionMinHeight[Voronoi.closestPoint[x, z, 0]] > finalHeightMap[x, z])
						{
							regionMinHeight[Voronoi.closestPoint[x, z, 0]] = finalHeightMap[x, z];
						}
					}
				}
			}
        }
		return finalHeightMap;

    }




	float focusFunc(float val)
    {
		//return (float)Math.Pow(val, 0.2);
		//return focusFuncCurve.Evaluate(val);
		return val;
	}

	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
	}

	public void initEnvironment(ref float[,] heightMap, Vector3[] regions)
	{

		//Murder Children
		Transform parent = GrassParent.transform;
		foreach (Transform child in parent)
        {
			DestroyImmediate(child.gameObject, false);
        }

		//spawn bases
		GameObject baseA = Instantiate(worldObjects[0].obj);
		Vector3 baseAloc = GameEnvironment.BaseLoc(heightMap, 0);
		baseA.transform.position = baseAloc;

		GameObject baseB = Instantiate(worldObjects[0].obj);
		Vector3 baseBloc = GameEnvironment.BaseLoc(heightMap, 1);
		baseB.transform.position = baseBloc;

		int[,] grid = new int[xSize, zSize];			//small objects like trees and cacti
		int[,] biggrid = new int[(xSize / 5)+6, (zSize / 5)+6]; //large objects like pyramids and bases

		biggrid[(int)baseAloc.x/5, (int)baseAloc.z/5] = 1;
		biggrid[(int)baseBloc.x/5, (int)baseBloc.z/5] = 1;

		//biggrid = GameEnvironment.AddObjectives(regions, biggrid);
		grid = GameEnvironment.AddObstacles(heightMap, grid, biggrid);
		grid = GameEnvironment.AddTrees(heightMap, grid, biggrid);
		grid = GameEnvironment.AddGrass(heightMap, grid, biggrid);

		for(int i = 0; i < xSize/5; i++)
        {
			for (int j = 0; j < zSize / 5; j++)
			{
				if(biggrid[i,j] == 10)
                {
					GameObject castleGate = Instantiate(worldObjects[10].obj);
				}
				else if(biggrid[i,j] == 11)
                {
					GameObject house1 = Instantiate(worldObjects[11].obj);
				}
				else if(biggrid[i,j] == 12)
                {
					GameObject house2 = Instantiate(worldObjects[12].obj);
                }else if(biggrid[i,j] == 13)
                {
					GameObject grass = Instantiate(worldObjects[1].obj);
				}
				else if(biggrid[i,j] == 14)
                {

                }else if(biggrid[i,j] == 15)
                {

                }
                else
                {

                }
			}
        }


		for (int i = 0; i < xSize; i++)
        {
			for(int j = 0; j < zSize; j++)
            {

				if(grid[i,j] == 2)
                {
					GameObject grass = Instantiate(worldObjects[1].obj);
					grass.transform.position = new Vector3(i, heightMap[i, j] + 0.05f, zSize - j);
					grass.transform.localScale = new Vector3(UnityEngine.Random.Range(1.25f,1.75f), UnityEngine.Random.Range(1.25f,1.75f), UnityEngine.Random.Range(1.25f,1.75f));
					grass.transform.rotation = new Quaternion(0, UnityEngine.Random.Range(0,180), 0, 0);
				}else if(grid[i,j] > 2 && grid[i,j] < 7){
					GameObject tree = Instantiate(worldObjects[grid[i, j] - 1].obj);

					if (grid[i, j] == 3) {
						tree.transform.position = new Vector3(i, heightMap[i, j]-.3f, zSize - j);
						tree.transform.localScale = new Vector3(UnityEngine.Random.Range(2, 2.5f), UnityEngine.Random.Range(2, 3.25f), UnityEngine.Random.Range(2, 2.5f));
                    }
                    else if(grid[i, j] == 6)
                    {
						tree.transform.position = new Vector3(i, heightMap[i, j]-.15f, zSize - j);
						tree.transform.localScale = new Vector3(UnityEngine.Random.Range(2, 2.5f), UnityEngine.Random.Range(2, 2.75f), UnityEngine.Random.Range(2, 2.5f));
                    }
                    else
                    {
						tree.transform.position = new Vector3(i, heightMap[i, j] - .35f, zSize - j);
						tree.transform.localScale = new Vector3(UnityEngine.Random.Range(1, 1.5f), UnityEngine.Random.Range(1, 1.5f), UnityEngine.Random.Range(1, 1.5f));
					}

					Quaternion rot = new Quaternion(0, 0, 0, 0);
					rot.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 180), 0);
					tree.transform.rotation = rot;
                }else if(grid[i,j] >= 7 && grid[i,j] < 9)
                {
					GameObject obstacle = Instantiate(worldObjects[grid[i, j]-1].obj);
					obstacle.transform.position = new Vector3(i, heightMap[i, j] - 3, zSize - j);
					obstacle.transform.localScale = new Vector3(UnityEngine.Random.Range(2, 2.2f), UnityEngine.Random.Range(1, 1.5f), UnityEngine.Random.Range(2, 2.2f));
                }
            }
        }
    }

	public static void flatten(int x, int z, double radius)
	{

	}

}



[System.Serializable]
public struct WorldObject
{
	public string name;
	public GameObject obj;
}



public struct MapData
{
	public float[,] heightMap;
	public List<List<Color[]>> colourMap;

	public MapData(float[,] heightMap, List<List<Color[]>> colourMap)
    {
		this.heightMap = heightMap;
		this.colourMap = colourMap;
    }

}


