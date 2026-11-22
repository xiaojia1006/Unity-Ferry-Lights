Shader "RPG/Pixel Water"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _WaterMask ("River mask", 2D) = "white" {}
        _ShoreTex ("Shore distance", 2D) = "white" {}
        _DeepColor ("Deep water", Color) = (0.16,0.31,0.39,1)
        _ShallowColor ("Shallow water", Color) = (0.36,0.55,0.59,1)
        _FoamColor ("Foam", Color) = (0.84,0.91,0.88,1)
        _PixelScale ("Pixels per world unit", Float) = 32
        _ShallowDistance ("Shallow distance", Float) = 2.2
        _FoamStrength ("Foam strength", Float) = 0.55
        _WinterAmount ("Shore ice", Range(0,1)) = 0
        _RainAmount ("Rain ripples", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _WaterMask, _ShoreTex;
            float4 _DeepColor, _ShallowColor, _FoamColor, _Flow;
            float _PixelScale, _ShallowDistance, _FoamStrength, _WaterTime, _WinterAmount, _RainAmount;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.uv;
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed waterMask = tex2D(_WaterMask, i.uv).a;
                clip(waterMask - 0.01);
                float2 p = floor(i.world * _PixelScale) / _PixelScale;
                float t = floor(_WaterTime * 12) / 12;
                float2 drift = p - t * _Flow.xy;
                float n = noise(drift*2.1) * 0.6 + noise((p+t*_Flow.yx*0.7)*5.4)*0.4;
                float depth = tex2D(_ShoreTex, i.uv).r * 6;
                float shallow = 1-smoothstep(0, max(0.01,_ShallowDistance),depth);
                float3 color = lerp(_DeepColor.rgb,_ShallowColor.rgb,shallow*0.85);
                color += (floor(n*7)/7-0.45)*0.10;
                float wave = sin(drift.y*23 + noise(drift*1.6)*9 + sin(drift.x*4)*1.5);
                float highlight = smoothstep(0.91,0.99,wave)*smoothstep(0.55,0.8,noise(drift*float2(5,2)));
                color += highlight * float3(0.10,0.13,0.14);
                float shoreBreak = noise(p*9+t*0.18);
                float foamLine = 1-smoothstep(0.035,0.10,abs(depth-(0.12+sin(t*1.7+noise(p*2)*4)*0.055)));
                float foam = foamLine*smoothstep(0.2,0.55,shoreBreak)*_FoamStrength;
                color = lerp(color,_FoamColor.rgb,foam);
                float ice = (1-smoothstep(0.16,0.65+noise(p*4)*0.2,depth))*_WinterAmount;
                float cracks = step(0.975,noise(p*float2(13,7)));
                color = lerp(color,float3(0.72,0.84,0.9)-cracks*0.12,ice*0.88);
                float2 cell = floor(p*2);
                float phase = frac(t*1.3+hash(cell));
                float2 center = 0.25+float2(hash(cell+17),hash(cell+41))*0.5;
                float2 delta = (frac(p*2)-center)*float2(1,1.8);
                float ring = (1-smoothstep(0.025,0.075,abs(length(delta)-phase*0.5)))*(1-phase);
                color = color*lerp(float3(1,1,1),float3(0.74,0.82,0.91),_RainAmount);
                color += ring*_RainAmount*0.22;
                return fixed4(color,waterMask);
            }
            ENDCG
        }
    }
}
