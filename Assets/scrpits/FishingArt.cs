using UnityEngine;

/// <summary>Loads the invented fishing sprite sheets and slices them into frames at runtime.</summary>
public static class FishingArt
{
    public static Sprite[] Sheet(string resource, int frames, float pixelsPerUnit, Vector2? pivot = null)
    {
        var tex = Resources.Load<Texture2D>("Fishing/" + resource);
        if (tex == null) return new Sprite[0];
        int fw = tex.width / frames, fh = tex.height;
        var result = new Sprite[frames];
        var p = pivot ?? new Vector2(.5f, .5f);
        for (int i = 0; i < frames; i++)
            result[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), p, pixelsPerUnit);
        return result;
    }

    public static Sprite Single(string resource, float pixelsPerUnit, Vector2? pivot = null)
    {
        var tex = Resources.Load<Texture2D>("Fishing/" + resource);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot ?? new Vector2(.5f, .5f), pixelsPerUnit);
    }
}
