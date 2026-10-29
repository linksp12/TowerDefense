using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 업그레이드 창(도트 디자인). 지금 능력치, 루트별로 바뀌는 수치, 강화·판매 버튼과 Q/W 단축키를 보여 준다.
/// 강화·판매 처리와 열고 닫는 흐름은 TowerUpgradeUI가 그대로 맡는다.
/// </summary>
public sealed class TowerUpgradePanelUI : MonoBehaviour
{
    private const float Width = 236f;
    private const float Height = 188f;
    private const float CardHeight = 126f;
    // 화면에서 너무 크지 않게 창 전체를 줄여 보여 준다(설치 링은 원래 크기).
    private const float PanelScale = 0.8f;

    private enum Mode { TwoPath, Final, Max }

    private sealed class Row
    {
        public GameObject root;
        public Image icon;
        public Image arrow;
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
    }

    private sealed class RouteCard
    {
        public RectTransform rect;
        public float width;
        public Image background;
        public Image glow;
        public Image arch;
        public Image art;
        public TextMeshProUGUI title;
        public TextMeshProUGUI caption;
        public TextMeshProUGUI levelName;
        public TextMeshProUGUI feature;
        public readonly Row[] rows = new Row[3];
        public Image button;
        public Button buttonComponent;
        public Image coin;
        public TextMeshProUGUI buttonText;
        public TextMeshProUGUI doneText;
        public TextMeshProUGUI keyText;
        public GameObject key;
        public bool hovered;
        public float previewRange;
    }

    private TowerUpgradeUI owner;
    private Canvas canvas;
    private RectTransform root;
    private CanvasGroup rootGroup;
    private RectTransform panel;
    private RectTransform tail;
    private TowerRangePreview preview;
    private TowerUpgrade tower;
    private TowerAttack attack;
    private Mode mode;
    private bool open;
    private float openedAt;
    private Coroutine shakeRoutine;

    private Image portrait;
    private TextMeshProUGUI title;
    private TextMeshProUGUI levelText;
    private readonly Image[] levelGems = new Image[3];
    private readonly TextMeshProUGUI[] stats = new TextMeshProUGUI[4];
    private Image elementIcon;
    private RouteCard cardA;
    private RouteCard cardB;
    private RouteCard cardWide;
    private GameObject middleGem;
    private TextMeshProUGUI sellText;
    private TextMeshProUGUI targetText;

    public bool IsVisible => open && root != null && root.gameObject.activeInHierarchy;
    public RectTransform PathATarget => mode == Mode.TwoPath ? cardA.rect : cardWide.rect;

    public static TowerUpgradePanelUI Create(TowerUpgradeUI owner, Canvas canvas)
    {
        if (owner == null || canvas == null)
            return null;
        var ui = owner.gameObject.AddComponent<TowerUpgradePanelUI>();
        ui.owner = owner;
        ui.canvas = canvas.rootCanvas;
        ui.Build();
        return ui;
    }

