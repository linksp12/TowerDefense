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

    // 웨이브 시작·클리어 리본 배너 (처음 표시할 때 생성)
    private RectTransform waveBanner;
    private CanvasGroup waveBannerGroup;
    private TextMeshProUGUI bannerTitle;
    private TextMeshProUGUI bannerSubtitle;
    private Coroutine bannerRoutine;
    private float bannerRestY = -170f;

    private void Awake()
    {
        CenterWaveAlert();
    }

    private void CenterWaveAlert()
    {
        if (waveAlertText == null)
            return;

        RectTransform alertRect = waveAlertText.rectTransform;
        alertRect.anchorMin = new Vector2(0.5f, 0.5f);
        alertRect.anchorMax = new Vector2(0.5f, 0.5f);
        alertRect.pivot = new Vector2(0.5f, 0.5f);
        alertRect.anchoredPosition = Vector2.zero;
        waveAlertText.alignment = TextAlignmentOptions.Center;
    }

    // 웨이브 확인
    private void Start()
    {
        if (waveText != null && waveManager != null)
        {
            waveText.text = $" 0 / {waveManager.TotalWaves}";
        }
    }

    // 웨이브 시작
    public void OnWaveStart(int waveNumber)
    {
        waveText.text = $" {waveNumber} / {waveManager.TotalWaves}";

        // 보스 웨이브는 보스 경고 연출이 대신 나오므로 일반 배너를 띄우지 않는다.
        if (!IsBossWaveWithWarning(waveNumber))
        {
            bool lastWave = waveManager != null && waveNumber >= waveManager.TotalWaves;
            ShowBanner($"WAVE {waveNumber}",
                lastWave ? "<color=#FFB25C>마지막 웨이브입니다!</color>" : "몬스터가 몰려옵니다!",
                $"Wave {waveNumber} 시작!");
        }
    }

    private bool IsBossWaveWithWarning(int waveNumber)
    {
        if (BossWarningUI.Instance == null || waveManager == null || waveManager.waves == null)
            return false;

        int index = waveNumber - 1;
        if (index < 0 || index >= waveManager.waves.Length || waveManager.waves[index] == null)
            return false;

        WaveData.SpawnInfo[] spawnInfos = waveManager.waves[index].spawnInfos;
        if (spawnInfos == null)
            return false;

        foreach (WaveData.SpawnInfo spawnInfo in spawnInfos)
        {
            if (spawnInfo == null || spawnInfo.monsterPrefab == null)
                continue;

            MonsterHealth health = spawnInfo.monsterPrefab.GetComponent<MonsterHealth>();
            if (health != null && health.monsterData != null && health.monsterData.isBoss)
                return true;
        }

        return false;
    }

    // 웨이브 클리어
    public void OnWaveCleared(int waveNumber)
    {
        ShowBanner($"WAVE {waveNumber} 클리어", "<color=#A6E39C>다음 웨이브를 준비하세요</color>",
            $"Wave {waveNumber} 클리어!");
    }

    // 전체 클리어
    public void OnAllWavesCleared()
    {
        PrepareForGameResult();
    }

    public void PrepareForGameResult()
    {
        StopAllCoroutines();

        if (waveAlertText != null)
        {
            waveAlertText.text = string.Empty;
            waveAlertText.gameObject.SetActive(false);
        }

        if (waveText != null)
        {
            waveText.text = string.Empty;
            waveText.gameObject.SetActive(false);
        }

        if (stealthTipObject != null)
            stealthTipObject.SetActive(false);

        bannerRoutine = null;
        if (waveBanner != null)
            waveBanner.gameObject.SetActive(false);
    }

    // 보스 등장 알림 (BossInfoUI에서 첫 보스 등록 시 호출)
    public void ShowBossBanner(string bossName)
    {
        // 상단 보스 체력바와 겹치지 않도록 조금 아래에 띄운다.
        ShowBanner("<color=#FFB25C>보스 출현!</color>", bossName, "보스 출현!", -240f);
    }

    // 리본 배너를 만들 수 없으면 기존 가운데 문구로 표시한다.
    private void ShowBanner(string title, string subtitle, string fallback, float restY = -170f)
    {
        if (!EnsureWaveBanner())
        {
            StartCoroutine(ShowAlert(fallback));
            return;
        }

        if (bannerRoutine != null)
            StopCoroutine(bannerRoutine);
        bannerTitle.text = title;
        bannerSubtitle.text = subtitle;
        bannerRestY = restY;
        bannerRoutine = StartCoroutine(AnimateBanner());
    }

    private bool EnsureWaveBanner()
    {
        if (waveBanner != null)
            return true;

        Canvas canvas = waveText != null ? waveText.canvas : FindAnyObjectByType<Canvas>();
        if (canvas == null || BattleUiSkin.Banner == null)
            return false;

        var bannerObject = new GameObject("WaveBanner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        bannerObject.layer = canvas.gameObject.layer;
        bannerObject.transform.SetParent(canvas.transform, false);
        waveBanner = bannerObject.GetComponent<RectTransform>();
        waveBanner.anchorMin = waveBanner.anchorMax = new Vector2(0.5f, 1f);
        waveBanner.pivot = new Vector2(0.5f, 0.5f);
        waveBanner.anchoredPosition = new Vector2(0f, -170f);
        waveBanner.sizeDelta = new Vector2(580f, 136f);
        var image = bannerObject.GetComponent<Image>();
        BattleUiSkin.Skin(image, BattleUiSkin.Banner, 1f);
        image.raycastTarget = false;
        waveBannerGroup = bannerObject.GetComponent<CanvasGroup>();
        waveBannerGroup.interactable = false;
        waveBannerGroup.blocksRaycasts = false;

        bannerTitle = CreateBannerText("Title", new Vector2(0f, 22f), 42f, new Color32(255, 236, 170, 255));
        bannerSubtitle = CreateBannerText("Subtitle", new Vector2(0f, -16f), 20f, new Color32(243, 234, 208, 255));
        bannerObject.SetActive(false);
        return true;
    }

    private TextMeshProUGUI CreateBannerText(string objectName, Vector2 position, float size, Color color)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = waveBanner.gameObject.layer;
        textObject.transform.SetParent(waveBanner, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(470f, size + 8f);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = BattleUiSkin.Font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.outlineColor = new Color32(40, 12, 8, 255);
        text.outlineWidth = 0.18f;
        return text;
    }

    // 위에서 살짝 튀며 내려와 머문 뒤 위로 사라진다. 배속과 관계없이 같은 시간 동안 보인다.
    private IEnumerator AnimateBanner()
    {
        waveBanner.gameObject.SetActive(true);
        waveBanner.SetAsLastSibling();
        const float enter = 0.28f;
        const float hold = 1.6f;
        const float exit = 0.3f;

        for (float t = 0f; t < enter; t += Time.unscaledDeltaTime)
        {
            float p = t / enter;
            float back = 1f + 2.2f * Mathf.Pow(p - 1f, 3f) + 1.2f * Mathf.Pow(p - 1f, 2f);
            waveBannerGroup.alpha = Mathf.Clamp01(p * 2f);
            waveBanner.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, back);
            waveBanner.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(bannerRestY + 30f, bannerRestY, back));
            yield return null;
        }
        waveBannerGroup.alpha = 1f;
        waveBanner.localScale = Vector3.one;
        waveBanner.anchoredPosition = new Vector2(0f, bannerRestY);

        yield return new WaitForSecondsRealtime(hold);

        for (float t = 0f; t < exit; t += Time.unscaledDeltaTime)
        {
            float p = t / exit;
            waveBannerGroup.alpha = 1f - p;
            waveBanner.anchoredPosition = new Vector2(0f, Mathf.Lerp(bannerRestY, bannerRestY + 20f, p));
            yield return null;
        }
        waveBanner.gameObject.SetActive(false);
        bannerRoutine = null;
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
        panelRect.sizeDelta = new Vector2(460f, 104f);

        Image background = stealthTipObject.GetComponent<Image>();
        background.color = new Color(0.03f, 0.04f, 0.07f, 0.82f);
        BattleUiSkin.Skin(background, BattleUiSkin.Frame, BattleUiSkin.PixelScale);
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
        textRect.offsetMin = new Vector2(26f, 18f);
        textRect.offsetMax = new Vector2(-26f, -18f);

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
