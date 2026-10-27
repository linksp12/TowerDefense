using System;
using UnityEngine;

/// <summary>
/// 전투 손맛 연출(효과음·이펙트·화면 흔들림·골드 연출) 설정입니다.
/// Resources/Combat/CombatFeelSettings 에 하나만 두고, 소리·그림·수치는 여기서 바꿉니다.
/// </summary>
[CreateAssetMenu(fileName = "CombatFeelSettings", menuName = "TowerDefense/Combat Feel Settings")]
public sealed class CombatFeelSettings : ScriptableObject
{
    [Serializable]
    public class Sound
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.6f;
        [Tooltip("같은 소리를 다시 재생하기까지 최소 간격(초). 많은 타워가 동시에 쏴도 소리가 뭉개지지 않게 한다.")]
        [Min(0f)] public float minInterval = 0.05f;
        [Tooltip("재생할 때마다 높낮이를 이 비율만큼 무작위로 바꾼다.")]
        [Range(0f, 0.3f)] public float pitchJitter = 0.06f;
    }

    [Serializable]
    public class Effect
    {
        public Sprite[] frames;
        [Min(1f)] public float fps = 20f;
        [Min(0.05f)] public float scale = 1f;
    }

    [Header("발사음")]
    public Sound arrowShot = new Sound();
    public Sound cannonShot = new Sound();
    public Sound magicShot = new Sound();

    [Header("타격음")]
    public Sound arrowHit = new Sound();
    public Sound cannonExplode = new Sound();
    public Sound magicHit = new Sound();
    public Sound fireHit = new Sound();
    public Sound frostHit = new Sound();
    public Sound critical = new Sound();

    [Header("처치·보상·보스")]
    public Sound killPop = new Sound();
    public Sound coin = new Sound();
    public Sound bossRoar = new Sound();

    [Header("이펙트")]
    public Effect arrowHitFx = new Effect();
    public Effect magicHitFx = new Effect();
    public Effect fireHitFx = new Effect();
    public Effect frostHitFx = new Effect();
    [Tooltip("캐논 폭발. 그림 지름이 폭발 반경에 맞춰 늘어난다.")]
    public Effect explosionFx = new Effect();
    public Effect muzzleSmokeFx = new Effect();
    public Effect deathPuffFx = new Effect();

    [Header("화면 흔들림 (설정의 흔들림 강도를 곱함)")]
    [Range(0f, 1f)] public float explosionShake = 0.18f;
    [Range(0f, 1f)] public float baseHitShake = 0.35f;
    [Range(0f, 1f)] public float bossAppearShake = 0.55f;
    [Range(0f, 1f)] public float bossDeathShake = 0.7f;
    [Tooltip("흔들림이 가장 셀 때 카메라가 움직이는 거리(월드 단위)")]
    [Min(0f)] public float maxShakeOffset = 0.35f;

    [Header("방어기지")]
    public Sound baseHit = new Sound();
    public Sound baseCrack = new Sound();
    public Effect baseShockwaveFx = new Effect();
    public Effect baseDebrisFx = new Effect();
    [Tooltip("기지 뒤에서 천천히 도는 보호막 고리 (흰색 그림에 상태별 색을 입힌다)")]
    public Sprite baseAura;
    public Color auraHealthy = new Color(0.45f, 0.78f, 1f, 0.55f);
    public Color auraDamaged = new Color(1f, 0.8f, 0.35f, 0.6f);
    public Color auraCritical = new Color(1f, 0.3f, 0.25f, 0.75f);
    [Tooltip("금이 갈 때(상태가 나빠질 때) 추가 흔들림")]
    [Range(0f, 1f)] public float baseCrackShake = 0.55f;

    [Header("새 타워(레이저·저격·감전)와 화상")]
    [Tooltip("지속 레이저 한 번 타격. 오래 맞힐수록 높게 재생된다.")]
    public Sound laserTick = new Sound();
    public Sound laserChargeShot = new Sound();
    public Sound energyBlast = new Sound();
    public Sound sniperShot = new Sound();
    public Sound sniperPierce = new Sound();
    public Sound shockZap = new Sound();
    public Sound burnTick = new Sound();
    public Effect laserSparkFx = new Effect();
    public Effect sniperMuzzleFx = new Effect();
    public Effect sniperHitFx = new Effect();
    public Effect shockHitFx = new Effect();
    [Tooltip("충전 레이저 폭발. 그림 지름이 폭발 반경에 맞춰 늘어난다.")]
    public Effect energyBlastFx = new Effect();
    [Tooltip("충전 레이저 탄(반복)")]
    public Effect energyBoltFx = new Effect();
    [Tooltip("충전 레이저가 모으는 에너지 구슬(반복)")]
    public Effect energyChargeFx = new Effect();
    [Tooltip("감전으로 멈춘 몬스터 머리 위 표시(반복)")]
    public Effect shockStunFx = new Effect();
    [Tooltip("화상 불꽃(반복). 화상이 쌓일수록 몸 가장자리에 하나씩 늘어난다. 큰 몬스터용")]
    public Effect burnFlameFx = new Effect();
    [Tooltip("작은 몬스터용 화상 불꽃(반복)")]
    public Effect burnFlameSmallFx = new Effect();
    public Effect emberFx = new Effect();
    [Tooltip("둔화(마법화살) 동안 발밑에 도는 서리 고리(반복)")]
    public Effect slowFrostFx = new Effect();
    [Range(0f, 1f)] public float energyBlastShake = 0.22f;
    [Range(0f, 1f)] public float sniperCriticalShake = 0.12f;

    [Header("골드 연출")]
    public Sprite coinIcon;
    [Tooltip("동시에 날아가는 동전 수 제한")]
    [Min(1)] public int maxFlyingCoins = 12;

    [Header("효과음 동시 재생")]
    [Min(1)] public int maxVoices = 10;
}
