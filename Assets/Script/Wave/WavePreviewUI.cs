using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 왼쪽 위 HUD 아래의 다음 웨이브 미리보기. 나올 몬스터(보스·방어 특성 표시)와 대기 시간,
/// 앞당겨 부르기 버튼(보너스 골드, N 키). 웨이브 진행은 WaveManager가 맡는다.
/// </summary>
public sealed class WavePreviewUI : MonoBehaviour
{
    private const float Width = 104f;
    private const float Height = 58f;
    private const int MaxSlots = 4;
    // 지도를 덜 가리게 조금 작게 보여 준다.
    private const float PanelScale = 0.8f;

    private sealed class Slot
    {
        public GameObject root;
        public Image frame;
        public Image icon;
        public Image trait;
        public TextMeshProUGUI count;
    }

    private WaveManager waves;
    private Canvas canvas;
    private RectTransform root;
    private RectTransform panel;
    private CanvasGroup group;
    private TextMeshProUGUI title;
    private TextMeshProUGUI timer;
    private TextMeshProUGUI more;
    private readonly Slot[] slots = new Slot[MaxSlots];
    private Image callButton;
    private TextMeshProUGUI callText;
    private GameObject callKey;
    private Image callIcon;
    private TextMeshProUGUI bonusPop;
    private float bonusPopAt = -10f;
    private int shownIndex = -2;
    private float visible;
    private float clearness = 1f;   // 아래로 몬스터가 지나가면 0에 가까워진다(반투명)
    private readonly Vector3[] corners = new Vector3[4];

    public static WavePreviewUI Create(WaveManager waves, Canvas canvas)
    {
        if (waves == null || canvas == null || waves.waves == null || waves.waves.Length == 0)
            return null;
        var ui = waves.gameObject.AddComponent<WavePreviewUI>();
        ui.waves = waves;
        ui.canvas = canvas.rootCanvas;
        ui.Build();
        waves.EarlyCalled += ui.OnEarlyCalled;
        return ui;
    }

    private void Build()
    {
        root = TowerPanelArt.CreateRoot("WavePreview", canvas);
        group = root.GetComponent<CanvasGroup>();
        TowerPanelArt.FitRoot(root, canvas);
        // 묶음 자체는 그림이 없어 패널 밖 지도 클릭을 막지 않는다.
        panel = TowerPanelArt.Group("Panel", root, 0f, 0f, Width, Height);

        TowerPanelArt.Image("Frame", panel, "wave_frame", 0f, 0f, true);
        title = TowerPanelArt.Text("Title", panel, 8f, 5f, 64f, 10f, 17f, TowerPanelArt.Gold, TextAlignmentOptions.Left);
        timer = TowerPanelArt.Text("Timer", panel, 66f, 5f, 30f, 10f, 16f, TowerPanelArt.Ivory, TextAlignmentOptions.Right);

        for (int i = 0; i < MaxSlots; i++)
        {
            var slot = new Slot();
            var rect = TowerPanelArt.Group("Slot" + i, panel, 8f + i * 20f, 17f, 18f, 18f);
            slot.root = rect.gameObject;
            slot.frame = TowerPanelArt.Image("Frame", rect, "wave_slot", 0f, 0f);
            slot.icon = TowerPanelArt.Picture("Icon", rect, 3f, 3f, 12f, 12f);
            slot.trait = TowerPanelArt.Picture("Trait", rect, 11.5f, -2f, 7f, 8f);
            slot.count = TowerPanelArt.Text("Count", rect, 2f, 10f, 15f, 8f, 14f, TowerPanelArt.Ivory, TextAlignmentOptions.BottomRight);
            slots[i] = slot;
        }
        more = TowerPanelArt.Text("More", panel, 88f, 21f, 12f, 10f, 15f, TowerPanelArt.Muted, TextAlignmentOptions.Left);

        callButton = TowerPanelArt.Image("Call", panel, "wave_call", 6f, 38f);
        TowerPanelArt.MakeButton(callButton).onClick.AddListener(Call);
        callIcon = TowerPanelArt.Image("Icon", callButton.transform, "icon_fast", 5f, 4f);
        callText = TowerPanelArt.Text("Text", callButton.transform, 16f, 0f, 62f, 14f, 17f, TowerPanelArt.Ink,
            TextAlignmentOptions.Left, false);
        callKey = TowerPanelArt.Image("Key", callButton.transform, "keycap", 79f, 2f).gameObject;
        TowerPanelArt.Text("KeyText", callKey.transform, 0f, 0f, 11f, 10f, 16f, TowerPanelArt.KeyInk,
            TextAlignmentOptions.Center, false).text = "N";

        bonusPop = TowerPanelArt.Text("BonusPop", panel, 50f, 38f, 60f, 12f, 22f, TowerPanelArt.Gold, TextAlignmentOptions.Left);
        bonusPop.gameObject.SetActive(false);
        group.alpha = 0f;
    }

