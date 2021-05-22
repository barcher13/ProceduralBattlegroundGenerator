using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (terrainGen))]
public class terrainGenEditor : Editor {
    // Start is called before the first frame update
    public override void OnInspectorGUI()
    {
        terrainGen terGen = (terrainGen)target;

        if (DrawDefaultInspector())
        {
            if (terGen.autoUpdate)
            {
                terGen.GenerateTerrain();

            }

        }

        if(GUILayout.Button ("Generate Terrain Base"))
        {
            terGen.GenerateTerrain();
        }
    }
}
