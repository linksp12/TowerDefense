using UnityEngine;

/// <summary>
/// 몬스터의 기본 밸런스 수치를 보관하는 데이터 에셋입니다.
/// 외형, 애니메이션, 특수 능력은 몬스터 프리팹과 전용 컴포넌트에서 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "TowerDefense/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("카탈로그와 코드에서 사용하는 고유 ID (예: goblin)")]
    public string id;
    public string monsterName;

    [Header("기본 능력치")]
    [Min(1)] public int maxHp;
    [Min(0f)] public float moveSpeed;

    [Header("처치 보상")]
    [Min(0)] public int goldReward;

    [Header("특수 능력치")]
    [Min(0)] public int maxShield;

    [Header("기지 피해량")]
    [Min(0)] public int baseDamage;

    [Header("보스 정보")]
    public bool isBoss;
    public int defense;
    public int magicResistance;
    public Sprite bossPortrait;
    public string bossName;

    [Header("도감 정보")]
    [Tooltip("도감 등 화면에 표시할 한글 이름")]
    public string displayName;
    [TextArea(2, 4)] public string description;
    [Tooltip("도감 초상화에 쓸 스프라이트 (보통 이동 애니메이션 첫 프레임)")]
    public Sprite icon;
}
