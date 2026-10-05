using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 개인 테스트씬에만 연결한다. 빌드 목록을 바꾸지 않고 Editor에서 출전 흐름을 검증한다.
public class StageSelectionTestDeployment : MonoBehaviour
{
    [SerializeField] private StageSelectionMarker marker;
    [SerializeField] private StageSelectionDetailsUI details;
    [SerializeField] private UnityEngine.UI.Button deployButton;
    [SerializeField] private TextMeshProUGUI deployLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private string battleScenePath;

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
#if UNITY_EDITOR
        deployButton.interactable = true;
        deployLabel.text = "출전";
        statusLabel.text = "준비가 되었다면 전장으로 출전하세요.";
#else
        deployButton.interactable = false;
        deployLabel.text = "출전 준비 중";
        statusLabel.text = "출전 테스트는 Unity Editor에서 진행할 수 있습니다.";
#endif
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

    public bool TryDeploy()
    {
        if (!Application.isPlaying || IsDeploying || deployButton == null ||
            !deployButton.IsInteractable() || marker == null || !marker.IsSelected ||
            details == null || !details.gameObject.activeInHierarchy || details.IsAnimating)
            return false;

#if UNITY_EDITOR
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(battleScenePath) == null)
        {
            statusLabel.text = "전장을 찾을 수 없습니다. 씬 연결을 확인해주세요.";
            return false;
        }

        IsDeploying = true;
        details.SetTransitioning(true);
        deployButton.interactable = false;
        deployLabel.text = "입장 중...";
        statusLabel.text = "전장을 준비하고 있습니다.";
        StartCoroutine(LoadBattleScene());
        return true;
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private IEnumerator LoadBattleScene()
    {
        // 한 프레임 동안 입장 표시를 그린다. 이후 버튼/ESC/배경 닫기는 잠겨 있다.
        yield return null;

        float previousTimeScale = Time.timeScale;
        bool previousAudioPause = AudioListener.pause;
        AsyncOperation operation = null;
        try
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                battleScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (operation == null)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            IsDeploying = false;
            details.SetTransitioning(false);
            deployButton.interactable = true;
            deployLabel.text = "출전";
            statusLabel.text = "전장에 입장하지 못했습니다. 다시 시도해주세요.";
            yield break;
        }

        while (!operation.isDone)
            yield return null;
    }
#endif
}
