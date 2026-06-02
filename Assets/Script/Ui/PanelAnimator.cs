using System.Collections;
using UnityEngine;

public class PanelAnimator : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("Animation")]
    public float animationTime = 0.15f;
    public Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
    public Vector3 endScale = Vector3.one;

    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        HideInstant();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        float timer = 0f;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = startScale;

        while (timer < animationTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / animationTime;

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = endScale;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        animationCoroutine = null;
    }

    private IEnumerator HideRoutine()
    {
        float timer = 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (timer < animationTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / animationTime;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            rectTransform.localScale = Vector3.Lerp(endScale, startScale, t);

            yield return null;
        }

        HideInstant();
        animationCoroutine = null;
    }

    public void HideInstant()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rectTransform != null)
            rectTransform.localScale = startScale;

        gameObject.SetActive(false);
    }
}
