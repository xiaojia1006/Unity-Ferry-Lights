using UnityEngine;
[RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D))]
public sealed class RPG1PushCrate : MonoBehaviour
{
    public Vector2 origin;
    public Rect allowed=new Rect(9,-8,10,8);
    private Rigidbody2D body;
    private void Awake(){body=GetComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.drag=7;body.mass=2;}
    private void FixedUpdate()
    {
        var p=body.position;var q=new Vector2(Mathf.Clamp(p.x,allowed.xMin+.4f,allowed.xMax-.4f),Mathf.Clamp(p.y,allowed.yMin+.4f,allowed.yMax-.4f));
        if(p!=q){body.position=q;body.velocity=Vector2.zero;}body.velocity=Vector2.ClampMagnitude(body.velocity,2.8f);
    }
    public bool ResetCrate()
    {
        foreach(var hit in Physics2D.OverlapBoxAll(origin,new Vector2(.95f,.75f),0))if(!hit.isTrigger&&hit.attachedRigidbody!=body)return false;
        body.position=origin;transform.position=origin;body.velocity=Vector2.zero;return true;
    }
}
