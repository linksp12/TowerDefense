using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("스킬 목록")]
    public List<SkillData> skills = new List<SkillData>();

    private Dictionary<string, float> cooldownTimers = new Dictionary<string, float>();
    private Dictionary<string, bool> isOnCooldown = new Dictionary<string, bool>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (var skill in skills)
        {
            cooldownTimers[skill.skillName] = 0f;
            isOnCooldown[skill.skillName] = false;
        }
    }

    public bool CanUseSkill(string skillName)
    {
        return isOnCooldown.ContainsKey(skillName) && !isOnCooldown[skillName];
    }

    public void UseSkill(string skillName)
    {
        if (!CanUseSkill(skillName)) return;

        SkillData skill = skills.Find(s => s.skillName == skillName);
        if (skill == null) return;

        ExecuteSkill(skillName);
        StartCoroutine(StartCooldown(skill));
    }

    private void ExecuteSkill(string skillName)
    {
        switch (skillName)
        {
            case "Fireball":    AirStrikeSkill();  break;
            case "Ice Attack": SpeedBoostSkill(); break;
            case "Lightning":    LightningSkill();  break;
        }
    }

    private IEnumerator StartCooldown(SkillData skill)
    {
        isOnCooldown[skill.skillName] = true;
        cooldownTimers[skill.skillName] = skill.cooldown;

        while (cooldownTimers[skill.skillName] > 0)
        {
            cooldownTimers[skill.skillName] -= Time.deltaTime;
            yield return null;
        }

        cooldownTimers[skill.skillName] = 0f;
        isOnCooldown[skill.skillName] = false;
    }

    public float GetCooldownNormalized(string skillName)
    {
        if (!skills.Exists(s => s.skillName == skillName)) return 0f;
        SkillData skill = skills.Find(s => s.skillName == skillName);
        return cooldownTimers[skillName] / skill.cooldown;
    }

    public float GetCooldownRemaining(string skillName)
    {
        return Mathf.Max(0, cooldownTimers.ContainsKey(skillName) 
            ? cooldownTimers[skillName] : 0f);
    }

    void AirStrikeSkill()
    {
        // ✅ Enemy → MonsterHealth, 50f → 50 수정
        MonsterHealth[] enemies = FindObjectsByType<MonsterHealth>();
        foreach (var enemy in enemies)
            enemy.TakeDamage(50);
        Debug.Log("폭격 스킬 사용!");
    }

    void SpeedBoostSkill()
    {
        Debug.Log("속도 증가 스킬 사용!");
    }

    void LightningSkill()
    {
        Debug.Log("번개 스킬 사용!");
    }
}
