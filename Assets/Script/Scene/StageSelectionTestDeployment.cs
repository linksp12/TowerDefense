using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using LastOfTheTower.Progression;

// 기존 컴포넌트/Inspector 연결을 유지하며 선택한 전장으로 런타임 출전한다.
public class StageSelectionTestDeployment : MonoBehaviour
{
    [SerializeField] private StageSelectionMarker marker;
    [SerializeField] private StageSelectionDetailsUI details;
    [SerializeField] private UnityEngine.UI.Button deployButton;
    [SerializeField] private TextMeshProUGUI deployLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private string battleScenePath;
    [SerializeField] private string stageId = StageIds.StageOne;
    [SerializeField] private StageSelectionNavigation navigation;

    private bool unlocked = true;
    private IStageProgressStorage progressStorage;

    // 기존 저장 키를 건드리지 않는 검증과 명시적 저장소 연결에 사용한다.
    public void UseStorage(IStageProgressStorage storage)
    {
        progressStorage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public bool IsDeploying { get; private set; }

    private void OnEnable()
    {
        if (deployButton == null || deployLabel == null || statusLabel == null ||
            marker == null || details == null)
        {
            if (deployButton != null)
                deployButton.interactable = false;
            Debug.LogError("StageSelectionTestDeployment: 출전 UI 연결이 누락됐습니다.", this);
            return;
        }

        deployButton.onClick.AddListener(OnDeployClicked);
        var progress = new StageProgressService(progressStorage ?? new PlayerPrefsStageProgressStorage());
        progress.Load();
        SetAvailability(StageIds.IsUnlocked(progress, stageId));
    }

    private void OnDisable()
    {
        if (deployButton != null)
            deployButton.onClick.RemoveListener(OnDeployClicked);
    }

    private void OnDeployClicked()
    {
        TryDeploy();
    }

    public void SetAvailability(bool value)
    {
        unlocked = value;
        if (deployButton == null || deployLabel == null || statusLabel == null)
            return;
        deployButton.interactable = unlocked && !IsDeploying;
        deployLabel.text = IsDeploying ? "입장 중..." : unlocked ? "출전" : "잠김";
        statusLabel.text = IsDeploying ? "전장을 준비하고 있습니다." : unlocked ?
            "준비가 되었다면 전장으로 출전하세요." : "이전 스테이지를 먼저 클리어하세요.";
    }

    public bool TryDeploy()
    {
        if (!Application.isPlaying || IsDeploying || SceneLoadingScreen.IsLoading || !unlocked || deployButton == null ||
            deployLabel == null || statusLabel == null ||
            !deployButton.IsInteractable() || marker == null || !marker.IsSelected ||
            details == null || !details.gameObject.activeInHierarchy || details.IsAnimating)
            return false;

        // 클릭 직전에도 저장 기록을 다시 확인해 표시 상태만으로 잠금을 우회하지 못하게 한다.
        var progress = new StageProgressService(progressStorage ?? new PlayerPrefsStageProgressStorage());
        progress.Load();
        if (!StageIds.IsUnlocked(progress, stageId))
        {
            SetAvailability(false);
            return false;
        }

        if (string.IsNullOrWhiteSpace(battleScenePath) ||
            !Application.CanStreamedLevelBeLoaded(battleScenePath))
        {
            statusLabel.text = "전장을 찾을 수 없습니다. 씬 연결을 확인해주세요.";
            return false;
        }

        if (navigation != null && !navigation.TryBeginTransition())
            return false;

        IsDeploying = true;
        details.SetTransitioning(true);
        deployButton.interactable = false;
        deployLabel.text = "입장 중...";
        statusLabel.text = "전장을 준비하고 있습니다.";
        if (SceneLoadingScreen.TryLoad(battleScenePath, "전장을 준비하고 있습니다", RestoreAfterLoadFailure))
            return true;
        RestoreAfterLoadFailure();
        return false;
    }

    private void RestoreAfterLoadFailure()
    {
        if (this == null)
            return;
        IsDeploying = false;
        if (navigation != null)
            navigation.CancelTransition();
        details.SetTransitioning(false);
        deployButton.interactable = unlocked;
        deployLabel.text = "출전";
        statusLabel.text = "전장에 입장하지 못했습니다. 다시 시도해주세요.";
    }
}
