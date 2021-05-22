using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEnvironment : MonoBehaviour
{
    public static float[,] heightMap;
    public static Vector3[] regions;
    public static int mapChunkSize;

    public GameObject startingBase;

    public void Populate(float[,] hM, Vector3[] r, int mCS)
    {
        heightMap = hM;
        regions = r;
        mapChunkSize = mCS;
        initializeSpawn();
    }

    public void initializeSpawn()
    {
        int borderAlign = mapChunkSize / 2;
        //spawn base A
        int xA = UnityEngine.Random.Range(50, 150);
        int zA = UnityEngine.Random.Range(100, MapGenerator.zSize - 100);
        int xB = UnityEngine.Random.Range(MapGenerator.xSize - 50, MapGenerator.xSize - 150);
        int zB = UnityEngine.Random.Range(100, MapGenerator.zSize - 100);
        MapGenerator.flatten(xA, zA, 10);
        MapGenerator.flatten(xB, zB, 10);

        Vector3 Aloc = new Vector3(xA - borderAlign, heightMap[xA, zA], zA + borderAlign);
        Vector3 Bloc = new Vector3(xB - borderAlign, heightMap[xB, zB], zB + borderAlign);

        Quaternion rotA = new Quaternion(0, 0, 0, 0);
        Quaternion rotB = new Quaternion(180, 0, 0, 0);

        GameObject baseA = Object.Instantiate(startingBase);
        GameObject baseB = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        baseB.transform.position = Bloc;
        baseB.transform.localScale = new Vector3(13, 13, 13);

    }

    public static Vector3 BaseLoc(float[,] heightMap, int side)
    {
        int x = 0;
        int z;

        if (side == 0)
        {
            x = UnityEngine.Random.Range(50, 150);
        }
        else
        {
            x = UnityEngine.Random.Range(MapGenerator.xSize - 50, MapGenerator.xSize - 150);
        }

        z = UnityEngine.Random.Range(100, MapGenerator.zSize - 100);
        //Debug.Log(x + " " + z);

        return new Vector3(x, heightMap[x, MapGenerator.zSize - z], z);
    }

    public static int[,] AddGrass(float[,] heightMap, int[,] grid, int[,] biggrid)
    {
        float radius = Mathf.Sqrt(5);

        for (int i = 0; i < MapGenerator.xSize; i++)
        {
            for (int j = 10; j < MapGenerator.zSize - 10; j++)
            {
                if (Voronoi.regionType[Voronoi.closestPoint[i, j, 0]] == 3)
                {
                    float rand = Random.Range(0f, 1f);
                    if (isValid(i, j, radius, grid) && isValid(i/5, j/5, 2, biggrid) && rand > 0.99f)
                    {
                        //Debug.Log(rand);
                        grid[i, j] = 2;
                    }
                }
            }
        }

        return grid;
    }

    public static int[,] AddTrees(float[,] heightMap, int[,] grid, int[,] biggrid)
    {

        for (int i = 0; i < MapGenerator.xSize; i++)
        {
            for (int j = 10; j < MapGenerator.zSize - 10; j++)
            {
                if (Voronoi.regionType[Voronoi.closestPoint[i, j, 0]] == 0)//Forest
                {
                    float radius = 2;
                    float rand = Random.Range(0f, 1f);
                    float randBaseRatio = Random.Range(0f, 1f);
                    if (isValid(i, j, radius, grid) && isValid(i/5, j/5, radius/2, biggrid) && rand > 0.975f && randBaseRatio < Voronoi.baseRatio[i, j])
                    {
                        //Debug.Log(rand);
                        grid[i, j] = 3;
                    }
                }
                else if (Voronoi.regionType[Voronoi.closestPoint[i, j, 0]] == 1)//Desert
                {
                    float rand = Random.Range(0f, 1f);
                    if (rand > 0.9999f && isValid(i/5, j/5, 2, biggrid))
                    {
                        grid[i, j] = 5;
                    }
                }
                else if (Voronoi.regionType[Voronoi.closestPoint[i, j, 0]] == 3)//Grassland
                {
                    float rand = Random.Range(0f, 1f);
                    if (rand > 0.9999f && isValid(i/5, j/5, 2, biggrid))
                    {
                        grid[i, j] = 4;
                    }
                }
                else if (Voronoi.regionType[Voronoi.closestPoint[i, j, 0]] == 4)//Tundra
                {
                    float rand = Random.Range(0f, 1f);
                    float radius = 10;
                    float randBaseRatio = Random.Range(0f, 1f);
                    if (rand > 0.999f && isValid(i, j, radius, grid) && isValid(i/5, j/5, radius/5, biggrid) && randBaseRatio < Voronoi.baseRatio[i, j])
                    {
                        grid[i, j] = 6;
                    }
                }
            }
        }

            return grid;
    }
     
    public static int[,] AddObstacles(float[,] heightMap, int[,] grid, int[,] biggrid)
    {
        for (int i = 0; i < MapGenerator.xSize; i++)
        {
            for (int j = 10; j < MapGenerator.zSize - 10; j++)
            {
                if (Voronoi.regionType[Voronoi.closestPoint[i, j, 0]] == 0)//grassland
                {
                    float rand = Random.Range(0f, 1f);
                    float radius = 15;
                    float randBaseRatio = Random.Range(0f, 1f);
                    if (rand > 0.9999f && isValid(i, j, radius, grid) && isValid(i/5, j/5, 2, biggrid) && randBaseRatio < Voronoi.baseRatio[i, j])
                    {
                        if (Random.Range(0f, 1f) > 0.5)
                            grid[i, j] = 7;

                        if (Random.Range(0f, 1f) < 0.5)
                            grid[i, j] = 8;
                    }
                }

            }
        }

        return grid;
    }

    public static int[,] AddObjectives(Vector3[] regions, int[,] biggrid)
    {
        for (int i = 0; i < regions.Length; i++)
        {
            for(int j = 0; j < UnityEngine.Random.Range(5, 8); j++)
            {
                int placed = 0;
                int attempts = 0;
                while (placed != 1)
                {
                    int val = 2;
                    int offsetX = UnityEngine.Random.Range(-val, val);
                    int offsetZ = UnityEngine.Random.Range(-val, val);
                    if (isValid((int)regions[i].x/5 + offsetX, (int)regions[i].z/5 + offsetZ, 2, biggrid))
                    {
                        biggrid[(int)regions[i].x/5 + offsetX, (int)regions[i].z/5 + offsetZ] = UnityEngine.Random.Range(10, 17);
                        placed = 1;
                    }
                    else
                    {
                        attempts++;
                    }

                    if(attempts > 10)
                    {
                        val += 2;
                        attempts = 0;
                    }
                }
            }
        }

        return biggrid;
    }


    public static bool isValid(int x, int z, float radius, int[,] grid)
    {
        int searchSquares = Mathf.CeilToInt(radius);
        for(int i = x-searchSquares; i < x + searchSquares; i++)
        {
            for (int j = z-searchSquares; j < z + searchSquares; j++)
            {
                if (i > -1 && i < MapGenerator.xSize && j > -1 && j < MapGenerator.zSize)
                {
                    if(grid[i,j] > 0)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}
