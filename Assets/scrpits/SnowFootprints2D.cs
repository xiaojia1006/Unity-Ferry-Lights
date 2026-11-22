using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// World-space pooled impressions: no physics, no renderer changes to the player.
[DisallowMultipleComponent, RequireComponent(typeof(WinterWeather2D))]
public sealed class SnowFootprints2D : MonoBehaviour
{
    private const int Capacity = 96;
    private const float StepDistance = 0.38f;
    private readonly List<Stamp> stamps = new List<Stamp>();
    private Tilemap[] maps;
    private WaterSurface2D[] rivers;
    private WinterWeather2D winter;
    private PlayerMovement player;
    private Collider2D body;
    private ObliqueDepth2D depth;
    private Material material;
    private Sprite sprite;
    private Texture2D texture;
    private MaterialPropertyBlock block;
    private Vector2 previous;
    private float travelled, nextLookup;
    private bool tracking, left;
    private int cursor;
    private sealed class Stamp { public SpriteRenderer renderer; public float time; }
    public int ActiveCount { get { int count=0; foreach(var s in stamps) if(s.renderer.enabled) count++; return count; } }

    private void Awake()
    {
        winter=GetComponent<WinterWeather2D>();
        material=Resources.Load<Material>("SnowFootprint");
        block=new MaterialPropertyBlock();
        texture=new Texture2D(1,1,TextureFormat.RGBA32,false) { name="Footprint quad",filterMode=FilterMode.Point };
        texture.SetPixel(0,0,Color.white); texture.Apply();
        sprite=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(0.5f,0.5f),1);
        RefreshGround();
    }

    public void RefreshGround()
    {
        maps=FindObjectsOfType<Tilemap>();
        rivers=FindObjectsOfType<WaterSurface2D>();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (Time.time >= nextLookup)
        {
            nextLookup=Time.time+1;
            if(player==null)
            {
                player=FindObjectOfType<PlayerMovement>();
                if(player!=null)
                {
                    depth=player.GetComponent<ObliqueDepth2D>();
                    foreach(var collider in player.GetComponents<Collider2D>())
                        if(!collider.isTrigger) { body=collider; break; }
                }
            }
        }
        if(player!=null && Time.deltaTime>0)
        {
            Vector2 feet=depth!=null && depth.feet!=null ? (Vector2)depth.feet.position :
                body!=null ? new Vector2(body.bounds.center.x,body.bounds.min.y) : (Vector2)player.transform.position;
            bool walking=player.isActiveAndEnabled && !player.isShooting &&
                (Mathf.Abs(Input.GetAxisRaw("Horizontal"))+Mathf.Abs(Input.GetAxisRaw("Vertical"))>0.1f);
            Track(feet,walking,depth!=null && depth.elevated);
        }
        UpdateStamps();
    }

    public void Track(Vector2 feet,bool walking,bool elevated)
    {
        if(!tracking) { previous=feet; tracking=true; return; }
        Vector2 delta=feet-previous;
        previous=feet;
        float distance=delta.magnitude;
        if(!walking || elevated || !winter.leaveFootprints || winter.EffectiveSnowCoverage<0.08f || distance>2)
        { travelled=0; return; }
        if(distance<0.0001f) return;
        Vector2 direction=delta/distance;
        float untilStep=StepDistance-travelled;
        // Interpolate along actual travel, so frame rate does not change stride length.
        for(float d=untilStep; d<=distance; d+=StepDistance)
        {
            Vector2 contact=feet-direction*(distance-d);
            Vector2 side=new Vector2(direction.y,-direction.x)*(left ? -0.075f : 0.075f);
            left=!left;
            TryStamp(contact+side,direction);
        }
        travelled=(travelled+distance)%StepDistance;
    }

    public bool TryStamp(Vector2 position,Vector2 direction)
    {
        if(material==null || !winter.leaveFootprints || winter.EffectiveSnowCoverage<0.08f) return false;
        foreach(var river in rivers)
        {
            if(river==null || !river.isActiveAndEnabled) continue;
            var collider=river.GetComponent<PolygonCollider2D>();
            if(collider==null || !collider.enabled) continue;
            // Full print margin prevents shore-crossing stamps, including decorative ice.
            for(int i=0;i<5;i++)
            {
                Vector2 probe=position+(i==0 ? Vector2.zero : new Vector2(i<=2 ? -0.17f : 0.17f,i%2==0 ? -0.17f : 0.17f));
                if(collider.OverlapPoint(probe)) return false;
            }
        }
        Tilemap ground=null;
        Sprite tile=null;
        TilemapRenderer groundRenderer=null;
        Vector3Int cell=default;
        int order=int.MinValue;
        foreach(var map in maps)
        {
            if(map==null || !map.gameObject.activeInHierarchy) continue;
            var renderer=map.GetComponent<TilemapRenderer>();
            var view=map.GetComponent<ObliqueTilemap2D>();
            int terrainOrder=view!=null ? view.terrainOrder : renderer!=null ? renderer.sortingOrder : 0;
            if(renderer==null || !renderer.enabled || terrainOrder>0 || (view!=null && view.splitTrees)) continue;
            var candidate=map.WorldToCell(position);
            var art=map.GetSprite(candidate);
            if(art==null || ObliqueTilemap2D.IsTree(art) || terrainOrder<order) continue;
            ground=map; tile=art; cell=candidate; groundRenderer=renderer; order=terrainOrder;
        }
        if(ground==null) return false;
        Stamp stamp;
        if(stamps.Count<Capacity)
        {
            var child=new GameObject("Snow footprint") { hideFlags=HideFlags.HideAndDontSave };
            child.transform.SetParent(transform,false);
            var renderer=child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial=material; renderer.sprite=sprite;
            stamp=new Stamp { renderer=renderer }; stamps.Add(stamp);
        }
        else { stamp=stamps[cursor]; cursor=(cursor+1)%Capacity; }
        stamp.time=Time.time;
        stamp.renderer.enabled=true;
        stamp.renderer.sortingLayerID=groundRenderer.sortingLayerID;
        stamp.renderer.sortingOrder=order+1;
        stamp.renderer.transform.SetPositionAndRotation(new Vector3(position.x,position.y,0),Quaternion.Euler(0,0,Mathf.Atan2(-direction.x,direction.y)*Mathf.Rad2Deg));
        stamp.renderer.transform.localScale=new Vector3(0.19f,0.29f,1);
        Matrix4x4 cellToWorld=ground.transform.localToWorldMatrix *
            Matrix4x4.Translate(ground.GetCellCenterLocal(cell)) * ground.orientationMatrix * ground.GetTransformMatrix(cell);
        Vector2 size=new Vector2(tile.texture.width,tile.texture.height);
        Matrix4x4 uv=Matrix4x4.Scale(new Vector3(tile.pixelsPerUnit/size.x,tile.pixelsPerUnit/size.y,1)) *
            Matrix4x4.Translate(new Vector3((tile.rect.x+tile.pivot.x)/tile.pixelsPerUnit,(tile.rect.y+tile.pivot.y)/tile.pixelsPerUnit,0)) * cellToWorld.inverse;
        stamp.renderer.GetPropertyBlock(block);
        block.SetTexture("_GroundTex",tile.texture);
        block.SetMatrix("_GroundUV",uv);
        block.SetVector("_GroundRect",new Vector4(tile.rect.x/size.x,tile.rect.y/size.y,tile.rect.xMax/size.x,tile.rect.yMax/size.y));
        stamp.renderer.SetPropertyBlock(block);
        UpdateStamp(stamp);
        return true;
    }

    public void UpdateStamps() { foreach(var stamp in stamps) UpdateStamp(stamp); }
    public void ClearTracks()
    {
        foreach(var stamp in stamps) { stamp.time=Time.time-Mathf.Max(1,winter.footprintLifetime)-1; stamp.renderer.enabled=false; }
        tracking=false; travelled=0;
    }
    private void UpdateStamp(Stamp stamp)
    {
        float age=Time.time-stamp.time;
        float life=Mathf.Max(1,winter.footprintLifetime);
        if(winter.EffectiveSnowCoverage<=0.01f) stamp.time=Time.time-life-1;
        stamp.renderer.enabled=winter.leaveFootprints && winter.EffectiveSnowCoverage>0.01f && age<life;
        if(!stamp.renderer.enabled) return;
        stamp.renderer.color=new Color(1,1,1,0.82f*(1-Mathf.SmoothStep(0,1,age/life)));
        stamp.renderer.GetPropertyBlock(block);
        block.SetFloat("_Coverage",winter.EffectiveSnowCoverage);
        block.SetFloat("_SnowVariation",winter.snowDepthVariation);
        block.SetFloat("_DriftScale",Mathf.Max(0.2f,winter.snowDriftSize));
        stamp.renderer.SetPropertyBlock(block);
    }
    private void OnDisable() { foreach(var s in stamps) if(s.renderer!=null) s.renderer.enabled=false; tracking=false; }
    private void OnDestroy()
    {
        foreach(var s in stamps) if(s.renderer!=null) Destroy(s.renderer.gameObject);
        if(sprite!=null) Destroy(sprite);
        if(texture!=null) Destroy(texture);
    }
}
