using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설치 자리 둘레에 타워 칸을 원형으로 펼치는 설치 창.
/// 칸에 마우스를 올리면 정보 카드·사거리·설치 미리보기, 클릭이나 숫자키 1~6으로 설치, 우클릭으로 닫는다.
/// 설치·골드 처리는 TowerBuildManager가 그대로 맡는다.
/// </summary>
public sealed class TowerBuildRingUI : MonoBehaviour
{
    private const float RingRadius = 46f;   // 도트
    private const float RingBox = 140f;     // 링 묶음 한 변(도트)
    private const float CardWidth = 120f;
    private const float CardHeight = 152f;

    private sealed class Slot
    {
        public GameObject prefab;
        public TowerAttack tower;
        public RectTransform rect;
        public Image glow;
        public Image frame;
        public Image icon;
        public TextMeshProUGUI price;
        public Button button;
        public float scale = 1f;
    }

    private TowerBuildManager manager;
    private Canvas canvas;
    private Stage1TutorialManager tutorial;
    private RectTransform root;
    private CanvasGroup rootGroup;
    private RectTransform ringGroup;
    private RectTransform card;
    private readonly List<Slot> slots = new List<Slot>();
    private TowerRangePreview preview;
    private BuildPoint target;
    private int selected = -1;
    private bool open;
    private float openedAt;
    private bool waitRelease;
    private Coroutine shakeRoutine;
    private Vector2 cardBase;
    private float maxDamage = 1f, maxSpeed = 1f, maxRange = 1f;

    // 정보 카드
    private TextMeshProUGUI cardTitle;
    private TextMeshProUGUI cardPrice;
    private Image chip;
    private Image chipIcon;
    private TextMeshProUGUI chipText;
    private readonly TextMeshProUGUI[] statValues = new TextMeshProUGUI[3];
    private TextMeshProUGUI damageLabel;
    private readonly Image[] statBars = new Image[3];
    private readonly TextMeshProUGUI[] routeTexts = new TextMeshProUGUI[2];
    private TextMeshProUGUI hintKey;

    public bool IsOpen => open;

    public static TowerBuildRingUI Create(TowerBuildManager manager, Canvas canvas, IReadOnlyList<GameObject> prefabs)
    {
        if (manager == null || canvas == null)
            return null;
        var ui = manager.gameObject.AddComponent<TowerBuildRingUI>();
        ui.manager = manager;
        ui.canvas = canvas.rootCanvas;
        ui.tutorial = FindAnyObjectByType<Stage1TutorialManager>();
        ui.Build(prefabs);
        return ui;
    }

