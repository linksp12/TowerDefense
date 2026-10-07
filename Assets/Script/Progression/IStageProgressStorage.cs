namespace LastOfTheTower.Progression
{
    public interface IStageProgressStorage
    {
        // null은 저장 없음, 빈 문자열은 존재하지만 손상된 저장으로 구분한다.
        bool TryRead(out string json, out string error);

        // 읽었던 값과 달라졌으면 덮어쓰지 않는다. true는 저장 요청 수락을 의미한다.
        bool TryWrite(string json, string expectedJson, out string error);
    }
}
