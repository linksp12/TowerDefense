using System.Collections;
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
                popupOriginalScale = rect.localScale;
            }

            bossInfoPanel.SetActive(false);
        }

        if (topBossHPPanel == null)
        {
            CreateRuntimeTopBossHPBar();
        }

        if (topBossHPPanel != null)
        {
            topBossHPPanel.SetActive(false);
        }
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
        // 현재 보스가 없으면 종료
        if (currentBoss == null)
            return;


        // =====================================================
        // 보스가 죽었는지 확인
        // =====================================================
        if (currentBoss.IsDead)
        {
            HideBossInfo();
            return;
        }


        // =====================================================
        // 큰 팝업 HP 업데이트
        // =====================================================
        if (bossInfoPanel != null &&
            bossInfoPanel.activeSelf)
        {
            UpdatePopupHP();
        }


        // =====================================================
        // 상단 HP바 업데이트
        // =====================================================
        if (topBossHPPanel != null &&
            topBossHPPanel.activeSelf)
        {
            UpdateTopHP();
        }
    }


    // =========================================================
    // 보스 등장
    // =========================================================
    public void ShowBossInfo(MonsterHealth boss)
    {
        // 보스가 없으면 종료
        if (boss == null)
            return;

        // 죽은 보스면 표시하지 않음
        if (boss.IsDead)
            return;


        // =====================================================
        // 기존 코루틴 정리
        // =====================================================
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }


        // =====================================================
        // 현재 보스 저장
        // =====================================================
        currentBoss = boss;


        // =====================================================
        // 기존 상단 HP바 숨기기
        // 새로운 보스가 등장했기 때문
        // =====================================================
        if (topBossHPPanel != null)
        {
            topBossHPPanel.SetActive(false);
        }


        // =====================================================
        // 보스 정보 설정
        // =====================================================
        SetBossData(boss);


        // =====================================================
        // 팝업 시작
        // =====================================================
        popupCoroutine =
            StartCoroutine(BossPopupCoroutine());
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


        if (defenseText != null)
        {
            defenseText.text =
                "방어력 : " + boss.defense;
        }


        if (magicResistanceText != null)
        {
            magicResistanceText.text =
                "마법 저항 : " + boss.magicResistance;
        }


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
}
