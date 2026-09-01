using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 버튼에 붙이면 클릭 시 확대→축소 애니메이션 + 클릭 사운드 재생
/// 호버 효과도 포함 (PC용)
/// 
/// 사용법: 버튼 오브젝트에 이 컴포넌트를 추가하면 끝
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("클릭 애니메이션")]
    public float clickScaleUp = 1.15f;      // 확대 크기
    public float clickScaleDown = 0.95f;     // 축소 크기
    public float scaleUpDuration = 0.08f;    // 확대 시간
    public float scaleDownDuration = 0.06f;  // 축소 시간
    public float returnDuration = 0.1f;      // 원래 크기로 돌아오는 시간

    [Header("호버 효과 (PC)")]
    public bool useHoverEffect = true;
    public float hoverScale = 1.05f;
    public float hoverDuration = 0.1f;

    [Header("사운드")]
    public bool playClickSound = true;
    public AudioClip customClickSound;       // 비어있으면 AudioManager 기본 사운드 사용

    private Vector3 originalScale;
    private Coroutine currentAnimation;
    private Button button;

    private void Awake()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }

    // ──────── 클릭 (포인터 다운) ────────
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(ClickAnimation());

        // 사운드 재생
        if (playClickSound)
        {
            if (AudioManager.Instance != null)
            {
                if (customClickSound != null)
                    AudioManager.Instance.PlayUISound(customClickSound);
                else
                    AudioManager.Instance.PlayButtonClick();
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 클릭 애니메이션이 끝난 후 처리는 코루틴에서 담당
    }

    // ──────── 호버 ────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!useHoverEffect || !button.interactable) return;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(ScaleTo(
            originalScale * hoverScale, hoverDuration));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!useHoverEffect || !button.interactable) return;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(ScaleTo(
            originalScale, hoverDuration));
    }

    // ──────── 애니메이션 ────────
    private IEnumerator ClickAnimation()
    {
        // 1단계: 확대
        yield return ScaleToImmediate(originalScale * clickScaleUp, scaleUpDuration);

        // 2단계: 축소 (살짝 작게)
        yield return ScaleToImmediate(originalScale * clickScaleDown, scaleDownDuration);

        // 3단계: 원래 크기로 복귀
        yield return ScaleToImmediate(originalScale, returnDuration);

        currentAnimation = null;
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 일시정지 중에도 작동
            float t = elapsed / duration;
            // EaseOutBack 곡선으로 자연스럽게
            t = EaseOutBack(t);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
        currentAnimation = null;
    }

    private IEnumerator ScaleToImmediate(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    // ──────── 이징 함수 ────────
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ──────── 외부에서 스케일 리셋 ────────
    public void ResetScale()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        transform.localScale = originalScale;
    }
}
