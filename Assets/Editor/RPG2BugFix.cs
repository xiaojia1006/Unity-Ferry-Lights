using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static partial class RPG2Workshop
{
    [MenuItem("Tools/RPG/RPG2 Workshop/Fix Reported UI And Physics")]
    public static void FixReportedBugs()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before applying scene fixes");
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();if(w==null||w.gameObject.scene.path!=ScenePath)throw new Exception("Open RPG2 first");
        string backup=File.ReadAllText("Backups/LatestRPG2Fix.txt").Trim();
        if(!File.Exists(backup+"/RPG2-live.unity"))EditorSceneManager.SaveScene(w.gameObject.scene,backup+"/RPG2-live.unity",true);
        const string scrollPath="Assets/RPG2Expansion/QuestScroll.png";
        AssetDatabase.ImportAsset(scrollPath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(scrollPath);importer.textureType=TextureImporterType.Sprite;importer.spriteBorder=new Vector4(14,20,14,20);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        w.questScroll=Art(scrollPath);
        w.questPaper=Art("Assets/Sprites/UI/Banners/Carved_9Slides.png");
        w.questRibbon=Art("Assets/Sprites/UI/Ribbons/Ribbon_Red_3Slides.png");
        w.rewardSlot=Art("Assets/Sprites/UI/Buttons/Button_Blue_9Slides.png");
        w.rewardDisplayIcons=new[]{w.gold,w.experience,w.meat,w.pumpkin,w.mushroom}.Select(CropRewardIcon).ToArray();
        foreach(var point in w.points)
        {
            point.title=point.title.Replace("Q 查看","R 查看");point.description=point.description.Replace("Q 委托","R 委托").Replace("Q 可","R 可");
            if(point.name.Contains("Q 查看"))point.name=point.name.Replace("Q 查看","R 查看");
        }
        foreach(var sr in w.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>()).Where(s=>s.name=="House_Yellow"||s.name=="House_Purple"))
        {
            var box=sr.GetComponent<BoxCollider2D>();if(box==null)box=sr.gameObject.AddComponent<BoxCollider2D>();
            sr.gameObject.layer=7;box.size=new Vector2(1.45f,.6f);box.offset=new Vector2(sr.sprite.bounds.center.x,sr.sprite.bounds.min.y+.35f);box.isTrigger=false;
        }
        // 2D movement collides with foot/trunk footprints, never whole canopy rectangles.
        foreach(var sr in w.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if(sr.sprite==null||sr.GetComponentInParent<RPG2Guard>()!=null)continue;
            bool tree=sr.name=="Animated riverside pine";
            bool rock=sr.name=="Forest floor rock"||sr.name=="Mine ore stock"||sr.name=="Meadow grasses and flowers"&&IsRock(sr.sprite);
            if(!tree&&!rock)continue;
            var collider=sr.GetComponent<BoxCollider2D>();if(collider==null)collider=sr.gameObject.AddComponent<BoxCollider2D>();
            sr.gameObject.layer=7;collider.enabled=true;collider.isTrigger=false;
            if(tree){collider.size=new Vector2(.72f,.42f);collider.offset=new Vector2(0,.08f);}
            else
            {
                var b=sr.sprite.bounds;
                collider.size=new Vector2(Mathf.Max(.22f,b.size.x*.78f),Mathf.Max(.16f,b.size.y*.56f));
                collider.offset=new Vector2(b.center.x,b.min.y+collider.size.y*.5f);
            }
            var depth=sr.GetComponent<ObliqueDepth2D>();if(depth==null)depth=sr.gameObject.AddComponent<ObliqueDepth2D>();depth.Refresh();
        }
        foreach(var guard in w.guards)
        {
            guard.transform.localScale=Vector3.one*1.65f;
            guard.transform.position=new Vector3(guard.home.x,guard.home.y,0);
            var body=guard.GetComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var foot=guard.GetComponent<CapsuleCollider2D>();
            if(foot!=null){foot.size=new Vector2(.36f,.23f);foot.offset=new Vector2(0,.65f);foot.direction=CapsuleDirection2D.Horizontal;}
        }
        // Remove decorative obstacles inside patrol corridors so larger guards remain mobile.
        foreach(var sr in w.GetComponentsInChildren<SpriteRenderer>())
        {
            if(sr.name!="Animated riverside pine"&&sr.name!="Forest floor rock"&&sr.name!="Meadow grasses and flowers")continue;
            if(w.guards.Any(g=>SegmentDistance(sr.transform.position,g.home,g.end)<1.35f))sr.gameObject.SetActive(false);
        }
        var boundary=w.transform.Find("Solid world boundaries");
        if(boundary==null){boundary=new GameObject("Solid world boundaries").transform;boundary.SetParent(w.transform,false);}
        Boundary(boundary,"North forest limit",new Vector2(-1.5f,20.5f),new Vector2(67,1));
        Boundary(boundary,"South forest limit",new Vector2(-1.5f,-19.6f),new Vector2(67,1));
        Boundary(boundary,"West forest limit",new Vector2(-34.4f,.5f),new Vector2(1,40));
        Boundary(boundary,"East limit below exit",new Vector2(30.5f,-5.8f),new Vector2(1,29));
        Boundary(boundary,"East limit above exit",new Vector2(30.5f,16.9f),new Vector2(1,8));
        var confiner=w.gameObject.scene.GetRootGameObjects().First(g=>g.name=="Confiner").GetComponent<PolygonCollider2D>();
        confiner.offset=Vector2.zero;confiner.pathCount=1;
        confiner.SetPath(0,new[]{new Vector2(-34,-19),new Vector2(31,-19),new Vector2(31,21),new Vector2(-34,21)}.Select(p=>(Vector2)confiner.transform.InverseTransformPoint(p)).ToArray());
        // Floor all scene-local interactive/static art in XY; camera and UI have their own depth.
        foreach(var point in w.points){var at=point.transform.position;point.transform.position=new Vector3(at.x,at.y,0);}
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
        BakeAtlas();EditorSceneManager.SaveScene(w.gameObject.scene);CheckScene();
        File.WriteAllText("Logs/RPG2BugFixApplied.txt","R shortcut; original scroll/red ribbon/blue reward UI; visible interaction prompts; enlarged guards; rock/trunk colliders; closed world boundaries with eastern exit.\n");
    }
    static Sprite CropRewardIcon(ItemSO item)
    {
        var source=item.icon;var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(source.texture)));
        var rect=source.rect;var pixels=texture.GetPixels((int)rect.x,(int)rect.y,(int)rect.width,(int)rect.height);
        int width=(int)rect.width,height=(int)rect.height,minX=width,minY=height,maxX=0,maxY=0;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;var c=pixels[i];
            if(item.isEXP&&c.r>.93f&&c.g>.93f&&c.b>.93f){c.a=0;pixels[i]=c;}
            if(c.a>.1f){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        }
        texture.Reinitialize(width,height);texture.SetPixels(pixels);texture.Apply();
        var output=new Texture2D(maxX-minX+1,maxY-minY+1);output.SetPixels(texture.GetPixels(minX,minY,output.width,output.height));output.Apply();
        string path="Assets/RPG2Expansion/Reward-"+(item.isGold?"Gold":item.isEXP?"Experience":item.name)+".png";
        File.WriteAllBytes(path,output.EncodeToPNG());UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return Art(path);
    }
    static bool IsRock(Sprite sprite)
    {
        string path=AssetDatabase.GetAssetPath(sprite);
        return path.EndsWith("/04.png")||path.EndsWith("/05.png")||path.EndsWith("/06.png")||path.Contains("GroundRock")||sprite.name=="04"||sprite.name=="05"||sprite.name=="06";
    }
    static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){var ab=b-a;return Vector2.Distance(p,a+ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude));}
    static void Boundary(Transform root,string name,Vector2 center,Vector2 size)
    {
        var child=root.Find(name);if(child==null){child=new GameObject(name).transform;child.SetParent(root,false);}
        child.position=center;child.gameObject.layer=7;var box=child.GetComponent<BoxCollider2D>();if(box==null)box=child.gameObject.AddComponent<BoxCollider2D>();box.size=size;box.isTrigger=false;
    }
    [MenuItem("Tools/RPG/RPG2 Workshop/Check Reported Bugs In Play Mode")]
    public static void CheckReportedBugs()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play first");
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();var report=new System.Collections.Generic.List<string>();
        Action<bool,string> check=(ok,message)=>{report.Add((ok?"PASS ":"FAIL ")+message);};
        try
        {
            w.CloseMenu();w.Notify("苇岸渡口：R 打开委托，E 与附近物体互动。");Canvas.ForceUpdateCanvases();
            var notice=w.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t=>t.name=="Notification");
            check(notice.transform.parent.GetComponent<UnityEngine.UI.Image>()!=null,"Notice background is behind child text");
            notice.ForceMeshUpdate(true);check(notice.textInfo.characterCount>10&&notice.textInfo.meshInfo.Any(m=>m.vertexCount>0),"Welcome notification generates visible text mesh");
            foreach(var p in w.points)
            {
                check(!string.IsNullOrWhiteSpace(w.Hint(p)),p.title+" has an interaction hint");
            }
            foreach(var sr in w.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Animated riverside pine"||s.name=="Forest floor rock"||s.name=="Mine ore stock"||s.name=="Meadow grasses and flowers"&&IsRock(s.sprite)))
                check(sr.GetComponents<Collider2D>().Any(c=>c.enabled&&!c.isTrigger),sr.name+" has a solid 2D foot collider");
            foreach(var g in w.guards)check(Mathf.Abs(g.transform.localScale.x)>=1.6f,g.name+" uses corrected character scale");
            check(w.transform.Find("Solid world boundaries").GetComponentsInChildren<BoxCollider2D>().Length==5,"Continuous north/south/west/east world boundaries retain exit gap");
            w.OpenMenu(false);Canvas.ForceUpdateCanvases();
            var icons=w.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(i=>i.name.StartsWith("Reward icon ")).ToArray();
            check(icons.Count(i=>i.gameObject.activeSelf&&i.sprite!=null)==w.QuestRewards(0).Count,"Each displayed reward uses its actual ItemSO icon");
            check(w.QuestRewards(0)[w.meat]==1&&w.QuestRewards(1)[w.pumpkin]==2&&w.QuestRewards(2)[w.mushroom]==2&&w.QuestRewards(3)[w.meat]==2,"Four quests expose different configured material rewards");
            w.CloseMenu();
            var foot=w.Player.GetComponents<Collider2D>().First(c=>!c.isTrigger);
            Physics2D.SyncTransforms();
            var sample=w.GetComponentsInChildren<SpriteRenderer>().First(s=>s.name=="Forest floor rock");
            var solid=sample.GetComponent<BoxCollider2D>();
            check(Physics2D.OverlapPointAll(solid.bounds.center,1<<7).Contains(solid),"Rock footprint exists in live Physics2D broadphase");
            check(!Physics2D.GetIgnoreLayerCollision(w.Player.gameObject.layer,7),"Player layer collides with static obstacle layer");
            var north=w.transform.Find("Solid world boundaries/North forest limit").GetComponent<BoxCollider2D>();
            check(Physics2D.LinecastAll(north.bounds.center-Vector3.up*2,north.bounds.center+Vector3.up*2,1<<7).Any(h=>h.collider==north),"North forest boundary blocks a ray crossing world edge");
            // Exercise the real player Rigidbody2D against a live obstacle, then restore it.
            var mode=Physics2D.simulationMode;var oldPosition=w.Player.rb.position;var oldVelocity=w.Player.rb.velocity;bool wasEnabled=w.Player.enabled;
            try
            {
                Physics2D.simulationMode=SimulationMode2D.Script;w.Player.enabled=false;
                var center=solid.bounds.center;var offset=foot.bounds.center-w.Player.transform.position;
                w.Player.rb.position=new Vector2(center.x-1.3f,center.y)-new Vector2(offset.x,offset.y);Physics2D.SyncTransforms();
                for(int step=0;step<30;step++){w.Player.rb.velocity=Vector2.right*4;Physics2D.Simulate(.02f);}
                check(foot.bounds.max.x<=solid.bounds.min.x+.08f,"Actual player Rigidbody2D is physically stopped by a rock");
                var tree=w.GetComponentsInChildren<SpriteRenderer>().First(s=>s.name=="Animated riverside pine"&&s.transform.position.y>17).GetComponent<BoxCollider2D>();
                var treeCenter=tree.bounds.center;
                w.Player.rb.position=new Vector2(treeCenter.x-1.5f,treeCenter.y)-new Vector2(offset.x,offset.y);Physics2D.SyncTransforms();
                for(int step=0;step<35;step++){w.Player.rb.velocity=Vector2.right*4;Physics2D.Simulate(.02f);}
                check(foot.bounds.max.x<=tree.bounds.min.x+.08f,"Actual player Rigidbody2D is stopped by a boundary tree trunk");
                w.Player.rb.position=new Vector2(0,18.5f)-new Vector2(offset.x,offset.y);Physics2D.SyncTransforms();
                for(int step=0;step<45;step++){w.Player.rb.velocity=Vector2.up*4;Physics2D.Simulate(.02f);}
                check(foot.bounds.max.y<=north.bounds.min.y+.08f,"Actual player cannot escape through gaps at north world edge");
            }
            finally{w.Player.rb.position=oldPosition;w.Player.transform.position=new Vector3(oldPosition.x,oldPosition.y,0);w.Player.rb.velocity=oldVelocity;w.Player.enabled=wasEnabled;Physics2D.simulationMode=mode;Physics2D.SyncTransforms();}
        }
        finally
        {
            w.CloseMenu();w.Notify("欢迎来到苇岸渡口！R 打开委托，E 互动，Tab 查看地图。");
            Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("Logs/RPG2WelcomeFixed.png");
            File.WriteAllLines("Logs/RPG2ReportedBugChecks.txt",report);
        }
    }
}
