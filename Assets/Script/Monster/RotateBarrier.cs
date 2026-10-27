using UnityEngine;

public class RotateBarrier : MonoBehaviour
{
    [Header("회전 속도 설정")]
    [Tooltip("초당 회전할 각도입니다. 양수면 시계 반대 방향, 음수면 시계 방향입니다.")]
    public float rotateSpeed = 100f;

    void Update()
    {
        // Z축을 기준으로 매 프레임 회전 (2D 게임 기준)
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}