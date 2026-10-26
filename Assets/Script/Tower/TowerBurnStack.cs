using System.Collections.Generic;
using UnityEngine;

/// <summary>재명중은 새 화상 스택을 추가한다. 각 스택은 정확한 횟수만 피해를 주며 표시 오브젝트는 하나다.</summary>
public sealed class TowerBurnStack : MonoBehaviour
{
    private sealed class Stack
    {
        public int Damage, Remaining;
        public float Next, Interval;
        public Sprite Sprite;
    }

    private readonly List<Stack> stacks = new List<Stack>();
    private MonsterHealth health;
    private SpriteRenderer marker;
    public int ActiveStackCount => stacks.Count;

    public static void Apply(MonsterHealth target, int damage, int ticks, float interval)
    {
        if (target == null || target.IsDead || damage <= 0 || ticks <= 0) return;
        var burn = target.GetComponent<TowerBurnStack>();
        if (burn == null) burn = target.gameObject.AddComponent<TowerBurnStack>();
        burn.health = target;
        interval = Mathf.Max(0.05f, interval);
        burn.stacks.Add(new Stack { Damage = damage, Remaining = ticks, Interval = interval,
            Next = Time.time + interval, Sprite = TowerVfx.Settings != null ? TowerVfx.Settings.burn : null });
        burn.enabled = true;
        burn.RefreshMarker();
    }

    private void Update()
    {
        if (health == null || health.IsDead) { Clear(); return; }
        for (int i = stacks.Count - 1; i >= 0; i--)
        {
            Stack stack = stacks[i];
            while (stack.Remaining > 0 && Time.time >= stack.Next)
            {
                stack.Remaining--;
                stack.Next += stack.Interval;
                if (health == null || health.IsDead) break;
                health.TakeDamage(stack.Damage, false);
            }
            if (stack.Remaining == 0) stacks.RemoveAt(i);
        }
        if (stacks.Count == 0) { Clear(); return; }
        RefreshMarker();
        if (marker != null) marker.color = new Color(1, 1, 1, 0.75f + 0.2f * Mathf.Sin(Time.time * 25));
    }

    private void RefreshMarker()
    {
        if (stacks.Count == 0 || stacks[stacks.Count - 1].Sprite == null) return;
        if (marker == null)
        {
            var go = new GameObject("Tower Burn Visual");
            go.transform.SetParent(transform, false);
            marker = TowerVfx.Renderer(go, stacks[stacks.Count - 1].Sprite, 0.45f);
            // Renderer width is in world units even if the monster prefab has a scaled root.
            float scale = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
            go.transform.localScale /= scale;
            go.transform.localPosition = new Vector3(0, 0.2f / scale, 0);
        }
        marker.sprite = stacks[stacks.Count - 1].Sprite;
    }

    private void Clear()
    {
        stacks.Clear();
        if (marker != null) Destroy(marker.gameObject);
        marker = null;
        enabled = false;
    }

    private void OnDisable() => Clear();
}
