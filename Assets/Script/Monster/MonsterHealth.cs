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
    public AudioClip hitSound;
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

        if (shieldFXObject != null && currentShield > 0)
        {
            currentShield -= 1;

            if (currentShield <= 0)
            {
                shieldFXObject.SetActive(false);
            }
        }
        else
        {
            if (criticalChance > 0f &&
                Random.value < Mathf.Clamp01(criticalChance))
            {
                finalDamage = Mathf.RoundToInt(
                    finalDamage * Mathf.Max(1f, criticalDamageMultiplier)
                );
                isCritical = true;
            }

            currentHp -= finalDamage;
            currentHp = Mathf.Max(currentHp, 0);
        }

        DamagePopup.Show(transform.position, finalDamage, isCritical);

        PlayHitFeedback(playHitSound);

        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        if (currentHp > 0 && golemRootSkill != null)
        {
            golemRootSkill.OnDamaged();
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void PlayHitFeedback(bool playHitSound)
    {
        if (playHitSound)
        {
            PlayHitSound();
        }

        SpawnHitEffect();
        PlayHitFlash();
    }

    private void PlayHitSound()
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

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

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        Debug.Log(gameObject.name + " 몬스터 사망");

        MonsterMove move = GetComponent<MonsterMove>();

        if (move != null)
        {
            move.enabled = false;
        }

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(
                deathSound,
                transform.position,
                0.8f
            );
        }

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager != null)
        {
            waveManager.OnMonsterKilled();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMoney(goldReward);
        }

        PlayDeathAnimation();
        StartCoroutine(DeathCoroutine());
    }

    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning(gameObject.name + " : Animator가 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(deathStateName))
        {
            Debug.LogWarning(
                gameObject.name + " : deathStateName이 비어 있습니다."
            );
            return;
        }

        string statePath = "Base Layer." + deathStateName;
        int stateHash = Animator.StringToHash(statePath);

        if (animator.HasState(0, stateHash))
        {
            animator.Play(statePath, 0, 0f);
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

    private IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(deathAnimationTime);
        Destroy(gameObject);
    }
}
