using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultSceneManager : MonoBehaviour
{
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    void Start()
    {
        Time.timeScale = 1f; 

        ShowResult(false); 
    }

    public void ShowResult(bool isVictory)
    {
        victoryPanel.SetActive(isVictory); //승리 조건 필요
        defeatPanel.SetActive(!isVictory);
    }

    public void OnClickRestart()
    {
        SceneManager.LoadScene("MonsterPlayerHpTestScene"); //변경 필요
    }

    public void OnClickMainMenu()
    {
        SceneManager.LoadScene("MainScene"); 
    }
}
