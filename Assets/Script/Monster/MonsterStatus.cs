using System.Collections;
using UnityEngine;

public class MonsterStatus : MonoBehaviour
{
    private Coroutine dotCoroutine;

    // 마법 타워 화염 지속 피해. 표시·숫자·소리는 다른 화상과 같은 모습(BurnVisual, 주황 숫자)을 쓴다.
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

        BurnVisual.Request(gameObject, this, 1);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            monsterHealth.TakeBurnDamage(damagePerTick);
            CombatFeedback.BurnTick();
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        BurnVisual.Request(gameObject, this, 0);
        dotCoroutine = null;
    }

    private void OnDisable()
    {
        BurnVisual.Request(gameObject, this, 0);
    }
}
