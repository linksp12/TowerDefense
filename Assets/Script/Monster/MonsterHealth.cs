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

    private int currentHp;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    void Awake()
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
    }

    void Start()
    {
        currentHp = maxHp;

        if (monsterHpSlider != null)
        {
            monsterHpSlider.maxValue = maxHp;
            monsterHpSlider.value = currentHp;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;

        Debug.Log("몬스터 피격! 남은 HP: " + currentHp);

        PlayHitFeedback();

        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void PlayHitFeedback()
    {
        PlayHitSound();
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
            return;

        GameObject effect = Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(effect, hitEffectDestroyTime);
    }

    private void PlayHitFlash()
    {
        if (!useHitFlash)
            return;

        if (spriteRenderer == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = hitColor;

        yield return new WaitForSeconds(flashTime);

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("몬스터 사망");

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 0.8f);
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

        Destroy(gameObject);
    }
}