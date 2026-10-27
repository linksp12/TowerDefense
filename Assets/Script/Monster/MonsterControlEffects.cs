using System.Collections.Generic;
using UnityEngine;

/// <summary>감전 정지와 둔화의 종료 시간을 독립 관리한다. 약한 둔화가 강한 둔화를 덮어쓰지 않는다.</summary>
internal sealed class MonsterControlEffects
{
    private struct Slow { public float Multiplier, Until; }
    private readonly List<Slow> slows = new List<Slow>();
    private float stunnedUntil;

    public bool IsStunned => Time.time < stunnedUntil;

    public void Apply(float stun, float reduction, float duration)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + Mathf.Max(0, stun));
        if (reduction <= 0 || duration <= 0) return;
        float multiplier = 1 - Mathf.Clamp01(reduction);
        for (int i = 0; i < slows.Count; i++)
        {
            if (!Mathf.Approximately(slows[i].Multiplier, multiplier)) continue;
            slows[i] = new Slow { Multiplier = multiplier, Until = Mathf.Max(slows[i].Until, Time.time + duration) };
            return;
        }
        slows.Add(new Slow { Multiplier = multiplier, Until = Time.time + duration });
    }

    public float GetSpeed(float existingSpeed, float baseSpeed)
    {
        float multiplier = 1;
        for (int i = slows.Count - 1; i >= 0; i--)
        {
            if (Time.time >= slows[i].Until) { slows.RemoveAt(i); continue; }
            multiplier = Mathf.Min(multiplier, slows[i].Multiplier);
        }
        return Mathf.Min(existingSpeed, baseSpeed * multiplier);
    }

    public void Clear() { stunnedUntil = 0; slows.Clear(); }
}
