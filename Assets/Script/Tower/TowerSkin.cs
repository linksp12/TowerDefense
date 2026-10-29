using System.Collections.Generic;
using LastOfTheTower.Progression;
using UnityEngine;

/// <summary>
/// 황금 타워 외형. 하드코어 1~4를 모두 클리어하면 열리고(별도 저장 없이 클리어 기록으로 판단),
/// 켜고 끄는 선택만 PlayerPrefs에 둔다. 능력치는 그대로이고 그림만 바뀐다.
/// </summary>
public static class TowerSkin
{
    private const string SkinSetPath = "TowerSkins/GoldenTowerSkin";
    internal const string EnabledKey = "tower_skin.golden";

    private static Dictionary<Sprite, Sprite> map;

    public static event System.Action Changed;

    /// <summary>하드코어 전장을 모두 클리어했는지(황금 외형·진엔딩·메인 화면 왕관 공통 조건).</summary>
    public static bool IsUnlocked
    {
        get
        {
            var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
            progress.Load();
            if (!progress.CanWrite)
                return false;
            foreach (string id in StageIds.HardcoreStageIds)
                if (!progress.IsCleared(id))
                    return false;
            return true;
        }
    }

    // 열린 뒤에는 기본으로 켜져 있다.
    public static bool Enabled => IsUnlocked && PlayerPrefs.GetInt(EnabledKey, 1) == 1;

    public static void SetEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    /// <summary>외형이 켜져 있으면 짝이 되는 황금 그림, 아니면 원래 그림.</summary>
    public static Sprite Resolve(Sprite original)
    {
        if (original == null || !Enabled)
            return original;
        return Map.TryGetValue(original, out Sprite skin) && skin != null ? skin : original;
    }

    private static Dictionary<Sprite, Sprite> Map
    {
        get
        {
            if (map != null)
                return map;
            map = new Dictionary<Sprite, Sprite>();
            var set = Resources.Load<TowerSkinSet>(SkinSetPath);
            if (set == null)
            {
                Debug.LogWarning($"TowerSkin: Resources/{SkinSetPath} 외형 목록이 없습니다.");
                return map;
            }
            for (int i = 0; i < Mathf.Min(set.originals.Length, set.skins.Length); i++)
                if (set.originals[i] != null)
                    map[set.originals[i]] = set.skins[i];
            return map;
        }
    }
}
