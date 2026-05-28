using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("스킬 목록")]
    public List<SkillData> skills = new List<SkillData>();

    [Header("스킬 이펙트")]
    public GameObject fireEffectPrefab;
    public GameObject iceEffectPrefab;
    public GameObject lightningEffectPrefab;

    // 쿨타임 저장
    private Dictionary<string, float> cooldownTimers =
        new Dictionary<string, float>();

    // 현재 쿨타임 여부
    private Dictionary<string, bool> isOnCooldown =
        new Dictionary<string, bool>();

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

    // 스킬 사용 가능 여부
    public bool CanUseSkill(string skillName)
    {
        return isOnCooldown.ContainsKey(skillName)
            && !isOnCooldown[skillName];
    }

    // 스킬 사용
    public void UseSkill(string skillName)
    {
        if (!CanUseSkill(skillName))
            return;

        SkillData skill =
            skills.Find(s => s.skillName == skillName);

        if (skill == null)
            return;

        ExecuteSkill(skillName);

        StartCoroutine(StartCooldown(skill));
    }

    // 스킬 실행
    private void ExecuteSkill(string skillName)
    {
        switch (skillName)
        {
            case "Fireball":
                FireballSkill();
                break;

            case "Ice Attack":
                IceAttackSkill();
                break;

            case "Lightning":
                LightningSkill();
                break;
        }
    }

    // 쿨타임 시작
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

    // 쿨타임 UI용
    public float GetCooldownNormalized(string skillName)
    {
        if (!skills.Exists(s => s.skillName == skillName))
            return 0f;

        SkillData skill =
            skills.Find(s => s.skillName == skillName);

        return cooldownTimers[skillName] / skill.cooldown;
    }

    // 남은 쿨타임
    public float GetCooldownRemaining(string skillName)
    {
        return Mathf.Max(
            0,
            cooldownTimers.ContainsKey(skillName)
            ? cooldownTimers[skillName]
            : 0f
        );
    }

    // ─────────────────────────────
    // 🔥 불 스킬
    // 모든 몬스터에게 약한 데미지
    // ─────────────────────────────
    void FireballSkill()
    {
        MonsterHealth[] enemies =
            FindObjectsByType<MonsterHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (var enemy in enemies)
        {
            enemy.TakeDamage(50);

            CreateEffect(
                fireEffectPrefab,
                enemy.transform.position,
                2f
            );
        }

        Debug.Log(
            $"불 스킬! {enemies.Length}명에게 20 데미지!"
        );
    }

    // ─────────────────────────────
    // ❄️ 얼음 스킬
    // 몬스터 전체 정지
    // ─────────────────────────────
    void IceAttackSkill()
    {
        MonsterMove[] enemies =
            FindObjectsByType<MonsterMove>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (var enemy in enemies)
        {
            enemy.Freeze(3f);

            CreateEffect(
                iceEffectPrefab,
                enemy.transform.position,
                3f
            );
        }

        Debug.Log(
            $"얼음 스킬! {enemies.Length}명 3초 정지!"
        );
    }

    // ─────────────────────────────
    // ⚡ 번개 스킬
    // 가까운 적 5명 강한 데미지
    // ─────────────────────────────
    void LightningSkill()
    {
        MonsterHealth[] allEnemies =
            FindObjectsByType<MonsterHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        List<MonsterHealth> sortedEnemies =
            new List<MonsterHealth>(allEnemies);

        // 카메라 기준 가까운 순 정렬
        sortedEnemies.Sort((a, b) =>
            Vector3.Distance(
                Camera.main.transform.position,
                a.transform.position
            ).CompareTo(
                Vector3.Distance(
                    Camera.main.transform.position,
                    b.transform.position
                )
            )
        );

        int hitCount =
            Mathf.Min(5, sortedEnemies.Count);

        for (int i = 0; i < hitCount; i++)
        {
            sortedEnemies[i].TakeDamage(80);

            CreateEffect(
                lightningEffectPrefab,
                sortedEnemies[i].transform.position,
                1f
            );
        }

        Debug.Log(
            $"번개 스킬! 가까운 {hitCount}명에게 80 데미지!"
        );
    }

    // ─────────────────────────────
    // 공통 이펙트 생성 함수
    // ─────────────────────────────
    void CreateEffect(
        GameObject effectPrefab,
        Vector3 position,
        float destroyTime
    )
    {
        if (effectPrefab == null)
            return;

        GameObject effect =
            Instantiate(
                effectPrefab,
                position,
                Quaternion.identity
            );

        // 배경 뒤로 숨는 문제 방지
        SpriteRenderer sr =
            effect.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.sortingOrder = 100;
        }

        Destroy(effect, destroyTime);
    }
}