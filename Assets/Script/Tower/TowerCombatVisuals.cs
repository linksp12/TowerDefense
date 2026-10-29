using UnityEngine;

/// <summary>저격 타워 탄 그림. 새 타워의 소리·타격 이펙트는 CombatFeelSettings(새 타워 칸)에 있다.</summary>
[CreateAssetMenu(menuName = "TowerDefense/Tower Combat Visuals")]
public sealed class TowerCombatVisuals : ScriptableObject
{
    public Sprite piercingRound, fragment;
}
