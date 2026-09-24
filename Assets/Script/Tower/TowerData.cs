using UnityEngine;

/// <summary>
/// 타워의 밸런스 수치와 표시 텍스트(이름·설명)를 보관하는 공용 데이터 에셋입니다.
/// 프리팹은 외형(스프라이트·발사체)을, 이 에셋은 수치와 텍스트를 담당합니다.
/// </summary>
[CreateAssetMenu(fileName = "TowerData", menuName = "TowerDefense/Tower Data")]
public class TowerData : ScriptableObject
{
    [System.Serializable]
    public struct CombatStats
    {
        public int damage;
        public float attackCooldown;
        public float attackRange;
    }

    [System.Serializable]
    public class PathTextInfo
    {
        [Tooltip("업그레이드 루트 선택 버튼에 표시되는 이름 (예: 연사 루트)")]
        public string routeName;

        [Tooltip("이 루트로 Lv.2가 되었을 때 타워 이름")]
        public string lv2Name;

        [Tooltip("이 루트로 Lv.3(최종)이 되었을 때 타워 이름")]
        public string lv3Name;

        [Tooltip("루트 선택 화면에 표시되는 설명")]
        [TextArea]
        public string featureText;

        [Tooltip("최종 업그레이드(Lv.3) 안내에 표시되는 효과 설명")]
        [TextArea]
        public string effectText;
    }

    [Header("기본 정보")]
    [Tooltip("카탈로그와 코드에서 사용하는 고유 ID (예: arrow_tower)")]
    public string id;
    public string towerName;
    public int buildCost;
    [Range(0f, 1f)] public float sellRate = 0.7f;

    [Header("기본 능력치")]
    public CombatStats baseStats;

    [Header("업그레이드 비용")]
    public int level2Cost;
    public int level3Cost;

    [Header("A 경로 능력치")]
    public CombatStats pathALv2Stats;
    public CombatStats pathALv3Stats;

    [Header("B 경로 능력치")]
    public CombatStats pathBLv2Stats;
    public CombatStats pathBLv3Stats;

    [Header("A 경로 텍스트")]
    public PathTextInfo pathA;

    [Header("B 경로 텍스트")]
    public PathTextInfo pathB;
}
