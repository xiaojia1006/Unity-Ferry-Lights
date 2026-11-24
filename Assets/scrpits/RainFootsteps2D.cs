using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Short-lived ground contacts, rather than screen-space rain attached to the actor.
[DisallowMultipleComponent]
public sealed class RainFootsteps2D : MonoBehaviour
{
    [Min(15)] public float minimumThunderInterval = 35;
    [Min(15)] public float maximumThunderInterval = 85;
    [Range(0,1)] public float thunderVolume = .32f;
    private WeatherCycle2D weather;
    private PlayerMovement player;
    private ObliqueDepth2D depth;
    private Collider2D body;
    private Tilemap[] maps;
    private WaterSurface2D[] rivers;
    private WillowreachWorld world;
    private Sprite[] frames;
    private Texture2D texture;
    private readonly List<Contact> contacts = new List<Contact>();
    private AudioSource audioSource;
    private AudioClip[] thunder;
    private Vector2 previous;
    private float travel, lookup, thunderRemaining;
    private bool tracking, left, wasRaining;
    public int ThunderCount { get; private set; }
    public int ActiveCount { get { int n=0; foreach(var c in contacts)if(c.renderer.enabled)n++; return n; } }
    private sealed class Contact { public SpriteRenderer renderer; public float born; }

    private void Awake()
    {
        weather=GetComponent<WeatherCycle2D>();
        maps=FindObjectsOfType<Tilemap>(); rivers=FindObjectsOfType<WaterSurface2D>();world=FindObjectOfType<WillowreachWorld>();
        // Six stepped ellipses with nearest filtering keep the existing pixel-art density.
        texture=new Texture2D(32*6,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        var pixels=new Color32[texture.width*texture.height];
        frames=new Sprite[6];
        for(int f=0;f<6;f++)for(int y=0;y<16;y++)for(int x=0;x<32;x++)
        {
            float rx=4+f*2,ry=2+f;
            float d=Mathf.Sqrt(Mathf.Pow((x-15.5f)/rx,2)+Mathf.Pow((y-7.5f)/ry,2));
            if(Mathf.Abs(d-1)<.13f) pixels[y*texture.width+f*32+x]=new Color32(173,221,227,230);
            if(f<2&&Mathf.Abs(x-16)==3+f&&y==10+f)pixels[y*texture.width+f*32+x]=new Color32(202,234,239,255);
        }
        texture.SetPixels32(pixels);texture.Apply();
        for(int i=0;i<6;i++)frames[i]=Sprite.Create(texture,new Rect(i*32,0,32,16),new Vector2(.5f,.5f),64);
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;audioSource.loop=false;
        thunder=new[]{MakeThunder(17),MakeThunder(83),MakeThunder(149)};
    }

    private void LateUpdate()
    {
        if(weather==null||!weather.isActiveAndEnabled)
        {audioSource.Stop();foreach(var c in contacts)c.renderer.enabled=false;tracking=false;wasRaining=false;return;}
        if(Time.deltaTime<=0){if(audioSource.isPlaying)audioSource.Pause();return;}
        if(audioSource.clip!=null&&!audioSource.isPlaying)audioSource.UnPause();
        AdvanceThunder(Time.deltaTime);
        if(player==null&&Time.time>=lookup)
        {
            lookup=Time.time+1;player=FindObjectOfType<PlayerMovement>();
            if(player!=null){depth=player.GetComponent<ObliqueDepth2D>();foreach(var c in player.GetComponents<Collider2D>())if(!c.isTrigger){body=c;break;}}
        }
        if(player!=null)
        {
            Vector2 feet=depth!=null&&depth.feet!=null?(Vector2)depth.feet.position:body!=null?new Vector2(body.bounds.center.x,body.bounds.min.y):(Vector2)player.transform.position;
            bool moving=player.isActiveAndEnabled&&!player.isShooting&&player.rb!=null&&player.rb.velocity.sqrMagnitude>.04f;
            Track(feet,moving,depth!=null&&depth.elevated);
        }
        foreach(var c in contacts)
        {
            float age=(Time.time-c.born)/.55f;c.renderer.enabled=age<1&&weather.RainAmount>.05f;
            if(!c.renderer.enabled)continue;
            c.renderer.sprite=frames[Mathf.Clamp((int)(age*6),0,5)];
            c.renderer.color=new Color(1,1,1,(1-age)*Mathf.Clamp01(weather.RainAmount)*.85f);
        }
    }
    public void Track(Vector2 feet,bool moving,bool elevated)
    {
        if(!tracking){previous=feet;tracking=true;return;}
        Vector2 delta=feet-previous;previous=feet;float distance=delta.magnitude;
        if(!moving||elevated||weather.RainAmount<.15f||distance>2){travel=0;return;}
        if(distance<.0001f)return;
        Vector2 direction=delta/distance;
        for(float d=.38f-travel;d<=distance;d+=.38f)
        {
            TryStep(feet-direction*(distance-d)+new Vector2(direction.y,-direction.x)*(left?-.07f:.07f));left=!left;
        }
        travel=(travel+distance)%.38f;
    }
    public bool TryStep(Vector2 p)
    {
        if(weather.RainAmount<.15f)return false;
        // Bridges are dry planks; river already owns its own water response.
        if(world!=null)
        {
            foreach(var bridge in world.bridges)if(bridge.Contains(p))return false;
            if(world.shortcutBridge!=null&&world.shortcutBridge.activeInHierarchy&&world.shortcutBridgeArea.Contains(p))return false;
        }
        foreach(var river in rivers)
        {if(river==null||!river.isActiveAndEnabled)continue;var col=river.GetComponent<PolygonCollider2D>();if(col!=null&&col.enabled&&col.OverlapPoint(p))return false;}
        TilemapRenderer ground=null;int order=int.MinValue;
        foreach(var map in maps)
        {
            if(map==null||!map.gameObject.activeInHierarchy)continue;
            var renderer=map.GetComponent<TilemapRenderer>();var view=map.GetComponent<ObliqueTilemap2D>();
            int n=view!=null?view.terrainOrder:renderer!=null?renderer.sortingOrder:0;
            var art=map.GetSprite(map.WorldToCell(p));
            if(renderer==null||!renderer.enabled||n>0||n<order||art==null||ObliqueTilemap2D.IsTree(art)||(view!=null&&view.splitTrees))continue;
            ground=renderer;order=n;
        }
        if(ground==null)return false;
        Contact contact=contacts.Find(c=>!c.renderer.enabled);
        if(contact==null)
        {
            if(contacts.Count>=48)return false;
            var go=new GameObject("Rain foot ripple"){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(transform,false);
            contact=new Contact{renderer=go.AddComponent<SpriteRenderer>()};contacts.Add(contact);
        }
        contact.born=Time.time;contact.renderer.enabled=true;contact.renderer.sprite=frames[0];
        contact.renderer.sortingLayerID=ground.sortingLayerID;contact.renderer.sortingOrder=order+2;
        contact.renderer.transform.position=new Vector3(p.x,p.y,0);contact.renderer.color=new Color(1,1,1,.85f);
        return true;
    }
    public void AdvanceThunder(float dt)
    {
        bool raining=weather.currentWeather==WeatherCycle2D.Weather.Rain&&weather.RainAmount>.5f;
        audioSource.volume=Mathf.Max(thunderVolume,.42f)*weather.RainAmount*GlobalSettings.EnvironmentVolume;
        if(!raining){wasRaining=false;if(weather.RainAmount<.05f)audioSource.Stop();return;}
        if(!wasRaining){thunderRemaining=UnityEngine.Random.Range(7f,15f);wasRaining=true;return;}
        thunderRemaining-=Mathf.Max(0,dt);
        if(thunderRemaining>0||audioSource.isPlaying)return;
        audioSource.clip=thunder[UnityEngine.Random.Range(0,thunder.Length)];audioSource.pitch=UnityEngine.Random.Range(.92f,1.05f);audioSource.Play();ThunderCount++;
        thunderRemaining=UnityEngine.Random.Range(Mathf.Max(15,minimumThunderInterval),Mathf.Max(minimumThunderInterval,maximumThunderInterval));
    }
    private static AudioClip MakeThunder(int seed)
    {
        const int rate=22050;var samples=new float[rate*5];var random=new System.Random(seed);float low=0,slow=0,peak=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)rate;float noise=(float)(random.NextDouble()*2-1);low+=.055f*(noise-low);slow+=.012f*(noise-slow);
            float envelope=Mathf.Min(1,t/.18f)*Mathf.Exp(-t*.8f)*(1+.25f*Mathf.Sin(t*9+seed));
            float rumble=(low*.6f+slow*2)*envelope;
            rumble+=noise*.025f*Mathf.Exp(-Mathf.Pow((t-.28f)*12,2));samples[i]=rumble;peak=Mathf.Max(peak,Mathf.Abs(rumble));
        }
        for(int i=0;i<samples.Length;i++)samples[i]=samples[i]/Mathf.Max(.01f,peak)*.8f*Mathf.Clamp01((5-i/(float)rate)*2);
        var clip=AudioClip.Create("Distant thunder "+seed,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    private void OnDisable(){if(audioSource!=null)audioSource.Stop();foreach(var c in contacts)if(c.renderer!=null)c.renderer.enabled=false;tracking=false;wasRaining=false;}
    private void OnDestroy()
    {
        foreach(var c in contacts)if(c.renderer!=null)Destroy(c.renderer.gameObject);
        if(frames!=null)foreach(var s in frames)Destroy(s);if(texture!=null)Destroy(texture);
        if(thunder!=null)foreach(var clip in thunder)Destroy(clip);if(audioSource!=null)Destroy(audioSource);
    }
}
