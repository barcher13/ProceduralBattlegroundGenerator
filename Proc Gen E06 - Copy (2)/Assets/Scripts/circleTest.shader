Shader "Custom/circleTest"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _SecondTex("Second", 2D) = "white" {}
        _ThirdTex("Third", 2D) = "white" {}
        _FourthTex("Fourth", 2D) = "white" {}
        _MainColor("Main Color", Color) = (0,1,0)
        _CircleColor("Circle Color", Color) = (1,0,0)
        _Center("Center", Vector) = (0,0,0,0)
        _Thickness("Thickness", Range(0, 100)) = 5
        xSize("xSize", Int) = 1470
        zSize("zSize", Int) = 735
    }
        SubShader
        {
                CGPROGRAM


                // Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
                #pragma exclude_renderers d3d11 gles
                                #pragma surface surfaceFunc Lambert

                                const static int arraySize = 108;

                sampler2D _MainTex;
                sampler2D _SecondTex;
                sampler2D _ThirdTex;
                sampler2D _FourthTex;
                fixed3 _MainColor;
                fixed3 _CircleColor;
                float3 _Center;
                float _Thickness;
                float _Radius;
                int xSize;
                int zSize;
                float baseRatio[arraySize];
                float regions[arraySize];
            
        struct Input {
            float2 uv_MainTex;
            float2 uv_SecondTex;
            float2 uv_ThirdTex;
            float2 uv_FourthTex;
            float3 worldPos;
        };

        void surfaceFunc(Input IN, inout SurfaceOutput o) {
            half4 c = tex2D(_MainTex, IN.uv_MainTex);
            half4 d = tex2D(_SecondTex, IN.uv_SecondTex);
            
            if (IN.worldPos.x < xSize && IN.worldPos.z < zSize) {
                int region = (int)regions[IN.worldPos.z * zSize + IN.worldPos.x];
                if (region == 0) {
                    o.Albedo = tex2D(_MainTex, IN.uv_MainTex) * baseRatio[IN.worldPos.z * zSize + IN.worldPos.x];
                }
                else if (region == 1) {
                    o.Albedo = tex2D(_SecondTex, IN.uv_SecondTex) * baseRatio[IN.worldPos.z * zSize + IN.worldPos.x];
                }
                else if (region == 2) {
                    o.Albedo = tex2D(_ThirdTex, IN.uv_ThirdTex) * baseRatio[IN.worldPos.z * zSize + IN.worldPos.x];
                }
                else{
                    o.Albedo = tex2D(_FourthTex, IN.uv_FourthTex) * baseRatio[IN.worldPos.z * zSize + IN.worldPos.x];
                }
            }


            //float dist = distance(_Center, IN.worldPos);

            //if (dist > _Radius && dist < (_Radius + _Thickness)) {
            //    o.Albedo = d.rgb;
            //}
            //else {
            //    o.Albedo = c.rgb;
            //}
            //o.Alpha = c.a;
        }
        ENDCG
    }

}
