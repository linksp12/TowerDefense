using UnityEngine;

/// <summary>
/// 마그마 슬라임: 죽으면 작은 슬라임 여러 마리로 갈라져 같은 경로를 이어서 걷습니다.
/// MonsterHealth가 처치 처리 직후 Split()을 호출합니다.
/// </summary>
public class MonsterSplitOnDeath : MonoBehaviour
{
    [Header("분열")]
    [SerializeField] private GameObject childPrefab;
    [SerializeField, Min(1)] private int count = 2;
    [SerializeField, Min(0f)] private float spread = 0.3f;

    private bool hasSplit;

    public void Split()
    {
        if (hasSplit)
            return;

        hasSplit = true;

        MonsterMove move = GetComponent<MonsterMove>();
        MonsterAbilityUtil.SpawnOnPath(childPrefab, move, transform.position, count, spread);
        CombatFeedback.ProjectileImpact(TowerData.AttackFeel.Magic, transform.position, false, 0f, true, false);
    }
}