    // ───────── 만들기 ─────────
    private void Build()
    {
        root = TowerPanelArt.CreateRoot("TowerUpgradePanel", canvas);
        rootGroup = root.GetComponent<CanvasGroup>();
        preview = TowerRangePreview.Create("TowerUpgradeRange");
        panel = TowerPanelArt.Group("Panel", root, 0f, 0f, Width, Height);

        tail = TowerPanelArt.Image("Tail", panel, "tail", Width / 2f - 7.5f, -8f).rectTransform;
        TowerPanelArt.Image("Frame", panel, "up_frame", 0f, 0f, true);
        TowerPanelArt.Image("Ribbon", panel, "ribbon_up", 52f, -11f, true);
        title = TowerPanelArt.Text("Title", panel, 58f, -11f, 90f, 16f, 27f, TowerPanelArt.Ivory, TextAlignmentOptions.Center);
        TowerPanelArt.Image("Medal", panel, "medal", 8f, -22f, true);
        // 메달 안쪽 원으로 잘라 그림이 테두리 밖으로 나가지 않게 한다.
        var medalMask = TowerPanelArt.Picture("MedalMask", panel, 13f, -17f, 32f, 32f, false);
        medalMask.sprite = TowerPanelArt.Sprite("mask_circle");
        medalMask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        portrait = TowerPanelArt.Picture("Portrait", medalMask.transform, 0f, 0f, 32f, 32f, false);
        for (int i = 0; i < 3; i++)
            levelGems[i] = TowerPanelArt.Image("Level" + (i + 1), panel, "lv_off", 150f + i * 14f, -8f);
        levelText = TowerPanelArt.Text("LevelText", panel, 146f, 3f, 48f, 8f, 15f, new Color32(225, 205, 255, 255), TextAlignmentOptions.Center);

        var close = TowerPanelArt.Image("Close", panel, "close", Width - 14f, -6f);
        TowerPanelArt.MakeButton(close).onClick.AddListener(() => owner.CloseFromPanel());
        TowerPanelArt.Text("X", close.transform, 0f, -0.5f, 17f, 17f, 26f, TowerPanelArt.Ivory, TextAlignmentOptions.Center).text = "×";

        TowerPanelArt.Image("Strip", panel, "strip", 12f, 14f);
        string[] icons = { "icon_sword", "icon_hourglass", "icon_target", "icon_snow" };
        float[] columns = { 18f, 74f, 130f, 180f };
        for (int i = 0; i < 4; i++)
        {
            var icon = TowerPanelArt.Image("StatIcon", panel, icons[i], columns[i], 18f);
            if (i == 3)
                elementIcon = icon;
            stats[i] = TowerPanelArt.Text("Stat", panel, columns[i] + 12f, 17f, 44f, 11f, 18f, TowerPanelArt.Ivory, TextAlignmentOptions.Left);
            stats[i].richText = true;
        }

        cardA = BuildCard("RouteA", 12f, 102f, "card_a", "arch_a", "sep", "btn_gold", "Q");
        cardB = BuildCard("RouteB", 122f, 102f, "card_b", "arch_b", "sep", "btn_gold", "W");
        cardWide = BuildCard("Final", 12f, 212f, "card_wide_a", "arch_a", "sep_wide", "btn_gold_wide", "Q");
        middleGem = TowerPanelArt.Image("MiddleGem", panel, "gem_mid", Width / 2f - 5.5f, 42f).gameObject;

        cardA.buttonComponent.onClick.AddListener(() => owner.ChoosePathA());
        cardB.buttonComponent.onClick.AddListener(() => owner.ChoosePathB());
        cardWide.buttonComponent.onClick.AddListener(() => owner.ChooseFinalUpgrade());

        // 공격 대상 고르기(누를 때마다 선두 → 후미 → 강한 적 → 가까운 적, E 키)
        var target = TowerPanelArt.Image("Target", panel, "btn_target", 12f, Height - 23f);
        TowerPanelArt.MakeButton(target).onClick.AddListener(CycleTarget);
        TowerPanelArt.Image("Icon", target.transform, "icon_target", 5f, 3f);
        targetText = TowerPanelArt.Text("Text", target.transform, 16f, 0f, 78f, 15f, 17f, TowerPanelArt.Ivory, TextAlignmentOptions.Left);
        targetText.richText = true;
        var targetKey = TowerPanelArt.Image("Key", target.transform, "keycap", 88f, -4f);
        TowerPanelArt.Text("KeyText", targetKey.transform, 0f, 0f, 11f, 10f, 17f, TowerPanelArt.KeyInk,
            TextAlignmentOptions.Center, false).text = "E";
        var sell = TowerPanelArt.Image("Sell", panel, "btn_red", Width - 72f, Height - 23f);
        TowerPanelArt.MakeButton(sell).onClick.AddListener(() => owner.SellSelected());
        TowerPanelArt.Image("Coin", sell.transform, "icon_coin", 6f, 3f);
        sellText = TowerPanelArt.Text("Text", sell.transform, 16f, 0f, 46f, 15f, 18f, TowerPanelArt.Ivory, TextAlignmentOptions.Left);

        root.gameObject.SetActive(false);
    }

