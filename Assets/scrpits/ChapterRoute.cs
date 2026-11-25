using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.TextCore.LowLevel;

// World-space route signs. No changes to the user's canvases, fonts or panel layout.
public sealed class ChapterRoute : MonoBehaviour
{
    string destination;Vector2 arrival;bool loading;TMP_Text label;PlayerMovement player;SpriteRenderer plate;

    static TMP_FontAsset pixelFont;

    // 用 RASTER 方式从字体生成图集：不做距离场、不加抗锯齿，配上 Point 过滤就是硬边点阵
    static TMP_FontAsset PixelFont()
    {
        if (pixelFont != null) return pixelFont;
        var font = Resources.Load<Font>("Settings/PixelChinese");
        if (font == null) return null;
        pixelFont = TMP_FontAsset.CreateFontAsset(font, 32, 2, GlyphRenderMode.RASTER, 1024, 1024,
                                                  AtlasPopulationMode.Dynamic, true);
        if (pixelFont != null)
        {
            foreach (var atlas in pixelFont.atlasTextures) atlas.filterMode = FilterMode.Point;
            var bitmapShader = Shader.Find("TextMeshPro/Bitmap");
            if (bitmapShader != null)
            {
                pixelFont.material = new Material(bitmapShader);
                pixelFont.material.SetTexture(ShaderUtilities.ID_MainTex, pixelFont.atlasTextures[0]);
            }
        }
        return pixelFont;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.name!="RPG2"&&scene.name!="Willowreach")return;
        var go=new GameObject("Chapter route sign");SceneManager.MoveGameObjectToScene(go,scene);var route=go.AddComponent<ChapterRoute>();
        route.destination=scene.name=="RPG2"?"Willowreach":"RPG2";
        route.arrival=scene.name=="RPG2"?new Vector2(-18,-14):new Vector2(-2,-8);
        go.transform.position=scene.name=="RPG2"?new Vector3(4,8,0):new Vector3(-18,-13,0);
        // 底板：白字直接压草地读不出来，参考路牌做成纸片
        var plateGo=new GameObject("Route plate",typeof(SpriteRenderer));plateGo.transform.SetParent(go.transform,false);
        route.plate=plateGo.GetComponent<SpriteRenderer>();
        var plateSprite=Resources.Load<Sprite>("UI/route_plate");
        if(plateSprite==null){var tex=Resources.Load<Texture2D>("UI/route_plate");if(tex!=null)plateSprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),64f);}
        route.plate.sprite=plateSprite;route.plate.sortingOrder=30999;

        var text=new GameObject("Route label",typeof(TextMeshPro));text.transform.SetParent(go.transform,false);route.label=text.GetComponent<TextMeshPro>();route.label.font=PixelFont();
        if(route.label.font==null)route.label.font=Resources.Load<TMP_FontAsset>("Settings/Chinese");
        route.label.fontSize=2.6f;route.label.color=new Color32(58,46,34,255);route.label.enableWordWrapping=false;
        route.label.extraPadding=false;route.label.fontStyle=FontStyles.Normal;route.label.alignment=TextAlignmentOptions.Center;route.label.rectTransform.sizeDelta=new Vector2(5,1.5f);route.label.rectTransform.localPosition=new Vector3(0,1.05f,0);route.label.GetComponent<MeshRenderer>().sortingOrder=31000;
    }
    void Update()
    {
        if(player==null)player=FindObjectOfType<PlayerMovement>();if(player==null)return;
        bool near=Vector2.Distance(player.transform.position,transform.position)<2.5f;
        var world=FindObjectOfType<RPG2Adventure>();bool unlocked=world==null||world.BridgeRepaired;
        label.text=destination=="Willowreach"?(unlocked?"柳湾入口"+(near?"\nG 前往柳湾":""):"柳湾方向 · 需先修桥"):("返回苇岸"+(near?"\nG 返回":""));
        FitPlate();
        if(!near||!unlocked||loading||Time.timeScale<=0||GlobalSettings.BlocksInput||!Input.GetKeyDown(KeyCode.G))return;
        StartCoroutine(Travel());
    }
    // 底板跟着文字尺寸走，避免出现"牌子比字小"或大片留白
    void FitPlate()
    {
        if(plate==null||plate.sprite==null||label==null)return;
        Vector2 size=label.GetPreferredValues(label.text);
        float w=Mathf.Max(1.6f,size.x+1.3f);
        float h=Mathf.Max(0.9f,size.y+0.9f);
        Vector2 unit=plate.sprite.bounds.size;
        if(unit.x<=0.001f||unit.y<=0.001f)return;
        plate.transform.localScale=new Vector3(w/unit.x,h/unit.y,1f);
        plate.transform.localPosition=new Vector3(0,1.05f,0);
    }

    IEnumerator Travel()
    {
        loading=true;SaveSystem.SaveNow();FishingRig2D.End();SceneManager.sceneLoaded+=Arrived;SceneManager.LoadScene(destination);yield break;
    }
    void Arrived(Scene scene,LoadSceneMode mode)
    {
        SceneManager.sceneLoaded-=Arrived; // The save system handles known destinations; defaults cover first visits.
        var p=FindObjectOfType<PlayerMovement>();if(p!=null){p.transform.position=arrival;p.rb.position=arrival;p.rb.velocity=Vector2.zero;}
    }
}
