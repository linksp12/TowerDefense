using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 창(도트 디자인). 지금 전장 정보, 계속·다시·광장·메인 버튼, 배경음·효과음 조절,
/// 전투를 그만두는 버튼은 한 번 더 묻는다. 멈춤·재개·이동 처리는 PauseManager가 그대로 맡는다.
/// </summary>
public sealed class PauseMenuUI : MonoBehaviour
{
    private const float Width = 150f;
    private const float Height = 182f;

    private enum PendingAction { None, Restart, Plaza, MainMenu }

    private PauseManager owner;
    private Canvas canvas;
    private RectTransform root;
    private CanvasGroup rootGroup;
    private RectTransform panel;
    private RectTransform confirm;
    private TextMeshProUGUI confirmText;
    private TextMeshProUGUI stageText;
    private TextMeshProUGUI waveText;
    private TextMeshProUGUI hpText;
    private TextMeshProUGUI goldText;
    private VolumeControl bgm;
    private VolumeControl sfx;
    private PendingAction pending;
    private float openedAt;
    private bool open;
    private WaveManager waveManager;

    private sealed class VolumeControl
    {
        public Slider slider;
        public Image fill;
        public TextMeshProUGUI percent;

        public void ShowValue(float value)
        {
            fill.fillAmount = value;
            percent.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        // 저장된 값을 표시만 한다(다시 저장하지 않음).
        public void Load(float value)
        {
            slider.SetValueWithoutNotify(value);
            ShowValue(value);
        }
    }

    public bool IsOpen => open;
    public bool IsConfirming => confirm != null && confirm.gameObject.activeSelf;

    public static PauseMenuUI Create(PauseManager owner, Canvas canvas)
    {
        if (owner == null || canvas == null)
            return null;
        var ui = owner.gameObject.AddComponent<PauseMenuUI>();
        ui.owner = owner;
        ui.canvas = canvas.rootCanvas;
        ui.waveManager = FindAnyObjectByType<WaveManager>();
        ui.Build();
        return ui;
    }

    // ───────── 만들기 ─────────
    private void Build()
    {
        root = TowerPanelArt.CreateRoot("PauseMenu", canvas);
        rootGroup = root.GetComponent<CanvasGroup>();

        // 뒤 화면 어둡게(클릭 막음)
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        dim.rectTransform.SetParent(root, false);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        dim.color = new Color(0.02f, 0.01f, 0.01f, 0.76f);

        panel = TowerPanelArt.Group("Panel", root, 0f, 0f, Width, Height);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(0f, -12f);

        TowerPanelArt.Image("Frame", panel, "pause_frame", 0f, 0f, true);
        TowerPanelArt.Image("Ribbon", panel, "pause_ribbon", 22f, -11f, true);
        TowerPanelArt.Text("Title", panel, 34f, -11f, 104f, 16f, 26f, TowerPanelArt.Ivory, TextAlignmentOptions.Center)
            .text = "일시 정지";
        TowerPanelArt.Image("Medal", panel, "medal", 2f, -22f, true);
        TowerPanelArt.Image("PauseIcon", panel, "icon_pause", 18.5f, -6.5f);

        // 지금 전장 정보
        TowerPanelArt.Image("Info", panel, "pause_info", 10f, 14f);
        stageText = TowerPanelArt.Text("Stage", panel, 15f, 16f, 66f, 11f, 19f, TowerPanelArt.Gold, TextAlignmentOptions.Left);
        TowerPanelArt.Image("WaveIcon", panel, "icon_sword", 84f, 17f);
        waveText = TowerPanelArt.Text("Wave", panel, 95f, 16f, 42f, 11f, 17f, TowerPanelArt.Ivory, TextAlignmentOptions.Left);
        TowerPanelArt.Image("HeartIcon", panel, "icon_heart", 15f, 32f);
        hpText = TowerPanelArt.Text("Hp", panel, 25f, 30f, 44f, 11f, 18f, TowerPanelArt.Ivory, TextAlignmentOptions.Left);
        TowerPanelArt.Image("CoinIcon", panel, "icon_coin", 84f, 31.5f);
        goldText = TowerPanelArt.Text("Gold", panel, 95f, 30f, 42f, 11f, 18f, TowerPanelArt.Gold, TextAlignmentOptions.Left);

        // 메뉴 버튼
        MenuButton("Resume", 48f, "pause_btn_gold", "icon_play", "계속하기", TowerPanelArt.Ink, () => owner.ResumeGame(), true);
        MenuButton("Restart", 71f, "pause_btn_dark", "icon_restart", "다시하기", TowerPanelArt.Ivory, () => Ask(PendingAction.Restart), false);
        MenuButton("Plaza", 94f, "pause_btn_dark", "icon_flag", "광장으로", TowerPanelArt.Ivory, () => Ask(PendingAction.Plaza), false);
        MenuButton("MainMenu", 117f, "pause_btn_red", "icon_door", "메인으로", TowerPanelArt.Ivory, () => Ask(PendingAction.MainMenu), false);

        // 소리
        TowerPanelArt.Image("Separator", panel, "sep_info", 25f, 142f);
        bgm = VolumeRow("Bgm", 148f, "배경음", value =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.BGMVolume = value;
        });
        sfx = VolumeRow("Sfx", 161f, "효과음", value =>
        {
            if (AudioManager.Instance == null) return;
            // 설정창과 같이 효과음·UI음을 함께 맞춘다.
            AudioManager.Instance.SFXVolume = value;
            AudioManager.Instance.UIVolume = value;
        });

