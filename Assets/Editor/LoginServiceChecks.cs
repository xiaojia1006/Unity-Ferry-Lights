using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs through the actual serialized button events and the loopback HTTP service.
public static class LoginServiceChecks
{
    private static int step;
    private static double deadline, next;
    private static string account;
    private static Sprite firstFrame;
    private static LoginScreen screen;
    private const string Report="Logs/LoginServiceChecks.txt";

    [MenuItem("Tools/RPG/Login/Check Register Login And RPG1 %#F10")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying) return;
        screen=UnityEngine.Object.FindObjectOfType<LoginScreen>();
        if(screen==null || !screen.account.interactable) return;
        EditorApplication.update-=Tick;
        account="qa_"+Guid.NewGuid().ToString("N").Substring(0,16);
        File.WriteAllText(Report,"Started real Unity/service integration checks\n");
        var tree=screen.GetComponentsInChildren<LoginTreeAnimation>().First();
        firstFrame=tree.GetComponent<UnityEngine.UI.Image>().sprite;
        step=0; deadline=EditorApplication.timeSinceStartup+45; next=EditorApplication.timeSinceStartup+.25;
        EditorApplication.update+=Tick;
    }

    private static void Pass(string value) { File.AppendAllText(Report,"PASS "+value+"\n"); }
    private static void Tick()
    {
        try
        {
            double now=EditorApplication.timeSinceStartup;
            if(!EditorApplication.isPlaying) throw new Exception("Play mode ended before checks completed");
            if(now>deadline) throw new Exception("Timed out at stage "+step);
            if(now<next) return;
            if(step==0)
            {
                var tree=screen.GetComponentsInChildren<LoginTreeAnimation>().First();
                if(tree.GetComponent<UnityEngine.UI.Image>().sprite==firstFrame) return;
                Pass("background tree sprite advances in Play mode");
                ScreenCapture.CaptureScreenshot("Logs/LoginAnimatedPreview.png");
                screen.Registration();
                screen.account.text=account; screen.password.text="UiCheck-12345";
                screen.login.onClick.Invoke(); step=1; next=now+.5; return;
            }
            if(step==1)
            {
                if(!screen.account.interactable) return;
                if(!screen.message.text.Contains("注册成功")) throw new Exception("Registration failed: "+screen.message.text);
                Pass("register button persists a new account through real HTTP service");
                screen.password.text="Deliberately-Wrong-123"; screen.login.onClick.Invoke(); step=2; next=now+.5; return;
            }
            if(step==2)
            {
                if(!screen.account.interactable) return;
                if(!screen.message.text.Contains("账号或密码错误") || SceneManager.GetActiveScene().name!="Login") throw new Exception("Wrong password must stay on Login");
                Pass("wrong password rejected and Login scene retained");
                screen.password.text="UiCheck-12345"; screen.login.onClick.Invoke(); step=3; next=now+.5; return;
            }
            if(step==3)
            {
                if(SceneManager.GetActiveScene().name!="RPG1") return;
                if(LoginSession.Account!=account || string.IsNullOrEmpty(LoginSession.Token)) throw new Exception("Authenticated session missing");
                Pass("correct password loads RPG1 and retains authenticated in-memory session");
                step=4; next=now+1; return;
            }
            if(step==4)
            {
                if(UnityEngine.Object.FindObjectOfType<PlayerMovement>()==null) throw new Exception("RPG1 player missing");
                Pass("RPG1 gameplay player initialized");
                ScreenCapture.CaptureScreenshot("Logs/LoginToRPG1.png");
                File.AppendAllText(Report,"COMPLETE\n");
                Debug.Log("Login service integration checks passed.");
                EditorApplication.update-=Tick;
            }
        }
        catch(Exception error)
        {
            EditorApplication.update-=Tick;
            File.AppendAllText(Report,"FAIL "+error.Message+"\n"); Debug.LogException(error);
        }
    }
}
