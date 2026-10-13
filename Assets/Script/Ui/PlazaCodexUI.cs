using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LastOfTheTower.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 광장 도감: 스테이지 웨이브에 등장하는 몬스터와 타워 정보를 정의 데이터에서 읽어 보여 준다.
// 기록을 저장하거나 수치를 바꾸지 않으며, 잠금 여부는 기존 스테이지 해금 기록으로 판단한다.
public sealed class PlazaCodexUI : MonoBehaviour
{
    [Serializable]
    private class StageEntry
    {
        public string stageId;
        public WaveData[] waves;
    }

    private sealed class MonsterEntry
    {
        public MonsterData data;
        public int stageNumber;
        public string stageId;
        public bool stealth;
        public bool unlocked;
        public readonly List<int> appearances = new List<int>();
    }

    [Header("데이터")]
    [SerializeField] private StageEntry[] stages;
    [SerializeField] private TowerData[] towers;

    [Header("연결")]
    [SerializeField] private DialogueUI dialogue;
    [SerializeField] private QuestUI questBoard;

    [Header("표시")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Sprite plateSprite;
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite darkSprite;
    [SerializeField] private Sprite insetSprite;
    [Tooltip("보호막이 있는 몬스터 초상화에 겹쳐 그릴 방어막 이미지")]
    [SerializeField] private Sprite shieldOverlay;

    private const float PixelScale = 4f / 3f;
    private const float PanelWidth = 1480f;
    private const float PanelHeight = 860f;
    private static readonly Color Gold = new Color32(247, 214, 126, 255);
    private static readonly Color Ivory = new Color32(243, 234, 208, 255);
    private static readonly Color Muted = new Color32(191, 169, 130, 255);
    private static readonly Color DarkLabel = new Color32(58, 31, 11, 255);
    private static readonly Color Bonus = new Color32(154, 220, 160, 255);
    private static readonly Color CardIdle = new Color(0.82f, 0.82f, 0.82f, 1f);
    private static readonly Color Silhouette = new Color(0f, 0f, 0f, 0.82f);

    private readonly List<MonsterEntry> monsters = new List<MonsterEntry>();
    private readonly List<Button> cards = new List<Button>();
    private readonly List<GameObject> detailItems = new List<GameObject>();
    private GameObject root;
    private CanvasGroup group;
    private RectTransform panel;
    private RectTransform listArea;
    private RectTransform detailArea;
    private TMP_Text countLabel;
    private Button monsterTab;
    private Button towerTab;
    private bool showTowers;
    private int selectedMonster;
    private int selectedTower;
    private bool closing;
    private Coroutine fade;
    private float panelFit = 1f;
    private Vector2 lastScreenSize;

    public bool IsOpen => root != null && root.activeSelf && !closing;

    public void Open()
    {
        if (IsOpen || SaveSlotWindow.IsOpen || SceneLoadingScreen.IsLoading)
            return;
        if (questBoard != null && questBoard.IsOpen)
            return;

        dialogue?.CloseDialogue();
        PlazaNPC.ResetAllHoverScales();
        if (root == null)
        {
            CollectMonsters();
            Build();
        }

        RefreshUnlocks();
        closing = false;
        root.SetActive(true);
        lastScreenSize = Vector2.zero;
        ShowTab(showTowers);
        StartFade(true);
        AudioManager.Instance?.PlayButtonClick();
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        closing = true;
        AudioManager.Instance?.PlayButtonClick();
        StartFade(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        if (screenSize != lastScreenSize)
        {
            lastScreenSize = screenSize;
            float canvasScale = Mathf.Sqrt(Screen.width / 1920f * Screen.height / 1080f);
            panelFit = Mathf.Min(1f, (Screen.width / canvasScale - 48f) / PanelWidth,
                (Screen.height / canvasScale - 48f) / PanelHeight);
            if (fade == null)
                panel.localScale = Vector3.one * panelFit;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // 스테이지 순서대로 웨이브를 훑어 처음 등장하는 스테이지를 기록한다.
    private void CollectMonsters()
    {
        monsters.Clear();
        var byData = new Dictionary<MonsterData, MonsterEntry>();
        for (int i = 0; i < (stages?.Length ?? 0); i++)
        {
            if (stages[i]?.waves == null)
                continue;
            foreach (WaveData wave in stages[i].waves)
            {
                if (wave?.spawnInfos == null)
                    continue;
                foreach (WaveData.SpawnInfo spawn in wave.spawnInfos)
                {
                    var health = spawn?.monsterPrefab != null ? spawn.monsterPrefab.GetComponent<MonsterHealth>() : null;
                    if (health == null || health.monsterData == null)
                        continue;
                    if (!byData.TryGetValue(health.monsterData, out MonsterEntry entry))
                    {
                        entry = new MonsterEntry
                        {
                            data = health.monsterData,
                            stageNumber = i + 1,
                            stageId = stages[i].stageId,
                            stealth = spawn.monsterPrefab.GetComponent<StealthMonster>() != null
                        };
                        byData.Add(health.monsterData, entry);
                        monsters.Add(entry);
                    }
                    if (!entry.appearances.Contains(i + 1))
                        entry.appearances.Add(i + 1);
                }
            }
        }
    }

    private void RefreshUnlocks()
    {
        var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
        progress.Load();
        foreach (MonsterEntry entry in monsters)
            entry.unlocked = StageIds.IsUnlocked(progress, entry.stageId);
    }

    private void ShowTab(bool towersTab)
    {
        showTowers = towersTab;
        SetTabStyle(monsterTab, !towersTab);
        SetTabStyle(towerTab, towersTab);

        // Destroy는 프레임 끝에 처리되므로 먼저 숨겨 새 카드와 겹쳐 보이지 않게 한다.
        foreach (Button card in cards)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        cards.Clear();

        int count = towersTab ? (towers?.Length ?? 0) : monsters.Count;
        for (int i = 0; i < count; i++)
        {
            int index = i;
            Vector2 position = new Vector2(i % 2 == 0 ? -162f : 162f, 196f - i / 2 * 132f);
            cards.Add(towersTab ? MakeTowerCard(towers[i], position, () => Select(index, true))
                : MakeMonsterCard(monsters[i], position, () => Select(index, true)));
        }

        int unlocked = 0;
        foreach (MonsterEntry entry in monsters)
            if (entry.unlocked) unlocked++;
        countLabel.text = towersTab ? $"타워 {count}종" : $"발견 {unlocked} / {monsters.Count}";
        Select(towersTab ? selectedTower : selectedMonster, false);
    }

    private void Select(int index, bool sound)
    {
        if (closing || cards.Count == 0)
            return;
        index = Mathf.Clamp(index, 0, cards.Count - 1);
        if (sound)
            AudioManager.Instance?.PlayButtonClick();
        if (showTowers) selectedTower = index;
        else selectedMonster = index;

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].GetComponent<Outline>().effectColor = i == index ? new Color(1f, 0.86f, 0.45f, 0.9f) : Color.clear;
            cards[i].image.color = i == index ? Color.white : CardIdle;
        }

        foreach (GameObject item in detailItems)
        {
            item.SetActive(false);
            Destroy(item);
        }
        detailItems.Clear();
        if (showTowers) ShowTowerDetail(towers[index]);
        else ShowMonsterDetail(monsters[index]);
    }

    // ──────── 상세 정보 ────────

    private void ShowMonsterDetail(MonsterEntry entry)
    {
        MonsterData data = entry.data;
        Portrait(data.icon, !entry.unlocked, entry.unlocked && data.maxShield > 0);
        if (!entry.unlocked)
        {
            DetailText("???", new Vector2(460f, 172f), new Vector2(420f, 56f), 40f, Muted, TextAlignmentOptions.Left);
            DetailText($"{entry.stageNumber}스테이지에서 등장", new Vector2(460f, 122f), new Vector2(420f, 32f), 21f, Muted,
                TextAlignmentOptions.Left);
            DetailText($"{entry.stageNumber}스테이지를 해금하면\n이 몬스터의 정보가 공개됩니다.", new Vector2(340f, -150f),
                new Vector2(640f, 110f), 26f, Muted, TextAlignmentOptions.Center);
            return;
        }

        DetailText(MonsterName(data), new Vector2(460f, 172f), new Vector2(420f, 56f), 38f, Gold, TextAlignmentOptions.Left);
        var tags = new List<(string, Color)>();
        if (data.isBoss) tags.Add(("보스", new Color(1f, 0.45f, 0.35f)));
        if (data.maxShield > 0) tags.Add(("방어막", new Color(0.55f, 0.8f, 1f)));
        if (entry.stealth) tags.Add(("은신", new Color(0.78f, 0.62f, 1f)));
        Tags(tags, 122f);
        DetailText($"등장 전장  ·  {string.Join(", ", entry.appearances)}스테이지", new Vector2(460f, tags.Count > 0 ? 78f : 122f),
            new Vector2(420f, 30f), 20f, Muted, TextAlignmentOptions.Left);
        DetailText(data.description, new Vector2(340f, -48f), new Vector2(660f, 96f), 22f, Ivory, TextAlignmentOptions.TopLeft);

        var stats = new List<(string, string)>
        {
            ("체력", data.maxHp.ToString("N0", CultureInfo.InvariantCulture)),
            ("이동 속도", data.moveSpeed.ToString("0.##", CultureInfo.InvariantCulture)),
            ("기지 피해", data.baseDamage.ToString(CultureInfo.InvariantCulture)),
            ("처치 보상", data.goldReward + " G")
        };
        if (data.maxShield > 0) stats.Add(("방어막", data.maxShield + "회 방어"));
        if (data.isBoss || data.defense > 0) stats.Add(("방어력", data.defense.ToString(CultureInfo.InvariantCulture)));
        if (data.isBoss || data.magicResistance > 0) stats.Add(("마법 저항", data.magicResistance.ToString(CultureInfo.InvariantCulture)));
        Stats(stats, -128f);
    }

    private void ShowTowerDetail(TowerData data)
    {
        Portrait(data.icon, false, false);
        DetailText(data.towerName, new Vector2(460f, 172f), new Vector2(420f, 56f), 38f, Gold, TextAlignmentOptions.Left);
        var effects = data.baseProjectileEffects;
        var tags = new List<(string, Color)>();
        if (effects.canExplode) tags.Add(("폭발", new Color(1f, 0.62f, 0.4f)));
        if (effects.canPierce) tags.Add(("관통", new Color(0.9f, 0.9f, 0.75f)));
        if (effects.canSlow) tags.Add(("둔화", new Color(0.55f, 0.8f, 1f)));
        if (effects.canDot) tags.Add(("지속 피해", new Color(1f, 0.45f, 0.35f)));
        if (tags.Count == 0) tags.Add(("단일 공격", new Color(0.9f, 0.9f, 0.75f)));
        Tags(tags, 122f);
        DetailText($"건설 비용  ·  {data.buildCost} G", new Vector2(460f, 78f), new Vector2(420f, 30f), 20f, Muted,
            TextAlignmentOptions.Left);
        DetailText(data.description, new Vector2(340f, -48f), new Vector2(660f, 96f), 22f, Ivory, TextAlignmentOptions.TopLeft);

        // 업그레이드 연구가 반영된 현재 수치를 전투와 같은 계산으로 보여 준다.
        var stats = data.baseStats;
        int damage = ResearchStatResolver.GetTowerDamage(data.id, stats.damage);
        float cooldown = ResearchStatResolver.GetTowerAttackCooldown(data.id, stats.attackCooldown);
        float range = ResearchStatResolver.GetTowerAttackRange(data.id, stats.attackRange);
        Stats(new List<(string, string)>
        {
            ("공격력", WithBonus(damage.ToString(CultureInfo.InvariantCulture), damage - stats.damage, "0")),
            ("공격 간격", WithBonus(cooldown.ToString("0.0#", CultureInfo.InvariantCulture) + "초",
                cooldown - stats.attackCooldown, "0.0#")),
            ("사거리", WithBonus(range.ToString("0.#", CultureInfo.InvariantCulture), range - stats.attackRange, "0.#")),
        }, -128f);

        Route(data.pathA, "A", -272f);
        Route(data.pathB, "B", -346f);
    }

    private static string WithBonus(string value, float delta, string format)
    {
        if (Mathf.Abs(delta) < 0.001f)
            return value;
        string sign = delta > 0f ? "+" : "-";
        return $"{value} <size=80%><color=#9ADCA0>({sign}{Mathf.Abs(delta).ToString(format, CultureInfo.InvariantCulture)})</color></size>";
    }

    private void Portrait(Sprite sprite, bool locked, bool shield)
    {
        var well = Track(MakeImage("PortraitWell", detailArea, Color.white));
        Skin(well, insetSprite, PixelScale);
        Place(well.rectTransform, new Vector2(120f, 115f), new Vector2(210f, 210f));
        if (sprite != null)
        {
            var icon = MakeImage("Portrait", well.transform, locked ? Silhouette : Color.white);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            Place(icon.rectTransform, Vector2.zero, new Vector2(168f, 168f));
        }
        if (shield && shieldOverlay != null)
        {
            var ring = MakeImage("Shield", well.transform, new Color(1f, 1f, 1f, 0.45f));
            ring.sprite = shieldOverlay;
            ring.preserveAspect = true;
            Place(ring.rectTransform, Vector2.zero, new Vector2(192f, 192f));
        }
    }

    private void Tags(List<(string label, Color color)> tags, float y)
    {
        for (int i = 0; i < tags.Count; i++)
        {
            var chip = Track(MakeImage("Tag", detailArea, tags[i].color));
            Skin(chip, goldSprite, 2f);
            chip.color = tags[i].color;
            Place(chip.rectTransform, new Vector2(306f + i * 116f, y), new Vector2(108f, 36f));
            MakeText("Label", chip.transform, tags[i].label, Vector2.zero, new Vector2(104f, 34f), 19f, DarkLabel);
        }
    }

    private void Stats(List<(string label, string value)> stats, float top)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            var cell = Track(MakeImage("Stat", detailArea, Color.white));
            Skin(cell, insetSprite, PixelScale);
            Place(cell.rectTransform, new Vector2(i % 2 == 0 ? 175f : 505f, top - i / 2 * 60f), new Vector2(316f, 52f));
            MakeText("Label", cell.transform, stats[i].label, new Vector2(-70f, 0f), new Vector2(140f, 40f), 20f, Muted)
                .alignment = TextAlignmentOptions.Left;
            MakeText("Value", cell.transform, stats[i].value, new Vector2(62f, 0f), new Vector2(170f, 40f), 24f, Ivory)
                .alignment = TextAlignmentOptions.Right;
        }
    }

