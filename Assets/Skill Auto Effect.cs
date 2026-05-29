using UnityEngine;

public class EffectAutoDestroy : MonoBehaviour
{
    public float destroyTime = 1f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
