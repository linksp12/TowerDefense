using System;
using System.Collections;
using LastOfTheTower.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메인 화면 연출. 배경이 천천히 움직이고 불씨·안개·번개가 흐르며,
/// 검은 화면 → 배경 → 타이틀 빛 → 메뉴 순서로 등장한다. 버튼 동작은 MainMenuManager가 맡는다.
/// </summary>
public sealed class MainMenuPresentation : MonoBehaviour
{
    [SerializeField] private RectTransform background;
    [Tooltip("배경 그림 안 타이틀 글자 자리(배경 기준 0~1). 빛이 이 자리에서 맥동한다.")]
    [SerializeField] private Rect titleArea = new Rect(0.33f, 0.6f, 0.34f, 0.36f);
    [SerializeField] private Button[] menuButtons;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_FontAsset font;

    private static readonly Color Gold = new Color32(255, 214, 120, 255);
    private static readonly Color Muted = new Color32(196, 184, 160, 255);

    private Image blackout, titleGlow, flash, backdrop;
    private RawImage fogA, fogB;
    private CanvasGroup[] buttonGroups;
    private TextMeshProUGUI continueInfo;
    private float introTime;
    private float nextLightning;
    private bool leaving;

    private void Awake()
    {
        BuildLayers();
        RefreshContinue();
    }

    private void Start()
    {
        StartCoroutine(Intro());
        nextLightning = Time.unscaledTime + UnityEngine.Random.Range(5f, 9f);
    }

