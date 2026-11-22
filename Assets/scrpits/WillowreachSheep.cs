using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class WillowreachSheep : MonoBehaviour
{
    public WillowreachNavigation navigation;
    public Sprite[] idleFrames,walkFrames;
    public float speed=1.65f;
    public int flockIndex;
    public bool following;
    public bool Moving { get; private set; }
    public IReadOnlyList<Vector2> CurrentPath => route;
    private readonly List<Vector2> route=new List<Vector2>();
    private SpriteRenderer target;
    private int waypoint;
    private float nextPlan,grazeUntil;
    private Vector2 lastGoal;
    private void Awake() => target=GetComponent<SpriteRenderer>();
    private void Start() { nextPlan=Time.time+flockIndex*.45f; }
    public void Follow(bool value) { following=value;route.Clear();waypoint=0;nextPlan=0; }
    public void Graze(float seconds) { grazeUntil=Time.time+seconds; }
    private void Update()
    {
        if(navigation==null||navigation.world.player==null||Time.timeScale<=0)return;
        var pos=(Vector2)transform.position;var player=(Vector2)navigation.world.player.position;
        bool resting=Time.time<grazeUntil || following&&Vector2.Distance(pos,player)<1.25f+flockIndex*.35f;
        if(!resting&&Time.time>=nextPlan&&(following||waypoint>=route.Count))
        {
            Vector2 goal=following?player+Vector2.down*(1.25f+flockIndex*.35f):navigation.RoamingGoal(pos);
            if(!following||route.Count==0||Vector2.Distance(lastGoal,goal)>1||waypoint>=route.Count)
            { route.Clear();route.AddRange(navigation.Path(pos,goal));waypoint=0;lastGoal=goal; }
            nextPlan=Time.time+(following?1.2f:Random.Range(5f,9f));
        }
        Moving=false;
        if(!resting&&waypoint<route.Count)
        {
            Vector2 destination=route[waypoint];
            Vector2 next=Vector2.MoveTowards(pos,destination,speed*Time.deltaTime);
            if(navigation.SegmentSafe(pos,next))
            {
                Moving=(next-pos).sqrMagnitude>.000001f;
                if(Mathf.Abs(next.x-pos.x)>.001f)target.flipX=next.x<pos.x;
                transform.position=new Vector3(next.x,next.y,transform.position.z);
                if(Vector2.Distance(next,destination)<.015f)waypoint++;
                if(waypoint>=route.Count&&!following)grazeUntil=Time.time+Random.Range(1.5f,4f);
            }
            else {route.Clear();nextPlan=Time.time+.6f;}
        }
        var frames=Moving?walkFrames:idleFrames;
        if(frames!=null&&frames.Length>0)target.sprite=frames[Mathf.FloorToInt(Time.time*(Moving?9:5)+flockIndex*2)%frames.Length];
        var depth=GetComponent<ObliqueDepth2D>();if(depth!=null)depth.Refresh();
    }
}
