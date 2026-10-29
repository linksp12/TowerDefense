using System;
using UnityEngine;

// 현재 스테이지의 환경을 보관하고, 최종 피해량 계산을 담당한다.
public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Tooltip("Default / Ice / Fire 환경 에셋 3개를 등록")]
    [SerializeField] private EnvironmentData[] environments;

    public EnvironmentData Current { get; private set; }

    // UI가 구독해서 환경 안내를 갱신한다. 해제될 때는 Default 환경이 전달된다.
    public event Action<EnvironmentData> OnEnvironmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 시작 시 기본 환경으로 초기화
        Current = Find(EnvironmentType.Default);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 1. 스테이지 진입: 환경 ID로 규칙 조회 후 적용
    public void Enter(EnvironmentType type)
    {
        EnvironmentData data = Find(type);
        if (data == null)
        {
            Debug.LogWarning($"[Environment] {type} 환경 데이터가 등록되지 않았습니다.");
            data = Find(EnvironmentType.Default);
        }

        Current = data;
        OnEnvironmentChanged?.Invoke(Current);

        // 맵 기믹(화산 용암 분출·설원 눈보라)
        var hazards = GetComponent<EnvironmentHazards>();
        if (hazards == null)
            hazards = gameObject.AddComponent<EnvironmentHazards>();
        hazards.Begin(Current != null ? Current.type : EnvironmentType.Default);
    }

    // 3. 스테이지 종료: 환경 보정 해제 (기본 환경으로 복귀)
    public void Exit()
    {
        var hazards = GetComponent<EnvironmentHazards>();
        if (hazards != null)
            hazards.Stop();
        Current = Find(EnvironmentType.Default);
        OnEnvironmentChanged?.Invoke(Current);
    }

    // 현재 환경에서 해당 속성의 보정률
    public float GetRate(ElementType element)
    {
        return Current != null ? Current.GetRate(element) : 0f;
    }

    // 2. 전투 중 적용: 최종 피해량 = 기본 피해량 × (1 + 환경 보정률)
    public float GetFinalDamage(float baseDamage, ElementType element)
    {
        float finalDamage = baseDamage * (1f + GetRate(element));
        return Mathf.Max(0f, finalDamage);
    }

    private EnvironmentData Find(EnvironmentType type)
    {
        if (environments == null) return null;

        for (int i = 0; i < environments.Length; i++)
        {
            if (environments[i] != null && environments[i].type == type)
                return environments[i];
        }
        return null;
    }

    public static int ApplyToDamage(int baseDamage, ElementType element)
    {
        if (Instance == null) return baseDamage;
        return Mathf.RoundToInt(Instance.GetFinalDamage(baseDamage, element));
    }
}