        BuildConfirm();
        root.gameObject.SetActive(false);
    }

    private void MenuButton(string name, float y, string sprite, string icon, string label, Color labelColor,
        System.Action onClick, bool escKey)
    {
        var image = TowerPanelArt.Image(name, panel, sprite, 15f, y);
        var button = TowerPanelArt.MakeButton(image);
        button.onClick.AddListener(() =>
        {
            if (SceneLoadingScreen.IsLoading || IsConfirming)
                return;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayButtonClick();
            onClick();
        });
        var iconImage = TowerPanelArt.Image("Icon", image.transform, icon, 7f, 0f);
        Sprite iconSprite = iconImage.sprite;
        if (iconSprite != null)
            iconImage.rectTransform.anchoredPosition = new Vector2(7f * TowerPanelArt.Dot,
                -(20f - iconSprite.rect.height) / 2f * TowerPanelArt.Dot);
        TowerPanelArt.Text("Label", image.transform, 0f, 0f, 120f, 19f, 22f, labelColor, TextAlignmentOptions.Center,
            labelColor != TowerPanelArt.Ink).text = label;
        if (escKey)
        {
            TowerPanelArt.Image("Key", image.transform, "keycap_wide", 96f, 4.5f);
            TowerPanelArt.Text("KeyText", image.transform, 96f, 4.5f, 19f, 10f, 14f, TowerPanelArt.KeyInk,
                TextAlignmentOptions.Center, false).text = "Esc";
        }
    }

