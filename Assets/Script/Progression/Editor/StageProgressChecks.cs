using System;
using UnityEditor;
using UnityEngine;

namespace LastOfTheTower.Progression.Editor
{
    // 수동 실행만 가능하다. 실제 저장 키 대신 독립 저장소/임시 키를 사용한다.
    public static class StageProgressChecks
    {
        [MenuItem("Tools/Last of the Tower/Progression/Validate Stage Progress")]
        private static void RunFromMenu()
        {
            Debug.Log(Validate());
        }

        public static string Validate()
        {
            CheckRecordingAndReload();
            CheckDamagedSaves();
            CheckStorageFailuresAndConflicts();
            CheckPlayerPrefs();
            return "PASS stage progress: clear/reload, defeat unchanged, duplicate writes blocked, invalid input, corrupt/unsupported saves preserved, read/write failure and retry, stale-writer conflict, isolated PlayerPrefs round-trip and cleanup. Browser persistence/process restart not tested.";
        }

        private static BattleResult Result(string stageId, bool victory, int health)
        {
            var session = new BattleSession(stageId);
            Require(session.TryComplete(victory, health, out BattleResult result), "Could not create test result.");
            return result;
        }

        private static void CheckRecordingAndReload()
        {
            var storage = new MemoryStorage();
            var progress = new StageProgressService(storage);
            BattleResult victory = Result(StageIds.StageOne, true, 12);
            Require(progress.RecordResult(victory) == StageClearRecordStatus.LoadRequired && storage.Writes == 0,
                "Writing before load was allowed.");
            Require(progress.Load() == StageProgressLoadStatus.Missing && progress.CanWrite &&
                !progress.IsCleared(StageIds.StageOne) && storage.Writes == 0, "Missing save was not distinguished.");
            Require(progress.RecordResult(Result(StageIds.StageOne, false, 0)) == StageClearRecordStatus.NotVictory &&
                !progress.IsCleared(StageIds.StageOne) && storage.Writes == 0, "Defeat changed clear progress.");
            Require(progress.RecordResult(null) == StageClearRecordStatus.InvalidResult, "Null result was accepted.");
            Require(progress.RecordResult(victory) == StageClearRecordStatus.SaveRequested &&
                progress.IsCleared(StageIds.StageOne) && storage.Writes == 1, "Clear was not recorded.");

            for (int i = 0; i < 100; i++)
                Require(progress.RecordResult(victory) == StageClearRecordStatus.AlreadyCleared, "Duplicate clear was accepted.");
            Require(storage.Writes == 1, "Duplicate clear wrote storage again.");
            Require(progress.RecordResult(Result(StageIds.StageOne, false, 0)) == StageClearRecordStatus.NotVictory &&
                progress.IsCleared(StageIds.StageOne), "Later defeat removed a previous clear.");

            var reloaded = new StageProgressService(storage);
            Require(reloaded.Load() == StageProgressLoadStatus.Loaded && reloaded.IsCleared(StageIds.StageOne), "Reload lost clear progress.");
            Require(reloaded.RecordResult(victory) == StageClearRecordStatus.AlreadyCleared && storage.Writes == 1,
                "Duplicate after reload wrote again.");
            Require(reloaded.RecordResult(Result("independent_stage", true, 50)) == StageClearRecordStatus.SaveRequested,
                "Independent stage could not be added.");
            var both = new StageProgressService(storage);
            Require(both.Load() == StageProgressLoadStatus.Loaded && both.IsCleared(StageIds.StageOne) &&
                both.IsCleared("independent_stage"), "Saving another stage lost existing progress.");
        }

        private static void CheckDamagedSaves()
        {
            string[] corrupt =
            {
                "", " ", "not json", "null", "[]", "{", "{}",
                "{\"version\":1}", "{\"version\":1,\"clearedStageIds\":null}",
                "{\"version\":1,\"clearedStageIds\":[\"Stage1Scene\"]}",
                "{\"version\":1,\"clearedStageIds\":[null]}",
                "{\"version\":1,\"clearedStageIds\":[\"stage_one\",\"stage_one\"]}",
                "{\"version\":\"1\",\"clearedStageIds\":[]}",
                "{\"version\":1.0,\"clearedStageIds\":[]}",
                "{\"version\":1,\"clearedStageIds\":[true]}",
                "{\"version\":1,\"clearedStageIds\":\"stage_one\"}",
                "{\"version\":1,\"version\":1,\"clearedStageIds\":[]}",
                "{\"version\":1,\"clearedStageIds\":[],\"other\":123}",
                "{\"version\":1,\"clearedStageIds\":[]} {\"version\":1}"
            };
            foreach (string json in corrupt)
            {
                var storage = new MemoryStorage { Json = json };
                var progress = new StageProgressService(storage);
                Require(progress.Load() == StageProgressLoadStatus.Corrupt && !progress.CanWrite &&
                    !string.IsNullOrEmpty(progress.LastError), "Bad save accepted: " + json);
                Require(progress.RecordResult(Result(StageIds.StageOne, true, 12)) == StageClearRecordStatus.LoadRequired &&
                    storage.Json == json && storage.Writes == 0, "Bad save was overwritten.");
            }

            var futureStorage = new MemoryStorage { Json = "{\"version\":2,\"clearedStageIds\":[\"stage_one\"]}" };
            var future = new StageProgressService(futureStorage);
            string original = futureStorage.Json;
            Require(future.Load() == StageProgressLoadStatus.UnsupportedVersion && !future.CanWrite &&
                future.RecordResult(Result(StageIds.StageOne, true, 1)) == StageClearRecordStatus.LoadRequired &&
                futureStorage.Json == original && futureStorage.Writes == 0, "Unknown version was overwritten.");
        }

