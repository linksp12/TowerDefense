using LastOfTheTower.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지도에서 본편 표지판과 하드코어 표지판을 바꿔 보여 준다. 진행 기록은 조회만 한다.
public class StageSelectionModeToggle : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI toggleLabel;
    [SerializeField] private GameObject[] mainMarkers;
    [SerializeField] private GameObject[] hardcoreMarkers;
    [SerializeField] private GameObject hardcoreOverlay;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI hint;
    [SerializeField] private StageSelectionNavigation navigation;
    [SerializeField] private string mainTitle = "스테이지 선택";
    [SerializeField] private string mainHint = "도전할 스테이지를 선택하세요";
    [SerializeField] private string hardcoreTitle = "하드코어 도전";
    [SerializeField] private string hardcoreHint = "본편을 정복한 용사를 위한 고난도 전장입니다";
    [SerializeField] private string hardcoreLockedHint = "8스테이지를 클리어하면 하드코어 전장이 열립니다";

    // 하드코어 전투 후 선택 화면으로 돌아오면 같은 지도를 보여 준다. 저장하지 않는 화면 상태다.
    private static bool showHardcore;

    private void OnEnable()
    {
        if (toggleButton == null || toggleLabel == null || title == null || hint == null)
        {
            Debug.LogError("StageSelectionModeToggle: 화면 전환 UI 연결이 누락됐습니다.", this);
            return;
        }

        toggleButton.onClick.AddListener(Toggle);
        Apply();
    }

    private void OnDisable()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(Toggle);
    }

    private void Toggle()
    {
        if (navigation != null && navigation.IsTransitioning)
            return;

        showHardcore = !showHardcore;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        Apply();
    }

    private void Apply()
    {
        foreach (GameObject marker in mainMarkers)
            if (marker != null)
                marker.SetActive(!showHardcore);
        foreach (GameObject marker in hardcoreMarkers)
            if (marker != null)
                marker.SetActive(showHardcore);
        if (hardcoreOverlay != null)
            hardcoreOverlay.SetActive(showHardcore);

        var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
        progress.Load();
        bool unlocked = StageIds.IsUnlocked(progress, StageIds.HardcoreOne);

        title.text = showHardcore ? hardcoreTitle : mainTitle;
        hint.text = !showHardcore ? mainHint : unlocked ? hardcoreHint : hardcoreLockedHint;
        toggleLabel.text = showHardcore ? "본편 지도" : unlocked ? "하드코어 도전" : "하드코어 (잠김)";
    }
}
