using UnityEngine;

public class Elevation_Entry : MonoBehaviour
{
    public Collider2D[] mountainColliders;
    public Collider2D[] boundaryColliders;
    public int playerSortingOrder = 15;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (mountainColliders != null)
        {
            foreach (Collider2D mountain in mountainColliders)
                mountain.enabled = false;
        }

        if (boundaryColliders != null)
        {
            foreach (Collider2D boundary in boundaryColliders)
                boundary.enabled = true;
        }

        SpriteRenderer spriteRenderer =
            collision.GetComponentInParent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            var depth = spriteRenderer.GetComponent<ObliqueDepth2D>();
            if (depth != null) { depth.elevated = true; depth.Refresh(); }
            else spriteRenderer.sortingOrder = playerSortingOrder;
        }
    }
}
