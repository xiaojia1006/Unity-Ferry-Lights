Shader "RPG/Pixel Snowflakes"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR;};
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=abs(floor(i.uv*5)-2);
                float shape=step(p.x+p.y,2)*step(max(p.x,p.y),2);
                clip(shape-0.5);
                return i.color;
            }
            ENDCG
        }
    }
}