    private RouteCard BuildCard(string name, float x, float width, string background, string arch,
        string separator, string button, string key)
    {
        var card = new RouteCard { width = width };
        card.rect = TowerPanelArt.Group(name, panel, x, 38f, width, CardHeight);
        card.glow = TowerPanelArt.Picture("Glow", card.rect, -8f, -8f, width + 16f, CardHeight + 16f, false);
        card.glow.sprite = TowerPanelArt.Sprite("soft_glow");
        card.glow.type = Image.Type.Simple;
        card.glow.color = new Color(1f, 0.82f, 0.35f, 0f);
        card.background = TowerPanelArt.Image("Background", card.rect, background, 0f, 0f, true);
        card.title = TowerPanelArt.Text("Title", card.rect, 0f, 2.5f, width, 15f, 21f, TowerPanelArt.Ivory, TextAlignmentOptions.Center);
        card.arch = TowerPanelArt.Image("Arch", card.rect, arch, 6f, 22f);
        card.art = TowerPanelArt.Picture("Art", card.rect, 8f, 25f, 26f, 31f);
        float textWidth = width - 46f;
        card.caption = TowerPanelArt.Text("Caption", card.rect, 40f, 22f, textWidth, 9f, 15f, TowerPanelArt.Muted, TextAlignmentOptions.Left);
        card.levelName = TowerPanelArt.Text("LevelName", card.rect, 40f, 32f, textWidth, 10f, 16f, TowerPanelArt.Gold, TextAlignmentOptions.Left);
        card.feature = TowerPanelArt.Text("Feature", card.rect, 40f, 43f, textWidth, 18f, 15f, TowerPanelArt.Ivory, TextAlignmentOptions.TopLeft);
        card.feature.textWrappingMode = TextWrappingModes.Normal;
        card.feature.enableAutoSizing = true;
        card.feature.fontSizeMin = 12f;
        card.feature.fontSizeMax = card.feature.fontSize;
        card.feature.overflowMode = TextOverflowModes.Ellipsis;
        TowerPanelArt.Image("Separator", card.rect, separator, 8f, 62f);
        for (int i = 0; i < card.rows.Length; i++)
        {
            float y = 67f + i * 11f;
            var row = new Row { root = TowerPanelArt.Group("Row", card.rect, 0f, y - 1f, width, 11f).gameObject };
            row.icon = TowerPanelArt.Image("Icon", row.root.transform, "icon_sword", 8f, 1f);
            row.label = TowerPanelArt.Text("Label", row.root.transform, 20f, 0f, 40f, 11f, 16f, TowerPanelArt.Muted, TextAlignmentOptions.Left);
            row.value = TowerPanelArt.Text("Value", row.root.transform, width - 74f, 0f, 60f, 11f, 17f, TowerPanelArt.Ivory, TextAlignmentOptions.Right);
            row.arrow = TowerPanelArt.Image("Arrow", row.root.transform, "icon_up", width - 11f, 3f);
            card.rows[i] = row;
        }

        float buttonWidth = TowerPanelArt.Sprite(button) != null ? TowerPanelArt.Sprite(button).rect.width : 90f;
        card.button = TowerPanelArt.Image("Button", card.rect, button, 6f, CardHeight - 24f);
        card.buttonComponent = TowerPanelArt.MakeButton(card.button);
        card.coin = TowerPanelArt.Image("Coin", card.button.transform, "icon_coin", buttonWidth / 2f - 27f, 4.5f);
        card.buttonText = TowerPanelArt.Text("Text", card.button.transform, buttonWidth / 2f - 15f, 0f, 60f, 16f, 21f,
            TowerPanelArt.Ink, TextAlignmentOptions.Left, false);
        card.doneText = TowerPanelArt.Text("Done", card.rect, 6f, CardHeight - 24f, buttonWidth, 17f, 19f, TowerPanelArt.Gold,
            TextAlignmentOptions.Center);
        card.doneText.text = "모든 강화를 마쳤습니다";

        card.key = TowerPanelArt.Image("Key", card.rect, "keycap", -3f, -3f).gameObject;
        card.keyText = TowerPanelArt.Text("KeyText", card.key.transform, 0f, 0f, 11f, 10f, 17f, TowerPanelArt.KeyInk,
            TextAlignmentOptions.Center, false);
        card.keyText.text = key;

        var hover = card.background.gameObject.AddComponent<TowerPanelHover>();
        hover.Entered = () => { card.hovered = true; UpdateRangePreview(); };
        hover.Exited = () => { card.hovered = false; UpdateRangePreview(); };
        var buttonHover = card.button.gameObject.AddComponent<TowerPanelHover>();
        buttonHover.Entered = hover.Entered;
        buttonHover.Exited = hover.Exited;
        return card;
    }

