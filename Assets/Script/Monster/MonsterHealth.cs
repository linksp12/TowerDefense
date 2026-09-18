using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHp = 200;
    public int maxShield = 10;
    public GameObject shieldFXObject;

    [Header("Reward")]
    public int goldReward = 20;

    [Header("Boss Stats")]
    [Tooltip("보스 몬스터인지 여부")]
    public bool isBoss = false;

    [Tooltip("보스의 방어력")]
    public int defense = 0;

    [Tooltip("보스의 마법 저항력")]
    public int magicResistance = 0;

    [Tooltip("보스 정보창에 표시할 초상화")]
    public Sprite bossPortrait;

    [Tooltip("보스 정보창에 표시할 이름")]
    public string bossName = "Forest Golem";

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

    public int CurrentHp => currentHp;
    public bool IsDead => isDead;

    private void Awake()
    {
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

        currentHp = maxHp;
        currentShield = maxShield;
        isDead = false;
    }

    private void Start()
    {
        if (monsterHpSlider != null)
        {
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
            }

            currentHp -= finalDamage;
            currentHp = Mathf.Max(currentHp, 0);
        }

        // =====================================================
        // 데미지 팝업
        // =====================================================
        if (shieldBlocked)
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
        if (GetComponent<TutorialPracticeMonster>() == null)
        {
            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
                waveManager.OnMonsterKilled();

            if (GameManager.Instance != null)
                GameManager.Instance.AddMoney(goldReward);
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
    // 죽음 후 삭제
    // =========================================================
    private IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(deathAnimationTime);

        Destroy(gameObject);
    }
}
