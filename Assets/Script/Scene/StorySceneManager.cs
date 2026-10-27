using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스토리 컷신. 위아래 검은 띠 안에서 그림이 천천히 줌·패닝되며 넘어가고, 자막이 한 글자씩 나온다.
/// 장면마다 분위기 연출(빛 알갱이·붉은 징조와 흔들림·포화·세계 지도 각성·마지막 탑)을 더한다.
/// 클릭·Space·Enter: 글자를 다 보이게 하거나 다음 장면. 건너뛰기는 길게 눌러야 한다(실수 방지).
/// </summary>
public class StorySceneManager : MonoBehaviour
{
    [Header("배경 이미지 (Story 1, 2, 3 순서)")]
    public Image backgroundImage;
    public Sprite[] backgroundSprites;

    [Header("4장 세계 지도 · 종장 그림")]
    public Sprite worldMapSprite;
    public Sprite finalSprite;

    [Header("4장 보스 초상화 (숲 · 설원 · 화산 순서)")]
    public Sprite[] bossPortraits;

    [Header("이전 자막 틀 (개편 후 사용 안 함, 숨김)")]
    public Image storyFrameImage;
    public Sprite[] storyFrameSprites;

    [Header("자막 텍스트")]
    public TextMeshProUGUI storyText;

    [Header("이전 버튼 (개편 후 숨김)")]
    public Button nextButton;
    public Button skipButton;

    [Header("효과음")]
    public AudioClip typeSound;
    public AudioClip whooshSound;
    public AudioClip rumbleSound;
    public AudioClip impactSound;

    [Header("설정")]
    [Tooltip("글자를 다 보여 준 뒤 다음 장면까지 기다리는 시간")]
    public float autoAdvanceTime = 4.5f;
    public float charactersPerSecond = 24f;
    [Tooltip("건너뛰기를 길게 누르는 시간")]
    public float skipHoldTime = 0.9f;

    private enum Mood { Prosperity, Omen, Invasion, Awakening, Finale }

    private sealed class Panel
    {
        public Sprite Sprite;
        public string Chapter;
        public string Text;
        public Mood Mood;
        // 장면이 들어올 때 이 배율에서 1까지 잠깐 당겨졌다가 멈춘다(계속 움직이면 어지럽다).
        public float ZoomFrom;
        public bool Fit;
    }

    private static readonly Color Gold = new Color32(255, 214, 120, 255);
    private static readonly Color Ivory = new Color32(244, 236, 220, 255);
    private static readonly Color Muted = new Color32(170, 160, 145, 255);
    private const float LetterboxHeight = 96f;

    private Panel[] panels;
    private int index = -1;
    private bool typing, waitingAfterText, finished, transitioning;
    private float textDoneAt;
    private float panelStart;
    private float skipHeld;
    private bool skipPointerDown;

    private Transform canvas;
    private Image layerA, layerB, active, backdrop, tint, flash, blackout, skipFill, hintArrow;
    private RectTransform topBar, bottomBar, mapOverlay;
    private TextMeshProUGUI chapterLabel, skipLabel, hintLabel;
    private CanvasGroup chapterGroup, mapGroup;
    private Image[] dots;
    private UiParticleField motes, ash, embers;
    private Coroutine panelRoutine, typeRoutine;
    private Vector2 shakeOffset;
    private float letterbox = 1f;

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGMForScene("StoryScene");

