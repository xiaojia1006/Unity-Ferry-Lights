using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Bow : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform launchPoint;
    public GameObject arrowPrefab;
    public Vector2 aimDirection = Vector2.right;
    public float shootCooldown = 0.5f;
    private float shootTimer;
    public Animator anim;
    public PlayerMovement playerMovement;
    // Update is called once per frame
    void Update()
    {
        if (shootTimer>0f)
        {
            shootTimer -= Time.deltaTime;
        }
        HandleAiming();
        if (Time.timeScale > 0f && Input.GetButtonDown("Shoot")&&shootTimer<=0)
        {
            anim.SetBool("isShooting",true);
            playerMovement.isShooting = true;
        }
        
    }
    private void OnEnable()
    {
        anim.SetLayerWeight(0,0);
        anim.SetLayerWeight(1,1);
    }
    private void OnDisable()
    {
        anim.SetBool("isShooting", false);
        playerMovement.isShooting = false;
        anim.SetLayerWeight(0, 1);
        anim.SetLayerWeight(1, 0);
    }
    public void HandleAiming() 
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0 ||vertical !=0)
        { 
            aimDirection = new Vector2(horizontal,vertical).normalized;
            anim.SetFloat("aimX",aimDirection.x);
            anim.SetFloat("aimY", aimDirection.y);
        }
    }
    public void Shoot()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f)
            return;
        if (shootTimer <= 0)
        {
            Arrow arrow = Instantiate(arrowPrefab, launchPoint.position, Quaternion.identity).GetComponent<Arrow>();
            arrow.direction = aimDirection;
            shootTimer = shootCooldown;
            Sfx.Bow();
        }
        anim.SetBool("isShooting",false);
        playerMovement.isShooting = false;
    }
}
