#ifndef RPG_SNOW_DISTRIBUTION
#define RPG_SNOW_DISTRIBUTION
float SnowHash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
float SnowNoise(float2 p)
{
    float2 b=floor(p), f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(SnowHash(b),SnowHash(b+float2(1,0)),f.x),lerp(SnowHash(b+float2(0,1)),SnowHash(b+1),f.x),f.y);
}
float SnowDepth(float2 p, float coverage, float variation, float scale)
{
    float drift=SnowNoise(p/max(0.2,scale))*0.72+SnowNoise(p/max(0.2,scale)*2.8+19)*0.28;
    // Broad, stable wind drifts. Thin areas expose grass; deep areas hide its texture.
    return saturate(coverage+ (drift-0.54)*variation*2.4);
}
float GroundSnowMask(float3 c,float2 world,float coverage,float variation,float scale)
{
    float brightness=max(c.r,max(c.g,c.b));
    float green=step(c.r*0.92,c.g)*step(c.b*1.03,c.g);
    float sand=step(c.b*1.3,c.r)*step(c.b*1.3,c.g);
    float depth=SnowDepth(world,coverage,variation,scale);
    float patches=SnowNoise(world*1.3)*0.7+SnowNoise(world*4.5)*0.3;
    float grass=smoothstep(0.12,0.68,depth)*green;
    float road=smoothstep(0.50,0.8,patches+depth*0.35)*sand*0.72;
    float stone=(1-sand)*(1-green)*smoothstep(0.55,0.85,brightness)*depth*0.65;
    return max(max(grass,road)*smoothstep(0.18,0.35,brightness),stone)*saturate(coverage*4);
}
#endif
