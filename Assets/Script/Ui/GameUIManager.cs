using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    // 이전 단일 스테이지 저장 키는 전체 초기화·슬롯 로드 시 정리한다.
    internal const string SavedStageKey = "SavedStageScene";
    public PauseManager pauseManager;

    public void PauseGame() => pauseManager?.PauseGame();
    public void ResumeGame() => pauseManager?.ResumeGame();
    public void GoMainMenu() => pauseManager?.GoToMainMenu();
}
