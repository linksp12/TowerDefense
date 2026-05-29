using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // START 버튼에 연결
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("StoryScene"); // 스토리씬으로 먼저 이동
    }

    // 이어하기 버튼에 연결
    public void OnContinueButtonClicked()
    {
        Debug.Log("이어하기 버튼 클릭됨");
        // 나중에 저장된 게임 불러오기 기능으로 교체
    }

    // 설정 버튼에 연결
    public void OnSettingsButtonClicked()
    {
        Debug.Log("설정 버튼 클릭됨");
    }

    // 종료 버튼에 연결
    public void OnQuitButtonClicked()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}