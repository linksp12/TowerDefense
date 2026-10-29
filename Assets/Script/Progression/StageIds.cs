using System.Collections.Generic;

namespace LastOfTheTower.Progression
{
    public static class StageIds
    {
        // 씬 이름과 별개인 진행 데이터 ID. 기존 ID는 저장 기록 호환을 위해 바꾸지 않는다.
        public const string StageOne = "stage_one";
        public const string StageTwo = "stage_two";
        public const string StageThree = "stage_three";
        public const string StageFour = "stage_four";
        public const string StageFive = "stage_five";
        public const string StageSix = "stage_six";
        public const string StageSeven = "stage_seven";
        public const string StageEight = "stage_eight";
        public const string HardcoreOne = "hardcore_one";
        public const string HardcoreTwo = "hardcore_two";
        public const string HardcoreThree = "hardcore_three";
        public const string HardcoreFour = "hardcore_four";

        // 본편 마지막 스테이지. 승리하면 엔딩(ResultScene)으로 이동한다.
        public const string FinalMainStage = StageEight;

        private readonly struct StageInfo
        {
            public readonly string Id;
            public readonly string SceneName;
            public readonly string Label;
            public readonly bool IsHardcore;

            public StageInfo(string id, string sceneName, string label, bool isHardcore)
            {
                Id = id;
                SceneName = sceneName;
                Label = label;
                IsHardcore = isHardcore;
            }
        }

        // 진행 순서 목록. 바로 앞 스테이지를 클리어하면 다음 스테이지가 열린다.
        // 하드코어 1은 본편 마지막 스테이지 클리어 후 열린다.
        private static readonly StageInfo[] Stages =
        {
            new StageInfo(StageOne, "Stage1Scene", "1스테이지", false),
            new StageInfo(StageTwo, "Stage2Scene", "2스테이지", false),
            new StageInfo(StageThree, "Stage3Scene", "3스테이지", false),
            new StageInfo(StageFour, "Stage4Scene", "4스테이지", false),
            new StageInfo(StageFive, "Stage5Scene", "5스테이지", false),
            new StageInfo(StageSix, "Stage6Scene", "6스테이지", false),
            new StageInfo(StageSeven, "Stage7Scene", "7스테이지", false),
            new StageInfo(StageEight, "Stage8Scene", "8스테이지", false),
            new StageInfo(HardcoreOne, "HardStage1Scene", "하드코어 1", true),
            new StageInfo(HardcoreTwo, "HardStage2Scene", "하드코어 2", true),
            new StageInfo(HardcoreThree, "HardStage3Scene", "하드코어 3", true),
            new StageInfo(HardcoreFour, "HardStage4Scene", "하드코어 4", true),
        };

        public static readonly IReadOnlyList<string> MainStageIds = CollectIds(false);
        public static readonly IReadOnlyList<string> HardcoreStageIds = CollectIds(true);

        private static string[] CollectIds(bool hardcore)
        {
            var ids = new List<string>();
            foreach (StageInfo stage in Stages)
                if (stage.IsHardcore == hardcore)
                    ids.Add(stage.Id);
            return ids.ToArray();
        }

        private static int IndexOf(string stageId)
        {
            for (int i = 0; i < Stages.Length; i++)
                if (Stages[i].Id == stageId)
                    return i;
            return -1;
        }

        public static string FromSceneName(string sceneName)
        {
            foreach (StageInfo stage in Stages)
                if (stage.SceneName == sceneName)
                    return stage.Id;
            return null;
        }

        public static string ToSceneName(string stageId)
        {
            int index = IndexOf(stageId);
            return index >= 0 ? Stages[index].SceneName : null;
        }

        public static string GetLabel(string stageId)
        {
            int index = IndexOf(stageId);
            return index >= 0 ? Stages[index].Label : null;
        }

        public static bool IsHardcore(string stageId)
        {
            int index = IndexOf(stageId);
            return index >= 0 && Stages[index].IsHardcore;
        }

        public static string GetPreviousStage(string stageId)
        {
            int index = IndexOf(stageId);
            return index > 0 ? Stages[index - 1].Id : null;
        }

        public static string GetNextStage(string stageId)
        {
            int index = IndexOf(stageId);
            return index >= 0 && index + 1 < Stages.Length ? Stages[index + 1].Id : null;
        }

        public static bool IsUnlocked(StageProgressService progress, string stageId)
        {
            if (stageId == StageOne)
                return true;
            string previous = GetPreviousStage(stageId);
            return previous != null && progress != null && progress.CanWrite && progress.IsCleared(previous);
        }
        internal static bool IsValid(string stageId)
        {
            if (string.IsNullOrEmpty(stageId) || stageId[0] == '_' ||
                stageId[stageId.Length - 1] == '_')
                return false;

            bool previousUnderscore = false;
            foreach (char character in stageId)
            {
                if (character == '_')
                {
                    if (previousUnderscore)
                        return false;
                    previousUnderscore = true;
                }
                else
                {
                    if (character < 'a' || character > 'z')
                        return false;
                    previousUnderscore = false;
                }
            }

            return true;
        }
    }
}
