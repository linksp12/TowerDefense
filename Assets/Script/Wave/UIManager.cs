using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("웨이브 UI")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI waveAlertText;
    public WaveManager waveManager;

    // 웨이브 시작
    public void OnWaveStart(int waveNumber)
    {
        waveText.text = $"Wave {waveNumber} / {waveManager.TotalWaves}";
        StartCoroutine(ShowAlert($"Wave {waveNumber} 시작!"));
    }

    // 웨이브 클리어
    public void OnWaveCleared(int waveNumber)
    {
        StartCoroutine(ShowAlert($"Wave {waveNumber} 클리어!"));
    }

    // 전체 클리어
    public void OnAllWavesCleared()
    {
        waveText.text = "게임 클리어!";
        StartCoroutine(ShowAlert("모든 웨이브 클리어!"));
    }

    IEnumerator ShowAlert(string message)
    {
        waveAlertText.text = message;
        waveAlertText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        waveAlertText.gameObject.SetActive(false);
    }
}
