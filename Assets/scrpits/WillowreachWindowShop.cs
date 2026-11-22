using System.Collections.Generic;
using UnityEngine;

// Separate window framing from the full-body shop portrait, without changing shared character art.
[ExecuteAlways]
public sealed class WillowreachWindowShop : MonoBehaviour
{
    public ShopKeeper merchant;
    public SpriteRenderer source, display, portrait, house;
    public Rect crop;
    public Transform counterFeet;
    private readonly Dictionary<Sprite,Sprite> cache=new Dictionary<Sprite,Sprite>();
    private void LateUpdate()
    {
        if(source==null||display==null)return;
        source.enabled=false;
        if(Application.isPlaying&&source.sprite!=null)
        {
            Sprite frame;
            if(!cache.TryGetValue(source.sprite,out frame))
            {
                var r=source.sprite.rect;
                var rect=new Rect(r.x+crop.x,r.y+crop.y,crop.width,crop.height);
                frame=Sprite.Create(source.sprite.texture,rect,new Vector2(.5f,0),source.sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
                frame.hideFlags=HideFlags.HideAndDontSave;cache[source.sprite]=frame;
            }
            display.sprite=frame;portrait.sprite=source.sprite;
        }
        int order=house.sortingOrder;display.sortingLayerID=house.sortingLayerID;display.sortingOrder=order+1;
        var sill=counterFeet.GetComponent<SpriteRenderer>();sill.sortingLayerID=house.sortingLayerID;sill.sortingOrder=order+2;
    }
    private void OnDisable(){foreach(var s in cache.Values)if(s!=null){if(Application.isPlaying)Destroy(s);else DestroyImmediate(s);}cache.Clear();}
}
