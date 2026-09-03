using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHp = 200;

    [Header("Reward")]
    public int goldReward = 20;

    // =========================================================
    // Boss Information
    // =========================================================
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

    // =========================================================
    // HP UI
    // =========================================================
    [Header("HP UI")]
    public Slider monsterHpSlider;

    // =========================================================
    // Hit Effect
    // =========================================================
    [Header("Hit Effect")]
    public GameObject hitEffectPrefab;

    public float hitEffectDestroyTime = 0.6f;

    // =========================================================
    // Hit Flash
    // =========================================================
    [Header("Hit Flash")]
    public bool useHitFlash = true;

    public Color hitColor = Color.red;

    public float flashTime = 0.08f;

    // =========================================================
    // Sound
    // =========================================================
    [Header("Sound")]
    public AudioSource audioSource;

    public AudioClip hitSound;

    public AudioClip deathSound;

    // =========================================================
    // Death Animation
    // =========================================================
    [Header("Death Animation")]
    [Tooltip("죽음 애니메이션이 재생되는 시간")]
    public float deathAnimationTime = 0.6f;

    // =========================================================
    // Animator
    // =========================================================
    [Header("Animator")]
    [Tooltip("실제 Animator Controller 안의 죽음 State 이름")]
    public string deathStateName = "death_NormalSlime";

    // =========================================================
    // Private Variables
    // =========================================================
    private int currentHp;

    private bool isDead = false;

    private SpriteRenderer spriteRenderer;

    private Color originalColor;

    private Coroutine flashCoroutine;

    private Animator animator;


    // =========================================================
    // Public Properties
    // =========================================================

    // BossInfoUI에서 현재 체력을 읽기 위한 프로퍼티
    public int CurrentHp => currentHp;

    // BossInfoUI에서 죽었는지 확인하기 위한 프로퍼티
    public bool IsDead => isDead;


    // =========================================================
    // Awake
    // =========================================================
    void Awake()
    {
        // -----------------------------------------------------
        // SpriteRenderer 가져오기
        // -----------------------------------------------------
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // -----------------------------------------------------
        // AudioSource 가져오기
        // -----------------------------------------------------
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // -----------------------------------------------------
        // Animator 가져오기
        // -----------------------------------------------------
        animator = GetComponent<Animator>();

        // -----------------------------------------------------
        // HP 초기화
        // -----------------------------------------------------
        currentHp = maxHp;

        isDead = false;
    }


    // =========================================================
    // Start
    // =========================================================
    void Start()
    {
        // -----------------------------------------------------
        // HP UI 설정
        // -----------------------------------------------------
        if (monsterHpSlider != null)
        {
            monsterHpSlider.maxValue = maxHp;
            monsterHpSlider.value = currentHp;
        }
    }


    // =========================================================
    // Take Damage
    // =========================================================
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, true);
    }


    public void TakeDamage(int damage, bool playHitSound)
    {
        // 이미 죽었으면 데미지 무시
        if (isDead)
            return;

        // -----------------------------------------------------
        // 데미지 적용
        // -----------------------------------------------------
        currentHp -= damage;

        // HP가 음수가 되지 않도록 제한
        currentHp = Mathf.Max(currentHp, 0);

        // -----------------------------------------------------
        // 피격 효과
        // -----------------------------------------------------
        PlayHitFeedback(playHitSound);

        // -----------------------------------------------------
        // HP UI 갱신
        // -----------------------------------------------------
        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        // -----------------------------------------------------
        // HP가 0 이하라면 사망
        // -----------------------------------------------------
        if (currentHp <= 0)
        {
            Die();
        }
    }


    // =========================================================
    // Hit Feedback
    // =========================================================
    private void PlayHitFeedback(bool playHitSound)
    {
        // 피격 사운드
        if (playHitSound)
        {
            PlayHitSound();
        }

        // 피격 이펙트
        SpawnHitEffect();

        // 피격 플래시
        PlayHitFlash();
    }


    // =========================================================
    // Hit Sound
    // =========================================================
    private void PlayHitSound()
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }


    // =========================================================
    // Hit Effect
    // =========================================================
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null)
            return;

        GameObject effect = Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(effect, hitEffectDestroyTime);
    }


    // =========================================================
    // Hit Flash
    // =========================================================
    private void PlayHitFlash()
    {
        if (!useHitFlash)
            return;

        if (spriteRenderer == null)
            return;

        // 기존 Flash 중단
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }


    // =========================================================
    // Hit Flash Coroutine
    // =========================================================
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
    // Die
    // =========================================================
    private void Die()
    {
        // 이미 죽었다면 중복 실행 방지
        if (isDead)
            return;

        isDead = true;

        Debug.Log(gameObject.name + " 몬스터 사망");


        // =====================================================
        // 이동 정지
        // =====================================================
        MonsterMove move = GetComponent<MonsterMove>();

        if (move != null)
        {
            move.enabled = false;
        }


        // =====================================================
        // Collider 비활성화
        // =====================================================
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }


        // =====================================================
        // 죽음 사운드
        // =====================================================
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(
                deathSound,
                transform.position,
                0.8f
            );
        }


        // =====================================================
        // WaveManager에 사망 알림
        // =====================================================
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager != null)
        {
            waveManager.OnMonsterKilled();
        }


        // =====================================================
        // 골드 지급
        // =====================================================
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(goldReward);
        }


        // =====================================================
        // 죽음 애니메이션 실행
        // =====================================================
        PlayDeathAnimation();


        // =====================================================
        // 죽음 애니메이션 후 삭제
        // =====================================================
        StartCoroutine(DeathCoroutine());
    }


    // =========================================================
    // Play Death Animation
    // =========================================================
    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " : Animator가 없습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // Death State 이름 확인
        // -----------------------------------------------------
        if (string.IsNullOrEmpty(deathStateName))
        {
            Debug.LogWarning(
                gameObject.name +
                " : deathStateName이 비어 있습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // Base Layer의 Death State 찾기
        // -----------------------------------------------------
        string statePath = "Base Layer." + deathStateName;

        int stateHash = Animator.StringToHash(statePath);


        // -----------------------------------------------------
        // State 존재 여부 확인
        // -----------------------------------------------------
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
    // Death Coroutine
    // =========================================================
    private IEnumerator DeathCoroutine()
    {
        // -----------------------------------------------------
        // 죽음 애니메이션 재생 시간 대기
        // -----------------------------------------------------
        yield return new WaitForSeconds(deathAnimationTime);

        // -----------------------------------------------------
        // 몬스터 삭제
        // -----------------------------------------------------
        Destroy(gameObject);
    }
}