    private void Build(IReadOnlyList<GameObject> prefabs)
    {
        root = TowerPanelArt.CreateRoot("TowerBuildRing", canvas);
        rootGroup = root.GetComponent<CanvasGroup>();
        preview = TowerRangePreview.Create("TowerBuildPreview");

        ringGroup = TowerPanelArt.Group("Ring", root, 0f, 0f, RingBox, RingBox);
        float c = RingBox / 2f;
        TowerPanelArt.Image("RingLine", ringGroup, "ring_line", c - 50.5f, c - 50.5f);
        var tag = TowerPanelArt.Image("CloseHint", ringGroup, "btn_tag", c - 42f, c + RingRadius + 22f);
        TowerPanelArt.Text("Text", tag.transform, 0f, 0f, 84f, 12f, 16f, TowerPanelArt.Muted, TextAlignmentOptions.Center)
            .text = "빈 곳 클릭 · 우클릭 닫기";

        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null || !prefab.TryGetComponent(out TowerAttack tower) || tower.towerData == null)
                continue;
            if (slots.Count >= 6)
                break;
            slots.Add(BuildSlot(prefab, tower, slots.Count));
            maxDamage = Mathf.Max(maxDamage, tower.GetDisplayDamage(tower.BaseDamage));
            maxSpeed = Mathf.Max(maxSpeed, 1f / Mathf.Max(0.05f, tower.BaseAttackCooldown));
            maxRange = Mathf.Max(maxRange, tower.BaseAttackRange);
        }
        for (int i = 0; i < slots.Count; i++)
        {
            float angle = (-90f + i * 360f / slots.Count) * Mathf.Deg2Rad;
            // 칸 기준점이 원 중심이라 중심 좌표를 그대로 넣는다.
            slots[i].rect.anchoredPosition = new Vector2(
                (c + RingRadius * Mathf.Cos(angle)) * TowerPanelArt.Dot,
                -(c + RingRadius * Mathf.Sin(angle)) * TowerPanelArt.Dot);
        }

        BuildCard();
        root.gameObject.SetActive(false);
    }

    private Slot BuildSlot(GameObject prefab, TowerAttack tower, int index)
    {
        var slot = new Slot { prefab = prefab, tower = tower };
        slot.rect = TowerPanelArt.Group("Slot" + (index + 1), ringGroup, 0f, 0f, 41f, 47f);
        slot.rect.pivot = new Vector2(20.5f / 41f, 1f - 18.5f / 47f);
        slot.glow = TowerPanelArt.Picture("Glow", slot.rect, -5.5f, -7.5f, 52f, 52f, false);
        slot.glow.sprite = TowerPanelArt.Sprite("soft_glow");
        slot.glow.color = new Color(1f, 0.82f, 0.35f, 0f);
        slot.frame = TowerPanelArt.Image("Frame", slot.rect, "slot", 4f, 2f, true);
        slot.icon = TowerPanelArt.Picture("Icon", slot.rect, 9.5f, 7.5f, 22f, 22f);
        slot.icon.sprite = tower.towerData.icon;
        TowerPanelArt.Image("PricePlate", slot.rect, "btn_price", 7.5f, 31f);
        TowerPanelArt.Image("Coin", slot.rect, "icon_coin", 9.5f, 32.5f);
        slot.price = TowerPanelArt.Text("Price", slot.rect, 18f, 31f, 15f, 11f, 18f, TowerPanelArt.Gold, TextAlignmentOptions.Center);
        slot.price.text = tower.BuildCost.ToString();
        TowerPanelArt.Image("Key", slot.rect, "keycap", 0f, 0f);
        TowerPanelArt.Text("KeyText", slot.rect, 0f, 0f, 11f, 10f, 17f, TowerPanelArt.KeyInk, TextAlignmentOptions.Center, false)
            .text = (index + 1).ToString();

        slot.button = TowerPanelArt.MakeButton(slot.frame);
        slot.button.onClick.AddListener(() => TryBuild(index));
        var hover = slot.frame.gameObject.AddComponent<TowerPanelHover>();
        hover.Entered = () => Select(index);
        return slot;
    }

    private void BuildCard()
    {
        card = TowerPanelArt.Group("InfoCard", root, 0f, 0f, CardWidth, CardHeight);
        TowerPanelArt.Image("Frame", card, "info_frame", 0f, 0f, true);
        TowerPanelArt.Image("Ribbon", card, "ribbon_info", -4f, -12f);
        cardTitle = TowerPanelArt.Text("Title", card, -4f, -12f, 128f, 16f, 26f, TowerPanelArt.Ivory, TextAlignmentOptions.Center);

        chip = TowerPanelArt.Image("Chip", card, "chip", 10f, 12f);
        chipIcon = TowerPanelArt.Image("Icon", chip.transform, "icon_snow", 3f, 2f);
        chipText = TowerPanelArt.Text("Text", chip.transform, 12f, 0f, 22f, 12f, 17f, new Color32(30, 40, 70, 255),
            TextAlignmentOptions.Left, false);

        TowerPanelArt.Image("Coin", card, "icon_coin", CardWidth - 40f, 14f);
        cardPrice = TowerPanelArt.Text("Price", card, CardWidth - 50f, 12f, 40f, 13f, 20f, TowerPanelArt.Gold, TextAlignmentOptions.Right);

        string[] icons = { "icon_sword", "icon_hourglass", "icon_target" };
        string[] labels = { "공격력", "공격 속도", "사거리" };
        string[] fills = { "bar_red", "bar_gold", "bar_blue" };
        for (int i = 0; i < 3; i++)
        {
            float y = 32f + i * 22f;
            TowerPanelArt.Image("StatIcon", card, icons[i], 10f, y);
            var statLabel = TowerPanelArt.Text("StatLabel", card, 22f, y - 1f, 50f, 11f, 17f, TowerPanelArt.Muted, TextAlignmentOptions.Left);
            statLabel.text = labels[i];
            if (i == 0)
                damageLabel = statLabel;
            statValues[i] = TowerPanelArt.Text("StatValue", card, 60f, y - 1f, 50f, 11f, 19f, TowerPanelArt.Ivory, TextAlignmentOptions.Right);
            TowerPanelArt.Image("BarFrame", card, "bar_frame", 10f, y + 11f);
            statBars[i] = TowerPanelArt.Image("BarFill", card, fills[i], 11f, y + 12f);
            statBars[i].type = Image.Type.Filled;
            statBars[i].fillMethod = Image.FillMethod.Horizontal;
        }

        TowerPanelArt.Image("Separator", card, "sep_info", 10f, 98f);
        TowerPanelArt.Text("RouteLabel", card, 10f, 101f, 100f, 10f, 16f, TowerPanelArt.Muted, TextAlignmentOptions.Center)
            .text = "강화 루트";
        for (int i = 0; i < 2; i++)
        {
            float x = 10f + i * 52f;
            var plate = TowerPanelArt.Image("Route", card, "btn_route", x, 112f);
            TowerPanelArt.Image("Gem", plate.transform, i == 0 ? "dia_a" : "dia_b", 3f, 3f);
            routeTexts[i] = TowerPanelArt.Text("Text", plate.transform, 9f, 0f, 37f, 13f, 15f, TowerPanelArt.Ivory, TextAlignmentOptions.Center);
        }

        TowerPanelArt.Image("HintKey", card, "keycap", 14f, CardHeight - 18f);
        hintKey = TowerPanelArt.Text("HintKeyText", card, 14f, CardHeight - 18f, 11f, 10f, 17f, TowerPanelArt.KeyInk,
            TextAlignmentOptions.Center, false);
        TowerPanelArt.Text("Hint", card, 28f, CardHeight - 18f, 86f, 11f, 17f, new Color32(220, 205, 170, 255), TextAlignmentOptions.Left)
            .text = "또는 클릭으로 설치";
        card.gameObject.SetActive(false);
    }

    public RectTransform GetSlotRect(GameObject prefab)
    {
        foreach (Slot slot in slots)
            if (slot.prefab == prefab)
                return slot.frame.rectTransform;
        return null;
    }

    public void Show(BuildPoint point)
    {
        if (point == null)
            return;
        target = point;
        open = true;
        openedAt = Time.unscaledTime;
        // 창을 연 클릭(마우스를 누른 채)이 그 자리에 뜬 버튼을 바로 누르지 않게, 뗄 때까지 클릭을 받지 않는다.
        // (자리를 누르는 순간 창이 열리고, 같은 프레임에 UI가 '눌린 곳'을 새 버튼으로 잡아 떼는 순간 클릭이 됐다)
        waitRelease = Input.GetMouseButton(0);
        rootGroup.blocksRaycasts = !waitRelease;
        selected = -1;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        TowerPanelArt.FitRoot(root, canvas);
        rootGroup.alpha = 0f;
        ringGroup.localScale = Vector3.one * 0.85f;

        // 화면 가장자리 자리에서는 링이 잘리지 않게 안쪽으로 당긴다(미리보기는 실제 자리 그대로).
        Rect area = root.rect;
        Vector2 center = TowerPanelArt.WorldToRoot(root, point.transform.position);
        float edge = (RingRadius + 22f) * TowerPanelArt.Dot;
        center.x = Mathf.Clamp(center.x, area.xMin + edge, area.xMax - edge);
        center.y = Mathf.Clamp(center.y, area.yMin + edge + 40f, area.yMax - edge);
        ringGroup.anchorMin = ringGroup.anchorMax = ringGroup.pivot = new Vector2(0.5f, 0.5f);
        ringGroup.anchoredPosition = center;

        // 정보 카드는 링 오른쪽, 자리가 없으면 왼쪽
        float cardW = CardWidth * TowerPanelArt.Dot;
        float cardH = CardHeight * TowerPanelArt.Dot;
        float gap = 66f * TowerPanelArt.Dot;
        float x = center.x + gap;
        if (x + cardW > area.xMax - 12f)
            x = center.x - gap - cardW;
        float top = Mathf.Clamp(center.y + cardH / 2f, area.yMin + cardH + 12f, area.yMax - 48f);
        cardBase = new Vector2(x, top);
        TowerPanelArt.PlaceTopLeft(card, cardBase);
        card.gameObject.SetActive(false);

        foreach (Slot slot in slots)
            SetSlotLook(slot, false);
        RefreshSlots();
        preview.Show(point.transform.position, 0f, Color.white, null, Vector3.one, true);
    }

    public void Hide()
    {
        open = false;
        target = null;
        selected = -1;
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
        Vector2 basePosition = ringGroup.anchoredPosition;
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < 0.25f)
        {
            float k = 1f - (Time.unscaledTime - start) / 0.25f;
            ringGroup.anchoredPosition = basePosition + new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * 9f * k, 0f);
            yield return null;
        }
        ringGroup.anchoredPosition = basePosition;
        shakeRoutine = null;
    }

    private bool IsRestricted(int index) =>
        tutorial != null && tutorial.IsTowerChoiceRestricted && index != 0;

    private void TryBuild(int index)
    {
        if (!open || index < 0 || index >= slots.Count || IsRestricted(index))
            return;
        Select(index);
        manager.BuildConfiguredTower(slots[index].prefab);
    }

    private void Select(int index)
    {
        if (!open || index < 0 || index >= slots.Count || index == selected)
            return;
        if (selected >= 0)
            SetSlotLook(slots[selected], false);
        selected = index;
        Slot slot = slots[index];
        SetSlotLook(slot, true);
        FillCard(slot, index);

        var renderer = slot.prefab.GetComponent<SpriteRenderer>();
        var upgrade = slot.prefab.GetComponent<TowerUpgrade>();
        Sprite ghost = TowerSkin.Resolve(upgrade != null && upgrade.basicTowerSprite != null ? upgrade.basicTowerSprite :
            renderer != null ? renderer.sprite : null);
        Color tint = new Color(0.47f, 0.86f, 1f, 1f);
        preview.Show(target.transform.position, slot.tower.BaseAttackRange, tint, ghost, slot.prefab.transform.localScale, true);
    }

    private void SetSlotLook(Slot slot, bool on)
    {
        TowerPanelArt.SetSprite(slot.frame, on ? "slot_sel" : "slot");
        slot.frame.rectTransform.anchoredPosition = on
            ? new Vector2(2f * TowerPanelArt.Dot, -0f)
            : new Vector2(4f * TowerPanelArt.Dot, -2f * TowerPanelArt.Dot);
        slot.scale = on ? 1.08f : 1f;
    }

    private void FillCard(Slot slot, int index)
    {
        TowerData data = slot.tower.towerData;
        cardTitle.text = data.towerName;
        cardPrice.text = slot.tower.BuildCost + " G";

        bool fire = data.element == ElementType.Fire;
        bool ice = data.element == ElementType.Ice;
        chip.gameObject.SetActive(fire || ice || data.detectsStealth);
        if (fire || ice)
        {
            TowerPanelArt.SetSprite(chipIcon, fire ? "icon_flame" : "icon_snow");
            chipText.text = fire ? "불" : "얼음";
        }
        else if (data.detectsStealth)
        {
            TowerPanelArt.SetSprite(chipIcon, "icon_target");
            chipText.text = "감지";
        }

        int damage = slot.tower.GetDisplayDamage(slot.tower.BaseDamage);
        float cooldown = slot.tower.BaseAttackCooldown;
        float range = slot.tower.BaseAttackRange;
        // 맵 환경 보정(설원: 불 +20%·얼음 -20% 등)은 이미 공격력에 들어가 있어 옆에 비율로 알려 준다.
        float rate = EnvironmentManager.Instance != null ? EnvironmentManager.Instance.GetRate(data.element) : 0f;
        statValues[0].text = Mathf.Approximately(rate, 0f) ? damage.ToString()
            : $"<size=75%><color={(rate > 0f ? "#8CE878" : "#FF8A6A")}>{(rate > 0f ? "+" : "")}{rate * 100f:0}%</color></size> {damage}";
        damageLabel.text = data.damageKind == DamageKind.Magic ? "마법 공격력" : "물리 공격력";
        statValues[1].text = cooldown.ToString("0.##") + "초";
        statValues[2].text = range.ToString("0.#");
        statBars[0].fillAmount = damage / maxDamage;
        statBars[1].fillAmount = 1f / Mathf.Max(0.05f, cooldown) / maxSpeed;
        statBars[2].fillAmount = range / maxRange;

        routeTexts[0].text = ShortRoute(data.pathA != null ? data.pathA.routeName : "");
        routeTexts[1].text = ShortRoute(data.pathB != null ? data.pathB.routeName : "");
        hintKey.text = (index + 1).ToString();
        card.gameObject.SetActive(true);
    }

    private static string ShortRoute(string routeName) =>
        string.IsNullOrEmpty(routeName) ? "-" : routeName.Replace(" 루트", "");

    private void RefreshSlots()
    {
        int money = GameManager.Instance != null ? GameManager.Instance.GetCurrentMoney() : int.MaxValue;
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            bool locked = IsRestricted(i);
            bool affordable = money >= slot.tower.BuildCost;
            slot.button.interactable = !locked;
            slot.icon.color = locked ? new Color(0.35f, 0.35f, 0.35f, 1f) :
                affordable ? Color.white : new Color(0.55f, 0.5f, 0.5f, 1f);
            slot.price.color = affordable ? TowerPanelArt.Gold : TowerPanelArt.Bad;
        }
    }

    private void Update()
    {
        if (!open)
            return;
        if (target == null)
        {
            manager.CloseBuildPanelSilently();
            return;
        }
        if (waitRelease && !Input.GetMouseButton(0))
        {
            waitRelease = false;
            rootGroup.blocksRaycasts = true;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                TryBuild(i);
                return;
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            manager.CloseBuildPanel();
            return;
        }

        RefreshSlots();

        // 펼침: 살짝 커지며 나타나고, 고른 칸은 조금 더 크다.
        float k = TowerPanelArt.EaseOut((Time.unscaledTime - openedAt) / 0.14f);
        rootGroup.alpha = k;
        ringGroup.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            float current = slot.rect.localScale.x;
            float next = Mathf.MoveTowards(current, slot.scale, Time.unscaledDeltaTime * 1.5f);
            slot.rect.localScale = new Vector3(next, next, 1f);
            slot.glow.color = new Color(1f, 0.82f, 0.35f, i == selected ? 0.45f + 0.25f * pulse : 0f);
        }
    }

    private void OnDestroy()
    {
        if (preview != null)
            Destroy(preview.gameObject);
    }
}