    // 강화 루트는 한 줄씩 가로로 배치해 한글 문장이 중간에서 끊기지 않게 한다.
    private void Route(TowerData.PathTextInfo path, string key, float y)
    {
        if (path == null)
            return;
        var box = Track(MakeImage("Route" + key, detailArea, Color.white));
        Skin(box, plateSprite, PixelScale);
        Place(box.rectTransform, new Vector2(340f, y), new Vector2(660f, 68f));
        var badge = MakeImage("Key", box.transform, Color.white);
        Skin(badge, goldSprite, 2f);
        Place(badge.rectTransform, new Vector2(-292f, 0f), new Vector2(40f, 40f));
        MakeText("Label", badge.transform, key, Vector2.zero, new Vector2(40f, 40f), 22f, DarkLabel);
        MakeText("Name", box.transform, path.routeName, new Vector2(-156f, 13f), new Vector2(220f, 28f), 21f, Gold)
            .alignment = TextAlignmentOptions.Left;
        var levels = MakeText("Levels", box.transform, $"{path.lv2Name} › {path.lv3Name}", new Vector2(-156f, -14f),
            new Vector2(220f, 24f), 16f, Muted);
        levels.alignment = TextAlignmentOptions.Left;
        levels.textWrappingMode = TextWrappingModes.NoWrap;
        levels.overflowMode = TextOverflowModes.Ellipsis;
        var effect = MakeText("Effect", box.transform, path.effectText, new Vector2(150f, 0f), new Vector2(330f, 52f), 19f, Ivory);
        effect.alignment = TextAlignmentOptions.Left;
        effect.textWrappingMode = TextWrappingModes.NoWrap;
        effect.enableAutoSizing = true;
        effect.fontSizeMin = 14f;
        effect.fontSizeMax = 19f;
    }

