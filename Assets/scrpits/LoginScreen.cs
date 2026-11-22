using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoginScreen : MonoBehaviour
{
    public TMP_InputField account;
    public TMP_InputField password;
    public TMP_Text message;
    public TMP_Text visibilityLabel;
    public Button login;
    public Image campfire;
    public Sprite[] fireFrames;
    public string serviceUrl = "http://127.0.0.1:18765";
    public string firstGameScene = "RPG1";
    public TMP_Text heading;
    public TMP_Text submitLabel;
    public TMP_Text registerLabel;
    public TMP_Text welcomeLabel;
    private bool registering;
    private bool busy;
    private UnityWebRequest activeRequest;
    [Serializable] private class Credentials { public string account; public string password; }
    [Serializable] private class Response { public bool success; public string message; public string account; public string token; }
    private bool visible;

    private void Start()
    {
        password.contentType = TMP_InputField.ContentType.Password;
        password.asteriskChar = '*';
        account.characterLimit = 32;
        account.onValueChanged.AddListener(ClearMessage);
        password.onValueChanged.AddListener(ClearMessage);
        account.Select();
        account.ActivateInputField();
    }

    private void Update()
    {
        if (GlobalSettings.BlocksInput) return;
        if (campfire != null && fireFrames != null && fireFrames.Length > 0)
            campfire.sprite = fireFrames[(int)(Time.unscaledTime * 8) % fireFrames.Length];
        if (busy) return;
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            Selectable next;
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (selected == account.gameObject) next = reverse ? (Selectable)login : password;
            else if (selected == password.gameObject) next = reverse ? (Selectable)account : login;
            else next = reverse ? (Selectable)password : account;
            next.Select();
            var input = next as TMP_InputField;
            if (input != null) input.ActivateInputField();
        }
        // Button handles Enter itself when it has focus.
        if ((account.isFocused || password.isFocused) &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) Submit();
    }

    public void TogglePassword()
    {
        if (busy) return;
        visible = !visible;
        password.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        password.ForceLabelUpdate();
        visibilityLabel.text = visible ? "隐藏" : "显示";
    }

    public void Submit()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(account.text))
        {
            message.text = "请先填写你的账号";
            account.Select(); account.ActivateInputField();
            return;
        }
        if (string.IsNullOrEmpty(password.text))
        {
            message.text = "请输入密码";
            password.Select(); password.ActivateInputField();
            return;
        }
        StartCoroutine(Authenticate());
    }

    private IEnumerator Authenticate()
    {
        bool create = registering;
        SetBusy(true);
        message.text = create ? "正在创建账号…" : "正在验证账号…";
        try
        {
            var credentials = new Credentials { account = account.text.Trim(), password = password.text };
            using (var request = new UnityWebRequest(serviceUrl.TrimEnd('/') + (create ? "/register" : "/login"), "POST"))
            {
                activeRequest = request;
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(credentials)));
                credentials.password = null;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    message.text = "无法连接账号服务，请启动本机服务后重试";
                    yield break;
                }
                Response reply = null;
                try { reply = JsonUtility.FromJson<Response>(request.downloadHandler.text); }
                catch (ArgumentException) { }
                if (reply == null || string.IsNullOrEmpty(reply.message))
                {
                    message.text = "账号服务响应异常，请稍后重试";
                    yield break;
                }
                if (request.result != UnityWebRequest.Result.Success || !reply.success)
                {
                    message.text = reply.message;
                    yield break;
                }
                if (create)
                {
                    registering = false;
                    RefreshMode();
                    password.text = "";
                    HidePassword();
                    message.text = "注册成功，请输入密码登录";
                    yield break;
                }
                if (string.IsNullOrEmpty(reply.token) || string.IsNullOrEmpty(reply.account))
                {
                    message.text = "登录响应不完整，请重试";
                    yield break;
                }
                if (!Application.CanStreamedLevelBeLoaded(firstGameScene))
                {
                    message.text = "游戏场景尚未加入构建，请检查场景配置";
                    yield break;
                }
                LoginSession.Set(reply.account, reply.token);
            }
            activeRequest = null;
            password.text = "";
            message.text = "登录成功，正在进入世界…";
            var load = SceneManager.LoadSceneAsync(firstGameScene, LoadSceneMode.Single);
            if (load != null) yield return load;
        }
        finally
        {
            activeRequest = null;
            if (this != null) SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        busy = value;
        account.interactable = !value;
        password.interactable = !value;
        foreach (var button in GetComponentsInChildren<Button>()) button.interactable = !value;
        if (submitLabel != null) submitLabel.text = value ? "请稍候…" : registering ? "创建账号" : "进入世界";
    }
    private void RefreshMode()
    {
        if (heading != null) heading.text = registering ? "创建冒险账号" : "冒险者登录";
        if (submitLabel != null) submitLabel.text = registering ? "创建账号" : "进入世界";
        if (registerLabel != null) registerLabel.text = registering ? "返回登录" : "注册账号";
        if (welcomeLabel != null)
        {
            welcomeLabel.text = registering ? "账号：3至32位字母、数字或下划线" : "欢迎回到营火旁";
            welcomeLabel.fontSize = registering ? 20 : 24;
        }
    }
    private void HidePassword()
    {
        visible = false;
        password.contentType = TMP_InputField.ContentType.Password;
        password.ForceLabelUpdate();
        if (visibilityLabel != null) visibilityLabel.text = "显示";
    }
    public void Registration()
    {
        if (busy) return;
        registering = !registering;
        password.text = "";
        HidePassword();
        RefreshMode();
        message.text = registering ? "密码需为8至128位，不能全为空格" : "";
    }
    public void Recovery() { if (!busy) message.text = "本机账号暂不支持找回密码，请妥善保存"; }
    private void ClearMessage(string unused) { if (!busy) message.text = ""; }
    private void OnDisable()
    {
        StopAllCoroutines();
        if (activeRequest != null) { activeRequest.Abort(); activeRequest.Dispose(); activeRequest = null; }
        busy = false;
        if (password != null) password.text = "";
        if (password != null) HidePassword();
    }
}
