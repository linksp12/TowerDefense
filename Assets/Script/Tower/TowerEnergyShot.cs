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
        TowerVfx.Renderer(go, TowerVfx.Settings != null ? TowerVfx.Settings.energyBolt : null, 0.75f);
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
        var visuals = TowerVfx.Settings;
        // B1도 큰 에너지 폭발을 보여 주지만, 광역 피해는 B2의 설정이 있을 때만 적용한다.
        TowerVfx.Burst(visuals != null ? visuals.energyImpact : null, destination,
            profile.splashRadius > 0 ? 2.6f : 1.8f, 0.36f);
        if (visuals != null) TowerVfx.Play(visuals.energyImpactSound, destination);
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
