using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
    // 현재 효과음은 SkillManager에서 실제 스킬 발동 순간 재생합니다.
    // 이 변수는 기존 Inspector 연결을 유지하기 위해 남겨둡니다.
    public AudioClip skillSound;

    [Range(0f, 1f)]
    public float skillSoundVolume = 0.35f;

    private AudioSource audioSource;

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

    private bool wasOnCooldown = false;

    private Transform animatedTransform;
    private Vector3 originalScale;
    private Coroutine scaleAnimation;

    private Coroutine tooltipShowCoroutine;
    private bool pointerInside = false;

    // 중앙 쿨타임 안내
    private static TextMeshProUGUI centralCooldownMessage;
    private static Coroutine centralMessageCoroutine;
    private static SkillButton centralMessageOwner;

    // 공용 툴팁
    private static GameObject tooltipObject;
    private static RectTransform tooltipRect;
    private static TextMeshProUGUI tooltipText;
    private static CanvasGroup tooltipCanvasGroup;
    private static SkillButton tooltipOwner;

    private RectTransform rectTransform;
    private Canvas parentCanvas;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        animatedTransform =
            iconImage != null
                ? iconImage.transform
                : transform;

        originalScale =
            animatedTransform.localScale;

        rectTransform =
            GetComponent<RectTransform>();

        parentCanvas =
            GetComponentInParent<Canvas>();
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        audioSource =
            GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource =
                gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;

        // -----------------------------------------------------
        // 클릭 방해 방지
        // -----------------------------------------------------

        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (cooldownOverlay != null)
            cooldownOverlay.raycastTarget = false;

        if (cooldownText != null)
            cooldownText.raycastTarget = false;


        // -----------------------------------------------------
        // Button 자동 연결
        // -----------------------------------------------------

        if (button == null)
        {
            button =
                GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(
                OnSkillButtonClick
            );
        }
        else
        {
            Debug.LogWarning(
                gameObject.name +
                " : Button 컴포넌트를 찾을 수 없습니다."
            );
        }


        // -----------------------------------------------------
        // 쿨타임 UI
        // -----------------------------------------------------

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(
                false
            );
        }


        // -----------------------------------------------------
        // 스킬 아이콘 적용
        // -----------------------------------------------------

        ApplySkillIcon();


        // 시작할 때 툴팁 강제 숨김
        HideTooltip();
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        UpdateCooldownUI();

        ApplySkillIcon();
    }


    // =========================================================
    // Pointer Enter
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (!useTooltip)
            return;

        pointerInside = true;

        if (tooltipShowCoroutine != null)
        {
            StopCoroutine(
                tooltipShowCoroutine
            );
        }

        tooltipShowCoroutine =
            StartCoroutine(
                ShowTooltipDelayed()
            );
    }


    // =========================================================
    // Pointer Exit
    // =========================================================

    public void OnPointerExit(
        PointerEventData eventData)
    {
        pointerInside = false;

        if (tooltipShowCoroutine != null)
        {
            StopCoroutine(
                tooltipShowCoroutine
            );

            tooltipShowCoroutine = null;
        }

        HideTooltip();
    }


    // =========================================================
    // 툴팁 표시 지연
    // =========================================================

    private IEnumerator ShowTooltipDelayed()
    {
        if (tooltipDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                tooltipDelay
            );
        }

        tooltipShowCoroutine = null;

        if (!pointerInside)
            yield break;

        ShowTooltip();
    }


    // =========================================================
    // ★ 스킬 버튼 클릭
    // =========================================================
    // 버튼을 누르면 바로 스킬을 발동하지 않고
    // 마법진 조준을 시작합니다.
    // =========================================================

    private void OnSkillButtonClick()
    {
        if (SkillManager.Instance == null)
            return;


        if (SkillAimController.Instance == null)
        {
            Debug.LogWarning(
                "SkillAimController를 찾을 수 없습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // 쿨타임 확인
        // -----------------------------------------------------

        if (!SkillManager.Instance.CanUseSkill(
                skillName))
        {
            ShowCentralCooldownMessage();
            return;
        }


        // -----------------------------------------------------
        // 마법진 조준 시작
        // -----------------------------------------------------

        SkillAimController.Instance.StartAiming(
            skillName
        );


        // -----------------------------------------------------
        // 버튼 애니메이션
        // -----------------------------------------------------

        PlayScaleAnimation(
            PunchAnim()
        );


        // -----------------------------------------------------
        // 툴팁 숨김
        // -----------------------------------------------------

        HideTooltip();


        Debug.Log(
            "스킬 선택됨 : " +
            skillName
        );
    }


    // =========================================================
    // 스킬 아이콘 적용
    // =========================================================

    private void ApplySkillIcon()
    {
        if (iconImage == null)
            return;

        if (SkillManager.Instance == null)
            return;


        SkillData skill =
            SkillManager.Instance.skills.Find(
                s =>
                    s != null &&
                    s.skillName == skillName
            );


        if (skill == null)
            return;


        if (
            skill.icon != null &&
            iconImage.sprite != skill.icon
        )
        {
            iconImage.sprite =
                skill.icon;
        }
    }


    // =========================================================
    // 쿨타임 UI
    // =========================================================

    private void UpdateCooldownUI()
    {
        if (SkillManager.Instance == null)
            return;


        bool onCooldown =
            !SkillManager.Instance.CanUseSkill(
                skillName
            );


        float remaining =
            SkillManager.Instance.GetCooldownRemaining(
                skillName
            );


        float normalized =
            SkillManager.Instance.GetCooldownNormalized(
                skillName
            );


        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount =
                normalized;
        }


        if (onCooldown)
        {
            if (cooldownText != null)
            {
                cooldownText.text =
                    remaining > 1f
                        ? Mathf.CeilToInt(
                            remaining
                        ).ToString()
                        : remaining.ToString(
                            "F1"
                        );


                cooldownText.gameObject.SetActive(
                    true
                );
            }


            wasOnCooldown = true;
        }
        else
        {
            if (wasOnCooldown)
            {
                wasOnCooldown = false;

                PlayScaleAnimation(
                    ReadyAnim()
                );
            }


            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount =
                    0f;
            }


            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(
                    false
                );
            }
        }
    }


    // =========================================================
    // 클릭 애니메이션
    // =========================================================

    private IEnumerator PunchAnim()
    {
        if (animatedTransform == null)
            yield break;


        Vector3 big =
            originalScale *
            punchScale;


        float half =
            Mathf.Max(
                0.001f,
                punchDuration * 0.5f
            );


        for (
            float t = 0f;
            t < half;
            t += Time.deltaTime
        )
        {
            animatedTransform.localScale =
                Vector3.Lerp(
                    originalScale,
                    big,
                    t / half
                );

            yield return null;
        }


        for (
            float t = 0f;
            t < half;
            t += Time.deltaTime
        )
        {
            animatedTransform.localScale =
                Vector3.Lerp(
                    big,
                    originalScale,
                    t / half
                );

            yield return null;
        }


        animatedTransform.localScale =
            originalScale;

        scaleAnimation = null;
    }


    // =========================================================
    // 쿨타임 종료 애니메이션
    // =========================================================

    private IEnumerator ReadyAnim()
    {
        if (animatedTransform == null)
            yield break;


        Vector3 big =
            originalScale * 1.15f;


        float dur = 0.2f;


        for (
            float t = 0f;
            t < dur;
            t += Time.deltaTime
        )
        {
            animatedTransform.localScale =
                Vector3.Lerp(
                    originalScale,
                    big,
                    t / dur
                );

            yield return null;
        }


        for (
            float t = 0f;
            t < dur;
            t += Time.deltaTime
        )
        {
            animatedTransform.localScale =
                Vector3.Lerp(
                    big,
                    originalScale,
                    t / dur
                );

            yield return null;
        }


        animatedTransform.localScale =
            originalScale;

        scaleAnimation = null;
    }


    private void PlayScaleAnimation(
        IEnumerator animation
    )
    {
        if (animatedTransform == null)
            return;


        if (scaleAnimation != null)
        {
            StopCoroutine(
                scaleAnimation
            );
        }


        animatedTransform.localScale =
            originalScale;


        scaleAnimation =
            StartCoroutine(animation);
    }


    // =========================================================
    // 중앙 쿨타임 안내
    // =========================================================

    private void ShowCentralCooldownMessage()
    {
        EnsureCentralCooldownMessage();


        if (centralCooldownMessage == null)
            return;


        if (
            centralMessageOwner != null &&
            centralMessageCoroutine != null
        )
        {
            centralMessageOwner.StopCoroutine(
                centralMessageCoroutine
            );
        }


        centralCooldownMessage.text =
            "스킬 쿨타임입니다";


        centralCooldownMessage.gameObject.SetActive(
            true
        );


        centralCooldownMessage.transform.SetAsLastSibling();


        centralMessageOwner =
            this;


        centralMessageCoroutine =
            StartCoroutine(
                HideCentralCooldownMessage()
            );
    }


    private void EnsureCentralCooldownMessage()
    {
        if (centralCooldownMessage != null)
            return;


        Canvas canvas =
            GetComponentInParent<Canvas>();


        if (canvas == null)
            return;


        GameObject messageObject =
            new GameObject(
                "SkillCooldownMessage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );


        messageObject.transform.SetParent(
            canvas.transform,
            false
        );


        RectTransform messageRect =
            messageObject.GetComponent<RectTransform>();


        messageRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );


        messageRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );


        messageRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );


        messageRect.anchoredPosition =
            Vector2.zero;


        messageRect.sizeDelta =
            new Vector2(
                700f,
                100f
            );


        centralCooldownMessage =
            messageObject.GetComponent<
                TextMeshProUGUI
            >();


        centralCooldownMessage.font =
            cooldownMessageFont != null
                ? cooldownMessageFont
                : TMP_Settings.defaultFontAsset;


        centralCooldownMessage.fontSize =
            42f;


        centralCooldownMessage.alignment =
            TextAlignmentOptions.Center;


        centralCooldownMessage.color =
            new Color(
                1f,
                0.82f,
                0.25f,
                1f
            );


        centralCooldownMessage.outlineColor =
            Color.black;


        centralCooldownMessage.outlineWidth =
            0.22f;


        centralCooldownMessage.overflowMode =
            TextOverflowModes.Overflow;


        centralCooldownMessage.raycastTarget =
            false;


        centralCooldownMessage.gameObject.SetActive(
            false
        );
    }


    private IEnumerator HideCentralCooldownMessage()
    {
        yield return new WaitForSecondsRealtime(
            1.1f
        );


        if (centralCooldownMessage != null)
        {
            centralCooldownMessage.gameObject.SetActive(
                false
            );
        }


        centralMessageCoroutine = null;
        centralMessageOwner = null;
    }


    // =========================================================
    // 툴팁 생성
    // =========================================================

    private void EnsureTooltip()
    {
        if (tooltipObject != null)
        {
            if (
                parentCanvas != null &&
                tooltipObject.transform.parent !=
                parentCanvas.transform
            )
            {
                Destroy(
                    tooltipObject
                );

                tooltipObject = null;
                tooltipRect = null;
                tooltipText = null;
                tooltipCanvasGroup = null;
                tooltipOwner = null;
            }
            else
            {
                return;
            }
        }


        if (parentCanvas == null)
        {
            parentCanvas =
                GetComponentInParent<Canvas>();
        }


        if (parentCanvas == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " : Canvas를 찾을 수 없습니다."
            );

            return;
        }


        tooltipObject =
            new GameObject(
                "SkillTooltip",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup)
            );


        tooltipObject.transform.SetParent(
            parentCanvas.transform,
            false
        );


        tooltipRect =
            tooltipObject.GetComponent<
                RectTransform
            >();


        tooltipRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );


        tooltipRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );


        tooltipRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );


        tooltipRect.sizeDelta =
            tooltipSize;


        Image background =
            tooltipObject.GetComponent<Image>();


        background.color =
            tooltipBackgroundColor;


        background.raycastTarget =
            false;


        if (tooltipBackground != null)
        {
            background.sprite =
                tooltipBackground;


            background.type =
                Image.Type.Sliced;
        }


        tooltipCanvasGroup =
            tooltipObject.GetComponent<
                CanvasGroup
            >();


        tooltipCanvasGroup.alpha = 1f;
        tooltipCanvasGroup.interactable = false;
        tooltipCanvasGroup.blocksRaycasts = false;


        GameObject textObject =
            new GameObject(
                "TooltipText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );


        textObject.transform.SetParent(
            tooltipObject.transform,
            false
        );


        RectTransform textRect =
            textObject.GetComponent<
                RectTransform
            >();


        textRect.anchorMin =
            Vector2.zero;


        textRect.anchorMax =
            Vector2.one;


        textRect.offsetMin =
            new Vector2(
                20f,
                16f
            );


        textRect.offsetMax =
            new Vector2(
                -20f,
                -16f
            );


        tooltipText =
            textObject.GetComponent<
                TextMeshProUGUI
            >();


        tooltipText.font = ResolveTooltipFont();


        tooltipText.fontSize =
            tooltipFontSize;


        tooltipText.color =
            tooltipTextColor;


        tooltipText.alignment =
            TextAlignmentOptions.TopLeft;


        tooltipText.enableWordWrapping =
            true;


        tooltipText.enableAutoSizing =
            false;


        tooltipText.overflowMode =
            TextOverflowModes.Overflow;


        tooltipText.raycastTarget =
            false;


        tooltipText.alpha =
            1f;


        tooltipObject.SetActive(
            false
        );
    }


    // =========================================================
    // 툴팁 표시
    // =========================================================

    private void ShowTooltip()
    {
        if (!useTooltip)
            return;


        if (SkillManager.Instance == null)
            return;


        SkillData skill =
            SkillManager.Instance.skills.Find(
                s =>
                    s != null &&
                    s.skillName == skillName
            );


        if (skill == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " : SkillData를 찾을 수 없습니다. " +
                "skillName = " +
                skillName
            );

            return;
        }


        EnsureTooltip();


        if (
            tooltipObject == null ||
            tooltipRect == null ||
            tooltipText == null
        )
        {
            return;
        }


        tooltipText.text =
            BuildTooltipText(skill);

        // 공용 툴팁을 다른 스킬 버튼이 재사용해도 현재 버튼의 한글 글꼴을 적용한다.
        tooltipText.font = ResolveTooltipFont();


        tooltipText.color =
            tooltipTextColor;


        tooltipText.fontSize =
            tooltipFontSize;


        tooltipText.alpha =
            1f;


        tooltipRect.sizeDelta =
            tooltipSize;


        tooltipOwner =
            this;


        tooltipObject.SetActive(
            true
        );


        tooltipObject.transform.SetAsLastSibling();


        PositionTooltip();
    }

    private TMP_FontAsset ResolveTooltipFont()
    {
        if (tooltipFont != null)
            return tooltipFont;

        if (cooldownMessageFont != null)
            return cooldownMessageFont;

        return TMP_Settings.defaultFontAsset;
    }


    // =========================================================
    // 툴팁 내용
    // =========================================================

    private string BuildTooltipText(
        SkillData skill
    )
    {
        SkillManager manager =
            SkillManager.Instance;


        string title =
            skill.skillName;


        string effect;
        string target;
        string range;
        string durationLine = "";


        switch (skill.skillName)
        {
            case "Fireball":

                effect =
                    $"마법진 범위 안의 몬스터에게 최초 " +
                    $"{manager.fireInitialDamage} 피해\n" +
                    $"이후 {manager.fireDotInterval:0.##}초마다 " +
                    $"{manager.fireDotDamage} 피해";


                target =
                    "적용 대상: 마법진 범위 안의 몬스터";


                range =
                    "범위: 마법진 내부";


                durationLine =
                    $"지속 시간: " +
                    $"{manager.fireDotDuration:0.##}초";

                break;


            case "Ice Attack":

                effect =
                    "마법진 범위 안의 몬스터를 얼려 이동을 멈춥니다.";


                target =
                    "적용 대상: 마법진 범위 안의 몬스터";


                range =
                    "범위: 마법진 내부";


                durationLine =
                    "지속 시간: 3초";

                break;


            case "Lightning":

                effect =
                    $"마법진 범위 안의 최대 " +
                    $"{manager.lightningMaxTargets}명의 " +
                    $"몬스터에게 {manager.lightningDamage} 피해";


                target =
                    $"적용 대상: 마법진 범위 안의 최대 " +
                    $"{manager.lightningMaxTargets}명";


                range =
                    "범위: 마법진 내부";

                break;


            default:

                effect =
                    string.IsNullOrWhiteSpace(
                        skill.description
                    )
                    ? "스킬 설명이 설정되지 않았습니다."
                    : skill.description;


                target =
                    "적용 대상: 설정값 기준";


                range =
                    "범위: 설정값 기준";


                if (skill.duration > 0f)
                {
                    durationLine =
                        $"지속 시간: " +
                        $"{skill.duration:0.##}초";
                }

                break;
        }


        string cooldownLine =
            $"재사용 대기시간: " +
            $"{skill.cooldown:0.##}초";


        if (!string.IsNullOrEmpty(durationLine))
        {
            return
                $"<size=28><b>{title}</b></size>\n\n" +
                $"효과: {effect}\n" +
                $"{target}\n" +
                $"{range}\n" +
                $"{durationLine}\n" +
                $"{cooldownLine}";
        }


        return
            $"<size=28><b>{title}</b></size>\n\n" +
            $"효과: {effect}\n" +
            $"{target}\n" +
            $"{range}\n" +
            $"{cooldownLine}";
    }


    // =========================================================
    // 툴팁 위치
    // =========================================================

    private void PositionTooltip()
    {
        if (
            parentCanvas == null ||
            tooltipRect == null ||
            rectTransform == null
        )
        {
            return;
        }


        RectTransform canvasRect =
            parentCanvas.GetComponent<
                RectTransform
            >();


        if (canvasRect == null)
            return;


        Camera uiCamera =
            parentCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : parentCanvas.worldCamera;


        Vector3[] corners =
            new Vector3[4];


        rectTransform.GetWorldCorners(
            corners
        );


        Vector3 topCenter =
            (corners[1] + corners[2]) *
            0.5f;


        Vector3 bottomCenter =
            (corners[0] + corners[3]) *
            0.5f;


        Vector2 topScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                topCenter
            );


        Vector2 bottomScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                bottomCenter
            );


        float tooltipWidth =
            tooltipRect.rect.width;


        float tooltipHeight =
            tooltipRect.rect.height;


        float screenX =
            topScreen.x;


        float screenY =
            topScreen.y +
            tooltipHeight * 0.5f +
            tooltipMargin;


        if (
            topScreen.y +
            tooltipHeight +
            tooltipMargin >
            Screen.height
        )
        {
            screenY =
                bottomScreen.y -
                tooltipHeight * 0.5f -
                tooltipMargin;
        }


        float halfWidth =
            tooltipWidth * 0.5f;


        screenX =
            Mathf.Clamp(
                screenX,
                halfWidth +
                tooltipMargin,
                Screen.width -
                halfWidth -
                tooltipMargin
            );


        float halfHeight =
            tooltipHeight * 0.5f;


        screenY =
            Mathf.Clamp(
                screenY,
                halfHeight +
                tooltipMargin,
                Screen.height -
                halfHeight -
                tooltipMargin
            );


        Vector2 finalScreenPoint =
            new Vector2(
                screenX,
                screenY
            );


        if (
            RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                finalScreenPoint,
                uiCamera,
                out Vector2 localPoint
            )
        )
        {
            tooltipRect.anchoredPosition =
                localPoint;
        }
    }


    // =========================================================
    // 툴팁 숨김
    // =========================================================

    private void HideTooltip()
    {
        if (
            tooltipOwner != null &&
            tooltipOwner != this
        )
        {
            return;
        }


        if (tooltipObject != null)
        {
            tooltipObject.SetActive(
                false
            );
        }


        tooltipOwner = null;
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        pointerInside = false;


        if (tooltipShowCoroutine != null)
        {
            StopCoroutine(
                tooltipShowCoroutine
            );

            tooltipShowCoroutine = null;
        }


        if (scaleAnimation != null)
        {
            StopCoroutine(
                scaleAnimation
            );
        }


        if (animatedTransform != null)
        {
            animatedTransform.localScale =
                originalScale;
        }


        scaleAnimation = null;


        HideTooltip();


        if (centralMessageOwner == this)
        {
            if (centralCooldownMessage != null)
            {
                centralCooldownMessage.gameObject.SetActive(
                    false
                );
            }


            centralMessageCoroutine = null;
            centralMessageOwner = null;
        }
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        if (tooltipOwner == this)
        {
            if (tooltipObject != null)
            {
                Destroy(
                    tooltipObject
                );
            }


            tooltipObject = null;
            tooltipRect = null;
            tooltipText = null;
            tooltipCanvasGroup = null;
            tooltipOwner = null;
        }
    }
}
