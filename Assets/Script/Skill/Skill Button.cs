using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("연결할 스킬 이름")]
    public string skillName;

    [SerializeField] private SkillData skillData;

    [Header("UI 컴포넌트")]
    public Button button;
    public Image iconImage;
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;

    [Header("쿨타임 안내")]
    public TMP_FontAsset cooldownMessageFont;

    [Header("사운드")]
    // 현재 효과음은 SkillManager에서 실제 스킬 발동 순간 재생합니다.
    // 이 변수는 기존 Inspector 연결을 유지하기 위해 남겨둡니다.
    public AudioClip skillSound;

    [Range(0f, 1f)]
    public float skillSoundVolume = 0.35f;


    [Header("애니메이션")]
    public float punchScale = 1.2f;
    public float punchDuration = 0.1f;

    [Header("툴팁")]
    public bool useTooltip = true;

    [Tooltip("비워두면 TMP 기본 폰트를 사용합니다.")]
    public TMP_FontAsset tooltipFont;

    [Tooltip("비워두면 어두운 기본 배경을 사용합니다.")]
    public Sprite tooltipBackground;

    public Color tooltipBackgroundColor =
        new Color(0.03f, 0.03f, 0.03f, 0.95f);

    public Color tooltipTextColor = Color.white;

    [Min(12f)]
    public float tooltipFontSize = 22f;

    public Vector2 tooltipSize =
        new Vector2(460f, 210f);

    public float tooltipMargin = 15f;

    [Header("툴팁 표시 지연")]
    [Tooltip("마우스를 올린 뒤 이 시간 후 툴팁이 표시됩니다.")]
    [Min(0f)]
    public float tooltipDelay = 0.15f;

    private bool wasOnCooldown;
    private bool pointerInside;
    private Coroutine tooltipShowCoroutine;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private SkillData displayedSkill;
    private SkillTooltipView tooltip;
    private SkillButtonFeedback feedback;

    // 단축키(같은 줄 버튼을 왼쪽부터 Q·W·E·R)와 버튼 꾸밈(키 표시·준비 완료 번쩍임)
    private static readonly KeyCode[] Hotkeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };
    private KeyCode hotkey = KeyCode.None;
    private string hotkeyLabel;
    private bool hotkeyBlockedLastFrame;
    private TowerUpgradePanelUI upgradePanel;
    private TowerBuildRingUI buildRing;
    private Image readyGlow;
    private float readyFlashAt = -10f;
    private int lastShownSecond = -1;
    private float secondPulseAt = -10f;
    private bool cooldownOutlineInitialized;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        feedback = new SkillButtonFeedback(this, iconImage != null ? iconImage.transform : transform);
    }

    private void Start()
    {
        if (iconImage != null) iconImage.raycastTarget = false;
        if (cooldownOverlay != null)
        {
            cooldownOverlay.raycastTarget = false;
            cooldownOverlay.fillAmount = 0f;
        }
        if (cooldownText != null)
        {
            cooldownText.raycastTarget = false;
            cooldownText.gameObject.SetActive(false);
        }
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnSkillButtonClick);
        else Debug.LogWarning($"{name}: Button 컴포넌트를 찾을 수 없습니다.", this);
        ApplySkillIcon(ResolveSkillData());
        AssignHotkey();
        BuildDecor();
    }

    private void AssignHotkey()
    {
        if (transform.parent == null) return;
        var row = new System.Collections.Generic.List<SkillButton>(transform.parent.GetComponentsInChildren<SkillButton>(true));
        row.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        int index = row.IndexOf(this);
        if (index < 0 || index >= Hotkeys.Length) return;
        hotkey = Hotkeys[index];
        hotkeyLabel = hotkey.ToString();
    }

    private void BuildDecor()
    {
        // 준비 완료 때 번쩍이는 금빛
        readyGlow = new GameObject("ReadyGlow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        readyGlow.transform.SetParent(transform, false);
        readyGlow.sprite = UiFx.SoftCircle;
        readyGlow.raycastTarget = false;
        readyGlow.color = new Color(1f, 0.85f, 0.4f, 0f);
        readyGlow.rectTransform.sizeDelta = rectTransform.rect.size * 1.7f;

        if (cooldownOverlay != null) cooldownOverlay.color = new Color(0.03f, 0.04f, 0.1f, 0.78f);
        if (cooldownText != null)
        {
            cooldownText.fontSize = 34f;
            cooldownText.color = new Color32(255, 230, 160, 255);
            if (cooldownText.font == null)
                cooldownText.font = cooldownMessageFont != null ? cooldownMessageFont : BattleUiSkin.Font;
            cooldownText.transform.SetAsLastSibling();
        }

        // 왼쪽 위 키 표시
        Sprite keycap = Resources.Load<Sprite>("UI/TowerPanel/keycap");
        if (string.IsNullOrEmpty(hotkeyLabel) || keycap == null) return;
        var cap = new GameObject("Keycap", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        cap.transform.SetParent(transform, false);
        cap.sprite = keycap;
        cap.preserveAspect = true;
        cap.raycastTarget = false;
        var capRect = cap.rectTransform;
        capRect.anchorMin = capRect.anchorMax = new Vector2(0f, 1f);
        capRect.pivot = new Vector2(0.5f, 0.5f);
        capRect.anchoredPosition = new Vector2(6f, -6f);
        capRect.sizeDelta = new Vector2(33f, 30f);
        var label = new GameObject("Key", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(cap.transform, false);
        label.font = cooldownMessageFont != null ? cooldownMessageFont : BattleUiSkin.Font;
        label.text = hotkeyLabel;
        label.fontSize = 19f;
        label.color = new Color32(40, 30, 24, 255);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(0f, 3f);
        labelRect.offsetMax = Vector2.zero;
        cap.transform.SetAsLastSibling();
    }

    // 업그레이드 창(Q·W·E 사용)·설치 링이 열려 있거나 바로 전 프레임에 열려 있었으면 단축키를 쓰지 않는다.
    private bool HotkeyBlockedNow()
    {
        if (upgradePanel == null) upgradePanel = FindAnyObjectByType<TowerUpgradePanelUI>();
        if (buildRing == null) buildRing = FindAnyObjectByType<TowerBuildRingUI>();
        return (upgradePanel != null && upgradePanel.IsVisible) || (buildRing != null && buildRing.IsOpen);
    }

    private void LateUpdate()
    {
        if (hotkey != KeyCode.None)
            hotkeyBlockedLastFrame = HotkeyBlockedNow();
    }

    private void HandleHotkey()
    {
        if (hotkey == KeyCode.None || !Input.GetKeyDown(hotkey)) return;
        if (hotkeyBlockedLastFrame || HotkeyBlockedNow()) return;
        if (Time.timeScale == 0f || StageDialogue.IsOpen) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameEnded()) return;
        if (button != null && (!button.interactable || !button.gameObject.activeInHierarchy)) return;
        OnSkillButtonClick();
    }

    private void UpdateDecor(bool onCooldown)
    {
        float now = Time.unscaledTime;
        if (readyGlow != null)
        {
            float k = Mathf.Clamp01((now - readyFlashAt) / 0.6f);
            readyGlow.color = new Color(1f, 0.85f, 0.4f, 0.85f * (1f - k));
            readyGlow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.3f, UiFx.EaseOut(k));
        }
        if (iconImage != null)
        {
            Color tint = onCooldown ? new Color(0.55f, 0.55f, 0.62f, 1f) : Color.white;
            if (iconImage.color != tint) iconImage.color = tint;
        }
        // 3초 이하로 남으면 숫자가 1초마다 살짝 튀며 초록빛으로 곧 준비됨을 알린다.
        if (cooldownText != null && onCooldown)
        {
            float pulse = Mathf.Clamp01((now - secondPulseAt) / 0.25f);
            cooldownText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.18f, 1f, UiFx.EaseOut(pulse));
        }
    }

    private void Update()
    {
        SkillData skill = ResolveSkillData();
        if (skill == null) return;
        if (displayedSkill != skill) ApplySkillIcon(skill);
        HandleHotkey();
        UpdateCooldownUI(skill);
        // 설명창이 떠 있으면 아래 줄(재사용까지 남은 시간)을 갱신한다.
        if (pointerInside) tooltip?.RefreshStatus(this, skill);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!useTooltip) return;
        pointerInside = true;
        CancelTooltipDelay();
        if (tooltipDelay <= 0f) ShowTooltip();
        else tooltipShowCoroutine = StartCoroutine(ShowTooltipDelayed());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        CancelTooltipDelay();
        tooltip?.Hide(this);
    }

    private IEnumerator ShowTooltipDelayed()
    {
        yield return new WaitForSecondsRealtime(tooltipDelay);
        tooltipShowCoroutine = null;
        if (pointerInside && useTooltip) ShowTooltip();
    }

    private void CancelTooltipDelay()
    {
        if (tooltipShowCoroutine != null) StopCoroutine(tooltipShowCoroutine);
        tooltipShowCoroutine = null;
    }

    private void OnSkillButtonClick()
    {
        SkillManager manager = SkillManager.Instance;
        SkillAimController aim = SkillAimController.Instance;
        SkillData skill = ResolveSkillData();
        if (manager == null || skill == null) return;
        if (aim == null)
        {
            Debug.LogWarning("SkillAimController를 찾을 수 없습니다.", this);
            return;
        }
        if (!manager.CanUseSkill(skill.SkillId))
        {
            feedback.ShowCooldown(parentCanvas, cooldownMessageFont, rectTransform,
                SkillDescriptionFormatter.Name(skill), manager.GetCooldownRemaining(skill.SkillId));
            return;
        }
        if (aim.IsAiming() && aim.GetSelectedSkillName() == skill.SkillId) aim.CancelAiming();
        else
        {
            aim.StartAiming(skill.SkillId);
            feedback.Punch(punchScale, punchDuration);
        }
        CancelTooltipDelay();
        tooltip?.Hide(this);
    }

    private SkillData ResolveSkillData()
    {
        if (skillData != null) return skillData;
        SkillManager manager = SkillManager.Instance;
        if (manager != null) manager.TryGetSkill(skillName, out skillData);
        return skillData;
    }

    private void ApplySkillIcon(SkillData skill)
    {
        if (skill == null) return;
        displayedSkill = skill;
        if (iconImage != null && skill.Icon != null)
        {
            iconImage.preserveAspect = true;
            iconImage.sprite = skill.Icon;
        }
        if (cooldownOverlay != null && skill.VisualProfile != null)
        {
            cooldownOverlay.sprite = skill.Icon;
            cooldownOverlay.preserveAspect = true;
        }
    }

    private void UpdateCooldownUI(SkillData skill)
    {
        SkillManager manager = SkillManager.Instance;
        if (manager == null) return;
        // 남은 시간을 한 번 조회하여 쿨타임 여부와 진행률을 함께 계산합니다.
        float remaining = manager.GetCooldownRemaining(skill.SkillId);
        bool onCooldown = remaining > 0f;
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = skill.Cooldown > 0f ? Mathf.Clamp01(remaining / skill.Cooldown) : 0f;
        if (cooldownText != null)
        {
            if (cooldownText.gameObject.activeSelf != onCooldown) cooldownText.gameObject.SetActive(onCooldown);
            if (onCooldown)
            {
                // 비활성 TMP는 CanvasRenderer가 초기화되지 않아 외곽선 재질을 만들 수 없다.
                // 처음 숫자를 표시할 때 활성화된 컴포넌트에 적용한다.
                if (!cooldownOutlineInitialized && cooldownText.fontSharedMaterial != null)
                {
                    cooldownText.outlineWidth = 0.3f;
                    cooldownText.outlineColor = new Color(0.05f, 0.03f, 0.02f, 1f);
                    cooldownOutlineInitialized = true;
                }
                string label = remaining > 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("F1");
                if (cooldownText.text != label) cooldownText.text = label;
                int second = Mathf.CeilToInt(remaining);
                if (second != lastShownSecond)
                {
                    lastShownSecond = second;
                    if (second <= 3) secondPulseAt = Time.unscaledTime;
                    cooldownText.color = second <= 3 ? new Color32(150, 240, 130, 255) : new Color32(255, 230, 160, 255);
                }
            }
        }
        UpdateDecor(onCooldown);
        if (wasOnCooldown && !onCooldown)
        {
            feedback.Ready();
            readyFlashAt = Time.unscaledTime;
            lastShownSecond = -1;
        }
        wasOnCooldown = onCooldown;
    }

    private void ShowTooltip()
    {
        if (!useTooltip || SkillManager.Instance == null) return;
        SkillData skill = ResolveSkillData();
        if (skill == null) return;
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        tooltip = SkillTooltipView.Get(parentCanvas);
        tooltip?.Show(this, rectTransform, skill, new SkillTooltipStyle
        {
            Font = tooltipFont != null ? tooltipFont : cooldownMessageFont,
            Background = tooltipBackground,
            BackgroundColor = tooltipBackgroundColor,
            TextColor = tooltipTextColor,
            FontSize = tooltipFontSize,
            Size = tooltipSize,
            Margin = tooltipMargin,
            Hotkey = hotkeyLabel
        });
    }

    private void OnDisable()
    {
        pointerInside = false;
        CancelTooltipDelay();
        tooltip?.Hide(this);
        feedback?.Cancel();
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnSkillButtonClick);
        tooltip?.Hide(this);
        feedback?.Cancel();
    }
}
