using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재명중은 새 화상 스택을 추가한다. 각 스택은 정확한 횟수만 피해를 준다.
/// 표시는 공용 화상 표시(BurnVisual)로, 스택이 쌓일수록 불꽃이 하나씩 늘어난다(최대 3개).
/// </summary>
public sealed class TowerBurnStack : MonoBehaviour
{
    private sealed class Stack
    {
        public int Damage, Remaining;
        public float Next, Interval;
    }

    private readonly List<Stack> stacks = new List<Stack>();
    private MonsterHealth health;
    public int ActiveStackCount => stacks.Count;

    public static void Apply(MonsterHealth target, int damage, int ticks, float interval)
    {
        if (target == null || target.IsDead || damage <= 0 || ticks <= 0) return;
        var burn = target.GetComponent<TowerBurnStack>();
        if (burn == null) burn = target.gameObject.AddComponent<TowerBurnStack>();
        burn.health = target;
        interval = Mathf.Max(0.05f, interval);
        burn.stacks.Add(new Stack { Damage = damage, Remaining = ticks, Interval = interval,
            Next = Time.time + interval });
        burn.enabled = true;
        BurnVisual.Request(burn.gameObject, burn, burn.stacks.Count);
    }

    private void Update()
    {
        if (health == null || health.IsDead) { Clear(); return; }
        bool ticked = false;
        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            Stack stack = stacks[i];
            while (stack.Remaining > 0 && Time.time >= stack.Next)
            {
                stack.Remaining--;
                stack.Next += stack.Interval;
                if (health == null || health.IsDead) break;
                health.TakeBurnDamage(stack.Damage);
                ticked = true;
            }
            if (stack.Remaining == 0) stacks.RemoveAt(i);
        }
        if (ticked) CombatFeedback.BurnTick();
        if (health == null || health.IsDead || stacks.Count == 0) { Clear(); return; }
        BurnVisual.Request(gameObject, this, stacks.Count);
    }

    private void Clear()
    {
        stacks.Clear();
        BurnVisual.Request(gameObject, this, 0);
        enabled = false;
    }

    private void OnDisable() => Clear();
}