    private void BuildLayers()
    {
        Transform canvas = transform;

        // 그림(3:2)을 자르지 않고 전부 보이게 두고, 남는 양옆은 같은 그림을 어둡게 크게 깔아 이어 준다.
        var source = background.GetComponent<Image>();
        backdrop = UiFx.Image("BackdropFill", canvas, source.sprite, new Color(0.32f, 0.3f, 0.42f, 1f));
        UiFx.Stretch(backdrop.rectTransform);
        var backdropFit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
        backdropFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        backdropFit.aspectRatio = source.sprite.rect.width / source.sprite.rect.height;
        backdrop.transform.SetSiblingIndex(background.GetSiblingIndex());
        foreach (float side in new[] { 0f, 1f })
        {
            var edge = UiFx.Image(side == 0f ? "EdgeFadeLeft" : "EdgeFadeRight", background, UiFx.HorizontalFade, new Color(0.02f, 0.02f, 0.05f, 0.9f));
            var rect = edge.rectTransform;
            rect.anchorMin = new Vector2(side, 0f);
            rect.anchorMax = new Vector2(side, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(150f, 0f);
            rect.anchoredPosition = Vector2.zero;
            // 오른쪽은 좌우를 뒤집어 바깥쪽이 어둡게 한다.
            rect.localScale = new Vector3(side == 0f ? 1f : -1f, 1f, 1f);
        }
        int backgroundIndex = background.GetSiblingIndex();

        // 화면 테두리를 어둡게, 아래쪽은 메뉴가 잘 보이게 더 어둡게
        var vignette = UiFx.Image("Vignette", canvas, UiFx.Vignette, new Color(1f, 1f, 1f, 0.85f));
        UiFx.Stretch(vignette.rectTransform);
        vignette.transform.SetSiblingIndex(backgroundIndex + 1);
        var bottom = UiFx.Image("BottomShade", canvas, UiFx.VerticalFade, new Color(0.02f, 0.02f, 0.05f, 0.92f));
        bottom.rectTransform.anchorMin = Vector2.zero;
        bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottom.rectTransform.pivot = new Vector2(0.5f, 0f);
        bottom.rectTransform.sizeDelta = new Vector2(0f, 300f);
        bottom.transform.SetSiblingIndex(backgroundIndex + 2);

        // 흐르는 안개 두 겹(속도·높이를 달리해 깊이감)
        fogA = MakeFog("FogFar", 0.10f, 260f, new Color(0.62f, 0.55f, 0.85f, 0.16f), backgroundIndex + 1);
        fogB = MakeFog("FogNear", 0.0f, 200f, new Color(0.75f, 0.62f, 0.7f, 0.2f), backgroundIndex + 2);

        // 타이틀 뒤에서 맥동하는 따뜻한 빛
        titleGlow = UiFx.Image("TitleGlow", background, UiFx.SoftCircle, new Color(1f, 0.78f, 0.42f, 0f));
        var glowRect = titleGlow.rectTransform;
        glowRect.anchorMin = new Vector2(titleArea.xMin, titleArea.yMin);
        glowRect.anchorMax = new Vector2(titleArea.xMax, titleArea.yMax);
        glowRect.offsetMin = new Vector2(-160f, -110f);
        glowRect.offsetMax = new Vector2(160f, 110f);

        // 떠오르는 불씨와 반짝이는 빛 알갱이
        var embers = UiParticleField.Create("Embers", canvas);
        embers.transform.SetSiblingIndex(backgroundIndex + 3);
        embers.palette = new[] { new Color(1f, 0.55f, 0.18f, 0.95f), new Color(1f, 0.78f, 0.3f, 0.9f), new Color(1f, 0.35f, 0.12f, 0.85f) };
        embers.sizeRange = new Vector2(3f, 7f);
        embers.speedMin = new Vector2(-14f, 35f);
        embers.speedMax = new Vector2(18f, 90f);
        embers.lifeRange = new Vector2(4f, 8f);
        embers.rate = 9f;
        embers.spawnArea = new Rect(0f, 0f, 1f, 0.35f);
        embers.twinkle = 0.4f;
        embers.Prewarm(6f);
        var motes = UiParticleField.Create("Motes", canvas);
        motes.transform.SetSiblingIndex(backgroundIndex + 3);
        motes.palette = new[] { new Color(0.55f, 0.75f, 1f, 0.5f), new Color(1f, 0.85f, 0.6f, 0.45f) };
        motes.softGlow = true;
        motes.sizeRange = new Vector2(4f, 9f);
        motes.speedMin = new Vector2(-8f, 4f);
        motes.speedMax = new Vector2(8f, 16f);
        motes.lifeRange = new Vector2(5f, 9f);
        motes.rate = 2.5f;
        motes.spawnArea = new Rect(0f, 0.2f, 1f, 0.7f);
        motes.twinkle = 0.8f;
        motes.Prewarm(8f);

        // 번개 섬광(하늘 쪽이 더 밝게)
        flash = UiFx.Image("LightningFlash", canvas, UiFx.VerticalFade, new Color(0.75f, 0.8f, 1f, 0f));
        UiFx.Stretch(flash.rectTransform);
        flash.rectTransform.localScale = new Vector3(1f, -1f, 1f);
        flash.transform.SetSiblingIndex(backgroundIndex + 4);

        // 본편 엔딩을 봤으면 왕관과 '마지막 탑의 수호자', 하드코어까지 모두 깼으면 빛나는 '전설의 수호자'
        bool legend = TowerSkin.IsUnlocked;
        if (legend || IsMainStoryCleared())
            BuildLegendMark(legend);

        // 메뉴 버튼은 처음에 숨겨 두고 차례로 등장시킨다.
        buttonGroups = new CanvasGroup[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
        {
            var group = menuButtons[i].GetComponent<CanvasGroup>();
            if (group == null) group = menuButtons[i].gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            buttonGroups[i] = group;
            menuButtons[i].gameObject.AddComponent<MainMenuButtonHighlight>();
        }

        var version = MakeText("Version", canvas, "v" + Application.version, 18f, Muted);
        version.alignment = TextAlignmentOptions.BottomRight;
        version.rectTransform.anchorMin = version.rectTransform.anchorMax = new Vector2(1f, 0f);
        version.rectTransform.pivot = new Vector2(1f, 0f);
        version.rectTransform.anchoredPosition = new Vector2(-24f, 14f);
        version.rectTransform.sizeDelta = new Vector2(240f, 30f);

        blackout = UiFx.Image("Blackout", canvas, UiFx.Pixel, Color.black);
        UiFx.Stretch(blackout.rectTransform);
        blackout.raycastTarget = true;
    }

    // 하드코어 완주 표식: 'Last'의 L 위에 비스듬히 얹힌 왕관과 제목 아래 '전설의 수호자'.
    private CanvasGroup legendGroup;
    private RectTransform legendCrown;
    private Image legendShine;

    private static bool IsMainStoryCleared()
    {
        var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
        progress.Load();
        return progress.IsCleared(StageIds.FinalMainStage);
    }

    private void BuildLegendMark(bool legend)
    {
        var root = UiFx.Rect("LegendMark", background);
        UiFx.Stretch(root);
        legendGroup = root.gameObject.AddComponent<CanvasGroup>();
        legendGroup.alpha = 0f;
        legendGroup.blocksRaycasts = false;

        legendShine = UiFx.Image("CrownShine", root, UiFx.SoftCircle, new Color(1f, 0.85f, 0.45f, 0.35f));
        legendShine.rectTransform.anchorMin = legendShine.rectTransform.anchorMax = new Vector2(0.418f, 0.94f);
        legendShine.rectTransform.sizeDelta = new Vector2(260f, 200f);
        legendShine.enabled = legend;

        var crownSprite = Resources.Load<Sprite>("UI/TowerPanel/crown_big");
        if (crownSprite != null)
        {
            var crown = UiFx.Image("Crown", root, crownSprite, Color.white);
            legendCrown = crown.rectTransform;
            legendCrown.anchorMin = legendCrown.anchorMax = new Vector2(0.418f, 0.94f);
            legendCrown.sizeDelta = new Vector2(46f, 34f) * 2.6f;
            legendCrown.localEulerAngles = new Vector3(0f, 0f, 14f);
        }

        var title = MakeText("LegendTitle", root, legend ? "— 전설의 수호자 —" : "— 마지막 탑의 수호자 —", 30f,
            legend ? Gold : new Color32(232, 224, 206, 255));
        title.alignment = TextAlignmentOptions.Center;
        title.characterSpacing = 8f;
        var rect = title.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, titleArea.yMin - 0.025f);
        rect.sizeDelta = new Vector2(600f, 44f);
    }

    private void UpdateLegendMark(float now)
    {
        if (legendGroup == null) return;
        legendGroup.alpha = Mathf.Clamp01((now - introTime - 1.2f) / 0.8f);
        if (legendCrown != null)
            legendCrown.anchoredPosition = new Vector2(0f, 3f * Mathf.Sin(now * 1.8f));
        legendShine.color = new Color(1f, 0.85f, 0.45f, 0.25f + 0.12f * Mathf.Sin(now * 2.4f) + lightning * 0.3f);
    }

    // 안개 띠: 가로로 반복되는 그림의 UV를 밀어 흐르게 한다.
    private RawImage MakeFog(string name, float y, float height, Color color, int index)
    {
        var rect = UiFx.Rect(name, transform);
        var image = rect.gameObject.AddComponent<RawImage>();
        image.texture = UiFx.Fog.texture;
        image.color = color;
        image.raycastTarget = false;
        image.uvRect = new Rect(0f, 0f, 2.2f, 1f);
        rect.anchorMin = new Vector2(0f, y);
        rect.anchorMax = new Vector2(1f, y);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(0f, 0f);
        rect.offsetMax = new Vector2(0f, height);
        rect.SetSiblingIndex(index);
        return image;
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, string content, float size, Color color)
    {
        var rect = UiFx.Rect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font != null ? font : BattleUiSkin.Font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.outlineWidth = 0.18f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        return text;
    }

    // 이어하기: 저장이 없으면 흐리게 막고, 있으면 가장 최근 기록의 진행도를 보여 준다.
    private void RefreshContinue()
    {
        if (continueButton == null) return;
        GameSaveSlotData latest = null;
        for (int slot = 0; slot < GameSaveSlotStore.SlotCount; slot++)
        {
            if (GameSaveSlotStore.Read(slot, out GameSaveSlotData data, out _) != SaveSlotStatus.Saved) continue;
            if (latest == null || string.CompareOrdinal(data.savedUtc, latest.savedUtc) > 0) latest = data;
        }
        continueInfo = MakeText("ContinueInfo", continueButton.transform,
            latest != null ? $"최근 기록 · {GameSaveSlotStore.ClearedStages(latest)}/{StageIds.MainStageIds.Count} 스테이지" : "저장된 여정 없음",
            17f, latest != null ? Gold : Muted);
        continueInfo.alignment = TextAlignmentOptions.Center;
        var rect = continueInfo.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -4f);
        rect.sizeDelta = new Vector2(320f, 26f);
        continueButton.interactable = latest != null;
        var image = continueButton.GetComponent<Image>();
        if (image != null && latest == null) image.color = new Color(0.55f, 0.55f, 0.6f, 1f);
    }

