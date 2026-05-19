using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultSceneManager : MonoBehaviour
{
    public static bool isVictory = false;

    [Header("Result Panels")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    void Start()
    {
        Time.timeScale = 1f;
        ShowResult(isVictory);
    }

    public void ShowResult(bool victory)
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(victory);

        if (defeatPanel != null)
            defeatPanel.SetActive(!victory);
    }

    public void OnClickRestart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnClickMainMenu()
    {
        SceneManager.LoadScene("MainScene");
    }
}
