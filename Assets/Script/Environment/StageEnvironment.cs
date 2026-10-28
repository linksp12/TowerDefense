using UnityEngine;

// 스테이지(씬 또는 스테이지 오브젝트)에 붙여서 이 스테이지의 환경을 지정한다.
// 진입 시 환경을 적용하고, 종료(파괴) 시 해제한다.
public class StageEnvironment : MonoBehaviour
{
    [SerializeField] private EnvironmentType environmentType = EnvironmentType.Default;

    public EnvironmentType Type => environmentType;

    private void Start()
    {
        if (EnvironmentManager.Instance != null)
            EnvironmentManager.Instance.Enter(environmentType);
    }

    private void OnDestroy()
    {
        if (EnvironmentManager.Instance != null)
            EnvironmentManager.Instance.Exit();
    }
}
