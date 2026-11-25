using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Visual pass over the authored Login scene: warms the palette, tidies the type, gives the
/// primary button a paper edge and adds a vignette so the village reads as a lit clearing.
/// Everything is applied at runtime, so the scene itself is never modified.</summary>
public sealed class LoginPolish : MonoBehaviour
{
    public static bool Enabled = true;

    // Palette shared with the in-game parchment UI.
    static readonly Color Cream = new Color32(246, 235, 208, 255);
    static readonly Color CreamSoft = new Color32(232, 217, 180, 235);
    static readonly Color Ink = new Color32(62, 54, 43, 255);
    static readonly Color Hint = new Color32(154, 139, 112, 255);
    static readonly Color Gold = new Color32(201, 162, 39, 255);
    static readonly Color SealRed = new Color32(156, 59, 43, 255);
    static readonly Color SealRedDark = new Color32(104, 38, 27, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Enabled || scene.name != "Login") return;
        var runner = new GameObject("Login Polish").AddComponent<LoginPolish>();
        runner.StartCoroutine(ApplyNextFrame(runner));
    }

    static System.Collections.IEnumerator ApplyNextFrame(LoginPolish runner)
    {
        yield return null;            // let LoginScreen.Start run first
        try { Apply(); }
        catch (System.Exception e) { Debug.LogWarning("LoginPolish: " + e.Message); }
        Destroy(runner.gameObject);
    }

    static GameObject Find(string name)
    {
        foreach (var t in Object.FindObjectsOfType<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    static TMP_Text FindText(string name)
    {
        var go = Find(name);
        if (go != null) return go.GetComponent<TMP_Text>();
        foreach (var t in Object.FindObjectsOfType<TMP_Text>(true))
            if (t.text != null && t.text.Trim() == name) return t;
        return null;
    }

    static void Apply()
    {
        var canvas = Find("Login Canvas");
        if (canvas == null) return;

        // Title block: warmer ink on cream, airier tracking, a hairline to anchor the subtitle.
        var title = FindText("渡口灯火");
        if (title == null) title = FindText("柳湾物语");   // 兼容旧场景名
        if (title != null)
        {
            title.color = Cream;
            title.characterSpacing = 6f;
        }

        var latin = FindText("F E R R Y   L I G H T S");
        if (latin != null)
        {
            latin.color = new Color32(217, 196, 143, 235);
            latin.characterSpacing = 16f;
            latin.fontSize = 22f;
        }

        var subtitle = FindText("旅途，从这里开始");
        if (subtitle != null)
        {
            subtitle.color = CreamSoft;
            subtitle.characterSpacing = 8f;
            subtitle.fontSize = 26f;
            AddHairline(subtitle.rectTransform);
        }

        // Panel: soften the cast shadow, keep the paper warm.
        var shadow = Find("Parchment shadow");
        if (shadow != null)
        {
            var img = shadow.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, .28f);
        }

        // Inputs: cream fields with ink typing, muted placeholders.
        foreach (var field in Object.FindObjectsOfType<TMP_InputField>(true))
        {
            var bg = field.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color32(251, 243, 223, 245);
                if (bg.sprite != null && bg.sprite.border.sqrMagnitude > 0) bg.type = Image.Type.Sliced;
            }
            if (field.textComponent != null) field.textComponent.color = Ink;
            if (field.placeholder is TMP_Text ph) ph.color = Hint;
        }

        // Primary button: seal red with a paper bottom edge, cream label.
        var submit = FindText("进入世界");
        if (submit != null)
        {
            var image = submit.transform.parent != null ? submit.transform.parent.GetComponent<Image>() : null;
            if (image == null) image = submit.GetComponentInParent<Image>();
            if (image != null)
            {
                image.color = SealRed;
                var outline = image.gameObject.GetComponent<Outline>();
                if (outline == null) outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = SealRedDark;
                outline.effectDistance = new Vector2(0f, -3f);
            }
            submit.color = Cream;
            submit.characterSpacing = 4f;
        }

        // Secondary links read as links, not as body text.
        foreach (var label in new[] { "注册账号", "忘记密码", "显示" })
        {
            var t = FindText(label);
            if (t == null) continue;
            t.color = label == "显示" ? SealRed : new Color32(120, 96, 62, 255);
        }

        var firstName = FindText("第一次来？");
        if (firstName != null) firstName.color = Hint;

        var welcome = FindText("欢迎回到营火旁");
        if (welcome != null)
        {
            welcome.color = new Color32(139, 120, 88, 255);
            welcome.characterSpacing = 4f;
        }

        var hint = FindText("TAB  切换   /   ENTER  提交");
        if (hint != null)
        {
            hint.color = new Color32(190, 176, 146, 190);
            hint.fontSize = 16f;
            hint.characterSpacing = 3f;
        }

        foreach (var t in Object.FindObjectsOfType<TMP_Text>(true))
            if (t.text != null && t.text.Trim().StartsWith("LOGIN UI")) { t.gameObject.SetActive(false); break; }

        AddVignette(canvas);
    }

    // A 2px gold rule under the tagline; built from a white sprite so no extra art is needed.
    static void AddHairline(RectTransform reference)
    {
        if (reference == null) return;
        var go = new GameObject("Title hairline", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(reference.parent, false);
        rt.sizeDelta = new Vector2(240f, 2f);
        rt.anchoredPosition = reference.anchoredPosition + new Vector2(0f, -34f);
        rt.anchorMin = rt.anchorMax = reference.anchorMin;
        rt.pivot = reference.pivot;
        var img = go.GetComponent<Image>();
        img.color = new Color(Gold.r, Gold.g, Gold.b, .5f);
        img.raycastTarget = false;
    }

    static void AddVignette(GameObject canvas)
    {
        if (canvas.transform.Find("Login Vignette") != null) return;
        var sprite = LoginBackdrop.LoadSprite("vignette");
        if (sprite == null) return;

        var go = new GameObject("Login Vignette", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;

        // Sit above the painted village but below the parchment panel.
        rt.SetAsLastSibling();
        var panel = Find("Parchment");
        if (panel != null && canvas != null)
        {
            var root = panel.transform;
            while (root.parent != null && root.parent != canvas.transform) root = root.parent;
            root.SetAsLastSibling();
        }
    }
}
