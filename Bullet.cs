using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Transform target;
    public float speed = 70f;

    public void Seek(Transform _target) { target = _target; }

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distanceThisFrame) {
            Destroy(gameObject); // 적에게 닿으면 화살 삭제
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
    }
}
