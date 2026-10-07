using System;

namespace LastOfTheTower.Progression
{
    // 출전/다시하기마다 하나 생성하고, 같은 전투의 모든 종료 경로에서 공유한다.
    // Unity 메인 스레드에서 호출한다. 씬 이동이나 보상/저장 처리는 이 클래스의 책임이 아니다.
    public sealed class BattleSession
    {
        public string BattleId { get; }
        public string StageId { get; }
        public BattleResult Result { get; private set; }
        public bool IsCompleted => Result != null;

        public BattleSession(string stageId)
        {
            if (!StageIds.IsValid(stageId))
                throw new ArgumentException("Stage ID must use lowercase words separated by underscores.", nameof(stageId));

            StageId = stageId;
            BattleId = Guid.NewGuid().ToString("N");
        }

        // true일 때만 결과를 전달한다. 중복/잘못된 입력은 false와 null을 반환한다.
        public bool TryComplete(bool isVictory, int remainingBaseHealth, out BattleResult result)
        {
            result = null;
            if (IsCompleted || remainingBaseHealth < 0)
                return false;

            Result = new BattleResult(BattleId, StageId, isVictory, remainingBaseHealth);
            result = Result;
            return true;
        }
    }
}
