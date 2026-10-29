using UnityEngine;

/// <summary>
/// 몬스터 상태 표시 공용 부품. 감전으로 멈춘 동안 머리 위 번개, 마법화살로 느려진 동안 발밑 서리 고리를 보여 준다.
/// 상태를 거는 쪽이 Attach로 붙이고, 상태가 모두 풀리면 스스로 꺼진다.
/// </summary>
public sealed class MonsterStatusMarkers : MonoBehaviour
{
    private MonsterMove move;
    private PixelLoop stun, frost;

    public static void Attach(GameObject monster)
    {
        if (monster == null || !monster.TryGetComponent(out MonsterMove move)) return;
        if (!monster.TryGetComponent(out MonsterStatusMarkers markers))
        {
            markers = monster.AddComponent<MonsterStatusMarkers>();
            markers.move = move;
        }
        markers.enabled = true;
        markers.LateUpdate();
    }

    private void LateUpdate()
    {
        bool stunned = move != null && move.IsShockStunned;
        bool slowed = move != null && move.IsSlowed;
        CombatFeelSettings fx = CombatFeedback.Settings;
        Bounds body = TowerBodyBounds.Of(gameObject);
        // 머리 위(몸 위쪽 가장자리 바로 위). 충돌체 기준이라 피벗이 발밑인 보스도 맞는다.
        Show(ref stun, stunned, fx != null ? fx.shockStunFx : null, "Shock Stun",
            new Vector3(body.center.x, body.max.y + 0.12f, transform.position.z), 107);
        // 발밑(몸 아래쪽 가장자리)
        Show(ref frost, slowed, fx != null ? fx.slowFrostFx : null, "Slow Frost",
            new Vector3(body.center.x, body.min.y + 0.04f, transform.position.z), 55);
        if (!stunned && !slowed) enabled = false;
    }

    private void Show(ref PixelLoop marker, bool active, CombatFeelSettings.Effect effect, string name,
        Vector3 position, int sortingOrder)
    {
        if (!active)
        {
            if (marker != null) marker.gameObject.SetActive(false);
            return;
        }
        if (marker == null)
        {
            marker = PixelLoop.Create(name, transform, effect, sortingOrder);
            if (marker == null) return;
            marker.SetWorldScale(1f);
        }
        marker.gameObject.SetActive(true);
        marker.transform.position = position;
    }

    private void OnDisable()
    {
        if (stun != null) stun.gameObject.SetActive(false);
        if (frost != null) frost.gameObject.SetActive(false);
    }
}
