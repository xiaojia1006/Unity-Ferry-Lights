using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class WillowreachJournalSetup
{
    [MenuItem("Tools/RPG/Scene Expansion/Style Journal And Map")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(world==null)return;
        var backup=File.ReadAllText("Backups/LatestFieldJournal.txt").Trim();
        if(!File.Exists(backup+"/Willowreach-live.unity"))EditorSceneManager.SaveScene(world.gameObject.scene,backup+"/Willowreach-live.unity",true);
        var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes("Assets/Sprites/UI/Banners/Banner_Horizontal.png"));
        // Crop transparent padding; retain the authored ink border and parchment texture.
        var colors=tex.GetPixels32();int minX=tex.width,minY=tex.height,maxX=0,maxY=0;
        for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(colors[y*tex.width+x].a>32){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
        var cut=new Texture2D(maxX-minX+1,maxY-minY+1);cut.SetPixels(tex.GetPixels(minX,minY,cut.width,cut.height));
        // Replace the protruding scroll curl with the original top ink edge, mirrored.
        for(int y=0;y<30;y++)for(int x=0;x<cut.width;x++)cut.SetPixel(x,y,cut.GetPixel(x,cut.height-1-y));
        cut.Apply();
        const string panelPath="Assets/SceneExpansion/JournalParchment.png";File.WriteAllBytes(panelPath,cut.EncodeToPNG());UnityEngine.Object.DestroyImmediate(cut);UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(panelPath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(panelPath);importer.textureType=TextureImporterType.Sprite;importer.spriteBorder=new Vector4(20,22,20,22);importer.spritePixelsPerUnit=100;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        world.journalSprite=AssetDatabase.LoadAssetAtPath<Sprite>(panelPath);world.ribbonSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Ribbons/Ribbon_Yellow_3Slides.png");
        world.mapSprite=DrawChart(world);EditorUtility.SetDirty(world);EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
        Debug.Log("Journal uses existing parchment and ribbon; atlas updated from actual paths and river.");
    }
    private static Sprite DrawChart(WillowreachWorld world)
    {
        const int width=608,height=416;var chart=new Texture2D(width,height,TextureFormat.RGBA32,false);
        var water=world.river.GetComponent<PolygonCollider2D>();var paths=world.GetComponentsInChildren<Tilemap>().Where(t=>t.name=="Paths - painted loop"||t.name=="Path edges").ToArray();
        var wet=new bool[width*height];var points=new Vector2[width*height];
        Physics2D.SyncTransforms();
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){int i=y*width+x;points[i]=new Vector2(((x+.5f)/width-.5f)*76,((y+.5f)/height-.5f)*52);wet[i]=water.OverlapPoint(points[i]);}
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;Vector2 p=points[i];float grain=Mathf.PerlinNoise(x*.35f,y*.35f)*.045f;
            Color c=p.x>10&&p.y< -7?new Color(.65f,.72f,.47f):p.y>10&&p.x<0?new Color(.76f,.80f,.66f):new Color(.76f,.79f,.57f);
            c+=new Color(grain,grain,grain,0);
            if(paths.Any(t=>t.HasTile(t.WorldToCell(p))))c=new Color(.91f+grain,.80f+grain,.54f+grain);
            if(wet[i])
            {
                bool shore=x<2||y<2||x>=width-2||y>=height-2||!wet[i-2]||!wet[i+2]||!wet[i-width*2]||!wet[i+width*2];
                c=shore?new Color(.41f,.65f,.65f):new Color(.31f+grain,.55f+grain,.59f+grain);
                if(!shore&&y%12==0&&(x/8+y)%5<2)c=new Color(.49f,.69f,.69f);
            }
            foreach(var bridge in world.bridges)if(bridge.Contains(p))c=y%4==0?new Color(.43f,.34f,.24f):new Color(.70f,.54f,.34f);
            chart.SetPixel(x,y,c);
        }
        foreach(var tree in world.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Pine grove"||s.name=="Boundary pine"))
        {
            int cx=Mathf.RoundToInt((tree.transform.position.x/76+.5f)*width),cy=Mathf.RoundToInt((tree.transform.position.y/52+.5f)*height);
            for(int y=0;y<7;y++)for(int x=-3;x<=3;x++)if(Mathf.Abs(x)<=((6-y)/2)&&cx+x>=0&&cx+x<width&&cy+y>=0&&cy+y<height)chart.SetPixel(cx+x,cy+y,new Color(.33f,.48f,.35f));
        }
        foreach(var house in world.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name.StartsWith("House_")||s.name.Contains("watchtower")))
        {
            int cx=Mathf.RoundToInt((house.transform.position.x/76+.5f)*width),cy=Mathf.RoundToInt((house.transform.position.y/52+.5f)*height);
            for(int y=-3;y<=4;y++)for(int x=-4;x<=4;x++)if(cx+x>=0&&cx+x<width&&cy+y>=0&&cy+y<height)chart.SetPixel(cx+x,cy+y,y>0?new Color(.39f,.45f,.44f):new Color(.86f,.73f,.48f));
        }
        chart.Apply();const string path="Assets/SceneExpansion/JournalFieldMap.png";File.WriteAllBytes(path,chart.EncodeToPNG());UnityEngine.Object.DestroyImmediate(chart);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
