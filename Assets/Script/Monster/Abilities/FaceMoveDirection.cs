using UnityEngine;

/// <summary>
/// 옆모습 몬스터(오른쪽을 보도록 그림)가 왼쪽으로 이동할 때 그림을 좌우 반전합니다.
/// </summary>
public class FaceMoveDirection : MonoBehaviour
{
    [SerializeField] private float threshold = 0.002f;

    private SpriteRenderer sr;
    private Vector3 last;

    public bool FacingLeft => sr != null && sr.flipX;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        last = transform.position;
    }

    private void LateUpdate()
    {
        float dx = transform.position.x - last.x;
        last = transform.position;

        if (sr != null && Mathf.Abs(dx) > threshold)
            sr.flipX = dx < 0f;
    }
}
