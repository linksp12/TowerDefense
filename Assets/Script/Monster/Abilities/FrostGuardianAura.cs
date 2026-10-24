using UnityEngine;

/// <summary>
/// 얼음 수호 슬라임: 주변 몬스터(자기 제외)가 받는 피해를 줄여 줍니다. 수호 슬라임을 먼저 잡아야 합니다.
/// </summary>
public class FrostGuardianAura : MonoBehaviour
{
    private const float RefreshInterval = 0.25f;

    [SerializeField, Min(0.5f)] private float radius = 2.2f;
    [SerializeField, Range(0f, 0.9f)] private float damageReduction = 0.3f;
    [SerializeField] private Sprite wardIcon;

    [Header("오라 그림")]
    [SerializeField] private SpriteRenderer auraVisual;
    [SerializeField] private float rotateSpeed = 18f;

    private MonsterHealth health;
    private float nextRefresh;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();

        // 오라 그림 지름을 실제 범위에 맞춘다.
        if (auraVisual != null && auraVisual.sprite != null)
        {
            float d = auraVisual.sprite.bounds.size.x;
            auraVisual.transform.localScale = Vector3.one * (radius * 2f / d);
        }

        // 길(Road 레이어)보다 위, 몬스터 몸 바로 아래에 그린다.
        SpriteRenderer body = GetComponent<SpriteRenderer>();
        if (auraVisual != null && body != null)
        {
            auraVisual.sortingLayerID = body.sortingLayerID;
            auraVisual.sortingOrder = body.sortingOrder - 2;
        }
    }

    private void Update()
    {
        bool alive = health == null || !health.IsDead;

        if (auraVisual != null)
        {
            auraVisual.enabled = alive;
            auraVisual.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        }

        if (!alive || Time.time < nextRefresh)
            return;

        nextRefresh = Time.time + RefreshInterval;

        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            if (col.gameObject == gameObject)
                continue;

            if (col.TryGetComponent(out MonsterHealth other) && !other.IsDead)
                MonsterWard.For(other.gameObject).ApplyReduction(damageReduction, RefreshInterval + 0.15f, wardIcon);
        }
    }
}
