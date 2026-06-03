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
        // 코루틴 시작 전에 자기 자신을 먼저 켜야 함
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // 부모 오브젝트가 꺼져 있으면 여전히 코루틴 실행 불가
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning(gameObject.name + "의 부모 오브젝트가 꺼져 있어서 패널을 열 수 없습니다.");
            return;
        }

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        float timer = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rectTransform != null)
            rectTransform.localScale = startScale;

        while (timer < animationTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / animationTime;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (rectTransform != null)
            rectTransform.localScale = endScale;

        animationCoroutine = null;
    }

    private IEnumerator HideRoutine()
    {
        float timer = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (timer < animationTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / animationTime;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            if (rectTransform != null)
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