    // ───────── 열기·닫기 ─────────
    public void Show(TowerUpgrade target)
    {
        if (target == null)
            return;
        tower = target;
        attack = target.GetComponent<TowerAttack>();
        open = true;
        openedAt = Time.unscaledTime;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        TowerPanelArt.FitRoot(root, canvas);
        rootGroup.alpha = 0f;   // 첫 프레임에 원래 크기로 번쩍이지 않게
        panel.localScale = Vector3.one * (PanelScale * 0.94f);
        cardA.hovered = cardB.hovered = cardWide.hovered = false;
        Refresh();
        PlaceNearTower();
    }

    public void Hide()
    {
        open = false;
        tower = null;
        attack = null;
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        if (root != null)
            root.gameObject.SetActive(false);
        if (preview != null)
            preview.Hide();
    }

    public void Shake()
    {
        if (!open)
            return;
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Vector2 basePosition = panel.anchoredPosition;
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < 0.25f)
        {
            float k = 1f - (Time.unscaledTime - start) / 0.25f;
            panel.anchoredPosition = basePosition + new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * 9f * k, 0f);
            yield return null;
        }
        panel.anchoredPosition = basePosition;
        shakeRoutine = null;
    }

    private Vector3 TowerPosition()
    {
        BuildPoint point = tower.GetOwnerBuildPoint();
        return point != null ? point.transform.position : tower.transform.position;
    }

    // 타워 아래 → 위 → 옆 순서로 타워를 가리지 않는 자리를 고른다. 꼬리는 타워 쪽을 가리킨다.
    private void PlaceNearTower()
    {
        Rect area = root.rect;
        Vector2 p = TowerPanelArt.WorldToRoot(root, TowerPosition());
        float unit = TowerPanelArt.Dot * PanelScale;
        float w = Width * unit;
        float h = Height * unit;
        const float margin = 8f;
        const float topReserve = 70f;   // 리본·메달이 창 위로 튀어나온 높이
        float belowTop = p.y - 60f;
        float aboveTop = p.y + 110f + h;
        tail.pivot = new Vector2(0.5f, 0.5f);

        if (belowTop - h >= area.yMin + margin || aboveTop <= area.yMax - topReserve)
        {
            bool below = belowTop - h >= area.yMin + margin;
            float left = Mathf.Clamp(p.x - w / 2f, area.xMin + margin, area.xMax - margin - w);
            TowerPanelArt.PlaceTopLeft(panel, new Vector2(left, below ? belowTop : aboveTop));
            float tailX = Mathf.Clamp((p.x - left) / unit, 24f, Width - 24f);
            tail.anchoredPosition = below
                ? new Vector2(tailX * TowerPanelArt.Dot, 4f * TowerPanelArt.Dot)
                : new Vector2(tailX * TowerPanelArt.Dot, -(Height + 3.5f) * TowerPanelArt.Dot);
            tail.localEulerAngles = new Vector3(0f, 0f, below ? 0f : 180f);
            return;
        }

        // 위아래 모두 좁으면 타워 옆(오른쪽 우선)
        bool right = p.x + 70f + w <= area.xMax - margin;
        float sideLeft = right ? p.x + 70f : p.x - 70f - w;
        sideLeft = Mathf.Clamp(sideLeft, area.xMin + margin, area.xMax - margin - w);
        float sideTop = Mathf.Clamp(p.y + h / 2f, area.yMin + margin + h, area.yMax - topReserve);
        TowerPanelArt.PlaceTopLeft(panel, new Vector2(sideLeft, sideTop));
        float tailY = Mathf.Clamp((sideTop - p.y) / unit, 40f, Height - 24f);
        tail.anchoredPosition = right
            ? new Vector2(-3.5f * TowerPanelArt.Dot, -tailY * TowerPanelArt.Dot)
            : new Vector2((Width + 3.5f) * TowerPanelArt.Dot, -tailY * TowerPanelArt.Dot);
        tail.localEulerAngles = new Vector3(0f, 0f, right ? 90f : -90f);
    }

    // ───────── 내용 채우기 ─────────
    public void Refresh()
    {
        if (tower == null || attack == null)
            return;
        mode = tower.level <= 1 ? Mode.TwoPath : tower.level == 2 ? Mode.Final : Mode.Max;

        title.text = tower.GetTowerName();
        var body = tower.GetComponent<SpriteRenderer>();
        TowerPanelArt.FitVisible(portrait, body != null ? body.sprite : null, 5f, 3f, 22f, 26f, false);
        for (int i = 0; i < levelGems.Length; i++)
            TowerPanelArt.SetSprite(levelGems[i], i < tower.level ? "lv_on" : "lv_off");
        levelText.text = $"Lv.{tower.level} / {tower.maxLevel}";

        int damage = attack.GetDisplayDamage(attack.damage);
        stats[0].text = Stat(attack.DamageKind == DamageKind.Magic ? "마법" : "물리", damage.ToString());
        RefreshTargetText();
        stats[1].text = Stat("간격", attack.attackCooldown.ToString("0.##") + "초");
        stats[2].text = Stat("사거리", attack.attackRange.ToString("0.#"));
        bool fire = attack.Element == ElementType.Fire;
        bool ice = attack.Element == ElementType.Ice;
        elementIcon.gameObject.SetActive(fire || ice);
        stats[3].gameObject.SetActive(fire || ice);
        if (fire || ice)
        {
            TowerPanelArt.SetSprite(elementIcon, fire ? "icon_flame" : "icon_snow");
            stats[3].text = fire ? "<color=#FFA05A>불 속성</color>" : "<color=#96D7FF>얼음 속성</color>";
        }

        int money = GameManager.Instance != null ? GameManager.Instance.GetCurrentMoney() : int.MaxValue;
        int cost = tower.GetUpgradeCost();
        cardA.rect.gameObject.SetActive(mode == Mode.TwoPath);
        cardB.rect.gameObject.SetActive(mode == Mode.TwoPath);
        middleGem.SetActive(mode == Mode.TwoPath);
        cardWide.rect.gameObject.SetActive(mode != Mode.TwoPath);

        if (mode == Mode.TwoPath)
        {
            FillRoute(cardA, TowerUpgrade.UpgradePath.PathA, 2, tower.GetPathAName(), tower.GetPathASprite(),
                "다음 단계", tower.GetPathFeature(TowerUpgrade.UpgradePath.PathA), cost, money);
            FillRoute(cardB, TowerUpgrade.UpgradePath.PathB, 2, tower.GetPathBName(), tower.GetPathBSprite(),
                "다음 단계", tower.GetPathFeature(TowerUpgrade.UpgradePath.PathB), cost, money);
        }
        else
        {
            bool pathA = tower.path == TowerUpgrade.UpgradePath.PathA;
            TowerPanelArt.SetSprite(cardWide.background, mode == Mode.Max ? "card_wide_max" : pathA ? "card_wide_a" : "card_wide_b");
            TowerPanelArt.SetSprite(cardWide.arch, mode == Mode.Max ? "arch_max" : pathA ? "arch_a" : "arch_b");
            string routeName = pathA ? tower.GetPathAName() : tower.GetPathBName();
            if (mode == Mode.Final)
            {
                FillRoute(cardWide, tower.path, 3, "최종 강화 · " + routeName, tower.GetFinalUpgradeSprite(),
                    "최종 단계", tower.GetPathEffect(tower.path), cost, money);
            }
            else
            {
                FillMax(cardWide, routeName, damage);
            }
        }

        sellText.text = $"판매  {tower.GetSellPrice()} G";
        UpdateRangePreview();
    }

    private void CycleTarget()
    {
        if (!open || attack == null)
            return;
        attack.targetPriority = (TargetPriority)(((int)attack.targetPriority + 1) % 4);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        RefreshTargetText();
    }

    private void RefreshTargetText() =>
        targetText.text = $"<color=#C4B092>공격 대상</color>  {TowerTargeting.Label(attack.targetPriority)}";

    private static string Stat(string label, string value) => $"<color=#C4B092>{label}</color> {value}";

    private void FillRoute(RouteCard card, TowerUpgrade.UpgradePath path, int level, string routeName, Sprite art,
        string caption, string feature, int cost, int money)
    {
        card.title.text = routeName;
        card.title.color = TowerPanelArt.Ivory;
        FitArchArt(card, art);
        card.caption.text = caption;
        card.levelName.text = tower.GetLevelName(path, level);
        card.feature.text = ShortFeature(feature);

        int row = 0;
        int damage = attack.GetDisplayDamage(attack.damage);
        card.previewRange = attack.attackRange;
        if (tower.TryGetStatPreview(path, level, out int nextDamage, out float nextCooldown, out float nextRange))
        {
            card.previewRange = nextRange;
            if (nextDamage != damage)
                SetRow(card.rows[row++], "icon_sword", "공격력", $"{damage} → {nextDamage}", nextDamage > damage);
            if (!Mathf.Approximately(nextCooldown, attack.attackCooldown))
                SetRow(card.rows[row++], "icon_hourglass", "간격",
                    $"{attack.attackCooldown:0.##} → {nextCooldown:0.##}초", nextCooldown < attack.attackCooldown);
            if (!Mathf.Approximately(nextRange, attack.attackRange) && row < card.rows.Length)
                SetRow(card.rows[row++], "icon_target", "사거리", $"{attack.attackRange:0.#} → {nextRange:0.#}", nextRange > attack.attackRange);
        }
        for (int i = row; i < card.rows.Length; i++)
            card.rows[i].root.SetActive(false);

        bool affordable = money >= cost;
        card.button.gameObject.SetActive(true);
        card.doneText.gameObject.SetActive(false);
        card.key.SetActive(true);
        string buttonSprite = card.width > 150f
            ? (affordable ? "btn_gold_wide" : "btn_stone_wide")
            : (affordable ? "btn_gold" : "btn_stone");
        TowerPanelArt.SetSprite(card.button, buttonSprite);
        card.buttonText.text = $"강화  {cost} G";
        card.buttonText.color = affordable ? TowerPanelArt.Ink : new Color32(150, 40, 30, 255);
    }

    private void FillMax(RouteCard card, string routeName, int damage)
    {
        card.title.text = "최대 레벨 · " + routeName;
        card.title.color = TowerPanelArt.Ink;   // 밝은 금색 띠 위라 진한 글자
        var body = tower.GetComponent<SpriteRenderer>();
        FitArchArt(card, body != null ? body.sprite : null);
        card.caption.text = "현재 단계";
        card.levelName.text = tower.GetTowerName();
        card.feature.text = ShortFeature(tower.GetPathEffect(tower.path));
        card.previewRange = attack.attackRange;
        SetRow(card.rows[0], "icon_sword", "공격력", damage.ToString(), null);
        SetRow(card.rows[1], "icon_hourglass", "간격", attack.attackCooldown.ToString("0.##") + "초", null);
        SetRow(card.rows[2], "icon_target", "사거리", attack.attackRange.ToString("0.#"), null);
        card.button.gameObject.SetActive(false);
        card.doneText.gameObject.SetActive(true);
        card.key.SetActive(false);
    }

    // 아치 안쪽(받침 위)에 타워를 세운다.
    private static void FitArchArt(RouteCard card, Sprite art) =>
        TowerPanelArt.FitVisible(card.art, art, 10f, 27f, 22f, 28f, true);

    private static void SetRow(Row row, string icon, string label, string value, bool? good)
    {
        row.root.SetActive(true);
        TowerPanelArt.SetSprite(row.icon, icon);
        row.label.text = label;
        row.value.text = value;
        row.arrow.gameObject.SetActive(good.HasValue);
        if (good.HasValue)
        {
            TowerPanelArt.SetSprite(row.arrow, good.Value ? "icon_up" : "icon_down");
            row.value.color = good.Value ? TowerPanelArt.Good : TowerPanelArt.Bad;
        }
        else
        {
            row.value.color = TowerPanelArt.Ivory;
        }
    }

    // 데이터 설명이 길면 "특징:" 뒤만 보여 준다.
    private static string ShortFeature(string feature)
    {
        if (string.IsNullOrEmpty(feature))
            return "";
        int index = feature.IndexOf("특징:");
        return index >= 0 ? feature.Substring(index + 3).Trim() : feature.Trim();
    }

    private void UpdateRangePreview()
    {
        if (!open || tower == null || attack == null)
            return;
        RouteCard hovered = cardA.hovered && mode == Mode.TwoPath ? cardA :
            cardB.hovered && mode == Mode.TwoPath ? cardB :
            cardWide.hovered && mode != Mode.TwoPath ? cardWide : null;
        bool changes = hovered != null && !Mathf.Approximately(hovered.previewRange, attack.attackRange);
        float radius = changes ? hovered.previewRange : attack.attackRange;
        Color color = changes ? new Color(0.55f, 0.92f, 0.5f, 1f) : new Color(1f, 0.86f, 0.47f, 1f);
        preview.Show(TowerPosition(), radius, color, null, Vector3.one, false);
    }

    // ───────── 매 프레임 ─────────
    private void Update()
    {
        if (!open)
            return;
        if (tower == null || attack == null)
        {
            owner.CloseInstantly();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (mode == Mode.TwoPath) owner.ChoosePathA();
            else if (mode == Mode.Final) owner.ChooseFinalUpgrade();
            return;
        }
        if (Input.GetKeyDown(KeyCode.W) && mode == Mode.TwoPath)
        {
            owner.ChoosePathB();
            return;
        }
        if (Input.GetKeyDown(KeyCode.E))
            CycleTarget();
        if (Input.GetMouseButtonDown(1))
        {
            owner.CloseFromPanel();
            return;
        }

        float k = TowerPanelArt.EaseOut((Time.unscaledTime - openedAt) / 0.14f);
        rootGroup.alpha = k;
        panel.localScale = Vector3.one * (PanelScale * Mathf.Lerp(0.94f, 1f, k));
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        Glow(cardA, pulse);
        Glow(cardB, pulse);
        Glow(cardWide, pulse);
    }

    private static void Glow(RouteCard card, float pulse)
    {
        card.glow.color = new Color(1f, 0.82f, 0.35f, card.hovered ? 0.35f + 0.2f * pulse : 0f);
    }

    private void OnDestroy()
    {
        if (preview != null)
            Destroy(preview.gameObject);
    }
}
