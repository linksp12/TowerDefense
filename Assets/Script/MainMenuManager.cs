using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // START 버튼에 연결
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("GameScene"); // GameScene 이름과 정확히 일치해야 함
    }

    // 설정 버튼에 연결
    public void OnSettingsButtonClicked()
    {
        Debug.Log("설정 버튼 클릭됨"); // 나중에 설정 패널 열기 등으로 교체
    }

    // 종료 버튼에 연결
    public void OnQuitButtonClicked()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}