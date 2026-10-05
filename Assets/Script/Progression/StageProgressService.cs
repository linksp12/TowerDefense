using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastOfTheTower.Progression
{
    public enum StageProgressLoadStatus
    {
        NotLoaded,
        Missing,
        Loaded,
        Corrupt,
        UnsupportedVersion,
        ReadFailed
    }

    public enum StageClearRecordStatus
    {
        SaveRequested,
        AlreadyCleared,
        NotVictory,
        InvalidResult,
        LoadRequired,
        WriteFailed
    }

    // 스테이지 클리어 기록만 관리한다. 성장·퀘스트·전투 골드는 다루지 않는다.
    public sealed class StageProgressService
    {
        private const int CurrentVersion = 1;
        private readonly IStageProgressStorage storage;
        private HashSet<string> clearedStages = new HashSet<string>(StringComparer.Ordinal);
        private string loadedJson;

        public StageProgressLoadStatus LoadStatus { get; private set; }
        public string LastError { get; private set; }
        public bool CanWrite => LoadStatus == StageProgressLoadStatus.Missing ||
            LoadStatus == StageProgressLoadStatus.Loaded;

        public StageProgressService(IStageProgressStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public bool IsCleared(string stageId)
        {
            return stageId != null && clearedStages.Contains(stageId);
        }

        // 명시적으로 읽어야 쓰기가 가능하다. 읽지 못한 기존 데이터를 새 저장으로 덮어쓰지 않는다.
        public StageProgressLoadStatus Load()
        {
            LastError = null;
            if (!storage.TryRead(out string json, out string error))
                return RejectLoad(StageProgressLoadStatus.ReadFailed, error);
            if (json == null)
            {
                clearedStages = new HashSet<string>(StringComparer.Ordinal);
                loadedJson = null;
                return LoadStatus = StageProgressLoadStatus.Missing;
            }

            JObject data;
            long version;
            try
            {
                data = JObject.Parse(json, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (data["version"]?.Type != JTokenType.Integer)
                    return RejectLoad(StageProgressLoadStatus.Corrupt, "Missing or invalid stage progress version.");
                version = data["version"].Value<long>();
            }
            catch (Exception exception)
            {
                return RejectLoad(StageProgressLoadStatus.Corrupt, exception.Message);
            }

            if (version <= 0)
                return RejectLoad(StageProgressLoadStatus.Corrupt, "Missing stage progress version.");
            if (version != CurrentVersion)
                return RejectLoad(StageProgressLoadStatus.UnsupportedVersion, "Unsupported stage progress version.");
            if (data.Count != 2 || !(data["clearedStageIds"] is JArray ids))
                return RejectLoad(StageProgressLoadStatus.Corrupt, "Missing stage IDs or unexpected save fields.");

            var candidate = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken token in ids)
            {
                if (token.Type != JTokenType.String)
                    return RejectLoad(StageProgressLoadStatus.Corrupt, "Stage ID must be a string.");
                string stageId = token.Value<string>();
                if (!StageIds.IsValid(stageId) || !candidate.Add(stageId))
                    return RejectLoad(StageProgressLoadStatus.Corrupt, "Invalid or duplicate stage ID.");
            }

            clearedStages = candidate;
            loadedJson = json;
            return LoadStatus = StageProgressLoadStatus.Loaded;
        }

        // 같은 클리어를 재전달해도 다시 저장하지 않는다. Unity 메인 스레드에서 호출한다.
        public StageClearRecordStatus RecordResult(BattleResult result)
        {
            if (!CanWrite)
                return StageClearRecordStatus.LoadRequired;
            LastError = null;
            if (result == null || !StageIds.IsValid(result.StageId) || result.RemainingBaseHealth < 0 ||
                !Guid.TryParseExact(result.BattleId, "N", out _))
                return StageClearRecordStatus.InvalidResult;
            if (!result.IsVictory)
                return StageClearRecordStatus.NotVictory;
            if (clearedStages.Contains(result.StageId))
                return StageClearRecordStatus.AlreadyCleared;

            var candidate = new HashSet<string>(clearedStages, StringComparer.Ordinal) { result.StageId };
            var ids = new string[candidate.Count];
            candidate.CopyTo(ids);
            Array.Sort(ids, StringComparer.Ordinal);
            string json = new JObject
            {
                ["version"] = CurrentVersion,
                ["clearedStageIds"] = new JArray(ids)
            }.ToString(Formatting.None);
            if (!storage.TryWrite(json, loadedJson, out string error))
            {
                LastError = error;
                return StageClearRecordStatus.WriteFailed;
            }

            clearedStages = candidate;
            loadedJson = json;
            LoadStatus = StageProgressLoadStatus.Loaded;
            return StageClearRecordStatus.SaveRequested;
        }

        private StageProgressLoadStatus RejectLoad(StageProgressLoadStatus status, string error)
        {
            LastError = error;
            return LoadStatus = status;
        }

    }
}
