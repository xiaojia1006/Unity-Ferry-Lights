using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class RPG1ActivitiesSetup
{
    [MenuItem("Tools/RPG/RPG1 Activities/Install And Open RPG1")]
    public static void Install()
    {
        RPG1FrontierBuilder.Build();
    }
    private static Sprite DrawMap(Tilemap ground,Rect bounds)
    {
        const int w=424,h=272;var tex=new Texture2D(w,h);var maps=UnityEngine.Object.FindObjectsOfType<Tilemap>().Where(t=>t!=ground&&t.name!="Rocks"&&t.name!="Foam").ToArray();
        var rivers=UnityEngine.Object.FindObjectsOfType<WaterSurface2D>().Select(r=>r.GetComponent<Collider2D>()).ToArray();
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            var p=new Vector2(bounds.xMin+(x+.5f)/w*bounds.width,bounds.yMin+(y+.5f)/h*bounds.height);
            Color c=new Color(.29f,.51f,.57f);var tile=ground.GetTile(ground.WorldToCell(p));
            if(tile!=null)c=new Color(.71f,.76f,.49f);
            foreach(var t in maps)if(t.HasTile(t.WorldToCell(p)))c=t.name.Contains("TreeForeground")?new Color(.26f,.44f,.31f):t.name.Contains("pengzhuang")?new Color(.79f,.72f,.47f):new Color(.59f,.66f,.41f);
            if(rivers.Any(r=>r.OverlapPoint(p)))c=new Color(.29f,.51f,.57f);
            float grain=Mathf.PerlinNoise(x*.3f,y*.3f)*.035f;c+=new Color(grain,grain,grain,0);tex.SetPixel(x,y,c);
        }
        tex.Apply();const string path="Assets/SceneExpansion/RPG1FieldMap.png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    [MenuItem("Tools/RPG/RPG1 Activities/Inspect RPG1")]
    public static void Inspect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var active=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/RPG1.unity");bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/RPG1.unity",OpenSceneMode.Additive);
        var roots=scene.GetRootGameObjects();var lines=new List<string>();
        foreach(var root in roots)lines.Add("ROOT "+root.name+" "+root.transform.position);
        foreach(var map in roots.SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)))lines.Add("MAP "+map.name+" position="+map.transform.position+" cells="+map.cellBounds+" local="+map.localBounds+" order="+map.GetComponent<TilemapRenderer>().sortingOrder);
        foreach(var sr in roots.SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Where(s=>s.gameObject.activeInHierarchy))lines.Add("SPRITE "+sr.name+" pos="+sr.transform.position+" bounds="+sr.bounds+" art="+AssetDatabase.GetAssetPath(sr.sprite));
        foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<Collider2D>(true)).Where(c=>c.gameObject.activeInHierarchy))lines.Add("COLLIDER "+c.name+" "+c.GetType().Name+" trigger="+c.isTrigger+" enabled="+c.enabled+" bounds="+c.bounds+" layer="+c.gameObject.layer);
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/RPG1Inspection.txt",lines);
        if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);Debug.Log("RPG1 inspected without changing its scene.");
    }
}
