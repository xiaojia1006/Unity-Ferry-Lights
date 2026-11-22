using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class WillowreachFlipbook : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond=6;
    [Range(0,1)] public float phase;
    private SpriteRenderer target;
    private void Awake() => target=GetComponent<SpriteRenderer>();
    private void Update()
    { if(target!=null && frames!=null && frames.Length>0) target.sprite=frames[Mathf.FloorToInt(Time.time*framesPerSecond+phase*frames.Length)%frames.Length]; }
}
