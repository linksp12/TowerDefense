/// <summary>
/// 지금 피해를 주고 있는 타워를 잠깐 기억해 전투 기록(타워별 피해량)에 넘긴다.
/// 타워가 공격하는 동안 From(this)로 감싸고, 화살·관통탄·화상처럼 나중에 맞는 피해는
/// 만들어질 때 Current를 받아 두었다가 맞힐 때 다시 From으로 감싼다. 스킬·맵 기믹 피해는 기록하지 않는다.
/// </summary>
public static class TowerDamageSource
{
    public static TowerAttack Current { get; private set; }

    public static Scope From(TowerAttack tower)
    {
        var previous = Current;
        Current = tower;
        return new Scope(previous);
    }

    public readonly struct Scope : System.IDisposable
    {
        private readonly TowerAttack previous;

        public Scope(TowerAttack previous) => this.previous = previous;

        public void Dispose() => Current = previous;
    }
}
