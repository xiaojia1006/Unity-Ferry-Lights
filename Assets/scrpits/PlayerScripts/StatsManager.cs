using TMPro;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    public StatsUI statsUI;//属性面板脚本，数值变化后调用它刷新。

    // 保留原字段名，避免 Inspector 引用丢失
    public TMP_Text healthTxet;

    [Header("Combat Stats")]//数字字段，不参与运行
    public int damage;//攻击力
    public float weaponRange;//攻击距离
    public float knockbackForce;//击退力度，给敌人施加的冲量
    public float knockbackTime;//击持续时间
    public float stunTime;//受击硬直时间

    [Header("Movement Stats")]
    public int speed;//移动速度

    [Header("Health Stats")]
    public int maxHealth;//最大生命值
    public int currentHealth;//当前血量

    private void Awake()//单例注册
    {
        if (Instance == null)//之前没人登记
            Instance = this;//this=当前这个组件，登记自己
        else
            Destroy(gameObject);//已经有人了，把整个物体删掉（全部删除以后会引起空引用报错）
    }

    public void UpdateHealth(int amount)//最大生命值上线
    {
        currentHealth += amount;//加血传入正数，扣血传入负数
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);//Clamp(让其控制在最大值和最小值之间)

        UpdateHealthText();//刷新显示
    }

    public void UpdateMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        UpdateHealthText();
    }

    public void UpdateHealthText()
    {
        if (healthTxet != null)
        {
            healthTxet.text =
                "生命:" + currentHealth + "/" + maxHealth;
        }

        PlayerHealth playerHealth =
            FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.RefreshHealthText();
    }

    public void UpdateSpeed(int amount)
    {
        speed += amount;

        if (statsUI != null)
            statsUI.UpdataAllStats();
    }
    public void UpdateDamage(int amount)
    {
        damage += amount;

        if (statsUI != null)
            statsUI.UpdateDamage();
    }
}