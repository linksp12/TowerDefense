using UnityEngine;

/// <summary>얼음 자국 한 칸: 일정 간격으로 위의 몬스터를 가속하고, 수명이 끝나면 사라집니다.</summary>
public class IceTrailPatch : MonoBehaviour
{
    private const float CheckInterval = 0.2f;

    private const float BaseAlpha = 0.5f;

    private float life;
    private float radius;
    private float boost;
    private float age;
    private float nextCheck;
    private SpriteRenderer sr;

    public void Init(float lifeTime, float boostRadius, float boostMultiplier)
    {
        life = lifeTime;
        radius = boostRadius;
        boost = boostMultiplier;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(1f, 1f, 1f, BaseAlpha);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }

        // 마지막 1초 동안 서서히 녹는다.
        if (sr != null)
        {
            Color c = sr.color;
            c.a = BaseAlpha * Mathf.Clamp01((life - age) / 1f);
            sr.color = c;
        }

        if (age < nextCheck)
            return;

        nextCheck = age + CheckInterval;

        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            if (col.TryGetComponent(out MonsterMove move))
                move.ApplySpeedBoost(boost, CheckInterval + 0.1f);
        }
    }
}
