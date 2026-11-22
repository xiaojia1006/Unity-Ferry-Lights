using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public string sceneToLoad;
    public Animator fadeAnim;
    public float fadeTime = 0.5f;
    public Vector2 newPlayerPosition;

    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isLoading || !collision.CompareTag("Player"))
            return;

        isLoading = true;

        if (fadeAnim != null)
            fadeAnim.Play("FadeToWhite");

        StartCoroutine(DelayFade(collision.transform));
    }

    private IEnumerator DelayFade(Transform player)
    {
        yield return new WaitForSeconds(fadeTime);

        if (player != null)
            player.position = newPlayerPosition;

        if (!string.IsNullOrWhiteSpace(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
    }
}
