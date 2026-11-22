using UnityEngine;

// Uses the existing animated pickup and inventory stack/capacity rules.
public sealed class RPG1Harvest : MonoBehaviour
{
    public ItemSO item;
    public Loot prefab;
    public int quantity=2;
    public float regrowSeconds=90;
    public string label="采集";
    private float availableAt;
    private bool spent;
    private SpriteRenderer art;
    private PlayerMovement player;
    private RPG1Activities hud;
    private void Start(){art=GetComponent<SpriteRenderer>();player=FindObjectOfType<PlayerMovement>();hud=FindObjectOfType<RPG1Activities>();}
    private void Update()
    {
        if(spent&&regrowSeconds>0&&Time.time>=availableAt){spent=false;art.color=Color.white;}
        if(spent||player==null||Time.timeScale<=0||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null||RPG1Activities.BlocksMenuInput)return;
        if(Vector2.Distance(player.transform.position,transform.position)>1.25f)return;
        if(hud!=null)hud.Notify("E "+label+" · 产出进入地面拾取，背包满时不会丢失");
        if(!Input.GetKeyDown(KeyCode.E))return;
        TryHarvest();
    }
    public bool TryHarvest()
    {
        if(spent||item==null||prefab==null)return false;
        spent=true;availableAt=Time.time+regrowSeconds;art.color=new Color(.55f,.55f,.55f,.55f);
        var drop=Instantiate(prefab,transform.position+Vector3.down*.55f,Quaternion.identity);drop.Initialize(item,quantity);
        return true;
    }
}
