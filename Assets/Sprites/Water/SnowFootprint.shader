Shader "RPG/Snow Footprint"
{
    Properties { _MainTex("Sprite",2D)="white" {} }
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
            #include "SnowDistribution.cginc"
            sampler2D _GroundTex;
            float4x4 _GroundUV;
            float4 _GroundRect;
            float _Coverage, _SnowVariation, _DriftScale;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; fixed4 color:COLOR; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 q=floor(i.uv*float2(7,11));
                float toe=step(2,q.y)*step(q.y,9)*step(1,q.x)*step(q.x,5);
                float heel=step(q.y,1)*step(2,q.x)*step(q.x,4);
                clip(max(toe,heel)-0.5);
                float2 uv=mul(_GroundUV,float4(i.world,0,1)).xy;
                clip(uv-_GroundRect.xy); clip(_GroundRect.zw-uv);
                fixed4 ground=tex2D(_GroundTex,uv);
                float mask=GroundSnowMask(ground.rgb,floor(i.world*32)/32,_Coverage,_SnowVariation,_DriftScale);
                clip(mask-0.25);
                float rim=step(q.x,1)+step(9,q.y);
                float tread=step(0.5,frac(q.y*0.5))*0.055;
                float3 color=lerp(float3(0.30,0.43,0.52)+tread,float3(0.87,0.94,0.96),saturate(rim));
                return fixed4(color,i.color.a*ground.a*smoothstep(0.25,0.8,mask));
            }
            ENDCG
        }
    }
}
