using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastOfTheTower.Progression
{
    public enum SaveSlotStatus { Empty, Saved, Unreadable }

    [Serializable]
    public sealed class ResearchSaveEntry
    {
        public string id;
        public int level;
    }

    [Serializable]
    public sealed class GameSaveSlotData
    {
        public int version = 1;
        public string savedUtc;
        public string stageProgressJson;
        public string questProgressJson;
        public int skillPoints;
        public List<ResearchSaveEntry> research = new List<ResearchSaveEntry>();
    }

    // 자동 기록은 현재 플레이 상태, 슬롯은 광장에서 수동 저장한 시점의 상태다.
    public static class GameSaveSlotStore
    {
        public const int SlotCount = 10;

        internal static string SlotKey(int slot)
        {
            if (slot < 0 || slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            return "last_of_the_tower_save_slot_" + (slot + 1).ToString("00", CultureInfo.InvariantCulture);
        }

        public static SaveSlotStatus Read(int slot, out GameSaveSlotData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                string key = SlotKey(slot);
                if (!PlayerPrefs.HasKey(key)) return SaveSlotStatus.Empty;
                using var reader = new JsonTextReader(new System.IO.StringReader(PlayerPrefs.GetString(key)))
                { DateParseHandling = DateParseHandling.None };
                JObject json = JObject.Load(reader, new JsonLoadSettings
                { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new FormatException("저장 문서 뒤에 잘못된 내용이 있습니다.");
                if (json["version"]?.Type != JTokenType.Integer || (int)json["version"] != 1 ||
                    json["savedUtc"]?.Type != JTokenType.String ||
                    !IsOptionalString(json["stageProgressJson"]) || !IsOptionalString(json["questProgressJson"]) ||
                    json["skillPoints"]?.Type != JTokenType.Integer || json["research"] is not JArray entries)
                    throw new FormatException("지원하지 않거나 손상된 저장 형식입니다.");
                foreach (JToken entry in entries)
                    if (entry["id"]?.Type != JTokenType.String || entry["level"]?.Type != JTokenType.Integer)
                        throw new FormatException("연구 기록 형식이 올바르지 않습니다.");
                data = json.ToObject<GameSaveSlotData>();
                if (!Validate(data, out error)) { data = null; return SaveSlotStatus.Unreadable; }
                return SaveSlotStatus.Saved;
            }
            catch (Exception exception)
            {
                error = "저장 기록을 읽을 수 없습니다: " + exception.Message;
                data = null;
                return SaveSlotStatus.Unreadable;
            }
        }

        public static bool TrySave(int slot, bool overwrite, out string error)
        {
            error = null;
            string key;
            string previous;
            try
            {
                key = SlotKey(slot);
                previous = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
                if (previous != null && !overwrite)
                {
                    error = "기존 기록을 덮어쓸지 먼저 확인해 주세요.";
                    return false;
                }
                var data = Capture();
                if (!Validate(data, out error)) return false;
                string json = JsonConvert.SerializeObject(data);
                try
                {
                    PlayerPrefs.SetString(key, json);
                    PlayerPrefs.Save();
                    return true;
                }
                catch
                {
                    RestoreString(key, previous);
                    PlayerPrefs.Save();
                    throw;
                }
            }
            catch (Exception exception)
            {
                error = "저장하지 못했습니다: " + exception.Message;
                return false;
            }
        }

        public static bool TryLoad(int slot, out Action rollback, out string error)
        {
            rollback = null;
            if (Read(slot, out var data, out error) != SaveSlotStatus.Saved)
            {
                if (error == null) error = "비어 있는 슬롯은 불러올 수 없습니다.";
                return false;
            }
            var strings = new Dictionary<string, string>();
            var integers = new Dictionary<string, int?>();
            try
            {
                CaptureRawString(strings, PlayerPrefsStageProgressStorage.DefaultKey);
                CaptureRawString(strings, QuestManager.SaveKey);
                CaptureRawString(strings, GameUIManager.SavedStageKey);
                CaptureRawInt(integers, "SavedWave");
                CaptureRawInt(integers, ResearchProgressStore.SkillPointKey);
                foreach (ResearchData definition in Resources.LoadAll<ResearchData>(string.Empty))
                    CaptureRawInt(integers, ResearchProgressStore.ResearchLevelKeyPrefix + definition.ResearchId);
                rollback = () =>
                {
                    foreach (var pair in strings) RestoreString(pair.Key, pair.Value);
                    foreach (var pair in integers)
                        if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
                        else PlayerPrefs.DeleteKey(pair.Key);
                    PlayerPrefs.Save();
                    RefreshQuestState();
                };
                RestoreString(PlayerPrefsStageProgressStorage.DefaultKey, data.stageProgressJson);
                RestoreString(QuestManager.SaveKey, data.questProgressJson);
                PlayerPrefs.DeleteKey(GameUIManager.SavedStageKey);
                PlayerPrefs.DeleteKey("SavedWave");
                foreach (string key in integers.Keys) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.SetInt(ResearchProgressStore.SkillPointKey, data.skillPoints);
                foreach (ResearchSaveEntry entry in data.research)
                    if (entry.level > 0) PlayerPrefs.SetInt(ResearchProgressStore.ResearchLevelKeyPrefix + entry.id, entry.level);
                PlayerPrefs.Save();
                RefreshQuestState();
                return true;
            }
            catch (Exception exception)
            {
                try { rollback?.Invoke(); }
                catch (Exception restoreException) { Debug.LogException(restoreException); }
                rollback = null;
                error = "불러오지 못했습니다: " + exception.Message;
                return false;
            }
        }

        public static int ClearedStages(GameSaveSlotData data)
        {
            var progress = new StageProgressService(new SnapshotStorage(data.stageProgressJson));
            progress.Load();
            int count = 0;
            foreach (string id in new[] { StageIds.StageOne, StageIds.StageTwo, StageIds.StageThree, StageIds.StageFour })
                if (progress.IsCleared(id)) count++;
            return count;
        }

        public static int ClaimedQuests(GameSaveSlotData data)
        {
            if (data.questProgressJson == null) return 0;
            var quests = JsonUtility.FromJson<QuestSaveData>(data.questProgressJson);
            return quests.quests.FindAll(q => q.state == QuestState.Rewarded).Count;
        }

        private static GameSaveSlotData Capture()
        {
            var data = new GameSaveSlotData
            {
                savedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                stageProgressJson = PlayerPrefs.HasKey(PlayerPrefsStageProgressStorage.DefaultKey)
                    ? PlayerPrefs.GetString(PlayerPrefsStageProgressStorage.DefaultKey) : null,
                questProgressJson = PlayerPrefs.HasKey(QuestManager.SaveKey) ? PlayerPrefs.GetString(QuestManager.SaveKey) : null,
                skillPoints = PlayerPrefs.GetInt(ResearchProgressStore.SkillPointKey, 0)
            };
            foreach (ResearchData definition in Resources.LoadAll<ResearchData>(string.Empty))
                data.research.Add(new ResearchSaveEntry { id = definition.ResearchId,
                    level = PlayerPrefs.GetInt(ResearchProgressStore.ResearchLevelKeyPrefix + definition.ResearchId, 0) });
            return data;
        }

        private static bool Validate(GameSaveSlotData data, out string error)
        {
            error = null;
            if (data == null || data.version != 1 || data.skillPoints < 0 || data.research == null ||
                !DateTime.TryParseExact(data.savedUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            { error = "저장 정보가 올바르지 않습니다."; return false; }
            var progress = new StageProgressService(new SnapshotStorage(data.stageProgressJson));
            progress.Load();
            if (!progress.CanWrite) { error = "스테이지 기록을 읽을 수 없습니다."; return false; }
            if (!QuestManager.TryReadSave(data.questProgressJson, out _, out error)) return false;
            var definitions = new Dictionary<string, ResearchData>(StringComparer.Ordinal);
            foreach (ResearchData definition in Resources.LoadAll<ResearchData>(string.Empty))
                definitions.Add(definition.ResearchId, definition);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ResearchSaveEntry entry in data.research)
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !ids.Add(entry.id) ||
                    !definitions.TryGetValue(entry.id, out var definition) || entry.level < 0 || entry.level > definition.MaxLevel)
                { error = "지원하지 않거나 손상된 업그레이드 기록입니다."; return false; }
            return true;
        }

        private static bool IsOptionalString(JToken value) => value != null &&
            (value.Type == JTokenType.Null || value.Type == JTokenType.String);

        private static void CaptureRawString(Dictionary<string, string> values, string key) =>
            values[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        private static void CaptureRawInt(Dictionary<string, int?> values, string key) =>
            values[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;

        private static void RestoreString(string key, string value)
        {
            if (value == null) PlayerPrefs.DeleteKey(key);
            else PlayerPrefs.SetString(key, value);
        }

        private static void RefreshQuestState()
        {
            if (QuestManager.Instance != null) QuestManager.Instance.ReloadProgress();
        }

        private sealed class SnapshotStorage : IStageProgressStorage
        {
            private readonly string json;
            public SnapshotStorage(string json) { this.json = json; }
            public bool TryRead(out string value, out string error) { value = json; error = null; return true; }
            public bool TryWrite(string value, string expected, out string error) { error = "Read only"; return false; }
        }
    }
}
