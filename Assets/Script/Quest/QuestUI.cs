using System.Collections;
using TMPro;
using UnityEngine;

// 퀘스트 게시판. 열려 있는 동안만 활성화되어 상태 변경을 반영한다.
public class QuestUI : MonoBehaviour
{
    [SerializeField] private GameObject questListObject;
    [SerializeField] private Transform questSlotParent;
    [SerializeField] private GameObject questSlotPrefab;
    [SerializeField] private TextMeshProUGUI skillPointText;
    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private AudioClip boardBgm;

    [Header("열기·닫기 연출")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform boardPanel;
    [SerializeField] private float openDuration = 0.22f;
    [SerializeField] private float closeDuration = 0.16f;

    private QuestManager subscribedManager;
    private AudioClip previousBgm;
    private bool previousBgmWasPlaying;
    private Coroutine fade;
    private bool closing;

    // 닫히는 중에는 열린 것으로 보지 않는다. (ESC·토글 재진입 방지)
    public bool IsOpen => questListObject != null && questListObject.activeSelf && !closing;

    private void OnEnable()
    {
        subscribedManager = QuestManager.Instance;
        if (subscribedManager != null)
            subscribedManager.StatesChanged += RefreshQuestList;
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
            subscribedManager.StatesChanged -= RefreshQuestList;
        subscribedManager = null;
        fade = null;
        closing = false;
        if (boardPanel != null)
            boardPanel.localScale = Vector3.one;
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (questListObject == null || IsOpen)
            return;

        bool wasClosing = closing;
        closing = false;
        if (!questListObject.activeSelf)
        {
            questListObject.SetActive(true);
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            if (boardPanel != null)
                boardPanel.localScale = Vector3.one * 0.96f;
        }
        if (!wasClosing)
            RefreshQuestList();
        StartFade(true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
            PlayBoardBgm(AudioManager.Instance);
        }
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        closing = true;
        StartFade(false);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
            RestorePreviousBgm(AudioManager.Instance);
        }
    }

    private void StartFade(bool opening)
    {
        if (fade != null)
            StopCoroutine(fade);
        fade = StartCoroutine(Fade(opening));
    }

    // 연출 중에도 뒤쪽 클릭은 막고, 게시판 버튼은 연출이 끝난 뒤에만 누를 수 있다.
    private IEnumerator Fade(bool opening)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        float duration = Mathf.Max(0.01f, opening ? openDuration : closeDuration);
        float fromAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        Vector3 fromScale = boardPanel != null ? boardPanel.localScale : Vector3.one;
        Vector3 toScale = Vector3.one * (opening ? 1f : 0.97f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 첫 프레임은 시작 상태로 그린 뒤 진행해 열자마자 튀지 않게 한다.
            yield return null;
            elapsed += Time.unscaledDeltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, opening ? 1f : 0f, eased);
            if (boardPanel != null)
                boardPanel.localScale = Vector3.Lerp(fromScale, toScale, eased);
        }

        fade = null;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = opening ? 1f : 0f;
            canvasGroup.interactable = opening;
        }

        if (!opening)
        {
            ClearQuestList();
            questListObject.SetActive(false);
        }
    }

    // 게시판 BGM을 틀기 전 상태를 기억했다가 닫을 때 되돌린다. (광장은 원래 무음)
    private void PlayBoardBgm(AudioManager audio)
    {
        if (boardBgm == null || audio.bgmSource == null)
            return;

        previousBgm = audio.bgmSource.clip;
        previousBgmWasPlaying = audio.bgmSource.isPlaying && previousBgm != boardBgm;
        audio.PlayBGM(boardBgm);
    }

    private void RestorePreviousBgm(AudioManager audio)
    {
        if (boardBgm == null || audio.bgmSource == null || audio.bgmSource.clip != boardBgm)
            return;

        if (previousBgmWasPlaying && previousBgm != null)
            audio.PlayBGM(previousBgm);
        else
            audio.StopBGM();

        previousBgm = null;
        previousBgmWasPlaying = false;
    }

    // 두루마리 아이콘·닫기 버튼에 연결된 기존 함수
    public void ToggleQuestList()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void RefreshQuestList()
    {
        ClearQuestList();

        if (skillPointText != null)
            skillPointText.text = ResearchProgressStore.SkillPoints.ToString();

        QuestManager manager = QuestManager.Instance;
        if (noticeText != null)
        {
            noticeText.text = manager == null ? "퀘스트 정보를 불러올 수 없습니다." :
                !manager.CanSave ? "퀘스트 기록을 읽지 못해 수락과 보상이 잠겨 있습니다." :
                "원하는 의뢰를 골라 수락하고, 전투에서 조건을 달성하세요.";
        }

        if (manager == null || manager.Quests == null || questSlotPrefab == null || questSlotParent == null)
            return;

        foreach (QuestData data in manager.Quests)
        {
            if (data == null)
                continue;

            GameObject slotObj = Instantiate(questSlotPrefab, questSlotParent);
            QuestSlotUI slotUI = slotObj.GetComponent<QuestSlotUI>();
            if (slotUI != null)
                slotUI.Setup(data, manager.GetState(data.questId), manager.CanSave);
        }
    }

    private void ClearQuestList()
    {
        if (questSlotParent == null)
            return;

        // Destroy는 프레임 끝에 실행되므로 먼저 숨겨 새 카드와 함께 배치되지 않게 한다.
        foreach (Transform child in questSlotParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}
