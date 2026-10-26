using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/Tower Combat Visuals")]
public sealed class TowerCombatVisuals : ScriptableObject
{
    public Sprite piercingRound, fragment, energyBolt, energyImpact, shockImpact, burn;
    public AudioClip sniperShot, electricShot, laserShot, energyImpactSound;
    [Range(0, 1)] public float soundVolume = 0.3f;
}
