using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public TMP_Text healthText;
    public Animator healthTextAnim;

    private void Start()
    {
        RefreshHealthText();
    }

    public void ChangeHealth(int amount)
    {
        if (StatsManager.Instance == null)
            return;

        StatsManager.Instance.currentHealth += amount;
        StatsManager.Instance.currentHealth = Mathf.Clamp(
            StatsManager.Instance.currentHealth,
            0,
            StatsManager.Instance.maxHealth
        );

        if (healthTextAnim != null)
            healthTextAnim.Play("Text_Update");

        RefreshHealthText();

        if (StatsManager.Instance.currentHealth <= 0)
            gameObject.SetActive(false);
    }

    public void RefreshHealthText()
    {
        if (healthText == null || StatsManager.Instance == null)
            return;

        healthText.text =
            "生命:" +
            StatsManager.Instance.currentHealth +
            "/" +
            StatsManager.Instance.maxHealth;
    }
}
