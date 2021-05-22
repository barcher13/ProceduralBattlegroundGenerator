Shader "Custom/terrainShader2"
{
    Properties
    {
        _MainTexture("Texture", 2D) = "white"{}
        _SecondTexture("Second Texture", 2D) = "white"{}
        testScale("Scale", Float) = 0.5
        a1("A1", Float) = 0.5
        a2("A2", Float) = 0.5
        a3("A3", Float) = 0.5
        b1("B1", Float) = 0.5
        b2("B2", Float) = 0.5
        b3("B3", Float) = 0.5
        c1("C1", Float) = 0.5
        c2("C2", Float) = 0.5
        d("D", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTexture;
        sampler2D _SecondTexture;
        float testScale;
        float a1;
        float a2;
        float a3;
        float b1;
        float b2;
        float b3;
        float c1;
        float c2;
        float d;

        int layerCount;
        //float3 regionMaps[layerCount];
        //float3 baseColours[maxLayerCount];
       //float baseStartHeights[maxLayerCount];
        //float baseBlends[maxLayerCount];
        //float baseColourStrength[maxLayerCount];
        //float baseTextureScales[maxLayerCount];

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            float t = testScale;
            float3 scaledWorldPos = IN.worldPos / float3(t,t,t);
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            float3 xProjection = tex2D(_MainTexture, scaledWorldPos.yz) * blendAxes.x;
            float3 yProjection = tex2D(_MainTexture, scaledWorldPos.xz) * blendAxes.y;
            float3 zProjection = tex2D(_MainTexture, scaledWorldPos.xy) * blendAxes.z;

            float3 finalProjection = xProjection + yProjection + zProjection;

            float3 a = float3(a1, a2, a3);
            float3 b = float3(b1, b2, b3);
            float c = float(c1);

            finalProjection.xy *= d;
            finalProjection.xz *= a1;

            o.Albedo = finalProjection;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
