using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>연구 대상 선택·구매·갱신을 담당하고, 표시는 씬의 View에 맡깁니다.</summary>
public sealed class ResearchSceneController : MonoBehaviour
{
    [SerializeField] private ResearchData[] researchEntries = Array.Empty<ResearchData>();
    [SerializeField] private TowerData[] towers = Array.Empty<TowerData>();
    [SerializeField] private SkillData[] skills = Array.Empty<SkillData>();
    [SerializeField] private ResearchSceneView view;

    private readonly Dictionary<ResearchData, ResearchOptionButtonView> researchButtons = new Dictionary<ResearchData, ResearchOptionButtonView>();
    private readonly Dictionary<UnityEngine.Object, TargetBinding> targets = new Dictionary<UnityEngine.Object, TargetBinding>();
    private readonly List<ResearchData> orderedResearch = new List<ResearchData>();
    private ResearchData selectedResearch;
    private TowerData selectedTower;
    private SkillData selectedSkill;
    private bool initialized;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool testToolsShortcutHeld;
#endif

    // 대상별 연구 항목은 카드를 만들 때 연결해 두고 갱신할 때 재사용합니다.
    private sealed class TargetBinding
    {
        public TowerData Tower;
        public SkillData Skill;
        public ResearchTargetCardView Card;
        public readonly List<ResearchData> Research = new List<ResearchData>();
    }

    private void Awake()
    {
        if (view == null)
        {
            Debug.LogError("ResearchSceneController: ResearchSceneView 연결이 필요합니다.", this);
            enabled = false;
            return;
        }
        if (!view.Initialize()) { enabled = false; return; }
        initialized = true;
        view.PurchaseButton.onClick.AddListener(TryPurchaseSelected);
        view.BackButton.onClick.AddListener(ReturnToPlaza);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        view.AddPointButton.onClick.AddListener(AddTestPoint);
        view.ResetButton.onClick.AddListener(ResetForTesting);
#endif
        BuildResearchOptions();
        TargetBinding first = null;
        int towerCount = 0, skillCount = 0;
        foreach (var tower in towers ?? Array.Empty<TowerData>())
        {
            var binding = AddTarget(tower, null);
            if (binding == null) continue;
            towerCount++;
            if (first == null) first = binding;
        }
        foreach (var skill in skills ?? Array.Empty<SkillData>())
        {
            var binding = AddTarget(null, skill);
            if (binding == null) continue;
            skillCount++;
            if (first == null) first = binding;
        }
        view.SetEmptyLists(towerCount == 0, skillCount == 0);
        SelectTarget(first?.Tower, first?.Skill);
    }

    private void OnDestroy()
    {
        if (!initialized || view == null) return;
        if (view.PurchaseButton != null) view.PurchaseButton.onClick.RemoveListener(TryPurchaseSelected);
        if (view.BackButton != null) view.BackButton.onClick.RemoveListener(ReturnToPlaza);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (view.AddPointButton != null) view.AddPointButton.onClick.RemoveListener(AddTestPoint);
        if (view.ResetButton != null) view.ResetButton.onClick.RemoveListener(ResetForTesting);
#endif
    }

