using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeBlackScreen : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    private void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
    }

    public void StartFadeSequence()
    {
        StartCoroutine(FadeSequence());
    }

    public void StartInstantFadeSequence()
    {
        StartCoroutine(InstantFadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        targetImage.gameObject.SetActive(true);
        Color color = targetImage.color;
        color.a = 0f;
        targetImage.color = color;

        // Fade in to 1 over 1 second
        yield return StartCoroutine(FadeTo(1f, 0.2f));

        // Wait 2 seconds
        yield return new WaitForSeconds(2f);

        // Fade out to 0
        yield return StartCoroutine(FadeTo(0f, 1f));

        targetImage.gameObject.SetActive(false);
    }

    private IEnumerator InstantFadeSequence()
    {
        targetImage.gameObject.SetActive(true);
        Color color = targetImage.color;
        color.a = 0f;
        targetImage.color = color;

        yield return StartCoroutine(FadeTo(1f, 0f));

        // Fade out to 0
        yield return StartCoroutine(FadeTo(0f, 1f));

        targetImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        Color color = targetImage.color;
        float startAlpha = color.a;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, timeElapsed / duration);
            targetImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        targetImage.color = color;
    }
}
