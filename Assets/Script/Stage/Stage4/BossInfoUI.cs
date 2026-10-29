using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossInfoUI : MonoBehaviour
{
    public static BossInfoUI Instance;

    // =========================================================
    // 큰 보스 등장 팝업
    // =========================================================
    [Header("Boss Popup Panel")]
    [Tooltip("보스 등장 시 크게 표시되는 Panel")]
    public GameObject bossInfoPanel;

    [Tooltip("보스 초상화")]
    public Image portraitImage;

    [Tooltip("보스 이름")]
    public TMP_Text bossNameText;

    [Tooltip("보스 HP Slider")]
    public Slider hpSlider;

    [Tooltip("보스 HP 숫자")]
    public TMP_Text hpText;

    [Tooltip("보스 방어력")]
    public TMP_Text defenseText;

    [Tooltip("보스 마법 저항력")]
    public TMP_Text magicResistanceText;


    // =========================================================
    // 상단 고정 보스 HP바
    // =========================================================
    [Header("Top Boss HP Bar")]
    [Tooltip("팝업이 끝난 후 화면 상단에 계속 표시되는 Panel")]
    public GameObject topBossHPPanel;

    [Tooltip("상단 보스 이름")]
    public TMP_Text topBossNameText;

    [Tooltip("상단 보스 HP Slider")]
    public Slider topHpSlider;

    [Tooltip("상단 보스 HP 숫자")]
    public TMP_Text topHpText;

    [Tooltip("상단 보스 초상화")]
    public Image topPortraitImage;


    // =========================================================
    // 팝업 설정
    // =========================================================
    [Header("Popup Settings")]
    [Tooltip("큰 팝업이 유지되는 시간")]
    public float popupDuration = 3f;

    [Tooltip("팝업이 커졌다가 나타나는 시간")]
    public float popupOpenDuration = 0.3f;

    [Tooltip("큰 팝업이 사라지고 상단바로 전환되는 시간")]
    public float popupCloseDuration = 0.4f;


    // =========================================================
    // 현재 보스
    // =========================================================
    private MonsterHealth currentBoss;

    // 팝업 코루틴
    private Coroutine popupCoroutine;


    // =========================================================
    // 원래 Panel 크기
    // =========================================================
    private Vector3 popupOriginalScale;

    // 위쪽 보스 체력 UI 전체 크기(배치·글씨 비율은 그대로 두고 크기만 줄인다)
    private const float TopBarScale = 0.75f;

    /// <summary>위쪽 보스 체력 UI가 보이는 중인지(배너가 겹치지 않게 아래로 내릴 때 쓴다).</summary>
    public bool IsTopBarVisible =>
        (bossInfoPanel != null && bossInfoPanel.activeSelf) || (topBossHPPanel != null && topBossHPPanel.activeSelf);

    private sealed class BossHealthEntry
    {
        public MonsterHealth boss;
        public int maxHp;
    }

    private readonly List<BossHealthEntry> registeredBosses =
        new List<BossHealthEntry>();

    private bool isSuppressedByTowerPanel;


    // =========================================================
    // Awake
    // =========================================================
    private void Awake()
    {
        // Singleton
        Instance = this;

        // ---------------------------------------------------------
        // 큰 팝업 저장
        // ---------------------------------------------------------
        if (bossInfoPanel != null)
        {
            RectTransform rect =
                bossInfoPanel.GetComponent<RectTransform>();

            if (rect != null)
            {
                ConfigureBossInfoPanelAsTopBar(rect);
                popupOriginalScale = Vector3.one * TopBarScale;
                rect.localScale = popupOriginalScale;
            }

            bossInfoPanel.SetActive(false);
        }

        if (topBossHPPanel != null)
        {
            topBossHPPanel.SetActive(false);
        }
    }

    private void ConfigureBossInfoPanelAsTopBar(RectTransform panelRect)
    {
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -8f);
        panelRect.sizeDelta = new Vector2(640f, 112f);
        panelRect.localScale = Vector3.one;

        // 자식은 패널 위쪽 기준 아래로 56(패널 중앙)에 맞춰 배치한다.
        RectTransform background = bossInfoPanel.transform.Find("Background") as RectTransform;
        SetTopBarRect(background, new Vector2(0f, -56f), new Vector2(640f, 112f));
        SetTopBarRect(
            portraitImage != null ? portraitImage.rectTransform : null,
            new Vector2(-262f, -56f),
            new Vector2(72f, 72f)
        );
        SetTopBarRect(
            bossNameText != null ? bossNameText.rectTransform : null,
            new Vector2(40f, -30f),
            new Vector2(460f, 30f)
        );
        SetTopBarRect(
            hpSlider != null ? hpSlider.GetComponent<RectTransform>() : null,
            new Vector2(40f, -60f),
            new Vector2(460f, 28f)
        );
        SetTopBarRect(
            hpText != null ? hpText.rectTransform : null,
            new Vector2(40f, -60f),
            new Vector2(300f, 26f)
        );
        SetTopBarRect(
            defenseText != null ? defenseText.rectTransform : null,
            new Vector2(-110f, -88f),
            new Vector2(160f, 22f)
        );
        SetTopBarRect(
            magicResistanceText != null
                ? magicResistanceText.rectTransform
                : null,
            new Vector2(60f, -88f),
            new Vector2(180f, 22f)
        );

        TMP_FontAsset maplestoryFont =
            defenseText != null ? defenseText.font : null;

        if (maplestoryFont != null)
        {
            if (bossNameText != null)
                bossNameText.font = maplestoryFont;

            if (hpText != null)
                hpText.font = maplestoryFont;
        }

        ApplyPixelSkin(background);
    }

    // 전투 화면 공통 픽셀 스타일: 금테 패널, 초상화 홈, 픽셀 체력바, 글자 정리.
    private void ApplyPixelSkin(RectTransform background)
    {
        Image backgroundImage = background != null ? background.GetComponent<Image>() : null;
        if (!BattleUiSkin.Skin(backgroundImage, BattleUiSkin.Frame, BattleUiSkin.PixelScale))
            return;

        if (portraitImage != null)
        {
            portraitImage.preserveAspect = true;
            var well = new GameObject("PortraitWell", typeof(RectTransform), typeof(Image));
            well.layer = portraitImage.gameObject.layer;
            well.transform.SetParent(portraitImage.transform.parent, false);
            well.transform.SetSiblingIndex(portraitImage.transform.GetSiblingIndex());
            SetTopBarRect(well.GetComponent<RectTransform>(), new Vector2(-262f, -56f), new Vector2(84f, 84f));
            var wellImage = well.GetComponent<Image>();
            BattleUiSkin.Skin(wellImage, BattleUiSkin.Inset, BattleUiSkin.PixelScale);
            wellImage.raycastTarget = false;
        }

        if (hpSlider != null)
        {
            Image barBackground = hpSlider.transform.Find("Background")?.GetComponent<Image>();
            Image fill = hpSlider.fillRect != null ? hpSlider.fillRect.GetComponent<Image>() : null;
            BattleUiSkin.Skin(barBackground, BattleUiSkin.HpFrame, 2f);
            if (BattleUiSkin.Skin(fill, BattleUiSkin.HpFill, 2f))
                fill.color = new Color32(216, 64, 47, 255);
            // 기본 Slider는 높이의 가운데 절반만 쓰므로 막대 전체 높이를 쓰게 맞춘다.
            if (barBackground != null)
            {
                RectTransform backgroundRect = barBackground.rectTransform;
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
            }
            if (hpSlider.fillRect != null && hpSlider.fillRect.parent is RectTransform fillArea)
            {
                fillArea.anchorMin = Vector2.zero;
                fillArea.anchorMax = Vector2.one;
                fillArea.offsetMin = new Vector2(4f, 4f);
                fillArea.offsetMax = new Vector2(-4f, -4f);
                hpSlider.fillRect.sizeDelta = Vector2.zero;
                hpSlider.fillRect.anchoredPosition = Vector2.zero;
            }
            // 보스 체력바는 표시 전용이므로 손잡이를 숨긴다.
            if (hpSlider.handleRect != null)
                hpSlider.handleRect.parent.gameObject.SetActive(false);
            hpSlider.interactable = false;
        }

        StyleText(bossNameText, 24f, new Color32(247, 214, 126, 255), TextAlignmentOptions.MidlineLeft);
        StyleText(hpText, 17f, Color.white, TextAlignmentOptions.Center);
        StyleText(defenseText, 17f, new Color32(191, 169, 130, 255), TextAlignmentOptions.MidlineLeft);
        StyleText(magicResistanceText, 17f, new Color32(191, 169, 130, 255), TextAlignmentOptions.MidlineLeft);
    }

    private static void StyleText(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineColor = new Color32(20, 10, 6, 255);
        text.outlineWidth = 0.16f;
    }

    private static void SetTopBarRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private void CreateRuntimeTopBossHPBar()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null && bossInfoPanel != null)
        {
            canvas = bossInfoPanel.GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning(
                "BossInfoUI: 상단 보스 HP바를 생성할 Canvas가 없습니다."
            );

            return;
        }

        int uiLayer = canvas.gameObject.layer;

        topBossHPPanel = new GameObject(
            "RuntimeTopBossHPPanel",
            typeof(RectTransform),
            typeof(Image)
        );

        topBossHPPanel.layer = uiLayer;
        topBossHPPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect =
            topBossHPPanel.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);
        panelRect.sizeDelta = new Vector2(560f, 86f);
        panelRect.localScale = Vector3.one * TopBarScale;

        Image panelImage = topBossHPPanel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.92f);
        panelImage.raycastTarget = false;

        GameObject portraitObject = new GameObject(
            "BossPortrait",
            typeof(RectTransform),
            typeof(Image)
        );

        portraitObject.layer = uiLayer;
        portraitObject.transform.SetParent(
            topBossHPPanel.transform,
            false
        );

        RectTransform portraitRect =
            portraitObject.GetComponent<RectTransform>();

        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.anchoredPosition = new Vector2(12f, 0f);
        portraitRect.sizeDelta = new Vector2(64f, 64f);

        topPortraitImage = portraitObject.GetComponent<Image>();
        topPortraitImage.preserveAspect = true;
        topPortraitImage.raycastTarget = false;

        GameObject nameObject = new GameObject(
            "BossName",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        nameObject.layer = uiLayer;
        nameObject.transform.SetParent(topBossHPPanel.transform, false);

        RectTransform nameRect =
            nameObject.GetComponent<RectTransform>();

        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(86f, -39f);
        nameRect.offsetMax = new Vector2(-14f, -7f);

        topBossNameText = nameObject.GetComponent<TextMeshProUGUI>();
        topBossNameText.fontSize = 21f;
        topBossNameText.fontStyle = FontStyles.Bold;
        topBossNameText.color = Color.white;
        topBossNameText.alignment = TextAlignmentOptions.Center;
        topBossNameText.raycastTarget = false;

        GameObject sliderObject = new GameObject(
            "BossHPSlider",
            typeof(RectTransform),
            typeof(Slider)
        );

        sliderObject.layer = uiLayer;
        sliderObject.transform.SetParent(topBossHPPanel.transform, false);

        RectTransform sliderRect =
            sliderObject.GetComponent<RectTransform>();

        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.offsetMin = new Vector2(86f, 12f);
        sliderRect.offsetMax = new Vector2(-14f, 36f);

        GameObject backgroundObject = new GameObject(
            "Background",
            typeof(RectTransform),
            typeof(Image)
        );

        backgroundObject.layer = uiLayer;
        backgroundObject.transform.SetParent(sliderObject.transform, false);

        RectTransform backgroundRect =
            backgroundObject.GetComponent<RectTransform>();

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.16f, 0.16f, 0.18f, 1f);
        backgroundImage.raycastTarget = false;

        GameObject fillObject = new GameObject(
            "Fill",
            typeof(RectTransform),
            typeof(Image)
        );

        fillObject.layer = uiLayer;
        fillObject.transform.SetParent(sliderObject.transform, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.78f, 0.08f, 0.08f, 1f);
        fillImage.raycastTarget = false;

        topHpSlider = sliderObject.GetComponent<Slider>();
        topHpSlider.transition = Selectable.Transition.None;
        topHpSlider.interactable = false;
        topHpSlider.fillRect = fillRect;
        topHpSlider.targetGraphic = fillImage;
        topHpSlider.direction = Slider.Direction.LeftToRight;

        GameObject hpTextObject = new GameObject(
            "HPText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        hpTextObject.layer = uiLayer;
        hpTextObject.transform.SetParent(sliderObject.transform, false);

        RectTransform hpTextRect =
            hpTextObject.GetComponent<RectTransform>();

        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.offsetMin = Vector2.zero;
        hpTextRect.offsetMax = Vector2.zero;

        topHpText = hpTextObject.GetComponent<TextMeshProUGUI>();
        topHpText.fontSize = 15f;
        topHpText.fontStyle = FontStyles.Bold;
        topHpText.color = Color.white;
        topHpText.alignment = TextAlignmentOptions.Center;
        topHpText.raycastTarget = false;
    }


    // =========================================================
    // Update
    // =========================================================
    private void Update()
    {
        UpdateCombinedTopHP();
    }


    // =========================================================
    // 보스 등장
    // =========================================================
    public void ShowBossInfo(MonsterHealth boss)
    {
        RegisterBoss(boss);
    }

    public void RegisterBoss(MonsterHealth boss)
    {
        if (boss == null || !boss.isBoss || boss.IsDead)
            return;

        for (int i = 0; i < registeredBosses.Count; i++)
        {
            if (registeredBosses[i].boss == boss)
                return;
        }

        registeredBosses.Add(
            new BossHealthEntry
            {
                boss = boss,
                maxHp = Mathf.Max(1, boss.maxHp)
            }
        );

        if (bossInfoPanel != null)
            bossInfoPanel.SetActive(false);

        if (topBossHPPanel != null)
            topBossHPPanel.SetActive(false);

        if (bossInfoPanel != null && !isSuppressedByTowerPanel)
            bossInfoPanel.SetActive(true);

        string displayName = string.IsNullOrWhiteSpace(boss.bossName)
            ? "BOSS"
            : boss.bossName.ToUpperInvariant();

        if (bossNameText != null)
        {
            bossNameText.text =
                "<color=#FF8A6A>BOSS</color>  " + displayName +
                (registeredBosses.Count > 1
                    ? " × " + registeredBosses.Count
                    : string.Empty);
        }

        // 보스 종류가 처음 나타나면 등장 카드(초상화·이름·별칭)를 띄운다.
        // 카드를 띄우지 못한 첫 보스는 웨이브와 같은 리본 배너로 알린다.
        bool carded = BossIntroCard.Show(boss);
        if (registeredBosses.Count == 1)
        {
            if (!carded)
            {
                UIManager waveUi = FindAnyObjectByType<UIManager>();
                if (waveUi != null)
                    waveUi.ShowBossBanner(displayName);
            }
            CombatFeedback.BossAppeared();
        }

        // 다른 씬에서 옮겨 온 패널에 이전 보스 그림이 남아 있을 수 있으므로 첫 보스로 덮어쓴다.
        if (portraitImage != null && (registeredBosses.Count == 1 || portraitImage.sprite == null))
            portraitImage.sprite = boss.bossPortrait;

        ShowDamageTaken(boss);

        UpdateCombinedTopHP();
    }

    private void UpdateCombinedTopHP()
    {
        if (registeredBosses.Count == 0)
            return;

        int totalMaxHp = 0;
        int totalCurrentHp = 0;
        bool anyAlive = false;

        for (int i = 0; i < registeredBosses.Count; i++)
        {
            BossHealthEntry entry = registeredBosses[i];
            totalMaxHp += entry.maxHp;

            if (entry.boss != null && !entry.boss.IsDead)
            {
                totalCurrentHp += Mathf.Max(0, entry.boss.CurrentHp);
                anyAlive = true;
            }
        }

        // 보스가 모두 쓰러지거나 기지에 닿아 사라지면 체력 패널을 닫는다.
        if (!anyAlive)
        {
            if (bossInfoPanel != null && bossInfoPanel.activeSelf)
                bossInfoPanel.SetActive(false);
            return;
        }

        if (bossInfoPanel != null &&
            !isSuppressedByTowerPanel &&
            !bossInfoPanel.activeSelf)
        {
            bossInfoPanel.SetActive(true);
        }

        if (hpSlider != null)
        {
            hpSlider.maxValue = Mathf.Max(1, totalMaxHp);
            hpSlider.value = totalCurrentHp;
        }

        if (hpText != null)
        {
            hpText.text =
                totalCurrentHp.ToString("N0") +
                " / " +
                totalMaxHp.ToString("N0");
        }
    }

    public void SetSuppressedByTowerPanel(bool suppressed)
    {
        isSuppressedByTowerPanel = suppressed;

        if (bossInfoPanel == null)
            return;

        bool hasActiveBoss = false;

        for (int i = 0; i < registeredBosses.Count; i++)
        {
            MonsterHealth boss = registeredBosses[i].boss;
            if (boss != null && !boss.IsDead)
            {
                hasActiveBoss = true;
                break;
            }
        }

        bossInfoPanel.SetActive(!suppressed && hasActiveBoss);
    }


    // =========================================================
    // 보스 데이터 설정
    // =========================================================
    private void SetBossData(MonsterHealth boss)
    {
        if (boss == null)
            return;


        // =====================================================
        // 큰 팝업
        // =====================================================
        if (bossNameText != null)
        {
            bossNameText.text = boss.bossName;
        }


        if (portraitImage != null)
        {
            portraitImage.sprite = boss.bossPortrait;
        }


        ShowDamageTaken(boss);




        // =====================================================
        // 상단 HP바
        // =====================================================
        if (topBossNameText != null)
        {
            topBossNameText.text = boss.bossName;
        }


        if (topPortraitImage != null)
        {
            topPortraitImage.sprite = boss.bossPortrait;
        }


        // =====================================================
        // HP
        // =====================================================
        UpdatePopupHP();
        UpdateTopHP();
    }


    // =========================================================
    // 보스 팝업 연출
    // =========================================================
    private IEnumerator BossPopupCoroutine()
    {
        if (bossInfoPanel == null)
            yield break;


        RectTransform rect =
            bossInfoPanel.GetComponent<RectTransform>();


        // =====================================================
        // Panel 활성화
        // =====================================================
        bossInfoPanel.SetActive(true);


        // =====================================================
        // 시작 크기
        // =====================================================
        if (rect != null)
        {
            rect.localScale =
                popupOriginalScale * 0.7f;
        }


        // =====================================================
        // 0.7 → 1.0 확대 등장
        // =====================================================
        float timer = 0f;


        while (timer < popupOpenDuration)
        {
            // 보스가 죽었으면 종료
            if (currentBoss == null ||
                currentBoss.IsDead)
            {
                HideBossInfo();
                yield break;
            }


            timer += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer / popupOpenDuration
                );


            // 부드러운 확대
            t = Mathf.SmoothStep(0f, 1f, t);


            if (rect != null)
            {
                rect.localScale =
                    Vector3.Lerp(
                        popupOriginalScale * 0.7f,
                        popupOriginalScale,
                        t
                    );
            }


            yield return null;
        }


        // 최종 크기
        if (rect != null)
        {
            rect.localScale =
                popupOriginalScale;
        }


        // =====================================================
        // 3초 동안 팝업 유지
        // =====================================================
        float remainTime = popupDuration;


        while (remainTime > 0f)
        {
            // 보스가 죽었으면 바로 종료
            if (currentBoss == null ||
                currentBoss.IsDead)
            {
                HideBossInfo();
                yield break;
            }


            remainTime -= Time.deltaTime;


            yield return null;
        }


        // =====================================================
        // 큰 팝업 닫기
        // =====================================================
        yield return StartCoroutine(
            ClosePopupAndShowTopBar()
        );
    }


    // =========================================================
    // 팝업 닫고 상단 HP바 표시
    // =========================================================
    private IEnumerator ClosePopupAndShowTopBar()
    {
        if (bossInfoPanel == null)
            yield break;


        RectTransform rect =
            bossInfoPanel.GetComponent<RectTransform>();


        float timer = 0f;


        // =====================================================
        // 1 → 0.7로 축소
        // =====================================================
        while (timer < popupCloseDuration)
        {
            if (currentBoss == null ||
                currentBoss.IsDead)
            {
                HideBossInfo();
                yield break;
            }


            timer += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer / popupCloseDuration
                );


            t = Mathf.SmoothStep(0f, 1f, t);


            if (rect != null)
            {
                rect.localScale =
                    Vector3.Lerp(
                        popupOriginalScale,
                        popupOriginalScale * 0.7f,
                        t
                    );
            }


            yield return null;
        }


        // =====================================================
        // 큰 팝업 숨기기
        // =====================================================
        bossInfoPanel.SetActive(false);


        // =====================================================
        // 상단 HP바 표시
        // =====================================================
        ShowTopBossHP();
    }


    // =========================================================
    // 상단 보스 HP바 표시
    // =========================================================
    private void ShowTopBossHP()
    {
        if (currentBoss == null)
            return;


        if (topBossHPPanel != null)
        {
            topBossHPPanel.SetActive(true);
        }


        // 이름
        if (topBossNameText != null)
        {
            topBossNameText.text =
                currentBoss.bossName;
        }


        // 초상화
        if (topPortraitImage != null)
        {
            topPortraitImage.sprite =
                currentBoss.bossPortrait;
        }


        // HP
        UpdateTopHP();
    }


    // =========================================================
    // 팝업 HP 업데이트
    // =========================================================
    private void UpdatePopupHP()
    {
        if (currentBoss == null)
            return;


        // Slider
        if (hpSlider != null)
        {
            hpSlider.maxValue =
                currentBoss.maxHp;

            hpSlider.value =
                currentBoss.CurrentHp;
        }


        // HP Text
        if (hpText != null)
        {
            hpText.text =
                currentBoss.CurrentHp.ToString("N0")
                + " / "
                + currentBoss.maxHp.ToString("N0");
        }
    }


    // =========================================================
    // 상단 HP 업데이트
    // =========================================================
    private void UpdateTopHP()
    {
        if (currentBoss == null)
            return;


        // Slider
        if (topHpSlider != null)
        {
            topHpSlider.maxValue =
                currentBoss.maxHp;

            topHpSlider.value =
                currentBoss.CurrentHp;
        }


        // HP Text
        if (topHpText != null)
        {
            topHpText.text =
                currentBoss.CurrentHp.ToString("N0")
                + " / "
                + currentBoss.maxHp.ToString("N0");
        }
    }


    // =========================================================
    // 보스 정보 전체 종료
    // =========================================================
    public void HideBossInfo()
    {
        // 현재 보스 초기화
        currentBoss = null;


        // 팝업 코루틴 중지
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }


        // 큰 팝업 숨기기
        if (bossInfoPanel != null)
        {
            bossInfoPanel.SetActive(false);


            RectTransform rect =
                bossInfoPanel.GetComponent<RectTransform>();


            if (rect != null)
            {
                rect.localScale =
                    popupOriginalScale;
            }
        }


        // 상단 HP바 숨기기
        if (topBossHPPanel != null)
        {
            topBossHPPanel.SetActive(false);
        }
    }

    // 실제 전투 규칙(방어 특성)대로 받는 피해 비율을 보여 준다.
    private void ShowDamageTaken(MonsterHealth boss)
    {
        int physical = Mathf.RoundToInt(100f * (ArmorRules.Resists(boss.ArmorTrait, DamageKind.Physical) ? 1f - ArmorRules.Reduction : 1f));
        int magic = Mathf.RoundToInt(100f * (ArmorRules.Resists(boss.ArmorTrait, DamageKind.Magic) ? 1f - ArmorRules.Reduction : 1f));
        if (defenseText != null)
            defenseText.text = $"물리 피해 {physical}%";
        if (magicResistanceText != null)
            magicResistanceText.text = $"마법 피해 {magic}%";
    }
}
