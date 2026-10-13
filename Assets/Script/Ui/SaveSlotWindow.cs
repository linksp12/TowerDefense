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
    private RectTransform panel;
    private CanvasGroup group;
    private GameObject returnFocus;
    private float panelFit = 1f;
    private Vector2 lastScreenSize;
    private static readonly Color Gold = new Color32(215, 169, 78, 255);
    private static readonly Color Ivory = new Color32(243, 234, 208, 255);
    private static readonly Color Muted = new Color32(149, 166, 174, 255);

    public static void Show(bool forSaving)
    {
        if (!Application.isPlaying || IsOpen || SceneLoadingScreen.IsLoading ||
            (forSaving && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "PlazaScene")) return;
        var root = new GameObject("SaveSlotWindow", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(SaveSlotWindow));
        instance = root.GetComponent<SaveSlotWindow>();
        instance.saveMode = forSaving;
        instance.returnFocus = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        instance.Build();
        instance.Refresh();
        if (instance.selected >= 0) instance.cards[instance.selected].Select();
        else instance.panel.Find("Cancel").GetComponent<Button>().Select();
        instance.StartCoroutine(instance.Appear());
        AudioManager.Instance?.PlayButtonClick();
        PlazaNPC.ResetAllHoverScales();
    }

    private void Build()
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            if (text.font != null && text.font.name == "Maplestory Bold SDF") { font = text.font; break; }
        if (font == null) font = TMP_Settings.defaultFontAsset;
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
        panel = MakeImage("Panel", transform, new Color32(15, 26, 33, 255)).rectTransform;
        Place(panel, Vector2.zero, new Vector2(1200f, 900f));
        var frame = panel.gameObject.AddComponent<Outline>();
        frame.effectColor = Gold;
        frame.effectDistance = new Vector2(2f, -2f);
        MakeText("Title", panel, saveMode ? "여정 기록 저장" : "여정 이어하기", new Vector2(0f, 370f),
            new Vector2(900f, 60f), 42f, Gold);
        MakeText("Subtitle", panel, saveMode ? "현재의 진행 상황을 보관할 슬롯을 선택하세요." :
            "보관한 기록을 선택하면 광장에서 여정을 이어갑니다.", new Vector2(0f, 322f), new Vector2(1050f, 40f), 23f, Muted);
        Place(MakeImage("Divider", panel, new Color(0.6f, 0.45f, 0.22f, 0.7f)).rectTransform,
            new Vector2(0f, 292f), new Vector2(1090f, 2f));
        MakeButton("Close", panel, "X", new Vector2(534f, 384f), new Vector2(48f, 48f), Close, new Color32(86, 41, 39, 255));

        for (int i = 0; i < cards.Length; i++)
        {
            int slot = i;
            Vector2 position = new Vector2(i % 2 == 0 ? -280f : 280f, 230f - i / 2 * 106f);
            cards[i] = MakeButton("Slot" + (i + 1).ToString("00"), panel, "", position,
                new Vector2(535f, 92f), () => Select(slot), new Color32(27, 43, 51, 255));
            MakeText("Number", cards[i].transform, (i + 1).ToString("00"), new Vector2(-228f, 0f),
                new Vector2(62f, 68f), 31f, Gold);
            Place(MakeImage("NumberDivider", cards[i].transform, new Color(0.6f, 0.45f, 0.22f, 0.5f)).rectTransform,
                new Vector2(-182f, 0f), new Vector2(1f, 58f));
            descriptions[i] = MakeText("Description", cards[i].transform, "", new Vector2(34f, 0f),
                new Vector2(406f, 78f), 21f, Ivory);
            descriptions[i].alignment = TextAlignmentOptions.MidlineLeft;
            descriptions[i].lineSpacing = 7f;
            var border = cards[i].gameObject.AddComponent<Outline>();
            border.effectDistance = new Vector2(1.5f, -1.5f);
        }
        selectionLabel = MakeText("Selection", panel, "", new Vector2(0f, -285f), new Vector2(1080f, 42f), 22f, Ivory);
        notice = MakeText("Notice", panel, "", new Vector2(0f, -325f), new Vector2(1080f, 48f), 21f, Muted);
        actionButton = MakeButton("Action", panel, saveMode ? "이 슬롯에 저장" : "선택한 기록 불러오기",
            new Vector2(130f, -390f), new Vector2(340f, 60f), Act, new Color32(139, 99, 38, 255));
        MakeButton("Cancel", panel, "닫기", new Vector2(-190f, -390f), new Vector2(200f, 60f), Close,
            new Color32(48, 65, 74, 255));
        BuildConfirmation();
    }

    private void Refresh()
    {
        int preferred = -1;
        for (int i = 0; i < cards.Length; i++)
        {
            statuses[i] = GameSaveSlotStore.Read(i, out var data, out _);
            if (statuses[i] == SaveSlotStatus.Empty)
                descriptions[i].text = "<size=25>비어 있는 기록</size>\n<color=#8FABB5>새로운 여정을 보관하세요</color>";
            else if (statuses[i] == SaveSlotStatus.Unreadable)
                descriptions[i].text = "<color=#DBA49A>읽을 수 없는 기록</color>\n<color=#8FABB5>불러오기 불가 · 기존 데이터 보존</color>";
            else
            {
                int levels = 0;
                foreach (var entry in data.research) levels += entry.level;
                string date = DateTime.ParseExact(data.savedUtc, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind).ToLocalTime().ToString("yyyy.MM.dd  HH:mm");
                descriptions[i].text = $"<color=#F2D594>{date}</color>\n클리어 {GameSaveSlotStore.ClearedStages(data)}/4  ·  연구 {levels}  ·  SP {data.skillPoints}";
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
            cards[i].GetComponent<Outline>().effectColor = i == selected ? Gold : new Color(0.3f, 0.4f, 0.45f, 0.6f);
            cards[i].GetComponent<Image>().color = i == selected ? new Color32(49, 61, 56, 255) : new Color32(27, 43, 51, 255);
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
        var box = MakeImage("Box", confirmation.transform, new Color32(20, 32, 39, 255)).rectTransform;
        Place(box, Vector2.zero, new Vector2(740f, 270f));
        var border = box.gameObject.AddComponent<Outline>();
        border.effectColor = Gold;
        border.effectDistance = new Vector2(2f, -2f);
        MakeText("Warning", box, "", new Vector2(0f, 40f), new Vector2(690f, 110f), 27f, Ivory);
        MakeButton("ConfirmOverwrite", box, "덮어쓰기", new Vector2(-130f, -72f), new Vector2(210f, 56f),
            () => { AudioManager.Instance?.PlayButtonClick(); Save(true); }, new Color32(126, 54, 43, 255));
        MakeButton("CancelOverwrite", box, "취소", new Vector2(130f, -72f), new Vector2(210f, 56f),
            CancelOverwrite, new Color32(48, 65, 74, 255));
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
            panelFit = Mathf.Min(1f, (Screen.width / canvasScale - 64f) / 1200f,
                (Screen.height / canvasScale - 64f) / 900f);
            if (group.alpha >= 1f) panel.localScale = Vector3.one * panelFit;
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !busy)
        {
            if (confirmation.activeSelf) CancelOverwrite();
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
        if (confirmation.activeSelf) { CancelOverwrite(); return; }
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

    private Button MakeButton(string name, Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction clicked, Color color)
    {
        var image = MakeImage(name, parent, color);
        Place(image.rectTransform, position, size);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.1f, 0.95f, 1f);
        colors.pressedColor = new Color(0.75f, 0.8f, 0.85f, 1f);
        button.colors = colors;
        button.onClick.AddListener(clicked);
        if (!string.IsNullOrEmpty(label)) MakeText("Label", image.transform, label, Vector2.zero, size - new Vector2(12f, 8f), 26f, Ivory);
        return button;
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
