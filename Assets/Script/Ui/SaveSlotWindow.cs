using System;
using System.Collections;
using System.Globalization;
using LastOfTheTower.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SaveSlotWindow : MonoBehaviour
{
    private static SaveSlotWindow instance;
    public static bool IsOpen => instance != null;
    private readonly Button[] cards = new Button[GameSaveSlotStore.SlotCount];
    private readonly SaveSlotStatus[] statuses = new SaveSlotStatus[GameSaveSlotStore.SlotCount];
    private readonly TMP_Text[] descriptions = new TMP_Text[GameSaveSlotStore.SlotCount];
    private bool saveMode;
    private bool busy;
    private int selected = -1;
    private TMP_FontAsset font;
    private TMP_Text notice;
    private TMP_Text selectionLabel;
    private Button actionButton;
    private GameObject confirmation;
    private Func<string> newGameConfirmed;
    private RectTransform panel;
    private CanvasGroup group;
    private GameObject returnFocus;
    private float panelFit = 1f;
    private Vector2 lastScreenSize;
    private static readonly Color Gold = new Color32(215, 169, 78, 255);
    private static readonly Color Ivory = new Color32(243, 234, 208, 255);
    private static readonly Color Muted = new Color32(191, 169, 130, 255);
    private static readonly Color DarkLabel = new Color32(58, 31, 11, 255);
    private static readonly Color CardIdle = new Color(0.82f, 0.82f, 0.82f, 1f);
    // 광장 UI와 같은 픽셀 테두리 스프라이트 (Resources/UI/Plaza)
    private const float PixelScale = 4f / 3f;
    private Sprite frameSprite;
    private Sprite plateSprite;
    private Sprite goldSprite;
    private Sprite darkSprite;

    public static void Show(bool forSaving)
    {
        if (!Application.isPlaying || IsOpen || SceneLoadingScreen.IsLoading ||
            (forSaving && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PlazaScene")) return;
        Create();
        instance.saveMode = forSaving;
        instance.Build();
        instance.Refresh();
        if (instance.selected >= 0) instance.cards[instance.selected].Select();
        else instance.panel.Find("Cancel").GetComponent<Button>().Select();
        instance.StartCoroutine(instance.Appear());
        AudioManager.Instance?.PlayButtonClick();
        PlazaNPC.ResetAllHoverScales();
    }

    // 새 여정 시작 전 현재 진행 초기화를 확인한다. 같은 모달(IsOpen)이라 기존 메뉴 가드를 그대로 쓴다.
    // confirmed는 실패 문구를 돌려주고, 성공하면 null을 돌려준다.
    public static void ShowNewGameConfirmation(Func<string> confirmed)
    {
        if (!Application.isPlaying || IsOpen || SceneLoadingScreen.IsLoading || confirmed == null) return;
        Create();
        instance.newGameConfirmed = confirmed;
        instance.BuildNewGameConfirmation();
        instance.panel.Find("Cancel").GetComponent<Button>().Select();
        instance.StartCoroutine(instance.Appear());
        AudioManager.Instance?.PlayButtonClick();
    }

    private static void Create()
    {
        var root = new GameObject("SaveSlotWindow", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(SaveSlotWindow));
        instance = root.GetComponent<SaveSlotWindow>();
        instance.returnFocus = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        instance.BuildBase();
    }

    private void BuildBase()
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            if (text.font != null && text.font.name == "Maplestory Bold SDF") { font = text.font; break; }
        if (font == null) font = TMP_Settings.defaultFontAsset;
        frameSprite = Resources.Load<Sprite>("UI/Plaza/plaza_ui_frame");
        plateSprite = Resources.Load<Sprite>("UI/Plaza/plaza_ui_plate");
        goldSprite = Resources.Load<Sprite>("UI/Plaza/plaza_ui_button_gold");
        darkSprite = Resources.Load<Sprite>("UI/Plaza/plaza_ui_button_dark");
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 10;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        var backdrop = MakeImage("Backdrop", transform, new Color(0.015f, 0.025f, 0.04f, 0.9f));
        Stretch(backdrop.rectTransform);
        backdrop.raycastTarget = true;
        var panelImage = MakeImage("Panel", transform, new Color32(15, 26, 33, 255));
        Skin(panelImage, frameSprite, 1f, Color.white);
        panel = panelImage.rectTransform;
    }

    private void BuildNewGameConfirmation()
    {
        Place(panel, Vector2.zero, new Vector2(760f, 380f));
        MakeText("Title", panel, "새 여정 시작", new Vector2(0f, 122f), new Vector2(600f, 56f), 38f, Gold);
        MakeText("Warning", panel, "새 여정을 시작하면 현재 진행이 초기화됩니다.\n저장 슬롯의 기록은 그대로 유지됩니다.",
            new Vector2(0f, 36f), new Vector2(660f, 96f), 26f, Ivory).lineSpacing = 10f;
        notice = MakeText("Notice", panel, "", new Vector2(0f, -40f), new Vector2(660f, 36f), 21f,
            new Color32(232, 164, 146, 255));
        // 실수로 지우지 않도록 '아니요'를 기본 선택으로 둔다.
        MakeButton("Cancel", panel, "아니요", new Vector2(-140f, -116f), new Vector2(220f, 62f), Close,
            new Color32(48, 65, 74, 255), darkSprite);
        actionButton = MakeButton("Action", panel, "네", new Vector2(140f, -116f), new Vector2(220f, 62f),
            ConfirmNewGame, new Color32(139, 99, 38, 255), goldSprite);
        actionButton.GetComponentInChildren<TMP_Text>().color = DarkLabel;
    }

    private void ConfirmNewGame()
    {
        if (busy || newGameConfirmed == null) return;
        busy = true;
        AudioManager.Instance?.PlayButtonClick();
        string error = newGameConfirmed();
        if (error == null) return; // 씬 이동과 함께 창이 정리된다.
        busy = false;
        notice.text = error;
    }

    private void Build()
    {
        Place(panel, Vector2.zero, new Vector2(1200f, 900f));
        MakeText("Title", panel, saveMode ? "여정 기록 저장" : "여정 이어하기", new Vector2(0f, 370f),
            new Vector2(900f, 60f), 42f, Gold);
        MakeText("Subtitle", panel, saveMode ? "현재의 진행 상황을 보관할 슬롯을 선택하세요." :
            "보관한 기록을 선택하면 광장에서 여정을 이어갑니다.", new Vector2(0f, 322f), new Vector2(1050f, 40f), 23f, Muted);
        Place(MakeImage("Divider", panel, new Color(0.6f, 0.45f, 0.22f, 0.7f)).rectTransform,
            new Vector2(0f, 292f), new Vector2(1090f, 2f));
        MakeButton("Close", panel, "X", new Vector2(522f, 372f), new Vector2(52f, 52f), Close, new Color32(86, 41, 39, 255), darkSprite);

        for (int i = 0; i < cards.Length; i++)
        {
            int slot = i;
            Vector2 position = new Vector2(i % 2 == 0 ? -280f : 280f, 230f - i / 2 * 106f);
            cards[i] = MakeButton("Slot" + (i + 1).ToString("00"), panel, "", position,
                new Vector2(535f, 92f), () => Select(slot), new Color32(27, 43, 51, 255), plateSprite);
            MakeText("Number", cards[i].transform, (i + 1).ToString("00"), new Vector2(-228f, 0f),
                new Vector2(62f, 68f), 31f, Gold);
            Place(MakeImage("NumberDivider", cards[i].transform, new Color(0.6f, 0.45f, 0.22f, 0.5f)).rectTransform,
                new Vector2(-182f, 0f), new Vector2(1f, 58f));
            descriptions[i] = MakeText("Description", cards[i].transform, "", new Vector2(34f, 0f),
                new Vector2(406f, 78f), 21f, Ivory);
            descriptions[i].alignment = TextAlignmentOptions.MidlineLeft;
            descriptions[i].lineSpacing = 7f;
            // 선택한 카드만 금색 테두리 빛을 켠다.
            var border = cards[i].gameObject.AddComponent<Outline>();
            border.effectDistance = new Vector2(3f, -3f);
        }
        selectionLabel = MakeText("Selection", panel, "", new Vector2(0f, -285f), new Vector2(1080f, 42f), 22f, Ivory);
        notice = MakeText("Notice", panel, "", new Vector2(0f, -325f), new Vector2(1080f, 48f), 21f, Muted);
        actionButton = MakeButton("Action", panel, saveMode ? "이 슬롯에 저장" : "선택한 기록 불러오기",
            new Vector2(130f, -384f), new Vector2(340f, 64f), Act, new Color32(139, 99, 38, 255), goldSprite);
        actionButton.GetComponentInChildren<TMP_Text>().color = DarkLabel;
        MakeButton("Cancel", panel, "닫기", new Vector2(-190f, -384f), new Vector2(200f, 64f), Close,
            new Color32(48, 65, 74, 255), darkSprite);
        BuildConfirmation();
    }

    private void Refresh()
    {
        int preferred = -1;
        for (int i = 0; i < cards.Length; i++)
        {
            statuses[i] = GameSaveSlotStore.Read(i, out var data, out _);
            if (statuses[i] == SaveSlotStatus.Empty)
                descriptions[i].text = "<size=25>비어 있는 기록</size>\n<color=#BFA982>새로운 여정을 보관하세요</color>";
            else if (statuses[i] == SaveSlotStatus.Unreadable)
                descriptions[i].text = "<color=#DBA49A>읽을 수 없는 기록</color>\n<color=#BFA982>불러오기 불가 · 기존 데이터 보존</color>";
            else
            {
                int levels = 0;
                foreach (var entry in data.research) levels += entry.level;
                string date = DateTime.ParseExact(data.savedUtc, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind).ToLocalTime().ToString("yyyy.MM.dd  HH:mm");
                descriptions[i].text = $"<color=#F2D594>{date}</color>\n클리어 {GameSaveSlotStore.ClearedStages(data)}/{LastOfTheTower.Progression.StageIds.MainStageIds.Count}  ·  연구 {levels}  ·  SP {data.skillPoints}";
            }
            if (preferred < 0 && statuses[i] == (saveMode ? SaveSlotStatus.Empty : SaveSlotStatus.Saved)) preferred = i;
            cards[i].interactable = saveMode || statuses[i] == SaveSlotStatus.Saved;
        }
        Select(selected >= 0 ? selected : preferred, false);
    }

    private void Select(int slot) => Select(slot, true);

    private void Select(int slot, bool sound)
    {
        if (sound && confirmation != null && confirmation.activeSelf) return;
        selected = slot;
        if (sound) AudioManager.Instance?.PlayButtonClick();
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].GetComponent<Outline>().effectColor = i == selected ? new Color(1f, 0.86f, 0.45f, 0.9f) : Color.clear;
            cards[i].GetComponent<Image>().color = plateSprite != null
                ? (i == selected ? Color.white : CardIdle)
                : (i == selected ? new Color32(49, 61, 56, 255) : new Color32(27, 43, 51, 255));
        }
        actionButton.interactable = slot >= 0 && (saveMode || statuses[slot] == SaveSlotStatus.Saved);
        if (slot < 0) { selectionLabel.text = saveMode ? "저장할 슬롯을 선택하세요." : "저장된 기록이 없습니다."; return; }
        string prefix = $"선택한 기록  ·  슬롯 {slot + 1:00}";
        if (GameSaveSlotStore.Read(slot, out var data, out _) == SaveSlotStatus.Saved)
            selectionLabel.text = prefix + $"  ·  퀘스트 보상 수령 {GameSaveSlotStore.ClaimedQuests(data)}건";
        else selectionLabel.text = prefix + (saveMode ? "  ·  현재 진행을 이곳에 저장합니다." : "");
        notice.text = "";
    }

    private void Act()
    {
        if (busy || selected < 0) return;
        AudioManager.Instance?.PlayButtonClick();
        if (saveMode)
        {
            if (GameSaveSlotStore.Read(selected, out _, out _) != SaveSlotStatus.Empty)
            {
                confirmation.GetComponentInChildren<TMP_Text>().text = $"슬롯 {selected + 1:00}의 기록을 덮어쓰시겠습니까?\n이전 기록은 복구할 수 없습니다.";
                confirmation.SetActive(true);
                confirmation.transform.Find("Box/CancelOverwrite").GetComponent<Button>().Select();
            }
            else Save(false);
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded("PlazaScene")) { notice.text = "광장 씬을 찾을 수 없습니다."; return; }
        busy = true;
        if (!GameSaveSlotStore.TryLoad(selected, out var rollback, out string error))
        { busy = false; notice.text = error; return; }
        MainMenuManager.ResetEasterEggBonus();
        Action failed = () =>
        {
            try { rollback?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception); }
            if (this != null) { busy = false; notice.text = "광장 이동에 실패했습니다. 다시 시도해 주세요."; }
        };
        if (!SceneLoadingScreen.TryLoad("PlazaScene", "저장된 여정을 불러오는 중", failed, font)) failed();
    }

    private void Save(bool overwrite)
    {
        if (busy || selected < 0) return;
        busy = true;
        confirmation.SetActive(false);
        bool saved = GameSaveSlotStore.TrySave(selected, overwrite, out string error);
        busy = false;
        Refresh();
        notice.color = saved ? new Color32(163, 214, 172, 255) : new Color32(232, 164, 146, 255);
        notice.text = saved ? $"슬롯 {selected + 1:00}에 여정을 저장했습니다." : error;
    }

    private void BuildConfirmation()
    {
        confirmation = MakeImage("OverwriteConfirmation", panel, new Color(0f, 0f, 0f, 0.85f)).gameObject;
        Stretch(confirmation.GetComponent<RectTransform>());
        confirmation.GetComponent<Image>().raycastTarget = true;
        var boxImage = MakeImage("Box", confirmation.transform, new Color32(20, 32, 39, 255));
        Skin(boxImage, frameSprite, PixelScale, Color.white);
        var box = boxImage.rectTransform;
        Place(box, Vector2.zero, new Vector2(740f, 280f));
        MakeText("Warning", box, "", new Vector2(0f, 40f), new Vector2(660f, 110f), 27f, Ivory);
        // 덮어쓰기는 경고용 붉은 금색, 취소는 기본 어두운 버튼
        MakeButton("ConfirmOverwrite", box, "덮어쓰기", new Vector2(-130f, -76f), new Vector2(210f, 58f),
            () => { AudioManager.Instance?.PlayButtonClick(); Save(true); }, new Color32(126, 54, 43, 255), goldSprite)
            .image.color = new Color(1f, 0.55f, 0.45f, 1f);
        MakeButton("CancelOverwrite", box, "취소", new Vector2(130f, -76f), new Vector2(210f, 58f),
            CancelOverwrite, new Color32(48, 65, 74, 255), darkSprite);
        confirmation.SetActive(false);
    }

    private void CancelOverwrite()
    {
        confirmation.SetActive(false);
        AudioManager.Instance?.PlayButtonClick();
        actionButton.Select();
    }

    private void Update()
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        if (screenSize != lastScreenSize)
        {
            lastScreenSize = screenSize;
            float canvasScale = Mathf.Sqrt(Screen.width / 1920f * Screen.height / 1080f);
            panelFit = Mathf.Min(1f, (Screen.width / canvasScale - 64f) / panel.sizeDelta.x,
                (Screen.height / canvasScale - 64f) / panel.sizeDelta.y);
            if (group.alpha >= 1f) panel.localScale = Vector3.one * panelFit;
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !busy)
        {
            if (confirmation != null && confirmation.activeSelf) CancelOverwrite();
            else Close();
        }
    }

    private IEnumerator Appear()
    {
        for (float elapsed = 0f; elapsed < 0.18f; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.18f);
            group.alpha = t;
            panel.localScale = Vector3.one * panelFit * Mathf.Lerp(0.96f, 1f, t);
            yield return null;
        }
        group.alpha = 1f;
        panel.localScale = Vector3.one * panelFit;
    }

    private void Close()
    {
        if (busy) return;
        if (confirmation != null && confirmation.activeSelf) { CancelOverwrite(); return; }
        busy = true;
        AudioManager.Instance?.PlayButtonClick();
        StartCoroutine(Disappear());
    }

    private IEnumerator Disappear()
    {
        for (float elapsed = 0f; elapsed < 0.12f; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - Mathf.Clamp01(elapsed / 0.12f);
            yield return null;
        }
        if (returnFocus != null && returnFocus.activeInHierarchy &&
            returnFocus.TryGetComponent<Selectable>(out var selectable)) selectable.Select();
        gameObject.SetActive(false);
        if (instance == this) instance = null;
        Destroy(gameObject);
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    private Image MakeImage(string name, Transform parent, Color color)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(Image));
        child.layer = LayerMask.NameToLayer("UI");
        child.transform.SetParent(parent, false);
        var image = child.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text MakeText(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        child.layer = LayerMask.NameToLayer("UI");
        child.transform.SetParent(parent, false);
        var text = child.GetComponent<TextMeshProUGUI>();
        Place(text.rectTransform, position, size);
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction clicked, Color color, Sprite sprite = null)
    {
        var image = MakeImage(name, parent, color);
        bool skinned = sprite != null;
        if (skinned) Skin(image, sprite, PixelScale, Color.white);
        Place(image.rectTransform, position, size);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        // 스프라이트는 흰색이 원본 색이므로 평소를 살짝 어둡게 두고 올렸을 때 밝힌다.
        colors.normalColor = skinned ? new Color(0.93f, 0.93f, 0.93f, 1f) : colors.normalColor;
        colors.selectedColor = colors.normalColor;
        colors.highlightedColor = skinned ? Color.white : new Color(1.15f, 1.1f, 0.95f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        button.colors = colors;
        button.onClick.AddListener(clicked);
        if (!string.IsNullOrEmpty(label)) MakeText("Label", image.transform, label, Vector2.zero, size - new Vector2(12f, 8f), 26f, Ivory);
        return button;
    }

    private static void Skin(Image image, Sprite sprite, float pixelScale, Color color)
    {
        if (sprite == null) return;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = pixelScale;
        image.color = color;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
