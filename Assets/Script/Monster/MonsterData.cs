using UnityEngine;

/// <summary>
/// 몬스터의 기본 밸런스 수치를 보관하는 데이터 에셋입니다.
/// 외형, 애니메이션, 특수 능력은 몬스터 프리팹과 전용 컴포넌트에서 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "TowerDefense/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string monsterName;

    [Header("기본 능력치")]
    [Min(1)] public int maxHp;
    [Min(0f)] public float moveSpeed;

    [Header("처치 보상")]
    [Min(0)] public int goldReward;
}
