using UnityEngine;

/// <summary>
/// 다른 몬스터가 걸어 주는 보호: 받는 피해 감소(얼음 수호 슬라임) 또는 잠깐 무적(얼음 마녀 결계).
/// 필요할 때 런타임에 붙고, MonsterHealth가 피해 계산에 사용합니다.
/// </summary>
public class MonsterWard : MonoBehaviour
{
    private float reduction;
    private float reductionUntil;
    private float invulnerableUntil;

    private SpriteRenderer icon;
    private SpriteRenderer shell;
    private SpriteRenderer body;
    private RectTransform hpBar;
    private Canvas hpCanvas;
    private readonly Vector3[] hpCorners = new Vector3[4];

    public bool IsInvulnerable => Time.time < invulnerableUntil;
    private bool IsReduced => Time.time < reductionUntil;

    public static MonsterWard For(GameObject monster)
    {
        if (!monster.TryGetComponent(out MonsterWard ward))
            ward = monster.AddComponent<MonsterWard>();
        return ward;
    }

    private void Awake()
    {
        body = GetComponent<SpriteRenderer>();

        MonsterHealth health = GetComponent<MonsterHealth>();
        if (health != null && health.monsterHpSlider != null)
        {
            hpBar = health.monsterHpSlider.transform as RectTransform;
            hpCanvas = health.monsterHpSlider.GetComponentInParent<Canvas>();
        }
    }

    public void ApplyReduction(float ratio, float duration, Sprite iconSprite)
    {
        if (!IsReduced || ratio >= reduction)
            reduction = Mathf.Clamp01(ratio);
        reductionUntil = Mathf.Max(reductionUntil, Time.time + duration);

        if (icon == null && iconSprite != null)
        {
            icon = CreateChild("WardIcon", iconSprite, 3);
            // 체력바(월드 캔버스)보다 위에 그린다.
            if (hpCanvas != null)
            {
                icon.sortingLayerID = hpCanvas.sortingLayerID;
                icon.sortingOrder = hpCanvas.sortingOrder + 1;
            }
        }
    }

    public void ApplyInvulnerable(float duration, Sprite shellSprite)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);

        if (shell == null && shellSprite != null)
        {
            shell = CreateChild("WardShell", shellSprite, 2);
            // 몸을 감싸도록 껍질 크기·위치를 맞춘다. (피벗이 발밑인 보스도 몸 가운데에)
            // 스프라이트 사각형은 투명 여백까지 포함해 실제 몸보다 크므로, 몸 크기는 충돌체로 잰다.
            if (body != null && body.sprite != null)
            {
                Bounds b = body.sprite.bounds;
                Vector2 bodySize = b.size;
                if (TryGetComponent(out Collider2D col) && col.enabled)
                {
                    Vector3 lossy = transform.lossyScale;
                    bodySize = new Vector2(col.bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                        col.bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)));
                }
                Vector2 shellSize = shellSprite.bounds.size;
                float scale = Mathf.Max(bodySize.x / shellSize.x, bodySize.y / shellSize.y) * 1.15f;
                shell.transform.localScale = Vector3.one * scale;
                shell.transform.localPosition = new Vector3(b.center.x, b.center.y, 0f);
            }
        }
    }

    public int ModifyDamage(int damage)
    {
        if (IsInvulnerable)
            return 0;
        if (IsReduced)
            return Mathf.Max(1, Mathf.RoundToInt(damage * (1f - reduction)));
        return damage;
    }

    private void LateUpdate()
    {
        if (shell != null)
            shell.enabled = IsInvulnerable;

        if (icon != null)
        {
            icon.enabled = IsReduced && !IsInvulnerable;
            if (!icon.enabled)
                return;

            if (hpBar != null && hpBar.gameObject.activeInHierarchy)
            {
                // 체력바 왼쪽 끝 바로 옆, 체력바 높이 가운데에 둔다.
                hpBar.GetWorldCorners(hpCorners);
                Vector3 leftMid = (hpCorners[0] + hpCorners[1]) * 0.5f;
                float half = icon.bounds.extents.x;
                icon.transform.position = new Vector3(leftMid.x - half - 0.04f, leftMid.y, transform.position.z);
            }
            else if (body != null && body.sprite != null)
            {
                Bounds b = body.sprite.bounds;
                icon.transform.localPosition = new Vector3(b.center.x - b.extents.x * 0.6f, b.max.y + 0.12f, 0f);
            }
        }
    }

    private SpriteRenderer CreateChild(string childName, Sprite sprite, int orderOffset)
    {
        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (body != null)
        {
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + orderOffset;
        }
        return sr;
    }
}
