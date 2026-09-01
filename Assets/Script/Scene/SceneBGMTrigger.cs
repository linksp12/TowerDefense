using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬에 배치하면 해당 씬의 BGM을 자동으로 재생
/// GameScene, ResultScene 등 별도 매니저가 BGM을 안 켜는 씬에 사용
/// </summary>
public class SceneBGMTrigger : MonoBehaviour
{
    [Tooltip("비어있으면 씬 이름으로 자동 판별")]
    public AudioClip overrideBGM;

    private void Start()
    {
        if (AudioManager.Instance == null) return;

        if (overrideBGM != null)
        {
            AudioManager.Instance.PlayBGM(overrideBGM);
        }
        else
        {
            string sceneName = SceneManager.GetActiveScene().name;
            AudioManager.Instance.PlayBGMForScene(sceneName);
        }
    }
}
