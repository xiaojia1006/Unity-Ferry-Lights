using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Explicit local requests only; idle editor never rebuilds a scene automatically.
[InitializeOnLoad]
public static class RPG1FrontierAutomation
{
    const string Request="Temp/RPG1Frontier.request";
    static RPG1FrontierAutomation(){EditorApplication.update+=Tick;Application.logMessageReceived+=CaptureError;}
    static void CaptureError(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)File.AppendAllText("Logs/RPG1FrontierErrors.txt",DateTime.Now+" "+message+"\n"+stack+"\n");}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        string command;
        try{command=File.ReadAllText(Request).Trim();if(command.Length==0)return;File.Delete(Request);}catch(IOException){return;}
        try{
            if(command=="build")RPG1FrontierBuilder.Build();
            else if(command=="scene-check")RPG1FrontierChecks.VerifyScene();
            else if(command=="play")EditorApplication.isPlaying=true;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="play-check")RPG1FrontierChecks.VerifyPlay();
            else if(command=="polish")RPG1FrontierPolish.Apply();
            else if(command=="integration")RPG1FrontierIntegration.Run();
            else if(command=="backpack-check")BackpackChecks.Run();
            else if(command=="shop-check")ShopPurchaseChecks.Start();
            else if(command=="view-east")View(new Vector3(14,-4,0));
            else if(command=="view-fort")View(new Vector3(0,14,0));
            else if(command=="view-camp")View(new Vector3(-23,-14,0));
            else if(command=="view-overview"){if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,0),Quaternion.identity,30,true);}
            else if(command=="save"){if(!EditorApplication.isPlaying)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());}
            else throw new InvalidOperationException("Unknown scoped request");
            File.WriteAllText("Logs/RPG1FrontierRequest.txt",command+" completed "+DateTime.Now);
        }catch(Exception e){File.WriteAllText("Logs/RPG1FrontierRequest.txt",command+" FAILED "+e);Debug.LogException(e);}
    }
    static void View(Vector3 at){if(EditorApplication.isPlaying){var p=UnityEngine.Object.FindObjectOfType<PlayerMovement>();p.transform.position=at;p.rb.position=at;p.rb.velocity=Vector2.zero;}else if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(at,Quaternion.identity,10,true);}
}
