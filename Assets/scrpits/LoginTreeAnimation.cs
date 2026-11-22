using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class LoginTreeAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 5f;
    private Image target;
    private void Awake() { target = GetComponent<Image>(); }
    private void Update()
    {
        if (frames != null && frames.Length > 0)
            target.sprite = frames[Mathf.FloorToInt(Time.unscaledTime * framesPerSecond) % frames.Length];
    }
}
