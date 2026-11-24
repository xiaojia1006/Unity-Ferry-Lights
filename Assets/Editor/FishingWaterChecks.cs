using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
[InitializeOnLoad]
public static class FishingWaterChecks
{
    static double ready;static bool waiting;
    static FishingWaterChecks(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Temp/FishingWater.request"))
        {
            try{File.Delete("Temp/FishingWater.request");}catch(IOException){return;}
            if(!EditorApplication.isPlaying){File.WriteAllText("Logs/FishingWaterChecks.txt","FAIL: enter Play first");return;}
            SceneManager.LoadScene("RPG2");waiting=true;ready=EditorApplication.timeSinceStartup+1;
        }
        if(!waiting||EditorApplication.timeSinceStartup<ready)return;
        var world=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();if(world==null||world.Player==null)return;
        waiting=false;
        try
        {
            int tested=0,valid=0,rejected=0;
            for(float y=-18;y<20;y+=2)for(float x=-33;x<30;x+=2)
            {
                var from=new Vector2(x,y);
                foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right})
                {
                    tested++;Vector3 cast;
                    if(world.TryFindFishingCast(from,direction,out cast))
                    {if(!world.ValidFishingWater(cast)||Vector2.Distance(from,cast)>4.81f)throw new Exception("Invalid water cast from "+from);valid++;}
                    else rejected++;
                }
            }
            if(valid==0||rejected==0)throw new Exception("Must test both water and land cases");
            var river=UnityEngine.Object.FindObjectsOfType<WaterSurface2D>();var enabled=river.Select(w=>w.enabled).ToArray();
            try{foreach(var w in river)w.enabled=false;Vector3 cast;if(world.TryFindFishingCast(world.Player.transform.position,Vector2.down,out cast))throw new Exception("Disabled water accepted");}
            finally{for(int i=0;i<river.Length;i++)river[i].enabled=enabled[i];}
            File.WriteAllText("Logs/FishingWaterChecks.txt","PASS actual RPG2 map: "+tested+" casts in four directions; "+valid+" valid water landings; "+rejected+" rejected; disabled water rejected. No land fallback.\n");
        }catch(Exception e){File.WriteAllText("Logs/FishingWaterChecks.txt","FAIL "+e);Debug.LogException(e);}
    }
}
