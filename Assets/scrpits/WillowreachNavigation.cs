using System.Collections.Generic;
using UnityEngine;

// Shared XY grid. River cells are forbidden even in snow; only authored bridges are dry.
public sealed class WillowreachNavigation : MonoBehaviour
{
    public WillowreachWorld world;
    public const float Radius = .22f;
    private const int Width=145, Height=97;
    private const float Step=.5f;
    private bool[] cells;
    private bool shortcut;
    private readonly Collider2D[] hits=new Collider2D[32];
    private PolygonCollider2D water;
    public Vector2 Point(int id) => new Vector2(-36+(id%Width)*Step,-24+(id/Width)*Step);
    private int Cell(Vector2 p) => Mathf.Clamp(Mathf.RoundToInt((p.y+24)/Step),0,Height-1)*Width+Mathf.Clamp(Mathf.RoundToInt((p.x+36)/Step),0,Width-1);
    private bool OnBridge(Vector2 p)
    {
        foreach(var r in world.bridges) if(r.Contains(p))return true;
        return world.shortcutBridge!=null&&world.shortcutBridge.activeInHierarchy&&world.shortcutBridgeArea.Contains(p);
    }
    public bool Safe(Vector2 p)
    {
        if(world==null||p.x< -36||p.x>36||p.y< -24||p.y>24)return false;
        if(water==null)water=world.river.GetComponent<PolygonCollider2D>();
        for(int i=0;i<5;i++)
        {
            Vector2 q=p+(i==0?Vector2.zero:i==1?Vector2.left*Radius:i==2?Vector2.right*Radius:i==3?Vector2.up*Radius:Vector2.down*Radius);
            if(water.OverlapPoint(q)&&!OnBridge(q))return false;
        }
        var filter=new ContactFilter2D();filter.NoFilter();filter.useTriggers=false;
        int count=Physics2D.OverlapCircle(p,Radius,filter,hits);
        if(count==hits.Length)return false;
        for(int i=0;i<count;i++)if(hits[i]!=null&&hits[i].attachedRigidbody==null)return false;
        return true;
    }
    public bool SegmentSafe(Vector2 a,Vector2 b)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.12f));
        for(int i=0;i<=steps;i++)if(!Safe(Vector2.Lerp(a,b,i/(float)steps)))return false;
        return true;
    }
    public void Rebuild()
    {
        Physics2D.SyncTransforms();cells=new bool[Width*Height];
        for(int i=0;i<cells.Length;i++)cells[i]=Safe(Point(i));
        shortcut=world.shortcutBridge!=null&&world.shortcutBridge.activeInHierarchy;
    }
    public List<Vector2> Path(Vector2 from,Vector2 to)
    {
        bool active=world.shortcutBridge!=null&&world.shortcutBridge.activeInHierarchy;
        if(cells==null||shortcut!=active)Rebuild();
        int start=Nearest(from),goal=Nearest(to);
        var result=new List<Vector2>();if(start<0||goal<0||!SegmentSafe(from,Point(start)))return result;
        var previous=new int[cells.Length];for(int i=0;i<previous.Length;i++)previous[i]=-1;
        var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
        int[] directions={-1,1,-Width,Width};
        while(queue.Count>0&&previous[goal]<0)
        {
            int c=queue.Dequeue();
            foreach(int d in directions)
            {
                int next=c+d;
                if(next<0||next>=cells.Length||previous[next]>=0||!cells[next]||(Mathf.Abs(d)==1&&c/Width!=next/Width))continue;
                // Endpoints alone are insufficient at shores and narrow collider corners.
                if(!Safe((Point(c)+Point(next))*.5f))continue;
                previous[next]=c;queue.Enqueue(next);
            }
        }
        if(previous[goal]<0)return result;
        for(int c=goal;c!=start;c=previous[c])result.Add(Point(c));
        result.Add(Point(start));result.Reverse();return result;
    }
    private int Nearest(Vector2 p)
    {
        int center=Cell(p),best=-1;float distance=float.MaxValue;
        for(int y=-4;y<=4;y++)for(int x=-4;x<=4;x++)
        {
            int cx=center%Width+x,cy=center/Width+y;if(cx<0||cx>=Width||cy<0||cy>=Height)continue;
            int id=cy*Width+cx;float d=(Point(id)-p).sqrMagnitude;
            if(cells[id]&&d<distance){distance=d;best=id;}
        }
        return best;
    }
    public Vector2 RoamingGoal(Vector2 current)
    {
        if(cells==null)Rebuild();
        for(int i=0;i<50;i++)
        {
            Vector2 p=current+Random.insideUnitCircle*Random.Range(4f,13f);
            if(Random.value<.2f)p=new Vector2(Random.Range(-30f,30f),Random.Range(-21f,21f));
            int id=Nearest(p);if(id>=0&&Vector2.Distance(current,Point(id))>2)return Point(id);
        }
        return current;
    }
}
