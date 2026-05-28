using System.Collections;
using UnityEngine;

public class MonsterStatus : MonoBehaviour
{
    private Coroutine dotCoroutine;

    public void ApplyDot(int damagePerTick, float duration, float interval)
    {
        if (dotCoroutine != null)
        {
            StopCoroutine(dotCoroutine);
        }

        dotCoroutine = StartCoroutine(DotRoutine(damagePerTick, duration, interval));
    }

    IEnumerator DotRoutine(int damagePerTick, float duration, float interval)
    {
        MonsterHealth monsterHealth = GetComponent<MonsterHealth>();

        if (monsterHealth == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            monsterHealth.TakeDamage(damagePerTick);
            Debug.Log("화염 지속 피해: " + damagePerTick);

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        dotCoroutine = null;
    }
}
