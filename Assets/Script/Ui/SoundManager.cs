using UnityEngine;
using TMPro;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public GameObject moneyWarningText;
    public AudioSource uiAudioSource;
    public AudioClip errorSound;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 골드 부족 알림 토스트 (처음 표시할 때 생성, 실패하면 기존 문구 사용)
    private RectTransform warningToast;
    private CanvasGroup warningGroup;
    private TextMeshProUGUI warningLabel;
    private Coroutine warningRoutine;

    public void ShowMoneyWarning(int amount)
    {
        CancelInvoke("HideMoneyWarning");

        if (EnsureWarningToast())
        {
            if (moneyWarningText != null)
                moneyWarningText.SetActive(false);
            warningLabel.text = $"골드가 <color=#FFD66B>{amount}</color> 부족합니다!";
            if (warningRoutine != null)
                StopCoroutine(warningRoutine);
            warningRoutine = StartCoroutine(AnimateWarning());
        }
        else if (moneyWarningText != null)
        {
            moneyWarningText.SetActive(true);

            var textComponent = moneyWarningText.GetComponent<TMPro.TextMeshProUGUI>();

            if (textComponent != null)
            {
                textComponent.text = $"{amount}원이 부족합니다!";
            }
            Invoke("HideMoneyWarning", 1.5f);
        }

        if (uiAudioSource != null && errorSound != null)
        {
            AudioManager.PlayUISoundOn(uiAudioSource, errorSound);
        }
    }

    void HideMoneyWarning()
    {
        if (moneyWarningText != null) moneyWarningText.SetActive(false);
    }

    private bool EnsureWarningToast()
    {
        if (warningToast != null)
            return true;

        Canvas canvas = moneyWarningText != null ? moneyWarningText.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null || BattleUiSkin.Plate == null)
            return false;

        var toast = new GameObject("MoneyWarningToast", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
        toast.layer = canvas.gameObject.layer;
        toast.transform.SetParent(canvas.transform, false);
        warningToast = toast.GetComponent<RectTransform>();
        warningToast.anchorMin = warningToast.anchorMax = warningToast.pivot = new Vector2(0.5f, 0.5f);
        warningToast.anchoredPosition = new Vector2(0f, 40f);
        warningToast.sizeDelta = new Vector2(440f, 70f);
        var plate = toast.GetComponent<UnityEngine.UI.Image>();
        BattleUiSkin.Skin(plate, BattleUiSkin.Plate, BattleUiSkin.PixelScale);
        plate.raycastTarget = false;
        warningGroup = toast.GetComponent<CanvasGroup>();
        warningGroup.interactable = false;
        warningGroup.blocksRaycasts = false;

        var coin = new GameObject("Coin", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        coin.layer = toast.layer;
        coin.transform.SetParent(warningToast, false);
        var coinRect = coin.GetComponent<RectTransform>();
        coinRect.anchoredPosition = new Vector2(-170f, 0f);
        coinRect.sizeDelta = new Vector2(40f, 40f);
        var coinImage = coin.GetComponent<UnityEngine.UI.Image>();
        coinImage.sprite = BattleUiSkin.Coin;
        coinImage.raycastTarget = false;
        coin.SetActive(coinImage.sprite != null);

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.layer = toast.layer;
        label.transform.SetParent(warningToast, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(24f, 1f);
        labelRect.sizeDelta = new Vector2(360f, 50f);
        warningLabel = label.GetComponent<TextMeshProUGUI>();
        warningLabel.font = BattleUiSkin.Font;
        warningLabel.fontSize = 27f;
        warningLabel.color = new Color32(255, 190, 170, 255);
        warningLabel.alignment = TextAlignmentOptions.Center;
        warningLabel.textWrappingMode = TextWrappingModes.NoWrap;
        warningLabel.raycastTarget = false;
        warningLabel.outlineColor = new Color32(30, 10, 6, 255);
        warningLabel.outlineWidth = 0.18f;

        toast.SetActive(false);
        return true;
    }

    // 툭 튀어나오며 좌우로 흔들린 뒤 잠시 머물고 사라진다. 일시정지 중에도 보이도록 실제 시간을 쓴다.
    private System.Collections.IEnumerator AnimateWarning()
    {
        warningToast.gameObject.SetActive(true);
        warningToast.SetAsLastSibling();
        const float shake = 0.32f;
        const float total = 1.45f;
        const float fade = 0.25f;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            float pop = Mathf.Clamp01(t / 0.12f);
            warningToast.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, 1f - (1f - pop) * (1f - pop));
            float sway = t < shake ? Mathf.Sin(t * 60f) * 10f * (1f - t / shake) : 0f;
            warningToast.anchoredPosition = new Vector2(sway, 40f);
            warningGroup.alpha = t > total - fade ? (total - t) / fade : 1f;
            yield return null;
        }
        warningToast.gameObject.SetActive(false);
        warningRoutine = null;
    }
}
