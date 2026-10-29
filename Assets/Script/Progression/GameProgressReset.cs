using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastOfTheTower.Progression
{
    // 게임 진행만 초기화한다. 화면·음량 설정과 SO 정의는 보존한다.
    public static class GameProgressReset
    {
        private static readonly string[] ProgressStringKeys =
            { QuestManager.SaveKey, PlayerPrefsStageProgressStorage.DefaultKey, GameUIManager.SavedStageKey, PlayRecord.Key };

        // 새 게임이 지울 현재 진행 기록(슬롯·옵션 제외)이 하나라도 있는지 확인한다.
        public static bool HasProgress()
        {
            foreach (string key in ProgressStringKeys)
                if (PlayerPrefs.HasKey(key)) return true;
            foreach (string key in ProgressIntKeys())
                if (PlayerPrefs.HasKey(key)) return true;
            return false;
        }

        public static bool TryReset(out string error, bool includeSaveSlots = true)
        {
            error = null;
            var strings = new Dictionary<string, string>();
            var integers = new Dictionary<string, int>();
            try
            {
                foreach (string key in ProgressStringKeys)
                    CaptureString(strings, key);
                if (includeSaveSlots)
                    for (int slot = 0; slot < GameSaveSlotStore.SlotCount; slot++)
                        CaptureString(strings, GameSaveSlotStore.SlotKey(slot));
                foreach (string key in ProgressIntKeys())
                    CaptureInt(integers, key);
                foreach (string key in strings.Keys) PlayerPrefs.DeleteKey(key);
                foreach (string key in integers.Keys) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                // 저장 요청 실패 시 초기화 전 값을 다시 복구한다.
                try
                {
                    foreach (var entry in strings) PlayerPrefs.SetString(entry.Key, entry.Value);
                    foreach (var entry in integers) PlayerPrefs.SetInt(entry.Key, entry.Value);
                    PlayerPrefs.Save();
                }
                catch (Exception rollbackException)
                {
                    Debug.LogException(rollbackException);
                }
                error = "데이터 초기화에 실패했습니다. 다시 시도해 주세요.";
                Debug.LogException(exception);
                return false;
            }

            // 씬을 넘겨도 살아 있는 퀘스트 상태와 세션 보너스도 갱신한다.
            if (QuestManager.Instance != null) QuestManager.Instance.ReloadProgress();
            MainMenuManager.ResetEasterEggBonus();
            ResultSceneManager.isVictory = false;
            ResultSceneManager.restartSceneName = "Stage1Scene";
            return true;
        }

        private static IEnumerable<string> ProgressIntKeys()
        {
            yield return "SavedWave";
            yield return ResearchProgressStore.SkillPointKey;
            foreach (ResearchData data in Resources.LoadAll<ResearchData>(string.Empty))
            {
                if (!string.IsNullOrWhiteSpace(data.ResearchId))
                    yield return ResearchProgressStore.ResearchLevelKeyPrefix + data.ResearchId;
            }
        }

        private static void CaptureString(Dictionary<string, string> values, string key)
        {
            if (PlayerPrefs.HasKey(key)) values[key] = PlayerPrefs.GetString(key);
        }

        private static void CaptureInt(Dictionary<string, int> values, string key)
        {
            if (PlayerPrefs.HasKey(key)) values[key] = PlayerPrefs.GetInt(key);
        }
    }
}
