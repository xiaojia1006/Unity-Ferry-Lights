using UnityEngine;
public sealed class RPG1TimberMotion : MonoBehaviour
{
    public RPG1SawHazard hazard;
    private Vector3 origin;
    private void Start(){origin=transform.position;}
    private void Update(){float t=Mathf.Repeat(Time.time+hazard.phase,5.2f);transform.position=origin+Vector3.right*(t>=2.8f&&t<3.7f?Mathf.Lerp(-2,2,(t-2.8f)/.9f):-2);}
}
