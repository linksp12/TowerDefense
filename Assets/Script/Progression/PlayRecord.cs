using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastOfTheTower.Progression
{
    /// <summary>
    /// 이번 여정 전체의 플레이 기록(엔딩 카드용): 처치 수, 전투 시간 합계, 타워 종류별 설치 수.
    /// 전투 결과가 확정될 때마다 더한다. 새 여정·진행 초기화(GameProgressReset) 때 함께 지운다.
    /// 읽을 수 없는 기록은 덮어쓰지 않고 그대로 둔다.
    /// </summary>
    public static class PlayRecord
    {
        public const string Key = "last_of_the_tower_play_record";
        private const int CurrentVersion = 1;

        public sealed class Data
        {
            public int Kills;
            public double Seconds;
            public readonly Dictionary<string, int> Builds = new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>가장 많이 설치한 타워(없으면 false).</summary>
            public bool TryGetFavoriteTower(out string name, out int count)
            {
                name = null;
                count = 0;
                foreach (var pair in Builds)
                {
                    if (pair.Value <= count) continue;
                    count = pair.Value;
                    name = Names.TryGetValue(pair.Key, out string label) ? label : pair.Key;
                }
                return name != null;
            }
        }

        /// <summary>기록을 읽는다. 없으면 빈 기록으로 true, 손상됐으면 false.</summary>
        public static bool TryLoad(out Data data)
        {
            data = new Data();
            if (!PlayerPrefs.HasKey(Key))
                return true;
            try
            {
                JObject root = JObject.Parse(PlayerPrefs.GetString(Key));
                if ((int?)root["version"] != CurrentVersion)
                    return false;
                data.Kills = Math.Max(0, (int)root["kills"]);
                data.Seconds = Math.Max(0, (double)root["seconds"]);
                if (root["builds"] is JObject builds)
                    foreach (var pair in builds)
                        data.Builds[pair.Key] = Math.Max(0, (int)pair.Value);
                if (root["names"] is JObject names)
                    foreach (var pair in names)
                        data.Names[pair.Key] = (string)pair.Value;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PlayRecord: 플레이 기록을 읽지 못했습니다. ({exception.Message})");
                data = new Data();
                return false;
            }
        }

        /// <summary>한 전투의 기록을 더해 저장한다. 같은 전투는 GameManager가 한 번만 넘긴다.</summary>
        public static void AddBattle(BattleRecord battle)
        {
            if (battle == null || !TryLoad(out Data data))
                return;
            data.Kills += battle.Kills;
            data.Seconds += Math.Max(0f, battle.Seconds);
            foreach (var pair in battle.Builds)
            {
                data.Builds.TryGetValue(pair.Key, out int count);
                data.Builds[pair.Key] = count + pair.Value;
            }
            foreach (var pair in battle.TowerNames)
                data.Names[pair.Key] = pair.Value;

            var builds = new JObject();
            foreach (var pair in data.Builds) builds[pair.Key] = pair.Value;
            var names = new JObject();
            foreach (var pair in data.Names) names[pair.Key] = pair.Value;
            string json = new JObject
            {
                ["version"] = CurrentVersion,
                ["kills"] = data.Kills,
                ["seconds"] = Math.Round(data.Seconds, 1),
                ["builds"] = builds,
                ["names"] = names,
            }.ToString(Formatting.None);
            try
            {
                PlayerPrefs.SetString(Key, json);
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PlayRecord: 플레이 기록을 저장하지 못했습니다. ({exception.Message})");
            }
        }

        public static string FormatTime(double seconds)
        {
            int total = (int)Math.Round(seconds);
            int hours = total / 3600, minutes = total % 3600 / 60;
            return hours > 0 ? $"{hours}시간 {minutes}분" : minutes > 0 ? $"{minutes}분 {total % 60}초" : $"{total}초";
        }

        /// <summary>엔딩 카드 한 줄: "처치 1,234 · 플레이 2시간 5분 · 가장 많이 쓴 타워 화살 타워 (38회)"</summary>
        public static string Summary()
        {
            if (!TryLoad(out Data data))
                return null;
            string line = $"처치한 적 {data.Kills:N0}  ·  플레이 시간 {FormatTime(data.Seconds)}";
            if (data.TryGetFavoriteTower(out string name, out int count))
                line += $"  ·  가장 많이 쓴 타워 {name} ({count}회)";
            return line;
        }
    }
}
