using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;


    // =========================================================
    // 스킬 목록
    // =========================================================

    [Header("스킬 목록")]
    public List<SkillData> skills =
        new List<SkillData>();


    // =========================================================
    // 스킬 이펙트
    // =========================================================

    [Header("스킬 이펙트")]
    public GameObject fireEffectPrefab;
    public GameObject iceEffectPrefab;
    public GameObject lightningEffectPrefab;


    // =========================================================
    // ★ 스킬 효과음
    // =========================================================

    [Header("스킬 효과음")]
    public AudioSource skillAudioSource;

    public AudioClip fireSkillSound;
    public AudioClip iceSkillSound;
    public AudioClip lightningSkillSound;

    [Range(0f, 1f)]
    public float skillSoundVolume = 0.7f;


    // =========================================================
    // 불 스킬 밸런스
    // =========================================================

    [Header("불 스킬 밸런스")]
    public int fireInitialDamage = 20;

    public int fireDotDamage = 5;

    public float fireDotDuration = 3f;

    public float fireDotInterval = 0.5f;


    // =========================================================
    // 번개 스킬 밸런스
    // =========================================================

    [Header("번개 스킬 밸런스")]
    public int lightningDamage = 100;

    public int lightningMaxTargets = 5;


    // =========================================================
    // 쿨타임 저장
    // =========================================================

    private Dictionary<string, float> cooldownEndTime =
        new Dictionary<string, float>();


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;


        // AudioSource 자동 연결
        if (skillAudioSource == null)
        {
            skillAudioSource =
                GetComponent<AudioSource>();
        }


        // AudioSource가 없으면 자동 생성
        if (skillAudioSource == null)
        {
            skillAudioSource =
                gameObject.AddComponent<AudioSource>();
        }


        skillAudioSource.playOnAwake = false;
        skillAudioSource.loop = false;
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        foreach (var skill in skills)
        {
            if (skill == null)
                continue;


            cooldownEndTime[
                skill.skillName
            ] = 0f;
        }
    }


    // =========================================================
    // 스킬 사용 가능 여부
    // =========================================================

    public bool CanUseSkill(
        string skillName
    )
    {
        if (string.IsNullOrEmpty(
                skillName))
        {
            return false;
        }


        if (!cooldownEndTime.ContainsKey(
                skillName))
        {
            return true;
        }


        return Time.time >=
               cooldownEndTime[
                   skillName
               ];
    }


    // =========================================================
    // 기존 스킬 사용
    // =========================================================
    // 다른 코드에서 사용할 수 있도록 유지
    // =========================================================

    public bool UseSkill(
        string skillName
    )
    {
        Debug.Log(
            "스킬 사용 요청 : " +
            skillName
        );


        if (!CanUseSkill(
                skillName))
        {
            Debug.Log(
                skillName +
                " 쿨타임 중!"
            );

            return false;
        }


        SkillData skill =
            skills.Find(
                s =>
                    s != null &&
                    s.skillName == skillName
            );


        if (skill == null)
        {
            Debug.LogError(
                "스킬 데이터를 찾을 수 없음 : " +
                skillName
            );

            return false;
        }


        bool executed =
            ExecuteSkill(
                skillName
            );


        if (!executed)
            return false;


        cooldownEndTime[
            skillName
        ] =
            Time.time +
            skill.cooldown;


        // 기존 방식으로 직접 UseSkill 호출해도
        // 스킬 사운드가 나오도록 유지
        PlaySkillSound(
            skillName
        );


        Debug.Log(
            skillName +
            " 사용!"
        );


        return true;
    }


    // =========================================================
    // ★ 마법진 위치에서 스킬 사용
    // =========================================================

    public bool UseSkillAtPosition(
        string skillName,
        Vector3 castPosition,
        float radius
    )
    {
        Debug.Log(
            "범위 스킬 사용 : " +
            skillName +
            " / 위치 : " +
            castPosition +
            " / 범위 : " +
            radius
        );


        // -----------------------------------------------------
        // 쿨타임 확인
        // -----------------------------------------------------

        if (!CanUseSkill(
                skillName))
        {
            Debug.Log(
                skillName +
                " 쿨타임 중!"
            );

            return false;
        }


        // -----------------------------------------------------
        // SkillData 찾기
        // -----------------------------------------------------

        SkillData skill =
            skills.Find(
                s =>
                    s != null &&
                    s.skillName == skillName
            );


        if (skill == null)
        {
            Debug.LogError(
                "스킬 데이터를 찾을 수 없음 : " +
                skillName
            );

            return false;
        }


        // -----------------------------------------------------
        // 범위 보정
        // -----------------------------------------------------

        radius =
            Mathf.Max(
                0f,
                radius
            );


        // -----------------------------------------------------
        // 실제 스킬 실행
        // -----------------------------------------------------

        bool executed =
            ExecuteSkillAtPosition(
                skillName,
                castPosition,
                radius
            );


        if (!executed)
            return false;


        // -----------------------------------------------------
        // 쿨타임 시작
        // -----------------------------------------------------

        cooldownEndTime[
            skillName
        ] =
            Time.time +
            skill.cooldown;


        // =====================================================
        // ★ 여기서 효과음 재생
        // =====================================================

        PlaySkillSound(
            skillName
        );


        Debug.Log(
            skillName +
            " 범위 스킬 발동!"
        );


        return true;
    }


    // =========================================================
    // 일반 스킬 실행
    // =========================================================

    private bool ExecuteSkill(
        string skillName
    )
    {
        switch (skillName)
        {
            case "Fireball":

                FireballSkill();

                return true;


            case "Ice Attack":

                IceAttackSkill();

                return true;


            case "Lightning":

                LightningSkill();

                return true;


            default:

                Debug.LogWarning(
                    "등록되지 않은 스킬 : " +
                    skillName
                );

                return false;
        }
    }


    // =========================================================
    // 범위 스킬 실행
    // =========================================================

    private bool ExecuteSkillAtPosition(
        string skillName,
        Vector3 castPosition,
        float radius
    )
    {
        switch (skillName)
        {
            case "Fireball":

                FireballSkill(
                    castPosition,
                    radius
                );

                return true;


            case "Ice Attack":

                IceAttackSkill(
                    castPosition,
                    radius
                );

                return true;


            case "Lightning":

                LightningSkill(
                    castPosition,
                    radius
                );

                return true;


            default:

                Debug.LogWarning(
                    "등록되지 않은 스킬 : " +
                    skillName
                );

                return false;
        }
    }


    // =========================================================
    // ★ 스킬 효과음
    // =========================================================

    private void PlaySkillSound(
        string skillName
    )
    {
        if (skillAudioSource == null)
            return;


        AudioClip clip = null;


        switch (skillName)
        {
            case "Fireball":

                clip =
                    fireSkillSound;

                break;


            case "Ice Attack":

                clip =
                    iceSkillSound;

                break;


            case "Lightning":

                clip =
                    lightningSkillSound;

                break;
        }


        if (clip == null)
        {
            Debug.LogWarning(
                "스킬 효과음이 설정되지 않았습니다 : " +
                skillName
            );

            return;
        }


        skillAudioSource.PlayOneShot(
            clip,
            skillSoundVolume
        );
    }


    // =========================================================
    // 쿨타임 UI 표시
    // =========================================================

    public float GetCooldownNormalized(
        string skillName
    )
    {
        if (!cooldownEndTime.ContainsKey(
                skillName))
        {
            return 0f;
        }


        SkillData skill =
            skills.Find(
                s =>
                    s != null &&
                    s.skillName == skillName
            );


        if (skill == null)
            return 0f;


        if (skill.cooldown <= 0f)
            return 0f;


        float remaining =
            cooldownEndTime[
                skillName
            ] -
            Time.time;


        return Mathf.Clamp01(
            remaining /
            skill.cooldown
        );
    }


    // =========================================================
    // 남은 쿨타임
    // =========================================================

    public float GetCooldownRemaining(
        string skillName
    )
    {
        if (!cooldownEndTime.ContainsKey(
                skillName))
        {
            return 0f;
        }


        return Mathf.Max(
            0f,
            cooldownEndTime[
                skillName
            ] -
            Time.time
        );
    }


    // =========================================================
    // 불 스킬 - 전체
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


            if (enemy.IsDead)
                continue;


            enemy.TakeDamage(
                fireInitialDamage,
                false
            );


            if (
                fireDotDamage > 0 &&
                fireDotDuration > 0f
            )
            {
                StartCoroutine(
                    ApplyFireDot(
                        enemy
                    )
                );
            }


            CreateEffect(
                fireEffectPrefab,
                enemy.transform.position,
                fireDotDuration
            );
        }


        Debug.Log(
            "불 스킬 발동!"
        );
    }


    // =========================================================
    // 불 스킬 - 마법진 범위
    // =========================================================

    private void FireballSkill(
        Vector3 castPosition,
        float radius
    )
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


            if (enemy.IsDead)
                continue;


            float distance =
                Vector2.Distance(
                    castPosition,
                    enemy.transform.position
                );


            if (distance > radius)
                continue;


            enemy.TakeDamage(
                fireInitialDamage,
                false
            );


            if (
                fireDotDamage > 0 &&
                fireDotDuration > 0f
            )
            {
                StartCoroutine(
                    ApplyFireDot(
                        enemy
                    )
                );
            }


            CreateEffect(
                fireEffectPrefab,
                enemy.transform.position,
                fireDotDuration
            );
        }


        Debug.Log(
            "불 범위 스킬 발동!"
        );
    }


    // =========================================================
    // 불 지속 데미지
    // =========================================================

    private IEnumerator ApplyFireDot(
        MonsterHealth enemy
    )
    {
        float interval =
            Mathf.Max(
                0.05f,
                fireDotInterval
            );


        float elapsed = 0f;


        while (
            elapsed <
            fireDotDuration
        )
        {
            yield return new WaitForSeconds(
                interval
            );


            if (
                enemy == null ||
                enemy.IsDead
            )
            {
                yield break;
            }


            enemy.TakeDamage(
                fireDotDamage,
                false
            );


            elapsed += interval;
        }
    }


    // =========================================================
    // 얼음 스킬 - 전체
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


            MonsterHealth health =
                enemy.GetComponent<
                    MonsterHealth
                >();


            if (
                health != null &&
                health.IsDead
            )
            {
                continue;
            }


            enemy.Freeze(3f);


            CreateIceEffect(
                enemy
            );
        }


        Debug.Log(
            "얼음 스킬 발동!"
        );
    }


    // =========================================================
    // 얼음 스킬 - 마법진 범위
    // =========================================================

    private void IceAttackSkill(
        Vector3 castPosition,
        float radius
    )
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


            MonsterHealth health =
                enemy.GetComponent<
                    MonsterHealth
                >();


            if (
                health != null &&
                health.IsDead
            )
            {
                continue;
            }


            float distance =
                Vector2.Distance(
                    castPosition,
                    enemy.transform.position
                );


            if (distance > radius)
                continue;


            enemy.Freeze(3f);


            CreateIceEffect(
                enemy
            );
        }


        Debug.Log(
            "얼음 범위 스킬 발동!"
        );
    }


    // =========================================================
    // 얼음 이펙트
    // =========================================================

    private void CreateIceEffect(
        MonsterMove enemy
    )
    {
        if (iceEffectPrefab == null)
            return;


        GameObject effect =
            Instantiate(
                iceEffectPrefab,
                enemy.transform
            );


        effect.transform.localPosition =
            Vector3.zero;


        effect.transform.localRotation =
            Quaternion.identity;


        SpriteRenderer sr =
            effect.GetComponent<
                SpriteRenderer
            >();


        if (sr != null)
        {
            sr.sortingOrder = 100;
        }


        Destroy(
            effect,
            3f
        );
    }


    // =========================================================
    // 번개 스킬 - 전체
    // =========================================================

    private void LightningSkill()
    {
        MonsterHealth[] allEnemies =
            FindObjectsByType<MonsterHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        List<MonsterHealth> enemies =
            new List<MonsterHealth>();


        foreach (var enemy in allEnemies)
        {
            if (enemy == null)
                continue;


            if (enemy.IsDead)
                continue;


            enemies.Add(
                enemy
            );
        }


        enemies.Sort(
            (a, b) =>
                Vector3.Distance(
                    transform.position,
                    a.transform.position
                ).CompareTo(
                    Vector3.Distance(
                        transform.position,
                        b.transform.position
                    )
                )
        );


        int hitCount =
            Mathf.Min(
                lightningMaxTargets,
                enemies.Count
            );


        for (
            int i = 0;
            i < hitCount;
            i++
        )
        {
            if (
                enemies[i] == null ||
                enemies[i].IsDead
            )
            {
                continue;
            }


            enemies[i].TakeDamage(
                lightningDamage,
                false
            );


            CreateEffect(
                lightningEffectPrefab,
                enemies[i].transform.position,
                1f
            );
        }


        Debug.Log(
            "번개 스킬 발동!"
        );
    }


    // =========================================================
    // 번개 스킬 - 마법진 범위
    // =========================================================

    private void LightningSkill(
        Vector3 castPosition,
        float radius
    )
    {
        MonsterHealth[] allEnemies =
            FindObjectsByType<MonsterHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        List<MonsterHealth> enemies =
            new List<MonsterHealth>();


        // -----------------------------------------------------
        // 마법진 범위 안의 몬스터만 찾기
        // -----------------------------------------------------

        foreach (var enemy in allEnemies)
        {
            if (enemy == null)
                continue;


            if (enemy.IsDead)
                continue;


            float distance =
                Vector2.Distance(
                    castPosition,
                    enemy.transform.position
                );


            if (distance > radius)
                continue;


            enemies.Add(
                enemy
            );
        }


        // -----------------------------------------------------
        // 가까운 순서
        // -----------------------------------------------------

        enemies.Sort(
            (a, b) =>
                Vector2.Distance(
                    castPosition,
                    a.transform.position
                ).CompareTo(
                    Vector2.Distance(
                        castPosition,
                        b.transform.position
                    )
                )
        );


        // -----------------------------------------------------
        // 최대 5마리
        // -----------------------------------------------------

        int hitCount =
            Mathf.Min(
                lightningMaxTargets,
                enemies.Count
            );


        for (
            int i = 0;
            i < hitCount;
            i++
        )
        {
            if (
                enemies[i] == null ||
                enemies[i].IsDead
            )
            {
                continue;
            }


            enemies[i].TakeDamage(
                lightningDamage,
                false
            );


            CreateEffect(
                lightningEffectPrefab,
                enemies[i].transform.position,
                1f
            );
        }


        Debug.Log(
            "번개 범위 스킬 발동! " +
            "적중 수 : " +
            hitCount
        );
    }


    // =========================================================
    // 일반 이펙트 생성
    // =========================================================

    private void CreateEffect(
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


        SpriteRenderer sr =
            effect.GetComponent<
                SpriteRenderer
            >();


        if (sr != null)
        {
            sr.sortingOrder = 100;
        }


        Destroy(
            effect,
            destroyTime
        );
    }
}
