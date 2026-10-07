using System;
using UnityEditor;
using UnityEngine;

namespace LastOfTheTower.Progression.Editor
{
    // 수동 실행 전용. 실제 게임의 초기화/Play 진입에서는 실행하지 않는다.
    public static class BattleResultContractChecks
    {
        [MenuItem("Tools/Last of the Tower/Progression/Validate Battle Results")]
        private static void RunFromMenu()
        {
            Debug.Log(Validate());
        }

        public static string Validate()
        {
            int delivered = 0;
            var victory = new BattleSession(StageIds.StageOne);
            if (!victory.TryComplete(true, 12, out BattleResult first))
                throw new InvalidOperationException("Victory result was rejected.");
            delivered++;
            Require(first.StageId == StageIds.StageOne && first.IsVictory &&
                first.RemainingBaseHealth == 12 && first.BattleId == victory.BattleId,
                "Victory result fields do not match the battle.");
            Require(victory.IsCompleted && ReferenceEquals(first, victory.Result),
                "Completed result was not retained.");

            for (int i = 0; i < 100; i++)
            {
                if (victory.TryComplete(i % 2 == 0, i, out BattleResult duplicate))
                    delivered++;
                Require(duplicate == null, "Duplicate returned a deliverable result.");
            }
            Require(delivered == 1 && ReferenceEquals(first, victory.Result) && first.IsVictory &&
                first.RemainingBaseHealth == 12, "Repeated end calls changed/delivered the first result.");

            var defeat = new BattleSession(StageIds.StageOne);
            Require(defeat.TryComplete(false, 0, out BattleResult lost) && !lost.IsVictory &&
                lost.RemainingBaseHealth == 0, "Zero-health defeat failed.");
            Require(defeat.BattleId != victory.BattleId, "New battle reused the previous battle ID.");

            var retry = new BattleSession(StageIds.StageOne);
            Require(!retry.TryComplete(true, -1, out BattleResult invalid) && invalid == null &&
                !retry.IsCompleted && retry.Result == null, "Invalid health consumed the session.");
            Require(retry.TryComplete(true, 50, out BattleResult retried) &&
                retried.RemainingBaseHealth == 50 && retry.BattleId != victory.BattleId &&
                retry.BattleId != defeat.BattleId, "New attempt or health above default maximum failed.");

            string[] invalidIds = { null, "", " ", "Stage1Scene", "stage_one ", "stage_1", "_stage", "stage_", "stage__one", "스테이지" };
            foreach (string id in invalidIds)
            {
                bool rejected = false;
                try { new BattleSession(id); }
                catch (ArgumentException) { rejected = true; }
                Require(rejected, "Malformed stage ID was accepted: " + id);
            }

            Require(new BattleSession("independent_stage").StageId == "independent_stage",
                "Valid independent test stage ID was rejected.");
            return "PASS battle result contract: victory/defeat fields, 100 duplicate endings delivered once, first result retained, unique retry IDs, invalid HP then valid retry, no default HP clamp, malformed IDs rejected. No scene/save/reward changes.";
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