    private void Call()
    {
        if (waves == null || !waves.CanCallEarly)
            return;
        if (waves.CallNextWaveEarly() && AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }

    private void OnEarlyCalled(int bonus)
    {
        bonusPop.text = $"+{bonus} G";
        bonusPop.gameObject.SetActive(true);
        bonusPopAt = Time.unscaledTime;
        // 누르고 나면 사라지는 중이므로 다시 누를 수 없게
        callButton.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (waves != null)
            waves.EarlyCalled -= OnEarlyCalled;
    }

    private void Update()
    {
        // 길·타워를 가리지 않도록 웨이브 사이 대기 시간에만 보인다. 앞당기기를 누르면 +골드만 잠깐 보이고 사라진다.
        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameEnded();
        bool waiting = !gameOver && waves.Phase == WaveManager.WavePhase.Waiting;
        bool popping = bonusPop.gameObject.activeSelf;
        WaveData wave = waiting ? waves.UpcomingWave : null;
        float target = wave != null || popping ? 1f : 0f;
        visible = Mathf.MoveTowards(visible, target, Time.unscaledDeltaTime * (target > 0f ? 5f : 3f));
        clearness = Mathf.MoveTowards(clearness, IsCoveringMonster() ? 0.22f : 1f, Time.unscaledDeltaTime * 4f);
        group.alpha = visible * clearness;
        panel.gameObject.SetActive(visible > 0.01f);
        if (popping)
            UpdateBonusPop();
        if (wave == null)
            return;

        if (waves.UpcomingWaveIndex != shownIndex)
        {
            shownIndex = waves.UpcomingWaveIndex;
            FillSlots(wave);
        }
        PlacePanel();

        title.text = $"다음 웨이브 {shownIndex + 1}/{waves.TotalWaves}";
        float wait = waves.WaitRemaining;
        timer.text = wait >= 0f ? Mathf.CeilToInt(wait) + "초" : "";

        bool canCall = waves.CanCallEarly;
        TowerPanelArt.SetSprite(callButton, canCall ? "wave_call" : "wave_call_off");
        callButton.raycastTarget = canCall;
        callIcon.gameObject.SetActive(canCall);
        callKey.SetActive(canCall);
        callText.text = canCall ? $"앞당기기 +{waves.EarlyCallBonus}G" : "몬스터 출현 중";
        callText.rectTransform.anchoredPosition = new Vector2((canCall ? 16f : 0f) * TowerPanelArt.Dot, 0f);
        callText.rectTransform.sizeDelta = new Vector2((canCall ? 62f : 92f) * TowerPanelArt.Dot, 14f * TowerPanelArt.Dot);
        callText.alignment = canCall ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
        callText.color = canCall ? TowerPanelArt.Ink : new Color32(70, 62, 56, 255);

        if (canCall && Input.GetKeyDown(KeyCode.N) && Time.timeScale > 0f)
            Call();
    }

    // 앞당긴 직후 "+골드"가 떠오르며 사라진다(이 동안만 창을 남겨 둔다).
    private void UpdateBonusPop()
    {
        float pop = Time.unscaledTime - bonusPopAt;
        bonusPop.rectTransform.anchoredPosition = new Vector2(50f * TowerPanelArt.Dot, -(38f - pop * 14f) * TowerPanelArt.Dot);
        bonusPop.alpha = Mathf.Clamp01(1.1f - pop);
        if (pop > 1.1f)
            bonusPop.gameObject.SetActive(false);
    }

    // 왼쪽 위 HUD(골드·체력·웨이브) 바로 아래
    private void PlacePanel()
    {
        TowerPanelArt.FitRoot(root, canvas);
        Rect area = root.rect;
        TowerPanelArt.PlaceTopLeft(panel, new Vector2(area.xMin + 14f, area.yMax - 148f));
        panel.localScale = new Vector3(PanelScale, PanelScale, 1f);
    }

    // 몬스터가 창 아래(길)를 지나가면 비켜 주듯 흐려진다. 마우스를 올리면 다시 또렷하게.
    private bool IsCoveringMonster()
    {
        if (panel == null || !panel.gameObject.activeInHierarchy || Camera.main == null)
            return false;
        panel.GetWorldCorners(corners);
        Rect screen = Rect.MinMaxRect(corners[0].x - 24f, corners[0].y - 24f, corners[2].x + 24f, corners[2].y + 24f);
        if (screen.Contains(Input.mousePosition))
            return false;
        foreach (MonsterHealth monster in TowerTargeting.All)
        {
            if (monster == null || monster.IsDead)
                continue;
            if (screen.Contains(Camera.main.WorldToScreenPoint(monster.transform.position)))
                return true;
        }
        return false;
    }

    private void FillSlots(WaveData wave)
    {
        // 같은 몬스터는 합치고 보스를 맨 앞에
        var kinds = new List<(MonsterData data, int count)>();
        if (wave.spawnInfos != null)
        {
            foreach (WaveData.SpawnInfo info in wave.spawnInfos)
            {
                if (info == null || info.monsterPrefab == null) continue;
                MonsterHealth health = info.monsterPrefab.GetComponent<MonsterHealth>();
                MonsterData data = health != null ? health.monsterData : null;
                if (data == null) continue;
                int index = kinds.FindIndex(k => k.data == data);
                if (index >= 0) kinds[index] = (data, kinds[index].count + info.count);
                else kinds.Add((data, info.count));
            }
        }
        kinds.Sort((a, b) => b.data.isBoss.CompareTo(a.data.isBoss));

        for (int i = 0; i < MaxSlots; i++)
        {
            Slot slot = slots[i];
            bool used = i < kinds.Count;
            slot.root.SetActive(used);
            if (!used) continue;
            MonsterData data = kinds[i].data;
            TowerPanelArt.SetSprite(slot.frame, data.isBoss ? "wave_slot_boss" : "wave_slot");
            slot.icon.sprite = data.icon;
            slot.count.text = "×" + kinds[i].count;
            slot.trait.gameObject.SetActive(data.armorTrait != ArmorTrait.None);
            if (data.armorTrait != ArmorTrait.None)
                slot.trait.sprite = TowerPanelArt.Sprite(data.armorTrait == ArmorTrait.Armored ? "trait_armor" : "trait_ward");
        }
        more.text = kinds.Count > MaxSlots ? "+" + (kinds.Count - MaxSlots) : "";
    }
}
