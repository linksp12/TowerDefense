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

    [Header("불 스킬 밸런스")]
    public int fireInitialDamage = 20;
    public int fireDotDamage = 5;
    public float fireDotDuration = 3f;
    public float fireDotInterval = 0.5f;

    [Header("번개 스킬 밸런스")]
    public int lightningDamage = 100;
    public int lightningMaxTargets = 5;

    // 스킬별 쿨타임 종료 시간 저장
    private Dictionary<string, float> cooldownEndTime =
        new Dictionary<string, float>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 모든 스킬 쿨타임 초기화
        foreach (var skill in skills)
        {
            cooldownEndTime[skill.skillName] = 0f;
        }
    }

    // =========================
    // 스킬 사용 가능한지 체크
    // =========================
    public bool CanUseSkill(string skillName)
    {
        // 등록 안된 스킬이면 사용 가능 처리
        if (!cooldownEndTime.ContainsKey(skillName))
            return true;

        // 현재 시간이 쿨타임 종료 시간보다 크면 사용 가능
        return Time.time >= cooldownEndTime[skillName];
    }

    // =========================
    // 스킬 사용
    // =========================
    public bool UseSkill(string skillName)
    {
        Debug.Log("버튼 눌림 : " + skillName);

        // 쿨타임 체크
        if (!CanUseSkill(skillName))
        {
            Debug.Log(skillName + " 쿨타임 중!");
            return false;
        }

        // 스킬 데이터 찾기
        SkillData skill =
            skills.Find(s => s.skillName == skillName);

        if (skill == null)
        {
            Debug.LogError(
                "스킬 데이터를 찾을 수 없음 : " + skillName
            );

            return false;
        }

        // 쿨타임 시작
        cooldownEndTime[skillName] =
            Time.time + skill.cooldown;

        // 스킬 실행
        ExecuteSkill(skillName);

        Debug.Log(skillName + " 사용!");

        return true;
    }

    // =========================
    // 스킬 실행
    // =========================
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

            default:
                Debug.LogWarning(
                    "등록되지 않은 스킬 : " + skillName
                );
                break;
        }
    }

    // =========================
    // UI 쿨타임 표시용
    // =========================
    public float GetCooldownNormalized(string skillName)
    {
        if (!cooldownEndTime.ContainsKey(skillName))
            return 0f;

        SkillData skill =
            skills.Find(s => s.skillName == skillName);

        if (skill == null)
            return 0f;

        float remaining =
            cooldownEndTime[skillName] - Time.time;

        return Mathf.Clamp01(
            remaining / skill.cooldown
        );
    }

    // =========================
    // 남은 쿨타임 반환
    // =========================
    public float GetCooldownRemaining(string skillName)
    {
        if (!cooldownEndTime.ContainsKey(skillName))
            return 0f;

        return Mathf.Max(
            0f,
            cooldownEndTime[skillName] - Time.time
        );
    }

    // =========================================================
    // 불 스킬
    // =========================================================
    private void FireballSkill()
    {
        MonsterHealth[] enemies =
            FindObjectsByType<MonsterHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (var enemy in enemies)
        {
            if (enemy == null)
                continue;

            // 이미 죽은 몬스터는 제외
            if (enemy.IsDead)
                continue;

            // 최초 데미지
            // 스킬 사운드는 버튼에서 한 번만 재생
            enemy.TakeDamage(
                fireInitialDamage,
                false
            );

            // 지속 데미지
            if (fireDotDamage > 0 &&
                fireDotDuration > 0f)
            {
                StartCoroutine(
                    ApplyFireDot(enemy)
                );
            }

            // 불 이펙트
            CreateEffect(
                fireEffectPrefab,
                enemy.transform.position,
                fireDotDuration
            );
        }

        Debug.Log("불 스킬 발동!");
    }

    // =========================
    // 불 지속 데미지
    // =========================
    private IEnumerator ApplyFireDot(
        MonsterHealth enemy)
    {
        float interval =
            Mathf.Max(0.05f, fireDotInterval);

        float elapsed = 0f;

        while (elapsed < fireDotDuration)
        {
            yield return new WaitForSeconds(interval);

            // 몬스터가 삭제되었거나 죽었으면 종료
            if (enemy == null || enemy.IsDead)
                yield break;

            enemy.TakeDamage(
                fireDotDamage,
                false
            );

            elapsed += interval;
        }
    }

    // =========================================================
    // 얼음 스킬
    // =========================================================
    private void IceAttackSkill()
    {
        MonsterMove[] enemies =
            FindObjectsByType<MonsterMove>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (var enemy in enemies)
        {
            if (enemy == null)
                continue;

            // MonsterHealth가 있으면 사망 여부 확인
            MonsterHealth monsterHealth =
                enemy.GetComponent<MonsterHealth>();

            if (monsterHealth != null &&
                monsterHealth.IsDead)
            {
                continue;
            }

            // 몬스터 빙결
            enemy.Freeze(3f);

            // =================================================
            // 중요:
            // 얼음 이펙트를 몬스터의 자식으로 생성
            // =================================================
            if (iceEffectPrefab != null)
            {
                GameObject effect =
                    Instantiate(
                        iceEffectPrefab,
                        enemy.transform
                    );

                // 몬스터 중심 위치
                effect.transform.localPosition =
                    Vector3.zero;

                effect.transform.localRotation =
                    Quaternion.identity;

                // 몬스터보다 앞에 표시
                SpriteRenderer sr =
                    effect.GetComponent<SpriteRenderer>();

                if (sr != null)
                {
                    sr.sortingOrder = 100;
                }

                // 최대 3초 후 삭제
                Destroy(effect, 3f);
            }
        }

        Debug.Log("얼음 스킬 발동!");
    }

    // =========================================================
    // 번개 스킬
    // =========================================================
    private void LightningSkill()
    {
        MonsterHealth[] allEnemies =
            FindObjectsByType<MonsterHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        List<MonsterHealth> sortedEnemies =
            new List<MonsterHealth>(
                allEnemies
            );

        // 죽은 몬스터 제거
        sortedEnemies.RemoveAll(
            enemy =>
                enemy == null ||
                enemy.IsDead
        );

        // 가까운 적 순으로 정렬
        sortedEnemies.Sort(
            (a, b) =>
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
            Mathf.Min(
                lightningMaxTargets,
                sortedEnemies.Count
            );

        for (int i = 0; i < hitCount; i++)
        {
            if (sortedEnemies[i] == null ||
                sortedEnemies[i].IsDead)
            {
                continue;
            }

            // 데미지
            sortedEnemies[i].TakeDamage(
                lightningDamage,
                false
            );

            // 번개 이펙트
            CreateEffect(
                lightningEffectPrefab,
                sortedEnemies[i].transform.position,
                1f
            );
        }

        Debug.Log("번개 스킬 발동!");
    }

    // =========================================================
    // 일반 이펙트 생성
    // =========================================================
    private void CreateEffect(
        GameObject effectPrefab,
        Vector3 position,
        float destroyTime)
    {
        if (effectPrefab == null)
            return;

        GameObject effect =
            Instantiate(
                effectPrefab,
                position,
                Quaternion.identity
            );

        SpriteRenderer sr =
            effect.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.sortingOrder = 100;
        }

        // 일정 시간 후 이펙트 삭제
        Destroy(
            effect,
            destroyTime
        );
    }
}
