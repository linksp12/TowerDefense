using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    public event Action<string> SkillUsed;

    [Header("Skill Data")]
    [Tooltip("비어 있으면 Resources/SkillData의 에셋을 자동으로 사용합니다.")]
    [SerializeField] private List<SkillData> skills = new List<SkillData>();

    [Header("Audio")]
    [SerializeField] private AudioSource skillAudioSource;

    private readonly Dictionary<string, SkillData> skillsById =
        new Dictionary<string, SkillData>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, float> cooldownEndTime =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SkillData> Skills => skills;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadAndValidateSkillData();
        SetupAudioSource();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryGetSkill(string idOrName, out SkillData skill)
    {
        skill = null;
        if (string.IsNullOrWhiteSpace(idOrName))
            return false;

        if (skillsById.TryGetValue(idOrName, out skill))
            return skill != null;

        skill = skills.Find(candidate => candidate != null && candidate.Matches(idOrName));
        return skill != null;
    }

    public bool CanUseSkill(string idOrName)
    {
        if (!TryGetSkill(idOrName, out SkillData skill))
            return false;

        return !cooldownEndTime.TryGetValue(skill.SkillId, out float endTime)
            || Time.time >= endTime;
    }

    public bool UseSkill(string idOrName)
    {
        if (!TryBeginUse(idOrName, out SkillData skill))
            return false;

        ExecuteGlobalSkill(skill);
        CompleteUse(skill);
        return true;
    }

    public bool UseSkillAtPosition(string idOrName, Vector3 castPosition, float radius)
    {
        if (!TryBeginUse(idOrName, out SkillData skill))
            return false;

        if (!Mathf.Approximately(radius, skill.Range))
        {
            Debug.LogWarning(
                $"{skill.DisplayName}: 전달된 범위 {radius:0.##} 대신 SkillData 범위 {skill.Range:0.##}를 사용합니다.",
                this);
        }

        ExecuteSkillAtPosition(skill, castPosition);
        CompleteUse(skill);
        return true;
    }

    public void ResetAllCooldowns()
    {
        foreach (SkillData skill in skills)
        {
            if (skill != null && !string.IsNullOrWhiteSpace(skill.SkillId))
                cooldownEndTime[skill.SkillId] = 0f;
        }
    }

    public float GetCooldownNormalized(string idOrName)
    {
        if (!TryGetSkill(idOrName, out SkillData skill) || skill.Cooldown <= 0f)
            return 0f;

        return Mathf.Clamp01(GetCooldownRemaining(skill.SkillId) / skill.Cooldown);
    }

    public float GetCooldownRemaining(string idOrName)
    {
        if (!TryGetSkill(idOrName, out SkillData skill))
            return 0f;

        return cooldownEndTime.TryGetValue(skill.SkillId, out float endTime)
            ? Mathf.Max(0f, endTime - Time.time)
            : 0f;
    }

    private void LoadAndValidateSkillData()
    {
        skills.RemoveAll(skill => skill == null);

        if (skills.Count == 0)
        {
            SkillData[] loadedSkills = Resources.LoadAll<SkillData>("SkillData");
            skills.AddRange(loadedSkills);
        }

        skillsById.Clear();
        cooldownEndTime.Clear();

        foreach (SkillData skill in skills)
        {
            if (!ValidateSkillData(skill))
                continue;

            if (skillsById.ContainsKey(skill.SkillId))
            {
                Debug.LogError($"중복된 스킬 ID입니다: {skill.SkillId}", skill);
                continue;
            }

            skillsById.Add(skill.SkillId, skill);
            cooldownEndTime[skill.SkillId] = 0f;
        }

        if (skillsById.Count == 0)
        {
            Debug.LogError(
                "SkillData가 연결되지 않았습니다. SkillManager 목록 또는 " +
                "Assets/Data/Skills/Resources/SkillData를 확인하세요.",
                this);
        }
    }

    private static bool ValidateSkillData(SkillData skill)
    {
        if (skill == null)
            return false;

        if (string.IsNullOrWhiteSpace(skill.SkillId))
        {
            Debug.LogError("SkillData의 ID가 비어 있습니다.", skill);
            return false;
        }

        if (string.IsNullOrWhiteSpace(skill.DisplayName))
        {
            Debug.LogError($"{skill.SkillId}: 표시 이름이 비어 있습니다.", skill);
            return false;
        }

        if (skill.Range <= 0f)
        {
            Debug.LogError($"{skill.SkillId}: 공격 범위는 0보다 커야 합니다.", skill);
            return false;
        }

        return true;
    }

    private void SetupAudioSource()
    {
        if (skillAudioSource == null)
            skillAudioSource = GetComponent<AudioSource>();

        if (skillAudioSource == null)
            skillAudioSource = gameObject.AddComponent<AudioSource>();

        skillAudioSource.playOnAwake = false;
        skillAudioSource.loop = false;
    }

    private bool TryBeginUse(string idOrName, out SkillData skill)
    {
        if (!TryGetSkill(idOrName, out skill))
        {
            Debug.LogError($"스킬 데이터를 찾을 수 없습니다: {idOrName}", this);
            return false;
        }

        if (!CanUseSkill(skill.SkillId))
        {
            Debug.Log($"{skill.DisplayName} 스킬은 현재 쿨타임 중입니다.", this);
            return false;
        }

        return true;
    }

    private void CompleteUse(SkillData skill)
    {
        cooldownEndTime[skill.SkillId] = Time.time + skill.Cooldown;
        PlaySkillSound(skill);
        SkillUsed?.Invoke(skill.DisplayName);
    }

    private void ExecuteGlobalSkill(SkillData skill)
    {
        switch (skill.EffectType)
        {
            case SkillEffectType.Fire:
                ApplyFire(skill, transform.position, float.PositiveInfinity);
                break;
            case SkillEffectType.Ice:
                ApplyIce(skill, transform.position, float.PositiveInfinity);
                break;
            case SkillEffectType.Lightning:
                ApplyLightning(skill, transform.position, float.PositiveInfinity);
                break;
            default:
                Debug.LogError($"지원하지 않는 스킬 효과입니다: {skill.EffectType}", skill);
                break;
        }
    }

    private void ExecuteSkillAtPosition(SkillData skill, Vector3 castPosition)
    {
        switch (skill.EffectType)
        {
            case SkillEffectType.Fire:
                ApplyFire(skill, castPosition, skill.Range);
                break;
            case SkillEffectType.Ice:
                ApplyIce(skill, castPosition, skill.Range);
                break;
            case SkillEffectType.Lightning:
                ApplyLightning(skill, castPosition, skill.Range);
                break;
            default:
                Debug.LogError($"지원하지 않는 스킬 효과입니다: {skill.EffectType}", skill);
                break;
        }
    }

    private void ApplyFire(SkillData skill, Vector3 castPosition, float radius)
    {
        float duration = ResearchStatResolver.GetSkillDuration(skill);
        MonsterHealth[] monsters = FindObjectsByType<MonsterHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (MonsterHealth monster in monsters)
        {
            if (!IsValidTarget(monster, castPosition, radius))
                continue;

            monster.TakeDamage(ResearchStatResolver.GetSkillDamage(skill), false);
            if (skill.PeriodicDamage > 0 && duration > 0f)
                StartCoroutine(ApplyPeriodicDamage(monster, skill));
        }

        CreateEffect(skill, castPosition, Mathf.Max(1f, duration), skill.EffectScale);
    }

    private IEnumerator ApplyPeriodicDamage(MonsterHealth monster, SkillData skill)
    {
        float elapsed = 0f;
        float interval = Mathf.Max(0.05f, skill.PeriodicInterval);
        float duration = ResearchStatResolver.GetSkillDuration(skill);

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(interval);
            if (monster == null || monster.IsDead)
                yield break;

            monster.TakeDamage(ResearchStatResolver.GetSkillPeriodicDamage(skill), false);
            elapsed += interval;
        }
    }

    private void ApplyIce(SkillData skill, Vector3 castPosition, float radius)
    {
        float duration = ResearchStatResolver.GetSkillDuration(skill);
        MonsterMove[] monsters = FindObjectsByType<MonsterMove>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (MonsterMove monster in monsters)
        {
            if (monster == null)
                continue;

            MonsterHealth health = monster.GetComponent<MonsterHealth>();
            if (!IsValidTarget(health, castPosition, radius))
                continue;

            monster.Freeze(duration);
            CreateAttachedEffect(skill, monster.transform, Mathf.Max(1f, duration));
        }
    }

    private void ApplyLightning(SkillData skill, Vector3 castPosition, float radius)
    {
        MonsterHealth[] allMonsters = FindObjectsByType<MonsterHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        List<MonsterHealth> targets = new List<MonsterHealth>();

        foreach (MonsterHealth monster in allMonsters)
        {
            if (IsValidTarget(monster, castPosition, radius))
                targets.Add(monster);
        }

        targets.Sort((left, right) =>
            Vector2.Distance(castPosition, left.transform.position).CompareTo(
                Vector2.Distance(castPosition, right.transform.position)));

        int targetLimit = skill.MaxTargets > 0 ? skill.MaxTargets : targets.Count;
        int hitCount = Mathf.Min(targetLimit, targets.Count);
        for (int index = 0; index < hitCount; index++)
        {
            MonsterHealth target = targets[index];
            target.TakeDamage(ResearchStatResolver.GetSkillDamage(skill), false);
            CreateEffect(skill, target.transform.position, 1f, skill.EffectScale);
        }
    }

    private static bool IsValidTarget(
        MonsterHealth monster,
        Vector3 center,
        float radius)
    {
        if (monster == null || monster.IsDead)
            return false;

        return float.IsPositiveInfinity(radius)
            || Vector2.Distance(center, monster.transform.position) <= radius;
    }

    private static void ConfigureEffectRenderer(GameObject effect)
    {
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        if (renderer == null)
            return;

        renderer.sortingLayerID = SortingLayer.NameToID("Effects");
        renderer.sortingOrder = 100;
    }

    private static void CreateAttachedEffect(
        SkillData skill,
        Transform parent,
        float destroyTime)
    {
        if (skill.EffectPrefab == null)
            return;

        GameObject effect = Instantiate(skill.EffectPrefab, parent);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        effect.transform.localScale *= skill.EffectScale;
        ConfigureEffectRenderer(effect);
        Destroy(effect, destroyTime);
    }

    private static void CreateEffect(
        SkillData skill,
        Vector3 position,
        float destroyTime,
        float scale)
    {
        if (skill.EffectPrefab == null)
            return;

        GameObject effect = Instantiate(skill.EffectPrefab, position, Quaternion.identity);
        effect.transform.localScale *= scale;
        ConfigureEffectRenderer(effect);
        Destroy(effect, destroyTime);
    }

    private void PlaySkillSound(SkillData skill)
    {
        if (skillAudioSource == null || skill.Sound == null)
        {
            Debug.LogWarning($"{skill.DisplayName}: 효과음이 연결되지 않았습니다.", skill);
            return;
        }

        AudioManager.PlaySFXOn(skillAudioSource, skill.Sound, skill.SoundVolume);
    }
}
