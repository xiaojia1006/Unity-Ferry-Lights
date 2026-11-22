Shader "Rpg/NightAtmosphere"
{
    Properties { _MainTex ("World", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Tint;
            float _Darkness, _Aspect, _Snow, _Pulse;
            int _LightCount, _WarningCount;
            float4 _Lights[8]; // viewport center, viewport vertical radius, intensity
            float4 _LightColors[8];
            float4 _Warnings[4]; // viewport center, viewport x/y radius
            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 world=tex2D(_MainTex,i.uv);
                float2 uv=i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0) uv.y=1-uv.y;
                #endif
                float2 centered=(uv-.5)*float2(_Aspect,1);
                float vignette=smoothstep(.32,.92,length(centered));
                float amount=clamp(_Darkness+vignette*.055*(_Darkness/.45)-_Pulse,0,.5);
                float illumination=0;float3 warmth=0;
                for(int n=0;n<8;n++)
                {
                    if(n>=_LightCount)break;
                    float2 delta=(uv-_Lights[n].xy)*float2(_Aspect,1)/max(.0001,_Lights[n].z);
                    float distance=length(delta);
                    float falloff=pow(saturate(1-distance),2);
                    float light=falloff*_Lights[n].w;
                    illumination+=light;
                    warmth+=_LightColors[n].rgb*light;
                }
                float safety=0;
                for(int w=0;w<4;w++)
                {
                    if(w>=_WarningCount)break;
                    float d=length((uv-_Warnings[w].xy)/max(float2(.0001,.0001),_Warnings[w].zw));
                    safety=max(safety,1-smoothstep(.9,1.18,d));
                }
                float night=saturate(_Darkness/.4);
                float localDark=amount*(1-saturate(illumination*1.75))*(1-safety);
                float3 result=lerp(world.rgb,_Tint.rgb,localDark);
                // Warm pool of actual ground illumination, rather than an opaque circle over the art.
                result+=warmth*.075*night*lerp(1,.6,_Snow)*(1-safety);
                return float4(saturate(result),world.a);
            }
            ENDCG
        }
    }
}
