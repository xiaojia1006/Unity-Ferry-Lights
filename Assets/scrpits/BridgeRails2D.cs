using UnityEngine;

// Solid XY rails follow the actual authored deck bounds, including resized/moved bridges.
public sealed class BridgeRails2D : MonoBehaviour
{
    public Rect Deck {get;private set;}
    public BoxCollider2D Left {get;private set;}
    public BoxCollider2D Right {get;private set;}
    void Awake()
    {
        var boards=GetComponentsInChildren<SpriteRenderer>(true);
        bool found=false;Bounds bounds=new Bounds();
        foreach(var sr in boards){if(sr.sprite==null||!sr.name.Contains("plank"))continue;if(!found){bounds=sr.bounds;found=true;}else bounds.Encapsulate(sr.bounds);}
        if(!found)foreach(var sr in boards){if(sr.sprite==null)continue;if(!found){bounds=sr.bounds;found=true;}else bounds.Encapsulate(sr.bounds);}
        if(!found)return;
        Deck=new Rect(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y);
        Left=Rail("Bridge left rail",bounds.min.x-.06f,bounds);Right=Rail("Bridge right rail",bounds.max.x+.06f,bounds);
    }
    BoxCollider2D Rail(string name,float x,Bounds bounds)
    {
        var go=new GameObject(name);go.layer=7;go.transform.SetParent(transform,false);go.transform.position=new Vector3(x,bounds.center.y,0);
        var box=go.AddComponent<BoxCollider2D>();box.size=new Vector2(.12f,bounds.size.y)/new Vector2(Mathf.Abs(go.transform.lossyScale.x),Mathf.Abs(go.transform.lossyScale.y));box.isTrigger=false;return box;
    }
}
