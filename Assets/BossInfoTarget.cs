using UnityEngine;

public class BossInfoTarget : MonoBehaviour
{
    private MonsterHealth monsterHealth;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
    }

    private void OnMouseDown()
    {
        if (monsterHealth == null)
            return;

        if (!monsterHealth.isBoss)
            return;

        if (monsterHealth.IsDead)
            return;

        if (BossInfoUI.Instance == null)
            return;

        BossInfoUI.Instance.ShowBossInfo(monsterHealth);
    }
}