        BuildPanels();
        BuildLayout();
        StartCoroutine(Opening());
    }

    // ───────── 장면 내용 ─────────
    private void BuildPanels()
    {
        Sprite Story(int i) => backgroundSprites != null && i < backgroundSprites.Length ? backgroundSprites[i] : null;
        panels = new[]
        {
            new Panel { Sprite = Story(0), Chapter = "제1장 · 번영", Mood = Mood.Prosperity,
                Text = "신소재 X의 발견과 AI의 폭발적인 성장으로, 인류는 전례 없는 번영을 누렸다.\n" +
                       "하늘에는 공중도시가 떠올랐고, 세계는 거대한 산업 문명으로 뒤덮였다.\n" +
                       "사람들은 이 번영이 영원하리라 믿었다.",
                ZoomFrom = 1.035f },
            new Panel { Sprite = Story(1), Chapter = "제2장 · 징조", Mood = Mood.Omen,
                Text = "그러나 발전이 거듭될수록, 대지는 비명을 지르기 시작했다.\n" +
                       "붉게 물든 바다, 갈라지는 땅, 하루에도 몇 번씩 뒤바뀌는 하늘.\n" +
                       "아무도 그 원인을 알지 못했다.",
                ZoomFrom = 1.035f },
            new Panel { Sprite = Story(2), Chapter = "제3장 · 침공", Mood = Mood.Invasion,
                Text = "어느 날, 정체불명의 생명체들이 도시 외곽에서 쏟아져 나왔다.\n" +
                       "발전소와 도시의 심장부가 차례로 무너졌고,\n" +
                       "인류는 거대한 방어탑을 세워 전면전에 나섰다.",
                ZoomFrom = 1.035f },
            new Panel { Sprite = worldMapSprite, Chapter = "제4장 · 각성", Mood = Mood.Awakening,
                Text = "문명이 무너진 자리에서, 대지에 잠들어 있던 것들이 깨어났다.\n" +
                       "숲의 골렘, 설원의 얼음 마녀, 화산의 화염 드래곤.\n" +
                       "세 땅의 마물들이 마지막 남은 인류를 향해 진군해 온다.",
                ZoomFrom = 1.035f },
            new Panel { Sprite = finalSprite, Chapter = "종장 · 마지막 탑", Mood = Mood.Finale, Fit = true,
                Text = "살아남은 이들은 마지막 탑 아래에 모였다.\n" +
                       "탑이 무너지면, 인류도 끝난다.\n" +
                       "이제 당신이 이 탑을 지켜야 한다.",
                ZoomFrom = 1.035f },
        };
    }

    // ───────── 화면 구성 ─────────
    private void BuildLayout()
    {
        // 배경 그림 오브젝트에는 하위 Canvas가 따로 붙어 있어서, 화면 전체 기준인 최상위 캔버스에 붙인다.
        canvas = backgroundImage.canvas.rootCanvas.transform;
        // 하위 Canvas들의 그리는 순서 직접 지정을 끄고 계층 순서를 따르게 한다(자막이 검은 화면 위로 뜨지 않게).
        foreach (var nested in canvas.GetComponentsInChildren<Canvas>(true))
            if (nested.transform != canvas) nested.overrideSorting = false;
        var camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }

        // 이전 자막 틀과 버튼은 숨긴다(화면 전체 클릭·키보드·길게 누르는 건너뛰기로 대체).
        if (storyFrameImage != null) storyFrameImage.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        // 그림 층은 전용 묶음 안에서만 순서를 바꾼다(위에 그리는 띠·자막·효과와 섞이지 않게).
        var layers = UiFx.Rect("Layers", canvas);
        UiFx.Stretch(layers);
        layers.SetAsFirstSibling();
        backdrop = UiFx.Image("Backdrop", layers, null, new Color(0.3f, 0.28f, 0.4f, 0f));
        Fit(backdrop, true);

        layerA = backgroundImage;
        layerA.raycastTarget = false;
        layerA.transform.SetParent(layers, false);
        layerB = UiFx.Image("BackgroundB", layers, null, Color.white);
        foreach (var layer in new[] { layerA, layerB })
        {
            Fit(layer, true);
            layer.color = new Color(1f, 1f, 1f, 0f);
        }

        mapOverlay = UiFx.Rect("MapOverlay", canvas);
        mapGroup = mapOverlay.gameObject.AddComponent<CanvasGroup>();
        mapGroup.alpha = 0f;
        BuildMapMarkers();

        motes = Particles("Motes", new[] { new Color(0.55f, 0.85f, 1f, 0.55f), new Color(1f, 0.85f, 0.55f, 0.5f) }, true,
            new Vector2(4f, 9f), new Vector2(-6f, 6f), new Vector2(6f, 22f), new Rect(0f, 0f, 1f, 0.9f), 4f);
        ash = Particles("Ash", new[] { new Color(0.55f, 0.5f, 0.5f, 0.7f), new Color(0.35f, 0.3f, 0.3f, 0.7f), new Color(0.9f, 0.35f, 0.2f, 0.6f) }, false,
            new Vector2(2f, 5f), new Vector2(10f, -70f), new Vector2(40f, -30f), new Rect(0f, 0.9f, 1f, 0.1f), 14f);
        embers = Particles("Embers", new[] { new Color(1f, 0.55f, 0.18f, 0.95f), new Color(1f, 0.8f, 0.3f, 0.9f) }, false,
            new Vector2(3f, 6f), new Vector2(-16f, 40f), new Vector2(16f, 110f), new Rect(0f, 0f, 1f, 0.3f), 12f);

        tint = UiFx.Image("Tint", canvas, UiFx.Pixel, new Color(0.7f, 0.05f, 0.05f, 0f));
        UiFx.Stretch(tint.rectTransform);
        flash = UiFx.Image("Flash", canvas, UiFx.Pixel, new Color(1f, 0.95f, 0.85f, 0f));
        UiFx.Stretch(flash.rectTransform);

        var shade = UiFx.Image("SubtitleShade", canvas, UiFx.VerticalFade, new Color(0.01f, 0.01f, 0.03f, 0.88f));
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = new Vector2(1f, 0f);
        shade.rectTransform.pivot = new Vector2(0.5f, 0f);
        shade.rectTransform.sizeDelta = new Vector2(0f, 460f);

        topBar = Bar("LetterboxTop", 1f);
        bottomBar = Bar("LetterboxBottom", 0f);

        chapterLabel = Text("Chapter", canvas, "", 30f, Gold);
        chapterLabel.characterSpacing = 6f;
        Place(chapterLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 342f), new Vector2(900f, 44f));
        chapterGroup = chapterLabel.gameObject.AddComponent<CanvasGroup>();
        chapterGroup.alpha = 0f;
        foreach (float side in new[] { -1f, 1f })
        {
            var line = UiFx.Image("Rule", chapterLabel.transform, UiFx.HorizontalFade, new Color(1f, 0.84f, 0.47f, 0.8f));
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            line.rectTransform.pivot = new Vector2(0f, 0.5f);
            line.rectTransform.sizeDelta = new Vector2(150f, 2f);
            line.rectTransform.anchoredPosition = new Vector2(side * 190f, 0f);
            line.rectTransform.localScale = new Vector3(side, 1f, 1f);
        }

        // 기존 자막 텍스트(폰트 유지)를 새 자리로 옮긴다.
        storyText.transform.SetParent(canvas, false);
        storyText.text = "";
        storyText.fontSize = 34f;
        storyText.color = Ivory;
        storyText.alignment = TextAlignmentOptions.Top;
        storyText.lineSpacing = 22f;
        storyText.textWrappingMode = TextWrappingModes.Normal;
        storyText.outlineWidth = 0.2f;
        storyText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        storyText.raycastTarget = false;
        Place(storyText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 212f), new Vector2(1560f, 170f));

        hintArrow = UiFx.Image("ContinueArrow", canvas, UiFx.Pixel, new Color(1f, 0.84f, 0.47f, 0f));
        Place(hintArrow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 132f), new Vector2(14f, 14f));
        hintArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        dots = new Image[panels.Length];
        for (int i = 0; i < panels.Length; i++)
        {
            dots[i] = UiFx.Image("Dot" + i, bottomBar, UiFx.Pixel, Muted);
            Place(dots[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - (panels.Length - 1) / 2f) * 30f, 0f), new Vector2(9f, 9f));
            dots[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        hintLabel = Text("Hint", bottomBar, "클릭 · Space  계속", 19f, Muted);
        hintLabel.alignment = TextAlignmentOptions.Left;
        Place(hintLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(230f, 0f), new Vector2(380f, 30f));

        // 건너뛰기: 길게 누르면 채워지는 막대
        var skip = UiFx.Rect("HoldToSkip", topBar);
        Place(skip, new Vector2(1f, 0.5f), new Vector2(-200f, 0f), new Vector2(300f, 60f));
        var hit = skip.gameObject.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0.001f);
        skip.gameObject.AddComponent<StorySkipHold>().Init(down => skipPointerDown = down);
        skipLabel = Text("Label", skip, "건너뛰기 ▶▶  <size=70%><color=#8C857A>길게 누르기</color></size>", 22f, Ivory);
        skipLabel.alignment = TextAlignmentOptions.Right;
        Place(skipLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(300f, 32f));
        var track = UiFx.Image("Track", skip, UiFx.Pixel, new Color(1f, 1f, 1f, 0.15f));
        Place(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(300f, 3f));
        skipFill = UiFx.Image("Fill", skip, UiFx.Pixel, Gold);
        skipFill.type = Image.Type.Filled;
        skipFill.fillMethod = Image.FillMethod.Horizontal;
        skipFill.fillAmount = 0f;
        Place(skipFill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(300f, 3f));

        blackout = UiFx.Image("Blackout", canvas, UiFx.Pixel, Color.black);
        UiFx.Stretch(blackout.rectTransform);
    }

    private void BuildMapMarkers()
    {
        // 세계 지도 그림 기준 자리: 숲(왼쪽), 설원(오른쪽 위), 화산(오른쪽 아래)
        // 화산 표시는 아래 자막과 겹치지 않게 조금 올린다.
        var spots = new[] { new Vector2(0.24f, 0.56f), new Vector2(0.78f, 0.74f), new Vector2(0.8f, 0.42f) };
        var colors = new[] { new Color(0.45f, 1f, 0.45f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.5f, 0.2f) };
        var names = new[] { "숲의 골렘", "설원의 얼음 마녀", "화산의 화염 드래곤" };
        for (int i = 0; i < spots.Length; i++)
        {
            var marker = UiFx.Rect("Region" + i, mapOverlay);
            marker.anchorMin = marker.anchorMax = spots[i];
            marker.sizeDelta = new Vector2(10f, 10f);
            marker.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            var glow = UiFx.Image("Glow", marker, UiFx.SoftCircle, new Color(colors[i].r, colors[i].g, colors[i].b, 0.55f));
            glow.rectTransform.sizeDelta = new Vector2(420f, 300f);
            var frame = UiFx.Image("Frame", marker, BattleUiSkin.Frame, Color.white);
            if (frame.sprite != null) BattleUiSkin.Skin(frame, frame.sprite, 2f);
            else frame.color = new Color(0.1f, 0.08f, 0.06f, 0.9f);
            frame.rectTransform.sizeDelta = new Vector2(150f, 150f);
            frame.rectTransform.anchoredPosition = new Vector2(0f, 40f);
            if (bossPortraits != null && i < bossPortraits.Length && bossPortraits[i] != null)
            {
                var portrait = UiFx.Image("Portrait", frame.transform, bossPortraits[i], Color.white);
                portrait.preserveAspect = true;
                UiFx.Stretch(portrait.rectTransform, 18f);
            }
            var label = Text("Name", marker, names[i], 26f, colors[i]);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(360f, 36f));
        }
    }

    private UiParticleField Particles(string name, Color[] palette, bool soft, Vector2 size, Vector2 speedMin, Vector2 speedMax,
        Rect area, float rate)
    {
        // 장면에 맞는 입자만 켜지도록 처음에는 생성량 0으로 두고 기본 생성량을 따로 기억한다.
        var field = UiParticleField.Create(name, canvas);
        field.palette = palette;
        field.softGlow = soft;
        field.sizeRange = size;
        field.speedMin = speedMin;
        field.speedMax = speedMax;
        field.spawnArea = area;
        field.lifeRange = new Vector2(4f, 8f);
        field.twinkle = soft ? 0.7f : 0.3f;
        field.sway = soft ? 10f : 18f;
        field.maxCount = 90;
        field.rate = 0f;
        fieldRates[field] = rate;
        return field;
    }

    private readonly System.Collections.Generic.Dictionary<UiParticleField, float> fieldRates =
        new System.Collections.Generic.Dictionary<UiParticleField, float>();

    private RectTransform Bar(string name, float y)
    {
        var bar = UiFx.Image(name, canvas, UiFx.Pixel, Color.black);
        var rect = bar.rectTransform;
        rect.anchorMin = new Vector2(0f, y);
        rect.anchorMax = new Vector2(1f, y);
        rect.pivot = new Vector2(0.5f, y);
        rect.sizeDelta = new Vector2(0f, LetterboxHeight);
        return rect;
    }

    private TextMeshProUGUI Text(string name, Transform parent, string content, float size, Color color)
    {
        var rect = UiFx.Rect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = storyText != null ? storyText.font : BattleUiSkin.Font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.outlineWidth = 0.18f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Fit(Image image, bool envelope)
    {
        UiFx.Stretch(image.rectTransform);
        var fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = envelope ? AspectRatioFitter.AspectMode.EnvelopeParent : AspectRatioFitter.AspectMode.FitInParent;
        if (image.sprite != null) fitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
    }

    private static void SetSprite(Image image, Sprite sprite, bool fit)
    {
        image.sprite = sprite;
        var fitter = image.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = fit ? AspectRatioFitter.AspectMode.FitInParent : AspectRatioFitter.AspectMode.EnvelopeParent;
        if (sprite != null) fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
    }

    // ───────── 진행 ─────────
    private IEnumerator Opening()
    {
        float start = Time.unscaledTime;
        ShowPanel(0);
        while (Time.unscaledTime - start < 1.2f)
        {
            blackout.color = new Color(0f, 0f, 0f, 1f - UiFx.EaseInOut((Time.unscaledTime - start) / 1.2f));
            yield return null;
        }
        blackout.color = Color.clear;
        blackout.raycastTarget = false;
    }

    private void ShowPanel(int next)
    {
        index = next;
        Panel panel = panels[index];
        Image previous = active;
        active = active == layerA ? layerB : layerA;
        SetSprite(active, panel.Sprite, panel.Fit);
        // 새 그림 층을 이전 층 바로 위로(묶음 안 맨 위) 올려 서서히 덮게 한다.
        active.transform.SetAsLastSibling();
        mapOverlay.SetParent(active.transform, false);
        UiFx.Stretch(mapOverlay);
        panelStart = Time.unscaledTime;
        Play(whooshSound, 0.7f);
        for (int i = 0; i < dots.Length; i++)
            dots[i].color = i < index ? new Color(Gold.r, Gold.g, Gold.b, 0.55f) : i == index ? Gold : Muted;
        if (panelRoutine != null) StopCoroutine(panelRoutine);
        panelRoutine = StartCoroutine(RunPanel(panel, previous));
    }

    private IEnumerator RunPanel(Panel panel, Image previous)
    {
        typing = waitingAfterText = false;
        storyText.text = "";
        hintArrow.color = Color.clear;
        foreach (var pair in fieldRates) pair.Key.rate = 0f;
        float moodRate(UiParticleField f) => fieldRates[f];
        if (panel.Mood == Mood.Prosperity) motes.rate = moodRate(motes);
        if (panel.Mood == Mood.Omen) ash.rate = moodRate(ash);
        if (panel.Mood == Mood.Invasion) embers.rate = moodRate(embers);
        if (panel.Mood == Mood.Finale) { embers.rate = moodRate(embers) * 0.6f; motes.rate = moodRate(motes) * 0.5f; }
        if (panel.Mood == Mood.Finale) Play(impactSound, 0.9f);

        // 장면 전환: 새 그림이 서서히 나타나고 이전 그림이 사라진다. 종장은 띠를 걷어 그림 전체를 보여 준다.
        float t = 0f;
        float chapterIn = 0.5f;
        while (t < 1.1f)
        {
            t = Time.unscaledTime - panelStart;
            float k = UiFx.EaseInOut(Mathf.Clamp01(t / 1.1f));
            active.color = new Color(1f, 1f, 1f, k);
            if (previous != null) previous.color = new Color(1f, 1f, 1f, 1f - k);
            chapterGroup.alpha = Mathf.Max(0f, chapterGroup.alpha - Time.unscaledDeltaTime * 4f);
            if (panel.Mood == Mood.Awakening) mapGroup.alpha = k;
            else mapGroup.alpha = Mathf.Min(mapGroup.alpha, 1f - k);
            float backdropTarget = panel.Fit ? 1f : 0f;
            backdrop.color = new Color(0.3f, 0.28f, 0.4f, Mathf.Lerp(backdrop.color.a, backdropTarget, k));
            if (panel.Fit && backdrop.sprite != panel.Sprite) { backdrop.sprite = panel.Sprite; Fit(backdrop, true); }
            yield return null;
        }
        if (previous != null) previous.color = new Color(1f, 1f, 1f, 0f);

        chapterLabel.text = panel.Chapter;
        float shownAt = Time.unscaledTime;
        while (Time.unscaledTime - shownAt < chapterIn)
        {
            chapterGroup.alpha = (Time.unscaledTime - shownAt) / chapterIn;
            yield return null;
        }
        chapterGroup.alpha = 1f;
        typeRoutine = StartCoroutine(TypeText(panel.Text));
    }

    private IEnumerator TypeText(string text)
    {
        typing = true;
        storyText.text = text;
        storyText.maxVisibleCharacters = 0;
        storyText.ForceMeshUpdate();
        int total = storyText.textInfo.characterCount;
        float shown = 0f;
        int lastTick = 0;
        while (shown < total)
        {
            shown += Time.unscaledDeltaTime * charactersPerSecond;
            int visible = Mathf.Min(total, Mathf.FloorToInt(shown));
            storyText.maxVisibleCharacters = visible;
            if (visible - lastTick >= 2)
            {
                lastTick = visible;
                char c = storyText.textInfo.characterInfo[Mathf.Max(0, visible - 1)].character;
                if (!char.IsWhiteSpace(c)) Play(typeSound, 0.35f);
            }
            yield return null;
        }
        FinishTyping();
    }

    private void FinishTyping()
    {
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = null;
        storyText.maxVisibleCharacters = 99999;
        typing = false;
        waitingAfterText = true;
        textDoneAt = Time.unscaledTime;
    }

    private void Advance()
    {
        if (transitioning || finished) return;
        if (typing) { FinishTyping(); return; }
        if (!waitingAfterText) return;
        if (index + 1 < panels.Length) ShowPanel(index + 1);
        else StartCoroutine(Finish());
    }

    private IEnumerator Finish()
    {
        if (finished) yield break;
        finished = true;
        transitioning = true;
        blackout.raycastTarget = true;
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < 1.0f)
        {
            blackout.color = new Color(0f, 0f, 0f, UiFx.EaseInOut((Time.unscaledTime - start) / 1.0f));
            yield return null;
        }
        blackout.color = Color.black;
        LoadPlazaScene();
    }

    // ───────── 매 프레임 ─────────
    private void Update()
    {
        if (panels == null || index < 0) return;
        float now = Time.unscaledTime;
        Panel panel = panels[index];

        // 장면이 들어올 때만 살짝 당겨졌다가 그대로 멈춘다.
        float p = UiFx.EaseOut(Mathf.Clamp01((now - panelStart) / 1.6f));
        float zoom = Mathf.Lerp(panel.ZoomFrom, 1f, p);
        active.rectTransform.localScale = new Vector3(zoom, zoom, 1f);
        active.rectTransform.anchoredPosition = shakeOffset;
        if (panel.Fit) backdrop.rectTransform.localScale = Vector3.one * 1.1f;

        UpdateMood(panel, now - panelStart);

        // 종장에서는 위아래 띠를 걷는다.
        letterbox = Mathf.MoveTowards(letterbox, panel.Mood == Mood.Finale ? 0f : 1f, Time.unscaledDeltaTime * 1.2f);
        // 위 띠는 투명해지기만 해서(크기 유지) 건너뛰기 표시는 그대로 남는다.
        topBar.GetComponent<Image>().color = new Color(0f, 0f, 0f, letterbox);
        bottomBar.sizeDelta = new Vector2(0f, LetterboxHeight);

        if (waitingAfterText)
        {
            float blink = 0.5f + 0.5f * Mathf.Sin(now * 5f);
            hintArrow.color = new Color(1f, 0.84f, 0.47f, blink);
            hintArrow.rectTransform.anchoredPosition = new Vector2(0f, 132f + 4f * Mathf.Sin(now * 5f));
            if (now - textDoneAt >= autoAdvanceTime + (index == panels.Length - 1 ? 1.5f : 0f)) Advance();
        }

        HandleInput();
    }

    private void UpdateMood(Panel panel, float t)
    {
        shakeOffset = Vector2.zero;
        float tintAlpha = 0f;
        float flashAlpha = Mathf.Max(0f, flash.color.a - Time.unscaledDeltaTime * 2.5f);
        switch (panel.Mood)
        {
            case Mood.Omen:
                // 붉은 맥동과 가끔 짧은 땅울림(작게 흔들고 바로 멈춘다)
                tintAlpha = 0.04f + 0.035f * Mathf.Sin(t * 2.2f);
                float cycle = (t - 1.8f) % 4.5f;
                if (t > 1.8f && cycle < 0.3f)
                {
                    if (cycle < Time.unscaledDeltaTime + 0.001f) Play(rumbleSound, 0.8f);
                    float strength = (1f - cycle / 0.3f) * 3f;
                    shakeOffset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * strength;
                }
                break;
            case Mood.Invasion:
                // 포화 섬광(화면은 흔들지 않는다)
                if (Random.value < Time.unscaledDeltaTime * 1.4f) flashAlpha = Mathf.Max(flashAlpha, Random.Range(0.05f, 0.12f));
                break;
            case Mood.Awakening:
                UpdateMapMarkers(t);
                tintAlpha = Mathf.Clamp01((t - 5.6f) / 2f) * 0.08f;
                break;
            case Mood.Finale:
                if (t < 0.4f) flashAlpha = Mathf.Max(flashAlpha, 0.35f * (1f - t / 0.4f));
                break;
        }
        tint.color = new Color(0.7f, 0.05f, 0.05f, Mathf.MoveTowards(tint.color.a, tintAlpha, Time.unscaledDeltaTime * 0.6f));
        flash.color = new Color(1f, 0.95f, 0.85f, flashAlpha);
    }

    // 세 지역이 차례로 밝아지며 보스가 나타난다.
    private void UpdateMapMarkers(float t)
    {
        for (int i = 0; i < mapOverlay.childCount; i++)
        {
            var marker = mapOverlay.GetChild(i);
            float start = 1.6f + i * 1.5f;
            float k = Mathf.Clamp01((t - start) / 0.5f);
            if (k > 0f && marker.GetComponent<CanvasGroup>().alpha == 0f) Play(rumbleSound, 0.45f);
            marker.GetComponent<CanvasGroup>().alpha = k;
            float pop = k < 1f ? Mathf.Lerp(0.6f, 1.12f, UiFx.EaseOut(k)) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((t - start - 0.5f) / 0.3f));
            marker.localScale = new Vector3(pop, pop, 1f) / Mathf.Max(0.01f, active.rectTransform.localScale.x);
            var glow = marker.GetChild(0).GetComponent<Image>();
            Color c = glow.color;
            c.a = 0.45f + 0.15f * Mathf.Sin(t * 3f + i);
            glow.color = c;
        }
    }

    private void HandleInput()
    {
        bool skipKey = Input.GetKey(KeyCode.Escape);
        if (skipPointerDown || skipKey) skipHeld += Time.unscaledDeltaTime;
        else skipHeld = Mathf.Max(0f, skipHeld - Time.unscaledDeltaTime * 3f);
        skipFill.fillAmount = Mathf.Clamp01(skipHeld / skipHoldTime);
        if (skipHeld >= skipHoldTime && !finished)
        {
            skipHeld = 0f;
            StartCoroutine(Finish());
            return;
        }

        if (finished || SceneLoadingScreen.IsLoading) return;
        bool click = Input.GetMouseButtonDown(0) && !skipPointerDown && !OverSkip();
        if (click || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow))
            Advance();
    }

    private bool OverSkip()
    {
        var skip = topBar.Find("HoldToSkip") as RectTransform;
        return skip != null && RectTransformUtility.RectangleContainsScreenPoint(skip, Input.mousePosition, null);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(clip, volume);
    }

    // ───────── 광장씬 전환 ─────────
    private void LoadPlazaScene()
    {
        if (SceneLoadingScreen.IsLoading)
            return;
        if (!SceneLoadingScreen.TryLoad("PlazaScene", "광장으로 이동 중", RestoreAfterLoadFailure))
            RestoreAfterLoadFailure();
    }

    private void RestoreAfterLoadFailure()
    {
        if (this == null)
            return;
        finished = false;
        transitioning = false;
        blackout.color = Color.clear;
        blackout.raycastTarget = false;
    }
}

/// <summary>건너뛰기 영역을 누르고 있는 동안을 알린다(길게 눌러 건너뛰기).</summary>
public sealed class StorySkipHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private System.Action<bool> changed;

    public void Init(System.Action<bool> onChanged) => changed = onChanged;
    public void OnPointerDown(PointerEventData eventData) => changed?.Invoke(true);
    public void OnPointerUp(PointerEventData eventData) => changed?.Invoke(false);
    public void OnPointerExit(PointerEventData eventData) => changed?.Invoke(false);
}
