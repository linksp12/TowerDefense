using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 화면 매니저 (업데이트 버전)
/// - 시작, 이어하기(임시: 게임 시작), 설정, 종료
/// - ESC로 설정창 열기 (SettingsPopup에서 처리)
/// - 씬 진입 시 메인 BGM 자동 재생
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    private void Start()
    {
        // 메인 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGMForScene("MainScene");
        }
    }

    // ──────── 시작 버튼 ────────
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("StoryScene");
    }

    // ──────── 이어하기 버튼 (임시: 게임 바로 시작) ────────
    public void OnContinueButtonClicked()
    {
        Debug.Log("이어하기 (임시) - GameScene으로 이동");
        SceneManager.LoadScene("GameScene");
    }

    // ──────── 설정 버튼 ────────
    public void OnSettingsButtonClicked()
    {
        if (SettingsPopup.Instance != null)
        {
            if (SettingsPopup.Instance.IsOpen)
                SettingsPopup.Instance.Close();
            else
                SettingsPopup.Instance.Open();
        }
        else
        {
            Debug.LogWarning("SettingsPopup이 씬에 없습니다.");
        }
    }

    // ──────── 종료 버튼 ────────
    public void OnQuitButtonClicked()
    {
        Debug.Log("게임 종료");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
