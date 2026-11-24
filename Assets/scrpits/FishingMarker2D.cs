using UnityEngine;

/// <summary>A quiet marker on the water so the fishing spot can be found without a HUD arrow:
/// two expanding ripples plus a bobbing float. Built at runtime next to the interaction point.</summary>
public sealed class FishingMarker2D : MonoBehaviour
{
    SpriteRenderer ringA, ringB, bobber;
    Sprite[] ringFrames;
    float period = 2.4f;
    float clock;
    Vector3 bobberHome;

    public static FishingMarker2D Create(Transform anchor, int sortingLayer, int sortingOrder)
    {
        if (anchor == null) return null;
        var go = new GameObject("Fishing marker");
        go.transform.SetParent(anchor, false);
        go.transform.localPosition = Vector3.zero;

        var marker = go.AddComponent<FishingMarker2D>();
        var rings = FishingArt.Sheet("ripple_sheet", 5, 64f);
        var bobberSprite = FishingArt.Single("bobber", 64f);
        if (rings.Length == 0) { Destroy(go); return null; }
        marker.ringFrames = rings;

        marker.ringA = MakeRenderer(go.transform, "Ripple A", rings[0], sortingLayer, sortingOrder, Color.white);
        marker.ringB = MakeRenderer(go.transform, "Ripple B", rings[0], sortingLayer, sortingOrder, Color.white);
        if (bobberSprite != null)
        {
            marker.bobber = MakeRenderer(go.transform, "Float", bobberSprite, sortingLayer, sortingOrder + 1, Color.white);
            marker.bobberHome = marker.bobber.transform.localPosition;
        }
        return marker;
    }

    static SpriteRenderer MakeRenderer(Transform parent, string name, Sprite sprite, int layer, int order, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;
        sr.color = color;
        return sr;
    }

    void Update()
    {
        // While the rig is out, the rod's own float takes over the spot's ripples.
        bool show = !FishingRig2D.Busy;
        if (ringA != null) ringA.enabled = show;
        if (ringB != null) ringB.enabled = show;
        if (bobber != null) bobber.enabled = show;

        clock += Time.deltaTime;
        float t = Mathf.Repeat(clock, period) / period;
        Pulse(ringA, t);
        Pulse(ringB, Mathf.Repeat(t + .5f, 1f));
        if (bobber != null)
        {
            bobber.transform.localPosition = bobberHome + new Vector3(0f, Mathf.Sin(clock * 2.2f) * .045f, 0f);
        }
    }

    // Frames carry the expansion and fade, so nothing is scaled at runtime.
    void Pulse(SpriteRenderer ring, float t)
    {
        if (ring == null || ringFrames == null || ringFrames.Length == 0) return;
        int index = Mathf.Clamp(Mathf.FloorToInt(t * ringFrames.Length), 0, ringFrames.Length - 1);
        ring.sprite = ringFrames[index];
        ring.transform.localScale = Vector3.one;
        var c = ring.color; c.a = 1f; ring.color = c;
    }
}
