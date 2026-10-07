using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectionNavigation : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button returnButton;
    [SerializeField] private TMPro.TextMeshProUGUI returnLabel;
    [SerializeField] private StageSelectionDetailsUI[] details;
    [SerializeField] private string plazaScenePath = "Assets/Scenes/PlazaScene.unity";

    public bool IsTransitioning { get; private set; }

    private void OnEnable()
    {
        if (returnButton != null)
            returnButton.onClick.AddListener(ReturnToPlaza);
    }

    private void OnDisable()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(ReturnToPlaza);
    }

    public bool TryBeginTransition()
    {
        if (!Application.isPlaying || IsTransitioning || SceneLoadingScreen.IsLoading)
            return false;
        IsTransitioning = true;
        if (returnButton != null)
            returnButton.interactable = false;
        return true;
    }

    public void CancelTransition()
    {
        IsTransitioning = false;
        if (returnButton != null)
            returnButton.interactable = true;
    }

    public void ReturnToPlaza()
    {
        if (!Application.isPlaying || IsTransitioning)
            return;
        if (string.IsNullOrWhiteSpace(plazaScenePath) ||
            !Application.CanStreamedLevelBeLoaded(plazaScenePath))
        {
            Debug.LogError($"광장 씬 연결을 확인하세요: {plazaScenePath}", this);
            return;
        }
        if (!TryBeginTransition())
            return;
        foreach (var detail in details)
            if (detail != null)
                detail.SetTransitioning(true);

        if (returnLabel != null)
            returnLabel.text = "이동 중...";
        if (!SceneLoadingScreen.TryLoad(plazaScenePath, "광장으로 이동 중", RestoreAfterLoadFailure))
            RestoreAfterLoadFailure();
    }

    private void RestoreAfterLoadFailure()
    {
        if (this == null)
            return;
        CancelTransition();
        foreach (var detail in details)
            if (detail != null)
                detail.SetTransitioning(false);
        if (returnLabel != null)
            returnLabel.text = "다시 시도";
    }
}
