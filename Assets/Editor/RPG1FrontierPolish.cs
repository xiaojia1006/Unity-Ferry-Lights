using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RPG1FrontierPolish
{
    public static void Apply()
    {
        if(Application.isPlaying)return;var w=Object.FindObjectOfType<RPG1Frontier>();if(w==null)return;
        foreach(var text in w.GetComponentsInChildren<TextMesh>())
        {
            text.characterSize=.055f;text.fontSize=48;text.color=new Color(.24f,.18f,.12f);
            if(text.text.StartsWith("推箱"))text.text="推箱压板 → 启动供能";
            if(text.text.StartsWith("落木"))text.text="落木道 · 黄灯预警";
            if(text.text.StartsWith("休息"))text.text="E 休息   K 商店   Tab 地图";
            var bg=text.transform.Find("Parchment sign backing");if(bg==null){var g=new GameObject("Parchment sign backing");g.transform.SetParent(text.transform,false);bg=g.transform;g.AddComponent<SpriteRenderer>();}
            var sr=bg.GetComponent<SpriteRenderer>();sr.sprite=w.hud.parchment;sr.sortingOrder=31000;text.GetComponent<MeshRenderer>().sortingOrder=31001;float width=Mathf.Max(1.3f,text.text.Length*.22f+.5f);bg.localScale=new Vector3(width/sr.sprite.bounds.size.x,.65f/sr.sprite.bounds.size.y,1);
        }
        // Rich but readable local clusters; keep the camp centre and all approach paths clear.
        var group=w.transform.Find("Camp furnishings");if(group!=null)Object.DestroyImmediate(group.gameObject);group=null;if(group==null)
        {
            group=new GameObject("Camp furnishings").transform;group.SetParent(w.transform,false);
            for(int i=0;i<6;i++)Prop(group,"Stored timber",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SceneExpansion/Driftwood.asset"),new Vector2(-29+i*.6f,-12.3f),1);
            for(int i=0;i<4;i++)Prop(group,"Kitchen vegetable baskets",Art("Assets/Sprites/Deco/12.png"),new Vector2(-17+i*.7f,-10.7f),.8f);
            for(int i=0;i<3;i++)Prop(group,"Supply mushrooms",Art("Assets/Sprites/Deco/02.png"),new Vector2(-28.5f+i*.55f,-8.9f),.8f);
            foreach(var p in new[]{new Vector2(-28,-18),new Vector2(-26,-19),new Vector2(-16,-17),new Vector2(-15,-15)})Prop(group,"Camp grass fringe",Art("Assets/Sprites/Deco/10.png"),p,1);
        }
        // Share a single foot contact for all crate boards, so layers cannot flicker while pushed.
        foreach(var d in w.crate.GetComponentsInChildren<ObliqueDepth2D>()){d.feet=w.crate.transform;d.Refresh();}
        foreach(var enemy in w.GetComponentsInChildren<RPG1Sentinel>())
        {
            enemy.transform.localScale=Vector3.one*1.6f;
            var c=enemy.GetComponent<CapsuleCollider2D>();c.size=new Vector2(.46f,.3f)/1.6f;
            var sprite=enemy.GetComponent<SpriteRenderer>().sprite;c.offset=new Vector2(0,sprite.bounds.min.y+.10f);
        }
        var fort=w.transform.Find("Fort working yard");if(fort==null)
        {
            fort=new GameObject("Fort working yard").transform;fort.SetParent(w.transform,false);
            for(int i=0;i<4;i++){Prop(fort,"Western timber rack",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SceneExpansion/Driftwood.asset"),new Vector2(-7.5f+i*.5f,18.8f),1.2f);Prop(fort,"Eastern timber rack",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SceneExpansion/Driftwood.asset"),new Vector2(6+i*.5f,18.8f),1.2f);}
            Prop(fort,"Guard supplies",Art("Assets/Sprites/Deco/12.png"),new Vector2(-7,14),1);Prop(fort,"Old fort flowers",Art("Assets/Sprites/Deco/08.png"),new Vector2(7,20),1);
        }
        // Smaller nearby sign stays below the tower crown and never covers the door.
        EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
    }
    static Sprite Art(string p)
    {
        var s=AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().FirstOrDefault();if(s!=null)return s;
        string target="Assets/SceneExpansion/FrontierDeco"+System.IO.Path.GetFileNameWithoutExtension(p)+".asset";s=AssetDatabase.LoadAssetAtPath<Sprite>(target);if(s!=null)return s;
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(p);s=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),64);AssetDatabase.CreateAsset(s,target);return s;
    }
    static void Prop(Transform parent,string name,Sprite art,Vector2 p,float scale){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=p;g.transform.localScale=Vector3.one*scale;g.AddComponent<SpriteRenderer>().sprite=art;g.AddComponent<ObliqueDepth2D>();}
}
