Shader "RPG/Winter Surface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Coverage ("Snow coverage", Range(0,1)) = 0.85
        _Kind ("Terrain 0, tree 1, roof 2", Float) = 0
        _Wetness ("Rain wetness", Range(0,1)) = 0
        _SnowVariation ("Snow depth variation", Range(0,1)) = 0.85
        _DriftScale ("Snow drift size", Float) = 3
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            #include "SnowDistribution.cginc"
            float _Coverage, _Kind, _Wetness, _SnowVariation, _DriftScale;
            struct snow_v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float2 world:TEXCOORD1; };
            snow_v2f vert(appdata_t v)
            {
                snow_v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                v.vertex = UnityFlipSprite(v.vertex, _Flip);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.texcoord;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 b=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(b),hash(b+float2(1,0)),f.x),lerp(hash(b+float2(0,1)),hash(b+1),f.x),f.y);
            }
            fixed4 frag(snow_v2f i):SV_Target
            {
                fixed4 tex = SampleSpriteTexture(i.uv);
                float3 c=tex.rgb;
                float brightness=max(c.r,max(c.g,c.b));
                float green=step(c.r*0.92,c.g)*step(c.b*1.03,c.g);
                float sand=step(c.b*1.3,c.r)*step(c.b*1.3,c.g);
                float2 p=floor(i.world*32)/32;
                float patches=noise(p*1.3)*0.7+noise(p*4.5)*0.3;
                float grain=hash(floor(i.world*32));
                float mask=0;
                float3 snow=lerp(float3(0.67,0.78,0.86),float3(0.95,0.97,0.96),saturate(brightness*0.8+grain*0.2));
                if (_Kind < 0.5)
                {
                    float depth=SnowDepth(p,_Coverage,_SnowVariation,_DriftScale);
                    mask=GroundSnowMask(c,p,_Coverage,_SnowVariation,_DriftScale);
                    float roadLight=dot(c,float3(0.3,0.59,0.11));
                    c=lerp(c,roadLight*float3(0.68,0.72,0.73),sand*_Coverage*0.85);
                    // Quantized contours and a directional rim suggest piled snow in pixel art.
                    float behind=SnowDepth(p+float2(-0.09,0.13),_Coverage,_SnowVariation,_DriftScale);
                    float rim=saturate((depth-behind)*18);
                    snow=lerp(float3(0.64,0.75,0.82),float3(0.96,0.98,0.97),floor(depth*5)/5);
                    snow+=rim*0.09;
                    snow+=(grain-0.5)*0.035;
                }
                else if (_Kind < 1.5)
                {
                    // Existing bright branch tops collect snow; dark needles and trunks remain visible.
                    mask=green*smoothstep(0.28,0.62,brightness)*smoothstep(0.15,0.35,patches+0.18)*_Coverage;
                    c=lerp(c,c*float3(0.65,0.86,0.94),green*_Coverage*0.55);
                }
                else
                {
                    float roof=step(c.r*1.04,c.b)+step(c.g*1.1,c.r)*step(c.g*1.1,c.b);
                    roof=saturate(roof)*smoothstep(0.4,0.58,i.uv.y);
                    mask=roof*smoothstep(0.12,0.32,brightness)*_Coverage;
                }
                c=lerp(c,snow,saturate(mask));
                c=lerp(c,c*float3(0.67,0.76,0.85),_Wetness);
                fixed4 result=fixed4(c,tex.a)*i.color;
                result.rgb*=result.a;
                return result;
            }
            ENDCG
        }
    }
}