    private IEnumerator Intro()
    {
        introTime = Time.unscaledTime;
        float t = 0f;
        while (t < 3.2f)
        {
            t = Time.unscaledTime - introTime;
            blackout.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / 1.1f));
            // 타이틀 빛: 1초 무렵 크게 번쩍였다가 맥동으로 이어진다.
            float burst = Mathf.Clamp01((t - 0.8f) / 0.35f) * (1f - Mathf.Clamp01((t - 1.15f) / 0.9f));
            titleGlow.color = new Color(1f, 0.78f, 0.42f, Mathf.Max(burst * 0.55f, GlowPulse() * Mathf.Clamp01((t - 1.2f) / 0.8f)));
            for (int i = 0; i < buttonGroups.Length; i++)
            {
                float k = UiFx.EaseOut(Mathf.Clamp01((t - 1.35f - i * 0.12f) / 0.45f));
                buttonGroups[i].alpha = k;
                buttonGroups[i].transform.localPosition = new Vector3(buttonGroups[i].transform.localPosition.x,
                    ButtonRestY(i) - 36f * (1f - k), 0f);
            }
            yield return null;
        }
        blackout.raycastTarget = false;
        blackout.gameObject.SetActive(false);
        if (EventSystem.current != null && menuButtons.Length > 0)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private float[] restY;

    private float ButtonRestY(int i)
    {
        if (restY == null)
        {
            restY = new float[menuButtons.Length];
            for (int k = 0; k < menuButtons.Length; k++) restY[k] = menuButtons[k].transform.localPosition.y;
        }
        return restY[i];
    }

    private float GlowPulse() => 0.1f + 0.05f * Mathf.Sin(Time.unscaledTime * 1.6f);

    private void Update()
    {
        float now = Time.unscaledTime;
        // 배경: 등장할 때만 살짝 당겨졌다가 멈춘다(계속 떠다니면 어지럽다).
        float settle = UiFx.EaseOut(Mathf.Clamp01((now - introTime) / 2.2f));
        float scale = Mathf.Lerp(1.05f, 1f, settle);
        background.localScale = new Vector3(scale, scale, 1f);
        float backScale = scale * 1.08f;
        backdrop.rectTransform.localScale = new Vector3(backScale, backScale, 1f);

        if (now - introTime > 3.2f)
            titleGlow.color = new Color(1f, 0.78f, 0.42f, GlowPulse() + LightningAmount() * 0.3f);

        UpdateLegendMark(now);
        ScrollFog(fogA, now * 0.012f);
        ScrollFog(fogB, now * 0.022f + 0.4f);
        SelectWithKeyboard();

        if (now >= nextLightning)
        {
            StartCoroutine(Lightning());
            nextLightning = now + UnityEngine.Random.Range(9f, 16f);
        }
    }

    private float lightning;
    private float LightningAmount() => lightning;

    private static void ScrollFog(RawImage fog, float offset)
    {
        if (fog == null) return;
        Rect uv = fog.uvRect;
        uv.x = offset % 1f;
        fog.uvRect = uv;
    }

    // 키보드로 처음 방향키를 누르면 첫 버튼을 고른다(이후 이동은 EventSystem이 처리).
    private void SelectWithKeyboard()
    {
        if (leaving || EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null) return;
        if (SaveSlotWindow.IsOpen || (SettingsPopup.Instance != null && SettingsPopup.Instance.IsOpen)) return;
        if (!Input.GetKeyDown(KeyCode.LeftArrow) && !Input.GetKeyDown(KeyCode.RightArrow) &&
            !Input.GetKeyDown(KeyCode.UpArrow) && !Input.GetKeyDown(KeyCode.DownArrow)) return;
        foreach (var button in menuButtons)
            if (button.interactable) { EventSystem.current.SetSelectedGameObject(button.gameObject); return; }
    }

    // 번개: 두 번 번쩍이고 사라진다.
    private IEnumerator Lightning()
    {
        float[] keys = { 0f, 0.05f, 0.11f, 0.17f, 0.24f, 0.7f };
        float[] values = { 0f, 0.42f, 0.08f, 0.3f, 0.12f, 0f };
        float start = Time.unscaledTime;
        while (true)
        {
            float t = Time.unscaledTime - start;
            if (t >= keys[keys.Length - 1]) break;
            int i = 0;
            while (i < keys.Length - 2 && t > keys[i + 1]) i++;
            lightning = Mathf.Lerp(values[i], values[i + 1], (t - keys[i]) / (keys[i + 1] - keys[i]));
            flash.color = new Color(0.75f, 0.8f, 1f, lightning);
            yield return null;
        }
        lightning = 0f;
        flash.color = new Color(0.75f, 0.8f, 1f, 0f);
    }

    // 배경 그림 안 'Last'의 T 자리(그림 기준 0~1). 배경이 움직여도 같은 글자를 누르게 한다.
    private static readonly Rect TitleLetterT = new Rect(0.55f, 0.79f, 0.065f, 0.15f);

    public bool IsOverTitleLetterT(Vector2 screenPoint)
    {
        if (background == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(background, screenPoint, null, out Vector2 local))
            return false;
        Rect r = background.rect;
        var normalized = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        return TitleLetterT.Contains(normalized);
    }

    // 다른 화면으로 넘어가기 전 검게 사라진다.
    public void FadeOutThen(Action done)
    {
        if (leaving) return;
        leaving = true;
        StartCoroutine(FadeOut(done));
    }

    private IEnumerator FadeOut(Action done)
    {
        blackout.gameObject.SetActive(true);
        blackout.raycastTarget = true;
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < 0.6f)
        {
            blackout.color = new Color(0f, 0f, 0f, UiFx.EaseInOut((Time.unscaledTime - start) / 0.6f));
            yield return null;
        }
        blackout.color = Color.black;
        done?.Invoke();
    }
}

