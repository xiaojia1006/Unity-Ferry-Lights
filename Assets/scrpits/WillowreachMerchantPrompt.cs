using UnityEngine;

// A compact world-space trade hint; the shop's portrait and inventory remain unchanged.
public sealed class WillowreachMerchantPrompt : MonoBehaviour
{
    public ShopKeeper merchant;
    public SpriteRenderer icon;
    public Vector2 headOffset;
    private Transform player;
    private void Start() { var p=FindObjectOfType<PlayerMovement>(); if(p!=null)player=p.transform; }
    private void LateUpdate()
    {
        if(merchant==null||icon==null)return;
        icon.transform.position=merchant.transform.TransformPoint(headOffset)+Vector3.up*Mathf.Sin(Time.time*3)*.045f;
        icon.enabled=player!=null&&Vector2.Distance(player.position,merchant.transform.position)<5&&ShopKeeper.currentShopKeeper==null;
    }
}
