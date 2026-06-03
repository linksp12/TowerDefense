using System.Collections;
using UnityEngine;

public class PanelShake : MonoBehaviour
{
    public RectTransform targetRect;

    [Header("Shake Setting")]
    public float shakeTime = 0.25f;
    public float shakePower = 12f;
    public float shakeSpeed = 45f;

    private Vector2 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (targetRect == null)
            targetRect = GetComponent<RectTransform>();

        if (targetRect != null)
            originalPosition = targetRect.anchoredPosition;
    }

    public void PlayShake()
    {
        if (targetRect == null)
            return;

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        originalPosition = targetRect.anchoredPosition;

        float timer = 0f;

        while (timer < shakeTime)
        {
            timer += Time.unscaledDeltaTime;

            float x = Mathf.Sin(timer * shakeSpeed) * shakePower;
            targetRect.anchoredPosition = originalPosition + new Vector2(x, 0f);

            yield return null;
        }

        targetRect.anchoredPosition = originalPosition;
        shakeCoroutine = null;
    }
}
