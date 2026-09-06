using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("웨이브 UI")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI waveAlertText;
    public WaveManager waveManager;

    [Header("은신 몬스터 TIP")]
    [SerializeField] private float stealthTipDuration = 4f;

    private bool hasShownStealthTip = false;
    private GameObject stealthTipObject;
    private CanvasGroup stealthTipCanvasGroup;

    // 웨이브 시작
    public void OnWaveStart(int waveNumber)
    {
        waveText.text = $" {waveNumber} / {waveManager.TotalWaves}";
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

    public void ShowStealthMonsterTip()
    {
        if (hasShownStealthTip)
            return;

        hasShownStealthTip = true;
        CreateStealthTip();

        if (stealthTipObject != null)
        {
            StartCoroutine(ShowStealthTipCoroutine());
        }
    }

    private void CreateStealthTip()
    {
        if (stealthTipObject != null)
            return;

        Canvas canvas = waveText != null ? waveText.canvas : FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("UIManager: 은신 몬스터 TIP을 표시할 Canvas가 없습니다.");
            return;
        }

        stealthTipObject = new GameObject(
            "StealthMonsterTip",
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup)
        );

        stealthTipObject.layer = canvas.gameObject.layer;
        stealthTipObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = stealthTipObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-24f, 0f);
        panelRect.sizeDelta = new Vector2(420f, 86f);

        Image background = stealthTipObject.GetComponent<Image>();
        background.color = new Color(0.03f, 0.04f, 0.07f, 0.82f);
        background.raycastTarget = false;

        stealthTipCanvasGroup = stealthTipObject.GetComponent<CanvasGroup>();
        stealthTipCanvasGroup.alpha = 0f;
        stealthTipCanvasGroup.interactable = false;
        stealthTipCanvasGroup.blocksRaycasts = false;

        GameObject textObject = new GameObject(
            "TipText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.layer = canvas.gameObject.layer;
        textObject.transform.SetParent(stealthTipObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 8f);
        textRect.offsetMax = new Vector2(-14f, -8f);

        TextMeshProUGUI tipText = textObject.GetComponent<TextMeshProUGUI>();
        tipText.text =
            "<b>은신 몬스터 감지!</b>\n" +
            "<size=80%><color=#FFD66B>TIP</color>  은신몬스터에게는 마법타워가 효과적입니다.</size>";
        tipText.font = waveText != null ? waveText.font : TMP_Settings.defaultFontAsset;
        tipText.fontSize = 18f;
        tipText.color = Color.white;
        tipText.alignment = TextAlignmentOptions.Center;
        tipText.raycastTarget = false;

        stealthTipObject.SetActive(false);
    }

    private IEnumerator ShowStealthTipCoroutine()
    {
        stealthTipObject.SetActive(true);
        yield return FadeStealthTip(0f, 1f, 0.2f);
        yield return new WaitForSecondsRealtime(stealthTipDuration);
        yield return FadeStealthTip(1f, 0f, 0.3f);
        stealthTipObject.SetActive(false);
    }

    private IEnumerator FadeStealthTip(float from, float to, float duration)
    {
        float elapsed = 0f;
        stealthTipCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            stealthTipCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        stealthTipCanvasGroup.alpha = to;
    }
}
