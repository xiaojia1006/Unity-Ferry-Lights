using UnityEngine;
public sealed class RPG1SawHazard : MonoBehaviour
{
    public Vector2 size=new Vector2(4,1.4f);
    public float phase;
    public SpriteRenderer warning;
    private float nextDamage;
    public bool Active => Mathf.Repeat(Time.time+phase,5.2f)>=2.8f&&Mathf.Repeat(Time.time+phase,5.2f)<3.7f;
    private void Update()
    {
        float t=Mathf.Repeat(Time.time+phase,5.2f);bool windup=t>=1.6f&&t<2.8f;
        warning.color=Active?new Color(.85f,.18f,.10f,.7f):windup?new Color(1,.63f,.13f,.35f+.2f*Mathf.Sin(Time.time*18)):new Color(.48f,.33f,.21f,.20f);
        if(!Active||Time.time<nextDamage||Time.timeScale<=0)return;
        foreach(var c in Physics2D.OverlapBoxAll(transform.position,size,0))
        {var health=c.GetComponentInParent<PlayerHealth>();if(health==null)continue;health.ChangeHealth(-2);nextDamage=Time.time+1.2f;break;}
    }
}
