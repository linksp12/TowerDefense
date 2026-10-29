using System.Collections.Generic;
using UnityEngine;

/// <summary>방향을 바꾸지 않는 직선 탄환. 한 프레임의 이동 구간을 검사하여 빠른 탄환의 누락을 막는다.</summary>
public sealed class TowerPiercingShot : MonoBehaviour
{
    private struct Hit { public MonsterHealth Enemy; public float Along; }
    private readonly HashSet<MonsterHealth> hit = new HashSet<MonsterHealth>();
    private readonly List<Hit> candidates = new List<Hit>();
    private Vector2 direction;
    private TowerAttackProfile profile;
    private int damage, hitCount;
    private float remainingDistance;
    private bool detectStealth, fragment, critical;
    private TowerAttack owner;   // 쏜 타워(타워별 피해량 기록용)

    public static TowerPiercingShot Fire(Vector3 position, Vector2 direction, int damage,
        TowerAttackProfile profile, float travelDistance, bool detect, bool isFragment = false,
        MonsterHealth ignore = null, bool critical = false)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        var go = new GameObject(isFragment ? "Sniper Fragment" : "Sniper Piercing Round");
        go.transform.SetPositionAndRotation(position,
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        var shot = go.AddComponent<TowerPiercingShot>();
        shot.direction = direction.normalized;
        shot.damage = Mathf.Max(0, damage);
        shot.profile = profile;
        shot.remainingDistance = Mathf.Max(0.1f, travelDistance);
        shot.detectStealth = detect;
        shot.fragment = isFragment;
        shot.critical = critical && !isFragment;
        shot.owner = TowerDamageSource.Current;
        if (ignore != null) shot.hit.Add(ignore);
        var visuals = TowerVfx.Settings;
        TowerVfx.Renderer(go, visuals == null ? null : isFragment ? visuals.fragment : visuals.piercingRound);
        // 금빛 탄도선. 치명타 탄은 더 굵고 밝다.
        TowerVfx.Trail(go, isFragment ? new Color(1f, 0.62f, 0.2f, 0.7f) :
                critical ? new Color(1f, 0.93f, 0.55f, 1f) : new Color(1f, 0.7f, 0.22f, 0.9f),
            isFragment ? 0.06f : critical ? 0.16f : 0.1f, isFragment ? 0.05f : 0.09f);
        return shot;
    }

    private void Update()
    {
        float distance = Mathf.Min(remainingDistance, Mathf.Max(1, profile.projectileSpeed) * Time.deltaTime);
        Vector2 start = transform.position;
        Vector2 end = start + direction * distance;
        candidates.Clear();
        foreach (var enemy in TowerTargeting.All)
        {
            if (!TowerTargeting.CanHit(enemy, detectStealth) || hit.Contains(enemy)) continue;
            Vector2 delta = (Vector2)enemy.transform.position - start;
            float along = Mathf.Clamp(Vector2.Dot(delta, direction), 0, distance);
            var collider = enemy.GetComponent<Collider2D>();
            float radius = collider != null ? Mathf.Clamp(Mathf.Min(collider.bounds.extents.x,
                collider.bounds.extents.y), 0.12f, 0.6f) : 0.2f;
            if ((delta - direction * along).sqrMagnitude <= radius * radius)
                candidates.Add(new Hit { Enemy = enemy, Along = along });
        }
        candidates.Sort((a, b) => a.Along.CompareTo(b.Along));
        foreach (var candidate in candidates)
        {
            if (!TowerTargeting.CanHit(candidate.Enemy, detectStealth)) continue;
            Vector3 point = start + direction * candidate.Along;
            using (TowerDamageSource.From(owner))
                Impact(candidate.Enemy, point);
            if (fragment || profile.fragments > 0 || hitCount >= Mathf.Max(1, profile.pierceCount))
            {
                Destroy(gameObject);
                return;
            }
        }
        transform.position = end;
        remainingDistance -= distance;
        if (remainingDistance <= 0) Destroy(gameObject);
    }

    private void Impact(MonsterHealth enemy, Vector3 point)
    {
        hit.Add(enemy);
        int amount = Mathf.RoundToInt(damage * (fragment ? 1 : profile.PierceMultiplier(hitCount)));
        amount = enemy.Resist(amount, DamageKind.Physical);   // 저격은 물리 피해
        bool criticalHit = critical && hitCount == 0;
        hitCount++;
        if (detectStealth) enemy.GetComponent<StealthMonster>()?.Reveal();
        if (criticalHit) enemy.TakeCriticalDamage(amount);
        else enemy.TakeDamage(amount, false);
        int explosion = fragment ? 0 : Mathf.RoundToInt(amount * profile.extraHitDamage);
        bool burns = explosion > 0 && profile.burnDamage > 0;
        CombatFeedback.SniperHit(point, fragment, explosion > 0, burns, criticalHit);
        if (fragment) return;
        if (explosion > 0)
        {
            // 추가 타격은 광역 판정이 아니다. 직접 타격과 별개의 실드 1회를 소모한다.
            enemy.TakeDamage(explosion, false);
            TowerBurnStack.Apply(enemy, Mathf.RoundToInt(explosion * profile.burnDamage),
                profile.burnTicks, profile.burnInterval);
        }
        if (profile.fragments <= 0) return;
        for (int i = 0; i < profile.fragments; i++)
        {
            float angle = profile.fragments == 1 ? 0 : -profile.fragmentAngle / 2 +
                profile.fragmentAngle * i / (profile.fragments - 1);
            Vector2 ray = Quaternion.Euler(0, 0, angle) * direction;
            Fire(point, ray, Mathf.RoundToInt(damage * profile.fragmentDamage), profile,
                4f, detectStealth, true, enemy);
        }
    }
}
