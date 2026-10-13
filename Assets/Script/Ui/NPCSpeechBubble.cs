
using System.Collections;
using TMPro;
using UnityEngine;

public class NPCSpeechBubble : MonoBehaviour
{
    [Header("대상 NPC")]
    [SerializeField] private Transform targetTransform;

    [Header("화면 UI")]
    [SerializeField] private Canvas screenSpaceCanvas;
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private TMP_Text bubbleText;

    [Header("위치 설정")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.4f, 0f);
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 10f);

    [Header("NPC 안내 문구")]
    [TextArea(1, 2)]
    [SerializeField] private string[] messages;

    [Header("표시 설정")]
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float minInterval = 5f;
    [SerializeField] private float maxInterval = 9f;
    [SerializeField] private float initialDelay = 3f;

    [Header("등장 애니메이션")]
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float hiddenScale = 0.75f;

    private RectTransform bubbleRect;
    private RectTransform bubbleParentRect;
    private CanvasGroup canvasGroup;

    private Vector3 originalScale;
    private Coroutine bubbleCoroutine;
    private Camera worldCamera;

    private void Awake()
    {
        // 대상 NPC가 지정되지 않았으면 이 오브젝트를 대상으로 사용
        if (targetTransform == null)
            targetTransform = transform;

        // 필수 UI 참조 확인
        if (screenSpaceCanvas == null ||
            bubbleRoot == null ||
            bubbleText == null)
        {
            Debug.LogError(
                $"{name}: NPC 말풍선 참조가 연결되지 않았습니다.",
                this
            );

            enabled = false;
            return;
        }

        bubbleRect = bubbleRoot.GetComponent<RectTransform>();

        // 말풍선의 실제 부모를 기준으로 위치를 계산
        bubbleParentRect =
            bubbleRoot.transform.parent as RectTransform;

        if (bubbleRect == null || bubbleParentRect == null)
        {
            Debug.LogError(
                $"{name}: 말풍선 또는 부모의 RectTransform을 찾을 수 없습니다.",
                this
            );

            enabled = false;
            return;
        }

        // 투명도 제어 컴포넌트 확인
        canvasGroup = bubbleRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = bubbleRoot.AddComponent<CanvasGroup>();

        // 원래 말풍선 크기 저장
        originalScale = bubbleRect.localScale;

        // 월드 카메라 가져오기
        worldCamera = Camera.main;

        // 게임 시작 시 말풍선 숨기기
        HideBubble();

        // NPC 위치에 맞춰 말풍선 위치 갱신
        UpdateBubblePosition();
    }

    private void OnEnable()
    {
        if (enabled && bubbleRoot != null)
        {
            if (bubbleCoroutine != null)
                StopCoroutine(bubbleCoroutine);

            bubbleCoroutine = StartCoroutine(BubbleLoop());
        }
    }

    private void OnDisable()
    {
        if (bubbleCoroutine != null)
        {
            StopCoroutine(bubbleCoroutine);
            bubbleCoroutine = null;
        }

        HideBubble();
    }

    private void LateUpdate()
    {
        UpdateBubblePosition();
    }

    // NPC의 월드 위치를 화면 좌표로 변환해 말풍선을 배치
    private void UpdateBubblePosition()
    {
        if (targetTransform == null ||
            screenSpaceCanvas == null ||
            bubbleRect == null ||
            bubbleParentRect == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        // NPC 위치에 월드 오프셋 적용
        Vector3 worldPosition =
            targetTransform.position + worldOffset;

        // 월드 좌표를 화면 좌표로 변환
        Vector3 screenPosition =
            worldCamera.WorldToScreenPoint(worldPosition);

        // NPC가 카메라 뒤에 있으면 위치 갱신하지 않음
        if (screenPosition.z <= 0f)
            return;

        // 화면 기준 추가 오프셋
        screenPosition += (Vector3)screenOffset;

        // Canvas 종류에 맞는 이벤트 카메라 설정
        Camera eventCamera =
            screenSpaceCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (screenSpaceCanvas.worldCamera != null
                ? screenSpaceCanvas.worldCamera
                : worldCamera);

        // 화면 좌표를 말풍선 부모의 로컬 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bubbleParentRect,
            screenPosition,
            eventCamera,
            out Vector2 localPosition))
        {
            bubbleRect.anchoredPosition = localPosition;
        }
    }

    // 말풍선을 반복해서 표시
    private IEnumerator BubbleLoop()
    {
        // 처음 표시하기 전 대기
        yield return new WaitForSecondsRealtime(
            Random.Range(0f, Mathf.Max(0f, initialDelay))
        );

        while (true)
        {
            // 대화창이 열려 있으면 말풍선 숨기기
            if (DialogueUI.IsOpen)
            {
                HideBubble();
                yield return null;
                continue;
            }

            // 표시할 문구가 없으면 종료
            if (messages == null || messages.Length == 0)
            {
                HideBubble();
                yield break;
            }

            // 안내 문구 무작위 선택
            int index = Random.Range(0, messages.Length);
            bubbleText.text = messages[index];

            // 말풍선 표시
            bubbleRoot.SetActive(true);

            // 등장 애니메이션
            yield return AnimateBubble(true, popDuration);

            // 표시 시간 계산
            float elapsed = 0f;

            while (elapsed < visibleDuration && !DialogueUI.IsOpen)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 대화창이 열리지 않았을 때 퇴장 애니메이션
            if (!DialogueUI.IsOpen)
                yield return AnimateBubble(false, popDuration);

            HideBubble();

            // 다음 말풍선까지 대기
            float waitTime = Random.Range(
                Mathf.Max(0f, minInterval),
                Mathf.Max(Mathf.Max(0f, minInterval), maxInterval)
            );

            float waitElapsed = 0f;

            while (waitElapsed < waitTime)
            {
                if (DialogueUI.IsOpen)
                    HideBubble();

                waitElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    // 작게 나타나고 작아지며 사라지는 애니메이션
    private IEnumerator AnimateBubble(bool show, float duration)
    {
        float startScale = show ? hiddenScale : 1f;
        float endScale = show ? 1f : hiddenScale;

        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;

        // 애니메이션 시작 상태 설정
        bubbleRect.localScale = originalScale * startScale;
        canvasGroup.alpha = startAlpha;

        if (duration <= 0f)
        {
            bubbleRect.localScale = originalScale * endScale;
            canvasGroup.alpha = endAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // 부드럽게 끝나는 보간
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            float scale = Mathf.Lerp(startScale, endScale, eased);

            bubbleRect.localScale = originalScale * scale;

            canvasGroup.alpha =
                Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        // 마지막 프레임 값 보정
        bubbleRect.localScale = originalScale * endScale;
        canvasGroup.alpha = endAlpha;
    }

    // 말풍선 숨기기
    private void HideBubble()
    {
        if (bubbleRoot == null)
            return;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (bubbleRect != null)
            bubbleRect.localScale = originalScale;

        bubbleRoot.SetActive(false);
    }
}
