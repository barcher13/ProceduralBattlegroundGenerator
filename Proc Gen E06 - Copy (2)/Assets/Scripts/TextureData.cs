using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TextureData : MonoBehaviour
{

    public Layer[] layers;
    public void ApplyToMaterial(Material material)
    {

    }

    public void UpdateMeshHeights(float[] minHeight, float[] maxHeight)
    {
        //material.SetFloatArray("minHeight", minHeight);
//material.SetFloatArray("maxHeight", maxHeight);
    }

    [System.Serializable]
    public class Layer
    {
        public Texture texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0,1)]
        public float startHeight;
        [Range(0,1)]
        public float blendStrength;
        public float textureScale;
    }
}
