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
            float4 _Player;
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
                    float falloff=1-smoothstep(.06,1,distance);
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
                float night=saturate(_Darkness/.45);
                float lamp=saturate(illumination*1.85);
                float2 playerDelta=(uv-_Player.xy)*float2(_Aspect,1)/max(.0001,_Player.z);
                float nearby=(1-smoothstep(.08,1,length(playerDelta)))*_Player.w;
                // Preserve the painted palette and dark silhouettes instead of mixing in grey luminance.
                float shade=saturate(amount/.5)*(1-lamp*.83)*(1-nearby*.20);
                float3 moonFilter=lerp(float3(.26,.36,.55),float3(.34,.45,.62),_Snow*.4);
                float3 moonlit=world.rgb*lerp(float3(1,1,1),moonFilter,shade);
                // Gentle S contrast protects black outlines and stops bright paths reading as daytime.
                float3 contrasted=saturate((moonlit-.055*night)*(1+night*.09));
                float3 warmGround=world.rgb*float3(1.04,.83,.57);
                float3 result=lerp(contrasted,warmGround,lamp*night*.74);
                result+=warmth*.085*night*lerp(1,.55,_Snow);
                result+=world.rgb*_Pulse*.7;
                // Keep actual red warning pixels readable; don't reveal the entire square beneath them.
                float redWarning=safety*smoothstep(.12,.35,world.r-max(world.g,world.b));
                result=lerp(result,world.rgb,redWarning);
                return float4(saturate(result),world.a);
            }
            ENDCG
        }
    }
}
