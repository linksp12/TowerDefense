using UnityEngine;

public class BuildManager : MonoBehaviour
{
    // 'instance'를 정의하여 다른 스크립트(Node 등)에서 접근할 수 있게 합니다.
    public static BuildManager instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("화면에 BuildManager가 두 개 이상 있습니다!");
            return;
        }
        instance = this;
    }

    public GameObject towerPrefab; // 설치할 타워 프리팹
}
