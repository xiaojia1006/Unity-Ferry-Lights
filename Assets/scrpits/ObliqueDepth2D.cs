using UnityEngine;

// Pure XY gameplay: depth comes from the ground contact point, never camera tilt.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class ObliqueDepth2D : MonoBehaviour
{
    [Tooltip("Optional ground contact marker. Does not move the sprite pivot.")]
    public Transform feet;
    public float footOffset;
    public bool elevated;
    private SpriteRenderer target;
    private Collider2D body;

    public static int Order(float groundY, bool onPlatform)
    {
        return (onPlatform ? 15000 : 5000) - Mathf.RoundToInt(Mathf.Clamp(groundY, -100, 100) * 20);
    }

    private void OnEnable()
    {
        target = GetComponent<SpriteRenderer>();
        foreach (var candidate in GetComponents<Collider2D>())
            if (!candidate.isTrigger) { body = candidate; break; }
        Refresh();
    }

    public void Refresh()
    {
        if (target == null) return;
        float y = feet != null ? feet.position.y :
            body != null && body.enabled ? body.bounds.min.y : transform.position.y;
        target.sortingOrder = Order(y + footOffset, elevated);
    }

    private void LateUpdate() => Refresh();
}
