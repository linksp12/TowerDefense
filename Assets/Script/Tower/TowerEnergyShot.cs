using UnityEngine;

/// <summary>충전 후 발사하는 에너지포. 큰 폭발 연출과 실제 주변 피해 판정을 분리한다.</summary>
public sealed class TowerEnergyShot : MonoBehaviour
{
    private MonsterHealth target;
    private Vector3 destination;
    private int damage;
    private TowerAttackProfile profile;
    private bool detect;
    private float endTime;

    public static void Fire(Vector3 origin, MonsterHealth target, int damage,
        TowerAttackProfile profile, bool detectStealth)
    {
        var go = new GameObject("Charged Energy Cannon Shot");
        go.transform.position = origin;
        var shot = go.AddComponent<TowerEnergyShot>();
        shot.target = target;
        shot.destination = target.transform.position;
        shot.damage = damage;
        shot.profile = profile;
        shot.detect = detectStealth;
        shot.endTime = Time.time + 5;
        CombatFeelSettings fx = CombatFeedback.Settings;
        if (fx != null) PixelLoop.Create("Bolt", go.transform, fx.energyBoltFx, 105);
        TowerVfx.Trail(go, new Color(1f, 0.3f, 0.25f, 0.7f), 0.3f, 0.12f);
    }

    private void Update()
    {
        if (target != null && !target.IsDead) destination = target.transform.position;
        Vector3 delta = destination - transform.position;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        transform.position = Vector3.MoveTowards(transform.position, destination,
            Mathf.Max(1, profile.projectileSpeed) * Time.deltaTime);
        if ((transform.position - destination).sqrMagnitude <= 0.01f) Impact();
        else if (Time.time >= endTime) Destroy(gameObject);
    }

    private void Impact()
    {
        // B1도 에너지 폭발을 보여 주지만, 광역 피해는 B2의 설정이 있을 때만 적용한다(폭발 그림도 그 반경까지 커진다).
        CombatFeedback.EnergyBlast(destination, profile.splashRadius > 0 ? profile.splashRadius : 0.8f);
        if (TowerTargeting.CanHit(target, detect))
            target.TakeDamage(Mathf.RoundToInt(damage * (target.isBoss ? profile.bossDamage : 1)), false);
        if (profile.splashRadius > 0)
            foreach (var enemy in TowerTargeting.All)
            {
                if (enemy == target || !TowerTargeting.CanHit(enemy, detect) ||
                    ((Vector2)(enemy.transform.position - destination)).sqrMagnitude >
                    profile.splashRadius * profile.splashRadius) continue;
                enemy.TakeDamage(Mathf.RoundToInt(damage * profile.splashDamage *
                    (enemy.isBoss ? profile.bossDamage : 1)), false);
            }
        Destroy(gameObject);
    }
}