        private static void CheckStorageFailuresAndConflicts()
        {
            var storage = new MemoryStorage { ReadFails = true };
            var progress = new StageProgressService(storage);
            Require(progress.Load() == StageProgressLoadStatus.ReadFailed && !progress.CanWrite &&
                progress.RecordResult(Result(StageIds.StageOne, true, 2)) == StageClearRecordStatus.LoadRequired,
                "Read failure was treated as a new save.");
            storage.ReadFails = false;
            Require(progress.Load() == StageProgressLoadStatus.Missing, "Read failure could not be retried.");
            storage.WriteFails = true;
            BattleResult victory = Result(StageIds.StageOne, true, 12);
            Require(progress.RecordResult(victory) == StageClearRecordStatus.WriteFailed &&
                !progress.IsCleared(StageIds.StageOne) && storage.Json == null &&
                !string.IsNullOrEmpty(progress.LastError), "Failed write changed memory or storage.");
            storage.WriteFails = false;
            Require(progress.RecordResult(victory) == StageClearRecordStatus.SaveRequested && progress.LastError == null,
                "Failed write consumed the result and prevented retry.");
            string goodJson = storage.Json;
            storage.WriteFails = true;
            Require(progress.RecordResult(Result("independent_stage", true, 3)) == StageClearRecordStatus.WriteFailed &&
                progress.IsCleared(StageIds.StageOne) && !progress.IsCleared("independent_stage") && storage.Json == goodJson,
                "Failed write lost previous progress.");
            storage.WriteFails = false;
            storage.Json = "broken after load";
            Require(progress.RecordResult(Result("independent_stage", true, 3)) == StageClearRecordStatus.WriteFailed &&
                storage.Json == "broken after load", "External change was silently overwritten.");
            Require(progress.Load() == StageProgressLoadStatus.Corrupt && !progress.CanWrite &&
                progress.IsCleared(StageIds.StageOne), "Failed reload destroyed the last known memory snapshot.");
        }

        private static void CheckPlayerPrefs()
        {
            string testKey = PlayerPrefsStageProgressStorage.DefaultKey + "_test_" + Guid.NewGuid().ToString("N");
            bool defaultExists = PlayerPrefs.HasKey(PlayerPrefsStageProgressStorage.DefaultKey);
            string defaultValue = PlayerPrefs.GetString(PlayerPrefsStageProgressStorage.DefaultKey);
            bool legacyExists = PlayerPrefs.HasKey("SavedStageScene");
            string legacyValue = PlayerPrefs.GetString("SavedStageScene");
            Require(!PlayerPrefs.HasKey(testKey), "Unexpected test key collision.");
            try
            {
                var storage = new PlayerPrefsStageProgressStorage(testKey);
                var first = new StageProgressService(storage);
                var stale = new StageProgressService(storage);
                Require(first.Load() == StageProgressLoadStatus.Missing && stale.Load() == StageProgressLoadStatus.Missing,
                    "Isolated key was not missing.");
                Require(first.RecordResult(Result(StageIds.StageOne, true, 12)) == StageClearRecordStatus.SaveRequested,
                    "PlayerPrefs write request failed.");
                Require(stale.RecordResult(Result("independent_stage", true, 10)) == StageClearRecordStatus.WriteFailed,
                    "Stale PlayerPrefs writer overwrote a newer save.");
                var second = new StageProgressService(new PlayerPrefsStageProgressStorage(testKey));
                Require(second.Load() == StageProgressLoadStatus.Loaded && second.IsCleared(StageIds.StageOne) &&
                    !second.IsCleared("independent_stage"), "PlayerPrefs reload did not preserve the accepted snapshot.");
                PlayerPrefs.SetString(testKey, "damaged");
                PlayerPrefs.Save();
                Require(second.RecordResult(Result("independent_stage", true, 10)) == StageClearRecordStatus.WriteFailed &&
                    PlayerPrefs.GetString(testKey) == "damaged", "PlayerPrefs changed data was overwritten.");
                Require(second.Load() == StageProgressLoadStatus.Corrupt && !second.CanWrite, "PlayerPrefs corruption was not blocked.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(testKey);
                PlayerPrefs.Save();
            }
            Require(!PlayerPrefs.HasKey(testKey) && defaultExists == PlayerPrefs.HasKey(PlayerPrefsStageProgressStorage.DefaultKey) &&
                defaultValue == PlayerPrefs.GetString(PlayerPrefsStageProgressStorage.DefaultKey) &&
                legacyExists == PlayerPrefs.HasKey("SavedStageScene") && legacyValue == PlayerPrefs.GetString("SavedStageScene"),
                "Test cleanup changed a real or legacy save key.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class MemoryStorage : IStageProgressStorage
        {
            public string Json;
            public bool ReadFails;
            public bool WriteFails;
            public int Writes;

            public bool TryRead(out string json, out string error)
            {
                json = Json;
                error = ReadFails ? "Injected read failure." : null;
                return !ReadFails;
            }

            public bool TryWrite(string json, string expectedJson, out string error)
            {
                error = null;
                if (WriteFails || Json != expectedJson)
                {
                    error = "Injected write failure or stale writer.";
                    return false;
                }
                Json = json;
                Writes++;
                return true;
            }
        }
    }
}