/// <summary>메뉴 버튼 강조: 마우스를 올리거나 키보드로 고르면 뒤에 금빛이 번지고 양옆에 표식이 뜬다.</summary>
public sealed class MainMenuButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private Image glow;
    private RectTransform left, right;
    private Button button;
    private bool hovered, selected;
    private float amount;

    private void Awake()
    {
        button = GetComponent<Button>();
        glow = UiFx.Image("HoverGlow", transform, UiFx.SoftCircle, new Color(1f, 0.75f, 0.35f, 0f));
        var rect = glow.rectTransform;
        UiFx.Stretch(rect);
        rect.offsetMin = new Vector2(-50f, -26f);
        rect.offsetMax = new Vector2(50f, 26f);
        glow.transform.SetAsFirstSibling();
        left = Marker("Left", new Vector2(0f, 0.5f), new Vector2(-18f, 0f));
        right = Marker("Right", new Vector2(1f, 0.5f), new Vector2(18f, 0f));
    }

    private RectTransform Marker(string name, Vector2 anchor, Vector2 offset)
    {
        var image = UiFx.Image("Marker" + name, transform, UiFx.Pixel, new Color(1f, 0.85f, 0.45f, 0f));
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(10f, 10f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        return rect;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;
    public void OnSelect(BaseEventData eventData) => selected = true;
    public void OnDeselect(BaseEventData eventData) => selected = false;

    private void Update()
    {
        bool on = (hovered || selected) && button != null && button.interactable;
        amount = Mathf.MoveTowards(amount, on ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f);
        glow.color = new Color(1f, 0.75f, 0.35f, 0.38f * amount * pulse);
        Color marker = new Color(1f, 0.85f, 0.45f, amount);
        left.GetComponent<Image>().color = marker;
        right.GetComponent<Image>().color = marker;
        float nudge = 6f * (1f - amount);
        left.anchoredPosition = new Vector2(-18f - nudge, 0f);
        right.anchoredPosition = new Vector2(18f + nudge, 0f);
    }
}
