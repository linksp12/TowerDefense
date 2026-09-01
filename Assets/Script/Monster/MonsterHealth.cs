using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 100;

    [Header("Reward")]
    public int goldReward = 20;

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
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("Death Animation")]
    public float deathAnimationTime = 0.6f;

    [Header("Animator")]
    [Tooltip("게임 시작 시 재생할 일반 애니메이션 State 이름")]
    public string normalStateName = "normal_slime";

    [Tooltip("몬스터가 죽을 때 재생할 Animator State 이름")]
    public string deathStateName = "death_NormalSlime";

    private int currentHp;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private Animator animator;


    // ==============================
    // Awake
    // ==============================
    void Awake()
    {
        // SpriteRenderer 가져오기
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // AudioSource 가져오기
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Animator 가져오기
        animator = GetComponent<Animator>();

        // HP 초기화
        currentHp = maxHp;
        isDead = false;
    }


    // ==============================
    // Start
    // ==============================
    void Start()
    {
        // HP UI 설정
        if (monsterHpSlider != null)
        {
            monsterHpSlider.maxValue = maxHp;
            monsterHpSlider.value = currentHp;
        }

        // ==============================
        // 게임 시작 시 정상 애니메이션 실행
        // ==============================
        if (animator != null && !string.IsNullOrEmpty(normalStateName))
        {
            animator.Play(normalStateName, 0, 0f);
        }
    }


    // ==============================
    // 데미지 받기
    // ==============================
    public void TakeDamage(int damage)
    {
        // 이미 죽었으면 추가 데미지 무시
        if (isDead)
            return;

        currentHp -= damage;

        Debug.Log("몬스터 피격! 남은 HP: " + currentHp);

        // 피격 효과
        PlayHitFeedback();

        // HP UI 갱신
        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        // HP가 0 이하가 되면 사망
        if (currentHp <= 0)
        {
            Die();
        }
    }


    // ==============================
    // 피격 효과 전체
    // ==============================
    private void PlayHitFeedback()
    {
        PlayHitSound();
        SpawnHitEffect();
        PlayHitFlash();
    }


    // ==============================
    // 피격 사운드
    // ==============================
    private void PlayHitSound()
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }


    // ==============================
    // 피격 이펙트
    // ==============================
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


    // ==============================
    // 피격 Flash
    // ==============================
    private void PlayHitFlash()
    {
        if (!useHitFlash)
            return;

        if (spriteRenderer == null)
            return;

        // 이전 Flash 코루틴이 실행 중이면 중지
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }


    // ==============================
    // 피격 Flash 코루틴
    // ==============================
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


    // ==============================
    // 몬스터 사망
    // ==============================
    private void Die()
    {
        // 이미 죽었으면 실행하지 않음
        if (isDead)
            return;

        isDead = true;

        Debug.Log("몬스터 사망");


        // ==============================
        // 이동 정지
        // ==============================

        MonsterMove move = GetComponent<MonsterMove>();

        if (move != null)
        {
            move.enabled = false;
        }


        // ==============================
        // Collider 비활성화
        // ==============================

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }


        // ==============================
        // 죽음 사운드
        // ==============================

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(
                deathSound,
                transform.position,
                0.8f
            );
        }


        // ==============================
        // WaveManager에 사망 알림
        // ==============================

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager != null)
        {
            waveManager.OnMonsterKilled();
        }


        // ==============================
        // 골드 지급
        // ==============================

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(goldReward);
        }


        // ==============================
        // 죽음 애니메이션 실행
        // ==============================
        // Animator의 Transition을 사용하지 않고
        // death_NormalSlime State를 직접 실행합니다.

        if (animator != null && !string.IsNullOrEmpty(deathStateName))
        {
            animator.Play(deathStateName, 0, 0f);
        }


        // ==============================
        // 죽음 애니메이션이 끝난 후 삭제
        // ==============================

        StartCoroutine(DeathCoroutine());
    }


    // ==============================
    // 죽음 애니메이션 후 삭제
    // ==============================
    private IEnumerator DeathCoroutine()
    {
        // 죽음 애니메이션이 보이는 시간
        yield return new WaitForSeconds(deathAnimationTime);

        Destroy(gameObject);
    }
}