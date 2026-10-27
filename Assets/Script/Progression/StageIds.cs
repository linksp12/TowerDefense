namespace LastOfTheTower.Progression
{
    public static class StageIds
    {
        // 씬 이름과 별개인 진행 데이터 ID. 실제 씬 매핑은 통합 단계에서 연결한다.
        public const string StageOne = "stage_one";
        public const string StageTwo = "stage_two";
        public const string StageThree = "stage_three";
        public const string StageFour = "stage_four";

        public static string FromSceneName(string sceneName)
        {
            switch (sceneName)
            {
                case "Stage1Scene": return StageOne;
                case "Stage2Scene": return StageTwo;
                case "Stage3Scene": return StageThree;
                case "Stage4Scene": return StageFour;
                default: return null;
            }
        }

        public static string GetPreviousStage(string stageId)
        {
            switch (stageId)
            {
                case StageTwo: return StageOne;
                case StageThree: return StageTwo;
                case StageFour: return StageThree;
                default: return null;
            }
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