    private VolumeControl VolumeRow(string name, float y, string label, System.Action<float> onChange)
    {
        TowerPanelArt.Image(name + "Icon", panel, "icon_speaker", 12f, y + 1f);
        TowerPanelArt.Text(name + "Label", panel, 23f, y, 28f, 11f, 16f, TowerPanelArt.Muted, TextAlignmentOptions.Left)
            .text = label;

        var area = TowerPanelArt.Group(name + "Slider", panel, 50f, y, 76f, 11f);
        TowerPanelArt.Image("Track", area, "pause_track", 0f, 2.5f, true);
        var fill = TowerPanelArt.Image("Fill", area, "pause_fill", 1f, 3.5f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        var handleArea = TowerPanelArt.Group("HandleArea", area, 4.5f, 0f, 67f, 11f);
        var knob = TowerPanelArt.Image("Knob", handleArea, "pause_knob", 0f, 0f, true);
        knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        knob.rectTransform.sizeDelta = new Vector2(9f * TowerPanelArt.Dot, 0f);

        var slider = area.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.handleRect = knob.rectTransform;
        slider.targetGraphic = knob;
        slider.transition = Selectable.Transition.ColorTint;
        var control = new VolumeControl
        {
            slider = slider,
            fill = fill,
            percent = TowerPanelArt.Text(name + "Percent", panel, 128f, y, 18f, 11f, 15f, TowerPanelArt.Ivory, TextAlignmentOptions.Left),
        };
        slider.onValueChanged.AddListener(value =>
        {
            control.ShowValue(value);
            onChange(value);
        });
        return control;
    }

    private void BuildConfirm()
    {
        confirm = TowerPanelArt.Group("Confirm", root, 0f, 0f, 1f, 1f);
        confirm.anchorMin = Vector2.zero;
        confirm.anchorMax = Vector2.one;
        confirm.offsetMin = confirm.offsetMax = Vector2.zero;
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        dim.rectTransform.SetParent(confirm, false);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        dim.color = new Color(0f, 0f, 0f, 0.45f);

        var box = TowerPanelArt.Group("Box", confirm, 0f, 0f, 140f, 66f);
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
        box.anchoredPosition = new Vector2(0f, -12f);
        TowerPanelArt.Image("Frame", box, "pause_confirm", 0f, 0f, true);
        confirmText = TowerPanelArt.Text("Message", box, 8f, 8f, 124f, 30f, 19f, TowerPanelArt.Ivory, TextAlignmentOptions.Center);
        confirmText.textWrappingMode = TextWrappingModes.Normal;

        var yes = TowerPanelArt.Image("Yes", box, "pause_btn_red_small", 12f, 41f);
        TowerPanelArt.MakeButton(yes).onClick.AddListener(Confirm);
        TowerPanelArt.Text("Label", yes.transform, 0f, 0f, 56f, 16f, 20f, TowerPanelArt.Ivory, TextAlignmentOptions.Center).text = "확인";
        var no = TowerPanelArt.Image("No", box, "pause_btn_dark_small", 72f, 41f);
        TowerPanelArt.MakeButton(no).onClick.AddListener(CancelConfirm);
        TowerPanelArt.Text("Label", no.transform, 0f, 0f, 56f, 16f, 20f, TowerPanelArt.Ivory, TextAlignmentOptions.Center).text = "취소";
        confirm.gameObject.SetActive(false);
    }

    // ───────── 열기·닫기 ─────────
    public void Show()
    {
        open = true;
        openedAt = Time.unscaledTime;
        pending = PendingAction.None;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        TowerPanelArt.FitRoot(root, canvas);
        rootGroup.alpha = 0f;
        panel.localScale = Vector3.one * 0.92f;
        confirm.gameObject.SetActive(false);
        RefreshInfo();

        // 소리 관리자가 없으면(전투 씬을 바로 실행한 경우) 조절을 잠근다.
        bool hasAudio = AudioManager.Instance != null;
        bgm.slider.interactable = sfx.slider.interactable = hasAudio;
        bgm.Load(hasAudio ? AudioManager.Instance.BGMVolume : 1f);
        sfx.Load(hasAudio ? AudioManager.Instance.SFXVolume : 1f);
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayOpenPanel();
    }

    public void Hide()
    {
        open = false;
        pending = PendingAction.None;
        if (root != null)
            root.gameObject.SetActive(false);
    }

    private void RefreshInfo()
    {
        string sceneName = gameObject.scene.name;
        string stageId = LastOfTheTower.Progression.StageIds.FromSceneName(sceneName);
        string label = stageId != null ? LastOfTheTower.Progression.StageIds.GetLabel(stageId) : null;
        stageText.text = string.IsNullOrEmpty(label) ? sceneName : label;
        waveText.text = waveManager != null && waveManager.TotalWaves > 0
            ? $"{Mathf.Min(waveManager.CurrentWave, waveManager.TotalWaves)} / {waveManager.TotalWaves}"
            : "-";
        GameManager game = GameManager.Instance;
        hpText.text = game != null ? $"{game.CurrentPlayerHp} / {game.maxPlayerHp}" : "-";
        goldText.text = game != null ? game.GetCurrentMoney().ToString() : "-";
    }

    // ───────── 그만두기 확인 ─────────
    private void Ask(PendingAction action)
    {
        pending = action;
        confirmText.text = action switch
        {
            PendingAction.Restart => "처음부터 다시 시작할까요?\n지금 전투 진행은 사라집니다.",
            PendingAction.Plaza => "광장으로 돌아갈까요?\n지금 전투 진행은 사라집니다.",
            _ => "메인 화면으로 나갈까요?\n지금 전투 진행은 사라집니다.",
        };
        confirm.gameObject.SetActive(true);
        confirm.SetAsLastSibling();
    }

    private void Confirm()
    {
        if (SceneLoadingScreen.IsLoading)
            return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        PendingAction action = pending;
        confirm.gameObject.SetActive(false);
        pending = PendingAction.None;
        switch (action)
        {
            case PendingAction.Restart: owner.RestartGame(); break;
            case PendingAction.Plaza: owner.GoToPlaza(); break;
            case PendingAction.MainMenu: owner.GoToMainMenu(); break;
        }
    }

    public void CancelConfirm()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        pending = PendingAction.None;
        confirm.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!open)
            return;
        float k = TowerPanelArt.EaseOut((Time.unscaledTime - openedAt) / 0.16f);
        rootGroup.alpha = k;
        panel.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
    }
}
