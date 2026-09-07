using UnityEngine;

public class BossInfoTarget : MonoBehaviour
{
    private MonsterHealth monsterHealth;

    private void Awake()
    {
        // BossClickArea에 붙어 있어도
        // 부모 ForestGolemMonster의 MonsterHealth를 찾는다.
        monsterHealth = GetComponentInParent<MonsterHealth>();
    }

    private void Start()
    {
        if (monsterHealth == null)
            return;

        if (!monsterHealth.isBoss)
            return;

        if (monsterHealth.IsDead)
            return;

        if (BossInfoUI.Instance == null)
            return;

        BossInfoUI.Instance.RegisterBoss(monsterHealth);
    }
}
