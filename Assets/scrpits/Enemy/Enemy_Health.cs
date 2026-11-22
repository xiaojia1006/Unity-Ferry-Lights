using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Health : MonoBehaviour
{
    // Start is called before the first frame update
    public int currenetHealth;
    public int maxHealth;
    public int expReward = 3;
    public delegate void MosterDefeated(int exp);
    public static event MosterDefeated OnMonsterDefeated;
    private bool defeated;
    public bool preserveForRetry;
    public bool invulnerable;
    public void Revive(){defeated=false;currenetHealth=maxHealth;gameObject.SetActive(true);}
    private void Start()
    {
        currenetHealth = maxHealth;
    }
    public void ChangeHealth(int amount)
    {
        if (defeated || invulnerable && amount < 0) return;
        currenetHealth += amount;
        if (currenetHealth > maxHealth)
        {
            currenetHealth = maxHealth;
        }
        else if (currenetHealth<=0) 
        {
            defeated = true;
            Sfx.Die();
            OnMonsterDefeated?.Invoke(expReward);
            if (preserveForRetry) gameObject.SetActive(false);
            else Destroy(gameObject);
        }
    }

}
