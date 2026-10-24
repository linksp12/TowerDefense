using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    [Header("Balance Data")]
    [Tooltip("연결하면 최대 체력과 처치 골드를 이 데이터에서 읽습니다.")]
    public MonsterData monsterData;
    [HideInInspector] public int maxHp;
    [HideInInspector] public int maxShield;
    public GameObject shieldFXObject;
    [HideInInspector] public int goldReward;
    [HideInInspector] public bool isBoss;
    [HideInInspector] public int defense;
    [HideInInspector] public int magicResistance;
    [HideInInspector] public Sprite bossPortrait;
    [HideInInspector] public string bossName;

    [Header("HP UI")]
    public Slider monsterHpSlider;

    [Header("Hit Effect")]
    public GameObject hitEffectPrefab;
    public float hitEffectDestroyTime = 0.6f;

    [Header("Hit Flash")]
    public bool useHitFlash = true;
    public Color hitColor = Color.red;
    public float flashTime = 0.08f;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip deathSound;

    [Header("Death Animation")]
    [Tooltip("죽음 애니메이션이 재생되는 시간")]
    public float deathAnimationTime = 0.6f;

    [Header("Animator")]
    [Tooltip("실제 Animator Controller 안의 죽음 State 이름")]
    public string deathStateName = "death_NormalSlime";

    private int currentHp;
    private int currentShield;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private Animator animator;
    private GolemRootSkill golemRootSkill;
    private MonsterReviveOnce reviveOnce;
    private MonsterSplitOnDeath splitOnDeath;

    public int CurrentHp => currentHp;
    public bool IsDead => isDead;

    /// <summary>체력이 깎였고 아직 살아 있을 때 호출됩니다. (몬스터, 받은 피해)</summary>
    public event System.Action<MonsterHealth, int> Damaged;

    private void Awake()
    {
        ApplyStatsFromData();

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 죽음 효과음 등에 사용할 AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        animator = GetComponent<Animator>();
        golemRootSkill = GetComponent<GolemRootSkill>();
        reviveOnce = GetComponent<MonsterReviveOnce>();
        splitOnDeath = GetComponent<MonsterSplitOnDeath>();

        currentHp = maxHp;
        currentShield = maxShield;
        isDead = false;
    }

    public void ApplyStatsFromData()
    {
        if (monsterData == null)
            return;

        maxHp = monsterData.maxHp;
        goldReward = monsterData.goldReward;
        maxShield = monsterData.maxShield;
        isBoss = monsterData.isBoss;
        defense = monsterData.defense;
        magicResistance = monsterData.magicResistance;
        bossPortrait = monsterData.bossPortrait;
        bossName = monsterData.bossName;
    }

    private void Start()
    {
        if (monsterHpSlider != null)
        {
            BattleUiSkin.ApplyMonsterHpBar(monsterHpSlider);
            monsterHpSlider.maxValue = maxHp;
            monsterHpSlider.value = currentHp;
        }

        if (shieldFXObject != null)
        {
            shieldFXObject.SetActive(currentShield > 0);
        }
    }

    // =========================================================
    // 데미지 처리
    // =========================================================
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, true);
    }

    public void TakeDamage(int damage, bool playHitSound)
    {
        TakeDamage(damage, playHitSound, 0f, 1f);
    }

    public void TakeDamage(
        int damage,
        bool playHitSound,
        float criticalChance,
        float criticalDamageMultiplier)
    {
        if (isDead)
        {
            return;
        }

        int finalDamage = Mathf.Max(0, damage);
        bool isCritical = false;
        bool shieldBlocked =
            shieldFXObject != null && currentShield > 0;

        // 얼음 수호·얼음 결계 등 외부 보호 (피해 감소 / 무적)
        TryGetComponent(out MonsterWard ward);
        bool wardBlocked = !shieldBlocked && ward != null && ward.IsInvulnerable;

        // =====================================================
        // 실드 처리
        // =====================================================
        if (shieldBlocked)
        {
            currentShield -= 1;

            if (currentShield <= 0)
            {
                shieldFXObject.SetActive(false);
            }
        }
        else if (wardBlocked)
        {
            finalDamage = 0;
        }
        else
        {
            // =================================================
            // 치명타 처리
            // =================================================
            if (criticalChance > 0f &&
                Random.value < Mathf.Clamp01(criticalChance))
            {
                finalDamage = Mathf.RoundToInt(
                    finalDamage *
                    Mathf.Max(1f, criticalDamageMultiplier)
                );

                isCritical = true;
                CombatFeedback.CriticalHit();
            }

            if (ward != null)
            {
                finalDamage = ward.ModifyDamage(finalDamage);
            }

            currentHp -= finalDamage;
            currentHp = Mathf.Max(currentHp, 0);
        }

        // =====================================================
        // 데미지 팝업
        // =====================================================
        if (shieldBlocked || wardBlocked)
        {
            DamagePopup.ShowShield(transform.position);
        }
        else
        {
            DamagePopup.Show(
                transform.position,
                finalDamage,
                isCritical
            );
        }

        // =====================================================
        // 피격 효과
        // =====================================================
        PlayHitFeedback(playHitSound);

        // =====================================================
        // HP UI
        // =====================================================
        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        // =====================================================
        // 골렘 스킬
        // =====================================================
        if (currentHp > 0 && golemRootSkill != null)
        {
            golemRootSkill.OnDamaged();
        }

        // 얼음 마녀·화염 드래곤 등 특수 능력
        if (currentHp > 0 && !shieldBlocked && !wardBlocked)
        {
            Damaged?.Invoke(this, finalDamage);
        }

        // =====================================================
        // 사망
        // =====================================================
        if (currentHp <= 0)
        {
            Die();
        }
    }

    // =========================================================
    // 피격 효과 전체
    // =========================================================
    private void PlayHitFeedback(bool playHitSound)
    {
        if (playHitSound)
        {
            PlayHitSound();
        }

        SpawnHitEffect();
        PlayHitFlash();
    }

    // =========================================================
    // 타격음
    // =========================================================
    private void PlayHitSound()
    {
        // 이제 몬스터 개별 AudioSource에서 재생하지 않고
        // 씬의 HitSoundManager가 전체 타격음을 관리함
        if (HitSoundManager.Instance != null)
        {
            HitSoundManager.Instance.PlayHitSound();
        }
    }

    // =========================================================
    // 피격 이펙트
    // =========================================================
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(effect, hitEffectDestroyTime);
    }

    // =========================================================
    // 피격 플래시
    // =========================================================
    private void PlayHitFlash()
    {
        if (!useHitFlash || spriteRenderer == null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = hitColor;

        yield return new WaitForSeconds(flashTime);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        flashCoroutine = null;
    }

    // =========================================================
    // 사망 처리
    // =========================================================
    private void Die()
    {
        if (isDead)
        {
            return;
        }

        // 해골 병사: 첫 사망은 쓰러졌다가 다시 일어난다.
        if (reviveOnce != null && reviveOnce.TryInterceptDeath())
        {
            return;
        }

        isDead = true;

        Debug.Log(gameObject.name + " 몬스터 사망");

        // =====================================================
        // 이동 중지
        // =====================================================
        MonsterMove move = GetComponent<MonsterMove>();

        if (move != null)
        {
            move.enabled = false;
        }

        // =====================================================
        // 콜라이더 비활성화
        // =====================================================
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }

        // =====================================================
        // 죽음 효과음
        // =====================================================
        if (deathSound != null)
        {
            AudioManager.PlaySFXAtPoint(
                deathSound,
                transform.position,
                0.8f
            );
        }

        // 연습 몬스터는 실제 웨이브 처치 수와 골드를 변경하지 않는다.
        bool practice = GetComponent<TutorialPracticeMonster>() != null;
        if (!practice)
        {
            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
                waveManager.OnMonsterKilled();

            if (GameManager.Instance != null)
                GameManager.Instance.AddMoney(goldReward);
        }

        // 처치 연출: 연기·처치음, 실제 보상이 있을 때만 골드 숫자와 날아가는 동전
        CombatFeedback.MonsterKilled(transform.position, practice ? 0 : goldReward, isBoss);

        // 마그마 슬라임: 작은 슬라임으로 분열
        if (!practice && splitOnDeath != null)
        {
            splitOnDeath.Split();
        }

        // =====================================================
        // 죽음 애니메이션
        // =====================================================
        PlayDeathAnimation();

        StartCoroutine(DeathCoroutine());
    }

    // =========================================================
    // 죽음 애니메이션
    // =========================================================
    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning(
                gameObject.name + " : Animator가 없습니다."
            );

            return;
        }

        if (string.IsNullOrEmpty(deathStateName))
        {
            Debug.LogWarning(
                gameObject.name +
                " : deathStateName이 비어 있습니다."
            );

            return;
        }

        string statePath = "Base Layer." + deathStateName;
        int stateHash = Animator.StringToHash(statePath);

        if (animator.HasState(0, stateHash))
        {
            animator.Play(
                statePath,
                0,
                0f
            );
        }
        else
        {
            Debug.LogError(
                gameObject.name +
                " : Animator에서 Death State를 찾을 수 없습니다.\n" +
                "입력한 이름 = " +
                statePath
            );
        }
    }

    // =========================================================
    // 특수 능력용: 쓰러짐(공격 대상 제외)·체력 회복·기본 색
    // =========================================================
    public void SetDowned(bool downed)
    {
        isDead = downed;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = !downed;

        MonsterMove move = GetComponent<MonsterMove>();
        if (move != null)
            move.enabled = !downed;

        if (monsterHpSlider != null)
            monsterHpSlider.gameObject.SetActive(!downed);
    }

    public void RestoreHpRatio(float ratio)
    {
        currentHp = Mathf.Clamp(Mathf.RoundToInt(maxHp * ratio), 1, maxHp);

        if (monsterHpSlider != null)
            monsterHpSlider.value = currentHp;
    }

    /// <summary>피격 깜빡임이 끝난 뒤 돌아갈 색을 바꿉니다. (분노 상태 붉은 빛 등)</summary>
    public void SetBaseColor(Color color)
    {
        originalColor = color;

        if (spriteRenderer != null && flashCoroutine == null)
            spriteRenderer.color = color;
    }

    // =========================================================
    // 죽음 후 삭제
    // =========================================================
    private IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(deathAnimationTime);

        Destroy(gameObject);
    }
}
