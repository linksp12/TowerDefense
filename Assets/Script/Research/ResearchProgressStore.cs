using UnityEngine;

/// <summary>
/// 영구 성장의 저장 경계를 한곳에 둡니다.
/// 퀘스트와 스테이지 보상은 이후 이 클래스의 공개 API를 통해 포인트만 지급하면 됩니다.
/// </summary>
public static class ResearchProgressStore
{
    private const string SkillPointKey = "research.skill_points";
    private const string ResearchLevelKeyPrefix = "research.level.";

    public static int SkillPoints => Mathf.Max(0, PlayerPrefs.GetInt(SkillPointKey, 0));

    public static int GetLevel(string researchId)
    {
        if (string.IsNullOrWhiteSpace(researchId))
            return 0;

        return Mathf.Max(0, PlayerPrefs.GetInt(ResearchLevelKeyPrefix + researchId, 0));
    }

    public static bool TryPurchase(ResearchData growth, out string failureMessage)
    {
        failureMessage = string.Empty;
        if (growth == null || string.IsNullOrWhiteSpace(growth.ResearchId))
        {
            failureMessage = "성장 데이터를 찾을 수 없습니다.";
            return false;
        }

        int level = GetLevel(growth.ResearchId);
        if (level >= growth.MaxLevel)
        {
            failureMessage = "이미 최대 단계입니다.";
            return false;
        }

        int cost = growth.GetRank(level).skillPointCost;
        if (SkillPoints < cost)
        {
            failureMessage = "스킬 포인트가 부족합니다.";
            return false;
        }

        PlayerPrefs.SetInt(SkillPointKey, SkillPoints - cost);
        PlayerPrefs.SetInt(ResearchLevelKeyPrefix + growth.ResearchId, level + 1);
        PlayerPrefs.Save();
        return true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ResearchScene에서만 쓰는 독립 검증 수단입니다. 보상 시스템 연결 시에는 호출하지 않습니다.
    public static void AddTestSkillPoints(int amount)
    {
        if (amount <= 0)
            return;

        PlayerPrefs.SetInt(SkillPointKey, SkillPoints + amount);
        PlayerPrefs.Save();
    }

    public static void ResetForTesting(ResearchData[] knownGrowths)
    {
        PlayerPrefs.DeleteKey(SkillPointKey);

        if (knownGrowths != null)
        {
            foreach (ResearchData growth in knownGrowths)
            {
                if (growth != null && !string.IsNullOrWhiteSpace(growth.ResearchId))
                    PlayerPrefs.DeleteKey(ResearchLevelKeyPrefix + growth.ResearchId);
            }
        }

        PlayerPrefs.Save();
    }
#endif
}
