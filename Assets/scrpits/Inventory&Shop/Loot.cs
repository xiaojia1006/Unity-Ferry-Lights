using System;
using System.Collections;
using UnityEngine;

public class Loot : MonoBehaviour
{
    public ItemSO itemSO;
    public SpriteRenderer sr;
    public Animator anim;
    public int quantity;

    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float pickupDelay = 0.6f;

    public static event Func<ItemSO, int, int> OnItemLooted;

    private Collider2D pickupCollider;
    private bool isDropping;
    private float pickupCooldown;
    private bool isCollected;
    public bool HasUnclaimedContents => !isCollected && itemSO!=null && quantity>0;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        ObliqueView2D.ConfigureActor(GetComponent<SpriteRenderer>());
    }

    private void Update()
    {
        if (pickupCooldown > 0f)
            pickupCooldown -= Time.deltaTime;
    }

    public void Initialize(ItemSO newItem, int newQuantity)
    {
        itemSO = newItem;
        quantity = newQuantity;

        if (itemSO == null)
            return;

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
            sr.sprite = itemSO.icon;

        gameObject.name = itemSO.itemName;

        // 手动丢弃后，先禁止拾取一小段时间
        isDropping = true;
        pickupCooldown = pickupDelay + 0.25f;

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        StartCoroutine(EnablePickupAfterDelay());
    }

    private IEnumerator EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(pickupDelay);

        if (pickupCollider != null)
            pickupCollider.enabled = true;

        isDropping = false;
    }

    private void OnValidate()
    {
        if (itemSO == null)
            return;

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
            sr.sprite = itemSO.icon;

        gameObject.name = itemSO.itemName;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryPickup(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryPickup(collision);
    }

    private void TryPickup(Collider2D collision)
    {
        if (itemSO == null || isCollected || quantity <= 0 || OnItemLooted == null)
            return;

        if (!collision.CompareTag("Player") ||
            isDropping ||
            pickupCooldown > 0f)
        {
            return;
        }

        int remaining = quantity;

        if (OnItemLooted != null)
        {
            remaining = OnItemLooted.Invoke(itemSO, quantity);
        }

        // 背包有空间，正常拾取
        if (remaining <= 0)
        {
            isCollected = true;
            quantity = 0;
            if (pickupCollider != null)
                pickupCollider.enabled = false;
            if (anim != null)
                anim.Play("LootPinkUp");

            Destroy(gameObject, 0.5f);
            return;
        }

        // 背包满了，放到玩家身后
        isDropping = true;
        StartCoroutine(
            DropBehindPlayer(collision.transform, remaining)
        );
    }

    private IEnumerator DropBehindPlayer(
        Transform player,
        int remaining
    )
    {
        quantity = remaining;

        float facing =
            player.localScale.x >= 0f ? 1f : -1f;

        Vector3 behindOffset = new Vector3(
            -facing * dropDistance,
            0f,
            0f
        );

        transform.position = player.position + behindOffset;

        pickupCooldown = pickupDelay + 0.25f;

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        yield return new WaitForSeconds(pickupDelay);

        if (pickupCollider != null)
            pickupCollider.enabled = true;

        isDropping = false;
    }
}