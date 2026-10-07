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
    }

    private void Update()
    {
        SkillData skill = ResolveSkillData();
        if (skill == null) return;
        if (displayedSkill != skill) ApplySkillIcon(skill);
        UpdateCooldownUI(skill);
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
            feedback.ShowCooldown(parentCanvas, cooldownMessageFont);
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
        if (iconImage != null && skill.Icon != null && iconImage.sprite != skill.Icon)
            iconImage.sprite = skill.Icon;
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
                string label = remaining > 1f ? Mathf.CeilToInt(remaining).ToString() : remaining.ToString("F1");
                if (cooldownText.text != label) cooldownText.text = label;
            }
        }
        if (wasOnCooldown && !onCooldown) feedback.Ready();
        wasOnCooldown = onCooldown;
    }

    private void ShowTooltip()
    {
        if (!useTooltip || SkillManager.Instance == null) return;
        SkillData skill = ResolveSkillData();
        if (skill == null) return;
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        tooltip = SkillTooltipView.Get(parentCanvas);
        tooltip?.Show(this, rectTransform, SkillDescriptionFormatter.Format(skill), new SkillTooltipStyle
        {
            Font = tooltipFont != null ? tooltipFont : cooldownMessageFont,
            Background = tooltipBackground,
            BackgroundColor = tooltipBackgroundColor,
            TextColor = tooltipTextColor,
            FontSize = tooltipFontSize,
            Size = tooltipSize,
            Margin = tooltipMargin
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
