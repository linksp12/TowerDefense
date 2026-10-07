using LastOfTheTower.Progression;
using TMPro;
using UnityEngine;

// 선택 UI는 기록을 조회한다. 승패 판정이나 클리어 저장은 하지 않는다.
public class StageSelectionProgressUI : MonoBehaviour
{
    [SerializeField] private string stageId = StageIds.StageOne;
    [SerializeField] private StageSelectionMarker marker;
    [SerializeField] private GameObject clearBadge;
    [SerializeField] private TextMeshProUGUI detailStatus;
    [SerializeField] private UnityEngine.UI.Image detailStatusSurface;
    [SerializeField] private UnityEngine.UI.Image detailStatusIcon;

    private void OnEnable()
    {
        RefreshProgress();
    }

    public void RefreshProgress()
    {
        var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
        progress.Load();
        DisplayProgress(progress);
    }

    // 명시적으로 전달한 기록을 표시해 테스트나 복귀 연결에서 재사용한다.
    public void DisplayProgress(StageProgressService progress)
    {
        if (marker == null || clearBadge == null || detailStatus == null || detailStatusSurface == null)
        {
            Debug.LogError("StageSelectionProgressUI: 진행 표시 UI 연결이 누락됐습니다.", this);
            return;
        }

        bool known = progress != null && progress.CanWrite && StageIds.IsValid(stageId);
        bool cleared = known && progress.IsCleared(stageId);
        marker.SetProgressState(known, cleared);
        clearBadge.SetActive(cleared);
        detailStatus.text = !known ? "기록 확인 불가" : cleared ? "클리어 완료" : "미클리어";
        detailStatus.color = !known
            ? new Color32(255, 209, 133, 255)
            : cleared ? new Color32(172, 233, 193, 255) : new Color32(193, 203, 217, 255);
        if (detailStatusIcon != null)
            detailStatusIcon.color = detailStatus.color;
        detailStatusSurface.color = !known
            ? new Color32(78, 53, 31, 255)
            : cleared ? new Color32(29, 65, 53, 255) : new Color32(35, 46, 61, 255);
    }
}
