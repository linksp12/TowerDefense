using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SkillButton : MonoBehaviour
{
    [Header("연결할 스킬 이름")]
    public string skillName;

    [Header("UI 컴포넌트")]
    public Button button;
    public Image iconImage;
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;

    [Header("쿨타임 안내")]
    public TMP_FontAsset cooldownMessageFont;

    [Header("사운드")]
    public AudioClip skillSound;
    [Range(0f, 1f)] public float skillSoundVolume = 0.35f;
    private AudioSource audioSource;

    [Header("애니메이션")]
    public float punchScale = 1.2f;
    public float punchDuration = 0.1f;

    private bool wasOnCooldown = false; // 쿨타임 끝남 감지용
    private Transform animatedTransform;
    private Vector3 originalScale;
    private Coroutine scaleAnimation;

    private static TextMeshProUGUI centralCooldownMessage;
    private static Coroutine centralMessageCoroutine;
    private static SkillButton centralMessageOwner;

    private void Awake()
    {
        // 버튼 루트 대신 아이콘만 움직여서 클릭 영역이 커지지 않게 한다.
        animatedTransform = iconImage != null ? iconImage.transform : transform;
        originalScale = animatedTransform.localScale;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // 자식 UI가 이웃 스킬 버튼의 클릭 영역을 가로채지 않도록 한다.
        if (iconImage != null)
            iconImage.raycastTarget = false;
        if (cooldownOverlay != null)
            cooldownOverlay.raycastTarget = false;
        if (cooldownText != null)
            cooldownText.raycastTarget = false;

        button.onClick.AddListener(OnSkillButtonClick);
        cooldownOverlay.fillAmount = 0f;
    }

    void Update()
    {
        UpdateCooldownUI();
    }

    void OnSkillButtonClick()
    {
        if (SkillManager.Instance == null)
            return;

        // 실제로 사용된 경우에만 소리와 애니메이션을 실행한다.
        if (!SkillManager.Instance.UseSkill(skillName))
        {
            ShowCentralCooldownMessage();
            return;
        }

        if (skillSound != null)
            audioSource.PlayOneShot(skillSound, skillSoundVolume);

        PlayScaleAnimation(PunchAnim());
    }

    void UpdateCooldownUI()
    {
        bool onCooldown = !SkillManager.Instance.CanUseSkill(skillName);
        float remaining = SkillManager.Instance.GetCooldownRemaining(skillName);
        float normalized = SkillManager.Instance.GetCooldownNormalized(skillName);

        cooldownOverlay.fillAmount = normalized;

        if (onCooldown)
        {
            cooldownText.text = remaining > 1f
                ? Mathf.CeilToInt(remaining).ToString()
                : remaining.ToString("F1");
            cooldownText.gameObject.SetActive(true);
            wasOnCooldown = true; // 쿨타임 중 기록
        }
        else
        {
            // 쿨타임이 방금 끝났을 때 반짝 애니메이션
            if (wasOnCooldown)
            {
                wasOnCooldown = false;
                PlayScaleAnimation(ReadyAnim());
            }

            cooldownOverlay.fillAmount = 0f;
            cooldownText.gameObject.SetActive(false);
        }
    }

    // 클릭 시 커졌다 작아지는 애니메이션
    IEnumerator PunchAnim()
    {
        Vector3 big = originalScale * punchScale;
        float half = punchDuration * 0.5f;

        for (float t = 0; t < half; t += Time.deltaTime)
        {
            animatedTransform.localScale = Vector3.Lerp(originalScale, big, t / half);
            yield return null;
        }
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            animatedTransform.localScale = Vector3.Lerp(big, originalScale, t / half);
            yield return null;
        }
        animatedTransform.localScale = originalScale;
        scaleAnimation = null;
    }

    // 쿨타임 끝났을 때 반짝이는 애니메이션
    IEnumerator ReadyAnim()
    {
        Vector3 big = originalScale * 1.15f;
        float dur = 0.2f;

        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            animatedTransform.localScale = Vector3.Lerp(originalScale, big, t / dur);
            yield return null;
        }
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            animatedTransform.localScale = Vector3.Lerp(big, originalScale, t / dur);
            yield return null;
        }
        animatedTransform.localScale = originalScale;
        scaleAnimation = null;
    }

    private void PlayScaleAnimation(IEnumerator animation)
    {
        if (scaleAnimation != null)
            StopCoroutine(scaleAnimation);

        animatedTransform.localScale = originalScale;
        scaleAnimation = StartCoroutine(animation);
    }

    private void ShowCentralCooldownMessage()
    {
        EnsureCentralCooldownMessage();

        if (centralCooldownMessage == null)
            return;

        if (centralMessageOwner != null && centralMessageCoroutine != null)
        {
            centralMessageOwner.StopCoroutine(centralMessageCoroutine);
        }

        centralCooldownMessage.text = "스킬 쿨타임입니다";
        centralCooldownMessage.gameObject.SetActive(true);
        centralCooldownMessage.transform.SetAsLastSibling();

        centralMessageOwner = this;
        centralMessageCoroutine = StartCoroutine(HideCentralCooldownMessage());
    }

    private void EnsureCentralCooldownMessage()
    {
        if (centralCooldownMessage != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            return;

        GameObject messageObject = new GameObject(
            "SkillCooldownMessage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        messageObject.transform.SetParent(parentCanvas.transform, false);

        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(700f, 100f);

        centralCooldownMessage = messageObject.GetComponent<TextMeshProUGUI>();
        centralCooldownMessage.font = cooldownMessageFont != null
            ? cooldownMessageFont
            : TMP_Settings.defaultFontAsset;
        centralCooldownMessage.fontSize = 42f;
        centralCooldownMessage.alignment = TextAlignmentOptions.Center;
        centralCooldownMessage.color = new Color(1f, 0.82f, 0.25f, 1f);
        centralCooldownMessage.outlineColor = Color.black;
        centralCooldownMessage.outlineWidth = 0.22f;
        centralCooldownMessage.overflowMode = TextOverflowModes.Overflow;
        centralCooldownMessage.raycastTarget = false;
        centralCooldownMessage.gameObject.SetActive(false);
    }

    private IEnumerator HideCentralCooldownMessage()
    {
        yield return new WaitForSecondsRealtime(1.1f);

        if (centralCooldownMessage != null)
            centralCooldownMessage.gameObject.SetActive(false);

        centralMessageCoroutine = null;
        centralMessageOwner = null;
    }

    private void OnDisable()
    {
        if (scaleAnimation != null)
            StopCoroutine(scaleAnimation);

        if (animatedTransform != null)
            animatedTransform.localScale = originalScale;

        scaleAnimation = null;

        if (centralMessageOwner == this)
        {
            if (centralCooldownMessage != null)
                centralCooldownMessage.gameObject.SetActive(false);

            centralMessageCoroutine = null;
            centralMessageOwner = null;
        }
    }
}
