namespace LastOfTheTower.Progression
{
    public static class StageIds
    {
        // 씬 이름과 별개인 진행 데이터 ID. 실제 씬 매핑은 통합 단계에서 연결한다.
        public const string StageOne = "stage_one";

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
