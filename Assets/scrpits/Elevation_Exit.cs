using UnityEngine;

public class Elevation_Exit : MonoBehaviour
{
    public Collider2D[] mountainColliders;
    public Collider2D[] boundaryColliders;
    public int playerSortingOrder = 9;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (mountainColliders != null)
        {
            foreach (Collider2D mountain in mountainColliders)
                mountain.enabled = true;
        }

        if (boundaryColliders != null)
        {
            foreach (Collider2D boundary in boundaryColliders)
                boundary.enabled = false;
        }

        SpriteRenderer spriteRenderer =
            collision.GetComponentInParent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            var depth = spriteRenderer.GetComponent<ObliqueDepth2D>();
            if (depth != null) { depth.elevated = false; depth.Refresh(); }
            else spriteRenderer.sortingOrder = playerSortingOrder;
        }
    }
}
