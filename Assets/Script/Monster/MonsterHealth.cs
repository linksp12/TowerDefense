using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHealth : MonoBehaviour
{
    public int maxHp = 100;

    [Header("Reward")]
    public int goldReward = 20;

    [Header("Hit Effect")]
    public float hitFlashTime = 0.1f;

    private int currentHp;
    private bool isDead = false;

    public Slider monsterHpSlider;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine hitFlashCoroutine;

    void Start()
    {
        currentHp = maxHp;

        if (monsterHpSlider != null)
        {
            monsterHpSlider.maxValue = maxHp;
            monsterHpSlider.value = currentHp;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;

        FlashHit();

        // 로그가 너무 많이 찍히면 Unity가 느려질 수 있어서 필요 없으면 주석 처리해도 됨
        Debug.Log("몬스터 피격! 남은 HP: " + currentHp);

        if (monsterHpSlider != null)
        {
            monsterHpSlider.value = currentHp;
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void FlashHit()
    {
        if (spriteRenderer == null) return;

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(hitFlashTime);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        hitFlashCoroutine = null;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("몬스터 사망");

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