using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Voronoi : MonoBehaviour
{

    public static int[] regionType;
    public static double[,,] voronoiMap;
    public static int[,,] closestPoint;
    public static double[,,] closestDistance;
    public static int[,] secondClosest;
    public static double[,] baseRatio;
    public static double[,] betweenPoints;
    public static int[,] midlinePoints;
   
    public static float[,] regionData = new float[,] { { 858f, 5f, 0.323f, 2.2f, 95f }, //Forest 0
                                                        {75f,  2f, 0.82f, 1.5f, 18f },  //Desert 1
                                                        {280f, 5f, 0.281f, 2.55f, 95f },//Mountain 2
                                                        {50f,  2f, 0.262f, 2f, 4f }, //Grassland 3
                                                        {400f, 4f, 0.6f, 1.75f, 29f} }; //Tundra 4
    public static int[,] testRegions = new int[,] { { 250, 250 },
                                                    { 450, 150 },
                                                    {750, 350 },
                                                    { 1000,400 } };
    //static int testCoordx = 640;
    //static int testCoordz = 640;

    // Start is called before the first frame update
    public static Vector3[] GenerateRegions()
    {
        int xSize = MapGenerator.xSize;
        int zSize = MapGenerator.zSize;
        int chunkSize = MapGenerator.mapChunkSize;

        int numRegions = UnityEngine.Random.Range(5,7);
        //int numRegions = 4;

        Vector3[] regions = new Vector3[numRegions];
        regionType = new int[numRegions];

        for (int i = 0; i < numRegions; i++)
        {
            if (i == 0)
            {
                regions[i] = new Vector3(UnityEngine.Random.Range(200, xSize - 200), 0, UnityEngine.Random.Range(0, zSize));
                //regions[i] = new Vector3(testRegions[i, 0], 20, testRegions[i, 1]);
            }
            else
            {
                int xTemp = UnityEngine.Random.Range(200, xSize-200);
                int zTemp = UnityEngine.Random.Range(0, zSize);
                //int xTemp = testRegions[i, 0];
                //int zTemp = testRegions[i, 1];

                Vector3 posTemp = new Vector3(xTemp, 0, zTemp);
                
                for (int j = 0; j <= i; j++)
                {
                    
                    if (50000f > (posTemp - regions[j]).sqrMagnitude)
                    {
                        j = 0;
                        xTemp = UnityEngine.Random.Range(200, xSize-200);
                        zTemp = UnityEngine.Random.Range(0, zSize);

                        //int xTemp = testRegions[i, 0];
                        //int zTemp = testRegions[i, 1];
                        posTemp = new Vector3(xTemp, 0, zTemp);
                    }
                    
                }
                
                regions[i] = new Vector3(xTemp, 20, zTemp);
            }

            int type = 0;
            for (int j = 0; j < numRegions; j++)
            {
                if(type >= 5)
                {
                    type = 0;
                }
                regionType[j] = type;
                type++;
            }
        }

        betweenPoints = new double[numRegions, numRegions];
        Vector3 offset;
        double result;

        for(int i = 0; i < numRegions; i++)
        {
            for(int j = 0; j < numRegions; j++)
            {
                if(betweenPoints[i,j] == 0)
                {
                    offset = regions[i] - regions[j];
                    result = Math.Sqrt(offset.sqrMagnitude);
                    betweenPoints[i, j] = result;
                    betweenPoints[j, i] = result;
                }
            }
        }

        return regions;
    }

    public static double[,,] DetermineClosestNeighbors(Vector3[] regions)
    {

        int xSize = MapGenerator.xSize;
        int zSize = MapGenerator.zSize;

        closestDistance = new double[xSize, zSize, regions.Length];
        closestPoint = new int[xSize, zSize, regions.Length];

        baseRatio = new double[xSize, zSize];

        voronoiMap = new double[xSize, zSize, regions.Length];
        midlinePoints = new int[xSize, zSize];

        Vector3 cur;
        Vector3 offset;
        double sqrLen;
        for (int x = 0; x < xSize; x++)
        {
            for (int z = 0; z < zSize; z++)
            {
                cur = new Vector3(x, 0, z);

                double[]  distanceTemp = new double[regions.Length];
                int[] pointTemp = new int[regions.Length];

                for (int i = 0; i < regions.Length; i++)
                {
                        offset = cur - regions[i];
                        sqrLen = Math.Sqrt(offset.sqrMagnitude);
                        distanceTemp[i] = sqrLen;
                        pointTemp[i] = i;

                }

                Array.Sort(distanceTemp, pointTemp);
                for(int i = 0; i < distanceTemp.Length; i++)
                {
                    closestDistance[x, z, i] = distanceTemp[i];
                    closestPoint[x, z, i] = pointTemp[i];
                }

                double closestMidlineDistance = float.MaxValue;
                int closestMidlinePoint = -1;

                for(int i = 1; i < distanceTemp.Length; i++)
                {
                    Vector3 midlinePoint = calculateIntersect(cur, regions[closestPoint[x, z, 0]], regions[closestPoint[x, z, i]]);
                    if(x == 500 && z == 568)
                    {
                        //Debug.Log(i);
                    }
                    offset = cur - midlinePoint;
                    if(offset.sqrMagnitude < closestMidlineDistance)
                    {
                        closestMidlineDistance = offset.sqrMagnitude;
                        closestMidlinePoint = closestPoint[x,z,i];
                    }
                }

                midlinePoints[x, z] = closestMidlinePoint;

                double midLineDist = Math.Sqrt(closestMidlineDistance);

                if (midLineDist > 125)
                {
                    baseRatio[x, z] = 1.0f;
                }
                else
                {
                    baseRatio[x, z] = midLineDist / 125f;
                }

            }
        }

        //printVoronoiMap(voronoiMap);

        return voronoiMap;
    }


    public static void printVoronoiMap(float[,,] voronoiMap)
    {
        for (int x = 0; x < MapGenerator.xSize; x += 10)
        {
            string temp = "";
            for (int z = 0; z < MapGenerator.zSize; z += 10)
            {
                temp = temp + " (" + voronoiMap[x, z, 0] + "," + voronoiMap[x, z, 1] + ")";
            }
            //Debug.Log(temp);
        }
    }

    public static Vector3 calculateIntersect(Vector3 cur, Vector3 centroidA, Vector3 centroidB)
    {
        double m1 = (double)(centroidA.z - centroidB.z) / (double)(centroidA.x - centroidB.x);
        double b1 = cur.z - (cur.x * m1);

        double midpointX = (double)(centroidA.x + centroidB.x) / 2.0;
        double midpointZ = (double)(centroidA.z + centroidB.z) / 2.0;

        double m2 = -1.0 / m1;
        double b2 = midpointZ - (m2 * midpointX);

        double m3 = m1 - m2;
        double b3 = b2 - b1;

        double x = b3 / m3;
        double z = (m1 * x) + b1;

        return new Vector3((float)x, 0, (float)z);
    }



    public static float heightMultFunc(int x, int z)
    {
        return regionData[regionType[closestPoint[x, z, 0]], 4];
    }

}