    private void DetailText(string content, Vector2 position, Vector2 size, float fontSize, Color color,
        TextAlignmentOptions alignment)
    {
        var text = MakeText("Text", detailArea, content, position, size, fontSize, color);
        text.alignment = alignment;
        detailItems.Add(text.gameObject);
    }

    private Image Track(Image image)
    {
        detailItems.Add(image.gameObject);
        return image;
    }

    private string MonsterName(MonsterData data)
    {
        if (!string.IsNullOrWhiteSpace(data.displayName)) return data.displayName;
        if (data.isBoss && !string.IsNullOrWhiteSpace(data.bossName)) return data.bossName;
        return data.monsterName;
    }

    // ──────── 목록 카드 ────────

    private Button MakeMonsterCard(MonsterEntry entry, Vector2 position, UnityEngine.Events.UnityAction clicked)
    {
        string subtitle = entry.unlocked ? $"{entry.stageNumber}스테이지" : $"{entry.stageNumber}스테이지 해금 필요";
        return MakeCard(position, entry.data.icon, !entry.unlocked, entry.unlocked && entry.data.maxShield > 0,
            entry.unlocked ? MonsterName(entry.data) : "???", subtitle, entry.unlocked && entry.data.isBoss, clicked);
    }

    private Button MakeTowerCard(TowerData data, Vector2 position, UnityEngine.Events.UnityAction clicked)
    {
        return MakeCard(position, data.icon, false, false, data.towerName, $"건설 {data.buildCost} G", false, clicked);
    }

