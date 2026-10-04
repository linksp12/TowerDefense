using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSelectionDetailsUI : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    public class MonsterEntry
    {
        public MonsterData data;
        public string displayName;
        public TextMeshProUGUI label;
    }

    [Serializable]
    public class TowerEntry
    {
        public TowerData data;
        public Image icon;
        public TextMeshProUGUI label;
    }

    [SerializeField] private StageSelectionMarker marker;
    [SerializeField] private Button closeButton;
    [SerializeField] private MonsterEntry[] monsters;
    [SerializeField] private TowerEntry[] towers;
    [SerializeField] private WaveData[] waves;
    [SerializeField] private TextMeshProUGUI waveSummaryLabel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private float openDuration = 0.22f;
    [SerializeField] private float closeDuration = 0.16f;

    private Coroutine animation;
    private Vector2 restingPosition;
    private bool closing;
    public bool IsAnimating => animation != null;

    private void Awake()
    {
        restingPosition = panel.anchoredPosition;
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(marker.CloseDetails);
        foreach (MonsterEntry entry in monsters)
            entry.label.text = string.IsNullOrWhiteSpace(entry.displayName)
                ? entry.data.monsterName
                : entry.displayName;
        foreach (TowerEntry entry in towers)
        {
            entry.label.text = entry.data.towerName;
            entry.icon.sprite = entry.data.icon;
        }
        if (waveSummaryLabel != null)
            waveSummaryLabel.text = BuildWaveSummary();
    }

    private void Update()
    {
        if (!closing && Input.GetKeyDown(KeyCode.Escape))
            marker.CloseDetails();
    }

    // 패널 바깥의 어두운 배경을 직접 눌렀을 때만 닫는다. 패널 안쪽 클릭이 올라온 경우는 무시한다.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!closing && eventData.pointerCurrentRaycast.gameObject == gameObject)
            marker.CloseDetails();
    }

    private string BuildWaveSummary()
    {
        int monsterCount = 0;
        foreach (WaveData wave in waves)
            foreach (WaveData.SpawnInfo spawn in wave.spawnInfos)
                monsterCount += spawn.count;
        return $"웨이브 {waves.Length}  ·  몬스터 {monsterCount}마리";
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(marker.CloseDetails);
        if (animation != null)
            StopCoroutine(animation);
        animation = null;
        panel.anchoredPosition = restingPosition;
    }

    public void Show()
    {
        if (animation != null)
            StopCoroutine(animation);
        closing = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;
        panel.localScale = Vector3.one * 0.95f;
        panel.anchoredPosition = restingPosition + Vector2.down * 18f;
        animation = StartCoroutine(Animate(true));
    }

    public void Hide()
    {
        if (closing)
            return;
        closing = true;
        if (animation != null)
            StopCoroutine(animation);
        canvasGroup.interactable = false;
        animation = StartCoroutine(Animate(false));
    }

    private IEnumerator Animate(bool opening)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, opening ? openDuration : closeDuration);
        float fromAlpha = canvasGroup.alpha;
        Vector3 fromScale = panel.localScale;
        Vector2 fromPosition = panel.anchoredPosition;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, opening ? 1f : 0f, eased);
            panel.localScale = Vector3.Lerp(fromScale, Vector3.one * (opening ? 1f : 0.97f), eased);
            panel.anchoredPosition = Vector2.Lerp(fromPosition,
                restingPosition + (opening ? Vector2.zero : Vector2.down * 10f), eased);
            yield return null;
        }
        animation = null;
        canvasGroup.alpha = opening ? 1f : 0f;
        canvasGroup.interactable = opening;
        if (!opening)
            gameObject.SetActive(false);
    }
}
