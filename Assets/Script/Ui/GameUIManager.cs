using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public PauseManager pauseManager;

    public void PauseGame()  => pauseManager?.PauseGame();
    public void ResumeGame() => pauseManager?.ResumeGame();
    public void GoMainMenu() => pauseManager?.GoToMainMenu();
}
