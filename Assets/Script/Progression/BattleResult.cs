namespace LastOfTheTower.Progression
{
    // 완료된 한 전투의 결과. 소비자가 전달 후 수치를 바꿀 수 없는 런타임 자료형이다.
    public sealed class BattleResult
    {
        public string BattleId { get; }
        public string StageId { get; }
        public bool IsVictory { get; }
        public int RemainingBaseHealth { get; }

        internal BattleResult(string battleId, string stageId, bool isVictory, int remainingBaseHealth)
        {
            BattleId = battleId;
            StageId = stageId;
            IsVictory = isVictory;
            RemainingBaseHealth = remainingBaseHealth;
        }
    }
}
