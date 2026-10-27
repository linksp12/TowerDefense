using System;
using UnityEngine;

namespace LastOfTheTower.Progression
{
    public sealed class PlayerPrefsStageProgressStorage : IStageProgressStorage
    {
        public const string DefaultKey = "last_of_the_tower_stage_progress";
        private readonly string key;

        public PlayerPrefsStageProgressStorage(string key = DefaultKey)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A save key is required.", nameof(key));
            this.key = key;
        }

        public bool TryRead(out string json, out string error)
        {
            json = null;
            error = null;
            try
            {
                if (PlayerPrefs.HasKey(key))
                    json = PlayerPrefs.GetString(key);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TryWrite(string json, string expectedJson, out string error)
        {
            if (!TryRead(out string previousJson, out error))
                return false;
            if (!string.Equals(previousJson, expectedJson, StringComparison.Ordinal))
            {
                error = "Stage progress changed since it was loaded. Reload before saving.";
                return false;
            }

            try
            {
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
                // Save는 반환값/영속화 완료 통지가 없으므로 저장 완료 대신 요청 수락으로 다룬다.
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                try
                {
                    if (previousJson == null)
                        PlayerPrefs.DeleteKey(key);
                    else
                        PlayerPrefs.SetString(key, previousJson);
                    PlayerPrefs.Save();
                }
                catch (Exception rollbackException)
                {
                    error += " Rollback failed: " + rollbackException.Message;
                }
                return false;
            }
        }
    }
}
