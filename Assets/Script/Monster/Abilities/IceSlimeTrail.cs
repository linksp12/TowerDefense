using UnityEngine;

/// <summary>
/// 아이스 슬라임: 지나간 자리에 잠깐 얼음 자국을 남기고, 그 위를 지나는 몬스터는 잠시 빨라집니다.
/// </summary>
public class IceSlimeTrail : MonoBehaviour
{
    [SerializeField] private Sprite patchSprite;
    [SerializeField, Min(0.1f)] private float dropDistance = 0.7f;
    [SerializeField, Min(0.5f)] private float patchLife = 3f;
    [SerializeField, Min(0.1f)] private float patchRadius = 0.45f;
    [SerializeField, Min(1f)] private float boostMultiplier = 1.3f;

    private Vector3 lastDrop;
    private MonsterHealth health;
    private SpriteRenderer body;

    private void Awake()
    {
        health = GetComponent<MonsterHealth>();
        body = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        lastDrop = transform.position;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        if ((transform.position - lastDrop).sqrMagnitude < dropDistance * dropDistance)
            return;

        lastDrop = transform.position;
        Drop();
    }

    private void Drop()
    {
        if (patchSprite == null)
            return;

        float footY = body != null && body.sprite != null ? body.sprite.bounds.extents.y * 0.55f : 0.25f;
        GameObject go = new GameObject("IceTrail");
        go.transform.position = transform.position - new Vector3(0f, footY, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = patchSprite;
        if (body != null)
        {
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder - 5;
        }

        go.AddComponent<IceTrailPatch>().Init(patchLife, patchRadius, boostMultiplier);
    }
}