    private Button MakeCard(Vector2 position, Sprite icon, bool locked, bool shield, string title, string subtitle,
        bool boss, UnityEngine.Events.UnityAction clicked)
    {
        var button = MakeButton("Card", listArea, "", position, new Vector2(308f, 118f), clicked, plateSprite);
        button.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        var well = MakeImage("IconWell", button.transform, Color.white);
        Skin(well, insetSprite, PixelScale);
        Place(well.rectTransform, new Vector2(-98f, 0f), new Vector2(92f, 92f));
        if (icon != null)
        {
            var image = MakeImage("Icon", well.transform, locked ? Silhouette : Color.white);
            image.sprite = icon;
            image.preserveAspect = true;
            Place(image.rectTransform, Vector2.zero, new Vector2(72f, 72f));
        }
        if (shield && shieldOverlay != null)
        {
            var ring = MakeImage("Shield", well.transform, new Color(1f, 1f, 1f, 0.45f));
            ring.sprite = shieldOverlay;
            ring.preserveAspect = true;
            Place(ring.rectTransform, Vector2.zero, new Vector2(84f, 84f));
        }
        MakeText("Title", button.transform, title, new Vector2(52f, 16f), new Vector2(188f, 36f), 24f,
            locked ? Muted : (boss ? new Color32(255, 150, 120, 255) : Ivory)).alignment = TextAlignmentOptions.Left;
        MakeText("Subtitle", button.transform, subtitle, new Vector2(52f, -20f), new Vector2(188f, 28f), 18f, Muted)
            .alignment = TextAlignmentOptions.Left;
        return button;
    }

