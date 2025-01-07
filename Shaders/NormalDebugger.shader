Shader "Debug/NormalDebugger"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(v.normal * 0.5 + 0.5); // Normalize and map to 0-1 range
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(i.normal, 1.0); // Output normals as RGB
            }
            ENDCG
        }
    }
}