    private void BuildResearchOptions()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var research in researchEntries ?? Array.Empty<ResearchData>())
        {
            if (research == null) continue;
            if (string.IsNullOrWhiteSpace(research.ResearchId) || !ids.Add(research.ResearchId))
            {
                Debug.LogWarning("연구소에서 비어 있거나 중복된 연구 ID를 건너뜁니다: " + research.name, this);
                continue;
            }
            var button = view.CreateOptionButton();
            button.name = research.ResearchId;
            button.Initialize(research.DisplayName, view.Palette, view.ButtonAudio, () => SelectResearch(research));
            button.gameObject.SetActive(false);
            researchButtons.Add(research, button);
            orderedResearch.Add(research);
        }
    }

    private TargetBinding AddTarget(TowerData tower, SkillData skill)
    {
        UnityEngine.Object target = tower != null ? (UnityEngine.Object)tower : skill;
        if (target == null || targets.ContainsKey(target)) return null;
        var binding = new TargetBinding { Tower = tower, Skill = skill, Card = view.CreateTargetCard(tower != null ? ResearchTargetType.Tower : ResearchTargetType.Skill) };
        binding.Card.name = "Target_" + target.name;
        binding.Card.Initialize(tower != null ? (tower.towerName ?? tower.name).Replace("기본 ", "") : skill.DisplayName,
            tower != null ? tower.icon : skill.Icon, tower != null ? view.Palette.Tower : view.Palette.Skill,
            view.Palette, view.ButtonAudio, () => SelectTarget(tower, skill));
        foreach (var research in orderedResearch)
            if (MatchesTarget(research, tower, skill)) binding.Research.Add(research);
        targets.Add(target, binding);
        return binding;
    }

    private static bool MatchesTarget(ResearchData research, TowerData tower, SkillData skill)
    {
        if (research == null) return false;
        return tower != null ? research.TargetType == ResearchTargetType.Tower && research.TargetId == tower.id
            : skill != null && research.TargetType == ResearchTargetType.Skill && research.TargetId == skill.SkillId;
    }

    private void SelectTarget(TowerData tower, SkillData skill)
    {
        selectedTower = tower;
        selectedSkill = skill;
        selectedResearch = null;
        foreach (var research in orderedResearch)
            if (MatchesTarget(research, tower, skill)) { selectedResearch = research; break; }
        view.SetNotice(string.Empty);
        Refresh();
        view.ResetStatScroll();
    }

    private void SelectResearch(ResearchData research)
    {
        selectedResearch = research;
        view.SetNotice(string.Empty);
        Refresh();
    }

    private void TryPurchaseSelected()
    {
        if (selectedResearch == null) return;
        view.SetNotice(ResearchProgressStore.TryPurchase(selectedResearch, out string failureMessage)
            ? $"{selectedResearch.DisplayName} 연구를 완료했습니다." : failureMessage);
        Refresh();
    }

    private void Refresh()
    {
        if (!initialized) return;
        view.SetSkillPoints(ResearchProgressStore.SkillPoints);
        foreach (var pair in targets)
        {
            var target = pair.Value;
            int level = 0, maxLevel = 0;
            foreach (var research in target.Research)
            {
                level += Mathf.Clamp(ResearchProgressStore.GetLevel(research.ResearchId), 0, research.MaxLevel);
                maxLevel += research.MaxLevel;
            }
            string status = target.Research.Count == 0 ? "연구 준비 중" : $"{GetStatLabel(target.Research[0])} 연구   {level} / {maxLevel}";
            target.Card.Render(pair.Key == (UnityEngine.Object)selectedTower || pair.Key == (UnityEngine.Object)selectedSkill, status, level, maxLevel);
        }
        foreach (var pair in researchButtons)
            pair.Value.Render(MatchesTarget(pair.Key, selectedTower, selectedSkill), pair.Key == selectedResearch);
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        view.SetTarget(selectedTower != null ? "타워 연구" : "스킬 연구", selectedTower != null,
            selectedTower != null ? selectedTower.towerName : selectedSkill != null ? selectedSkill.DisplayName : "대상을 선택하세요",
            selectedTower != null ? selectedTower.description : selectedSkill != null ? selectedSkill.Description : string.Empty,
            selectedTower != null ? selectedTower.icon : selectedSkill != null ? selectedSkill.Icon : null);
        RefreshStatRows();
        if (selectedResearch == null)
        {
            view.SetPurchaseInfo("아직 등록된 연구 항목이 없습니다.");
            view.SetPurchaseAppearance("연구 준비 중", false, false);
            return;
        }
        int level = ResearchProgressStore.GetLevel(selectedResearch.ResearchId);
        if (level >= selectedResearch.MaxLevel)
        {
            view.SetPurchaseInfo($"<color=#{view.HighlightHex}>연구 완료   {level} / {selectedResearch.MaxLevel}</color>\n" +
                $"{GetStatLabel(selectedResearch)}  {FormatStat(selectedResearch, ResearchStatResolver.GetResearchStatValue(selectedResearch, level))}");
            view.SetPurchaseAppearance("최대 강화 완료", false, false);
            return;
        }
        ResearchRank nextRank = selectedResearch.GetRank(level);
        float current = ResearchStatResolver.GetResearchStatValue(selectedResearch, level);
        float next = ResearchStatResolver.GetResearchStatValue(selectedResearch, level + 1);
        view.SetPurchaseInfo($"연구 단계   {level} / {selectedResearch.MaxLevel}\n" +
            $"<size=115%>{GetStatLabel(selectedResearch)}  {FormatStat(selectedResearch, current)} → <color=#{view.HighlightHex}>{FormatStat(selectedResearch, next)}</color></size>\n" +
            $"<size=80%><color=#{view.HintHex}>{BuildRankEffect(selectedResearch, nextRank)}</color></size>");
        bool affordable = ResearchProgressStore.SkillPoints >= nextRank.skillPointCost;
        view.SetPurchaseAppearance((affordable ? "연구하기" : "SP 부족") + $"   ·   {nextRank.skillPointCost} SP", true, affordable);
    }

    private static string GetStatLabel(ResearchData research)
    {
        switch (research.ModifierType)
        {
            case ResearchModifierType.AttackCooldownPercent: return "공격 간격";
            case ResearchModifierType.AttackRangePercent: return "사거리";
            case ResearchModifierType.PeriodicDamagePercent: return "지속 피해";
            case ResearchModifierType.DurationFlat: return "지속 시간";
            case ResearchModifierType.AttackSpeedBonusFlat: return "공격 속도";
            default: return research.TargetType == ResearchTargetType.Tower ? "공격력" : "피해량";
        }
    }

    private static string BuildRankEffect(ResearchData growth, ResearchRank rank)
    {
        string value = rank.modifierValue.ToString("0.##");
        switch (growth.ModifierType)
        {
            case ResearchModifierType.DamageFlat: return growth.TargetType == ResearchTargetType.Tower
                ? $"공격력 +{value}" : $"피해량 +{value}";
            case ResearchModifierType.DamagePercent: return $"피해량 +{value}%";
            case ResearchModifierType.AttackCooldownPercent: return $"공격 간격 -{value}%";
            case ResearchModifierType.AttackRangePercent: return $"사거리 +{value}%";
            case ResearchModifierType.PeriodicDamagePercent: return $"지속 피해 +{value}%";
            case ResearchModifierType.DurationFlat: return $"지속 시간 +{value}초";
            case ResearchModifierType.AttackSpeedBonusFlat: return $"공격 속도 +{value}%p";
            default: return $"수치 +{value}";
        }
    }

    private static string FormatStat(ResearchData research, float value)
    {
        string unit = research.ModifierType == ResearchModifierType.AttackCooldownPercent ||
            research.ModifierType == ResearchModifierType.DurationFlat ? "초" : "";
        if (research.ModifierType == ResearchModifierType.AttackSpeedBonusFlat)
            return $"+{value:0.##}%";
        return $"{value:0.##}{unit}";
    }

    private void RefreshStatRows()
    {
        int index = 0;
        if (selectedTower != null)
        {
            TowerData data = selectedTower;
            SetStatRow(index++, "공격력", data.baseStats.damage.ToString(), ResearchStatResolver.GetTowerDamage(data.id, data.baseStats.damage).ToString());
            SetStatRow(index++, "공격 간격", $"{data.baseStats.attackCooldown:0.##}초", $"{ResearchStatResolver.GetTowerAttackCooldown(data.id, data.baseStats.attackCooldown):0.##}초");
            SetStatRow(index++, "사거리", $"{data.baseStats.attackRange:0.##}", $"{ResearchStatResolver.GetTowerAttackRange(data.id, data.baseStats.attackRange):0.##}");
        }
        else if (selectedSkill != null)
        {
            SkillData data = selectedSkill;
            if (data.EffectType == SkillEffectType.TowerHaste)
                SetStatRow(index++, "공격 속도", $"+{data.AttackSpeedBonus * 100f:0.#}%",
                    $"+{ResearchStatResolver.GetSkillAttackSpeedBonus(data) * 100f:0.#}%");
            if (data.Damage > 0) SetStatRow(index++, "피해량", data.Damage.ToString(), ResearchStatResolver.GetSkillDamage(data).ToString());
            if (data.PeriodicDamage > 0) SetStatRow(index++, "지속 피해", $"{data.PeriodicDamage} / {data.PeriodicInterval:0.##}초", $"{ResearchStatResolver.GetSkillPeriodicDamage(data)} / {data.PeriodicInterval:0.##}초");
            SetStatRow(index++, "범위", $"{data.Range:0.##}", $"{data.Range:0.##}");
            SetStatRow(index++, "재사용 대기시간", $"{data.Cooldown:0.##}초", $"{data.Cooldown:0.##}초");
            if (data.Duration > 0) SetStatRow(index++, "지속 시간", $"{data.Duration:0.##}초", $"{ResearchStatResolver.GetSkillDuration(data):0.##}초");
            if (data.MaxTargets > 0) SetStatRow(index++, "최대 대상", data.MaxTargets.ToString(), data.MaxTargets.ToString());
        }
        view.SetStatCount(index);
    }

    private void SetStatRow(int index, string name, string baseValue, string currentValue)
        => view.SetStatRow(index, name, baseValue, currentValue);

    private void ReturnToPlaza()
    {
        if (SceneLoadingScreen.IsLoading) return;
        SceneLoadingScreen.TryLoad("PlazaScene", "광장으로 이동 중");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
        => HandleTestToolsShortcut(Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.E));

    private void HandleTestToolsShortcut(bool held)
    {
        if (held && !testToolsShortcutHeld) view.SetTestToolsVisible(!view.TestToolsVisible);
        testToolsShortcutHeld = held;
    }

    private void AddTestPoint()
    {
        ResearchProgressStore.AddTestSkillPoints(1);
        view.SetNotice("테스트 스킬 포인트를 1 지급했습니다.");
        Refresh();
    }

    private void ResetForTesting()
    {
        ResearchProgressStore.ResetForTesting(researchEntries);
        view.SetNotice("스킬 포인트와 연구 기록을 초기화했습니다.");
        Refresh();
    }
#endif
}