    // ──────── 창 골격 ────────

    private void Build()
    {
        root = new GameObject("CodexCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = root.GetComponent<CanvasGroup>();

        var backdrop = MakeImage("Backdrop", root.transform, new Color(0.015f, 0.025f, 0.04f, 0.88f));
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
        backdrop.raycastTarget = true;

        var panelImage = MakeImage("Panel", root.transform, new Color32(43, 26, 16, 255));
        Skin(panelImage, frameSprite, 1f);
        if (frameSprite != null)
            panelImage.color = Color.white;
        panel = panelImage.rectTransform;
        Place(panel, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

        MakeText("Title", panel, "몬스터 · 타워 도감", new Vector2(0f, 376f), new Vector2(800f, 56f), 40f, Gold);
        MakeButton("Close", panel, "X", new Vector2(668f, 376f), new Vector2(54f, 54f), Close, darkSprite);
        monsterTab = MakeButton("MonsterTab", panel, "몬스터", new Vector2(-596f, 300f), new Vector2(196f, 56f),
            () => SwitchTab(false), darkSprite);
        towerTab = MakeButton("TowerTab", panel, "타워", new Vector2(-388f, 300f), new Vector2(196f, 56f),
            () => SwitchTab(true), darkSprite);
        countLabel = MakeText("Count", panel, "", new Vector2(-170f, 300f), new Vector2(220f, 40f), 22f, Muted);
        countLabel.alignment = TextAlignmentOptions.Right;
        Place(MakeImage("Divider", panel, new Color32(140, 90, 30, 255)).rectTransform, new Vector2(0f, 262f),
            new Vector2(PanelWidth - 96f, 3f));

        listArea = new GameObject("List", typeof(RectTransform)).GetComponent<RectTransform>();
        listArea.SetParent(panel, false);
        Place(listArea, new Vector2(-384f, -20f), new Vector2(640f, 560f));

        var detail = MakeImage("Detail", panel, Color.white);
        Skin(detail, insetSprite, PixelScale);
        detail.color = new Color(1f, 1f, 1f, 0.6f);
        detailArea = detail.rectTransform;
        // 상세 좌표는 패널 중심 기준으로 쓰므로 영역을 패널과 같은 기준점에 둔다.
        detailArea.anchorMin = detailArea.anchorMax = new Vector2(0.5f, 0.5f);
        detailArea.pivot = new Vector2(0.5f, 0.5f);
        detailArea.anchoredPosition = new Vector2(340f, -80f);
        detailArea.sizeDelta = new Vector2(720f, 630f);
        // 자식은 상세 영역 중심이 아닌 패널 좌표계를 쓰도록 위치를 보정한다.
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(detailArea, false);
        Place(content, new Vector2(-340f, 80f), Vector2.zero);
        detailArea = content;

        root.SetActive(false);
    }

    private void SwitchTab(bool towersTab)
    {
        if (closing || showTowers == towersTab)
            return;
        AudioManager.Instance?.PlayButtonClick();
        ShowTab(towersTab);
    }

    private void SetTabStyle(Button tab, bool active)
    {
        Skin(tab.image, active ? goldSprite : darkSprite, PixelScale);
        tab.GetComponentInChildren<TMP_Text>().color = active ? DarkLabel : Ivory;
    }

    private void StartFade(bool opening)
    {
        if (fade != null)
            StopCoroutine(fade);
        fade = StartCoroutine(Fade(opening));
    }

    private IEnumerator Fade(bool opening)
    {
        group.blocksRaycasts = true;
        group.interactable = false;
        float duration = opening ? 0.2f : 0.14f;
        float from = group.alpha;
        float elapsed = 0f;
        if (opening)
        {
            group.alpha = from = 0f;
            panel.localScale = Vector3.one * panelFit * 0.96f;
        }
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            group.alpha = Mathf.Lerp(from, opening ? 1f : 0f, t);
            panel.localScale = Vector3.one * panelFit * Mathf.Lerp(opening ? 0.96f : 1f, opening ? 1f : 0.97f, t);
        }
        fade = null;
        group.alpha = opening ? 1f : 0f;
        panel.localScale = Vector3.one * panelFit;
        group.interactable = opening;
        if (!opening)
        {
            closing = false;
            root.SetActive(false);
        }
    }

    // ──────── 생성 도우미 ────────

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
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction clicked, Sprite sprite)
    {
        var image = MakeImage(name, parent, Color.white);
        Skin(image, sprite, PixelScale);
        Place(image.rectTransform, position, size);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(clicked);
        if (!string.IsNullOrEmpty(label))
            MakeText("Label", image.transform, label, Vector2.zero, size - new Vector2(12f, 8f), 24f, Ivory);
        return button;
    }

    private static void Skin(Image image, Sprite sprite, float pixelScale)
    {
        if (sprite == null)
            return;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = pixelScale;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
