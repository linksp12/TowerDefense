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
    private float previousBgmTime;
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

    // 게시판 BGM을 틀기 전 상태를 기억했다가 닫을 때 되돌린다.
    private void PlayBoardBgm(AudioManager audio)
    {
        if (boardBgm == null || audio.bgmSource == null)
            return;

        previousBgm = audio.bgmSource.clip;
        previousBgmWasPlaying = audio.bgmSource.isPlaying && previousBgm != boardBgm;
        previousBgmTime = audio.bgmSource.time;
        audio.PlayBGM(boardBgm);
    }

    private void RestorePreviousBgm(AudioManager audio)
    {
        if (boardBgm == null || audio.bgmSource == null || audio.bgmSource.clip != boardBgm)
            return;

        if (previousBgmWasPlaying && previousBgm != null)
        {
            audio.PlayBGM(previousBgm);
            audio.bgmSource.time = Mathf.Clamp(previousBgmTime, 0f,
                Mathf.Max(0f, previousBgm.length - 0.01f));
        }
        else
            audio.StopBGM();

        previousBgm = null;
        previousBgmWasPlaying = false;
        previousBgmTime = 0f;
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

        EnsureScroll();

        // 대상 스테이지가 아직 잠겨 있으면 의뢰도 잠근다(스테이지가 열리면 바로 수락 가능).
        var progress = new LastOfTheTower.Progression.StageProgressService(
            new LastOfTheTower.Progression.PlayerPrefsStageProgressStorage());
        progress.Load();

        // 받을 보상 → 수락 가능 → 진행 중 → 잠김 → 완료 순으로 보여 준다.
        var entries = new System.Collections.Generic.List<(QuestData data, QuestState state, bool locked, int order)>();
        int index = 0;
        foreach (QuestData data in manager.Quests)
        {
            // 스테이지 도전 과제(별)는 스테이지 선택·결과창에서 보여 주므로 게시판에는 넣지 않는다.
            if (data == null || data.kind != QuestData.QuestKind.Board)
                continue;
            QuestState state = manager.GetState(data.questId);
            bool locked = state == QuestState.None &&
                !LastOfTheTower.Progression.StageIds.IsUnlocked(progress, data.targetStageId);
            entries.Add((data, state, locked, index++));
        }
        int Rank((QuestData data, QuestState state, bool locked, int order) e) =>
            e.state == QuestState.Completed ? 0 : e.locked ? 3 : e.state == QuestState.None ? 1 :
            e.state == QuestState.InProgress ? 2 : 4;
        entries.Sort((a, b) => Rank(a) != Rank(b) ? Rank(a).CompareTo(Rank(b)) : a.order.CompareTo(b.order));

        int claim = 0, inProgress = 0, lockedCount = 0;
        foreach (var entry in entries)
        {
            GameObject slotObj = Instantiate(questSlotPrefab, questSlotParent);
            QuestSlotUI slotUI = slotObj.GetComponent<QuestSlotUI>();
            if (slotUI != null)
                slotUI.Setup(entry.data, entry.state, manager.CanSave, entry.locked);
            if (entry.state == QuestState.Completed) claim++;
            else if (entry.state == QuestState.InProgress) inProgress++;
            else if (entry.locked) lockedCount++;
        }

        if (noticeText != null && manager.CanSave)
        {
            noticeText.richText = true;
            noticeText.text = "원하는 의뢰를 골라 수락하고, 전투에서 조건을 달성하세요.    " +
                $"<color=#8FE3A8>받을 보상 {claim}</color>  ·  <color=#9FD8FF>진행 중 {inProgress}</color>  ·  " +
                $"<color=#A9A39A>잠김 {lockedCount}</color>";
        }
    }

    // 의뢰가 칸보다 많으면 마우스 휠로 넘겨 볼 수 있게, 목록 칸을 한 번만 스크롤 영역으로 감싼다.
    private bool scrollReady;

    private void EnsureScroll()
    {
        if (scrollReady || !(questSlotParent is RectTransform content))
            return;
        scrollReady = true;
        var viewportObject = new GameObject("QuestScroll", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D),
            typeof(UnityEngine.UI.ScrollRect), typeof(UnityEngine.UI.Image));
        viewportObject.layer = content.gameObject.layer;
        var viewport = (RectTransform)viewportObject.transform;
        viewport.SetParent(content.parent, false);
        viewport.SetSiblingIndex(content.GetSiblingIndex());
        viewport.anchorMin = content.anchorMin;
        viewport.anchorMax = content.anchorMax;
        viewport.pivot = content.pivot;
        viewport.anchoredPosition = content.anchoredPosition;
        viewport.sizeDelta = content.sizeDelta;
        // 휠 입력을 받으려면 그림이 있어야 해서 투명 그림을 깐다.
        viewportObject.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0f);

        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
        var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewportObject.GetComponent<UnityEngine.UI.ScrollRect>();
        scroll.content = content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        // 목록 오른쪽 도트 스크롤바(넘칠 때만 보임)
        var barObject = new GameObject("QuestScrollbar", typeof(RectTransform), typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Scrollbar));
        barObject.layer = viewport.gameObject.layer;
        var bar = (RectTransform)barObject.transform;
        bar.SetParent(viewport.parent, false);
        // 목록 영역 오른쪽 바깥에 같은 높이로(부모 기준 실제 위치로 계산)
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.pivot = new Vector2(0f, 0.5f);
        bar.sizeDelta = new Vector2(24f, viewport.rect.height);
        bar.localPosition = new Vector3(
            viewport.localPosition.x + viewport.rect.xMax + 12f,
            viewport.localPosition.y + viewport.rect.center.y, 0f);
        var track = barObject.GetComponent<UnityEngine.UI.Image>();
        track.sprite = TowerPanelArt.Sprite("scroll_track");
        track.type = UnityEngine.UI.Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 1f / TowerPanelArt.Dot;

        var slide = new GameObject("SlidingArea", typeof(RectTransform));
        slide.layer = bar.gameObject.layer;
        var slideRect = (RectTransform)slide.transform;
        slideRect.SetParent(bar, false);
        slideRect.anchorMin = Vector2.zero;
        slideRect.anchorMax = Vector2.one;
        slideRect.offsetMin = new Vector2(3f, 3f);
        slideRect.offsetMax = new Vector2(-3f, -3f);
        var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        handleObject.layer = bar.gameObject.layer;
        var handleRect = (RectTransform)handleObject.transform;
        handleRect.SetParent(slideRect, false);
        handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
        var handle = handleObject.GetComponent<UnityEngine.UI.Image>();
        handle.sprite = TowerPanelArt.Sprite("scroll_handle");
        handle.type = UnityEngine.UI.Image.Type.Sliced;
        handle.pixelsPerUnitMultiplier = 1f / TowerPanelArt.Dot;

        var scrollbar = barObject.GetComponent<UnityEngine.UI.Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        var colors = scrollbar.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f, 1f);
        colors.pressedColor = new Color(0.85f, 0.75f, 0.55f, 1f);
        scrollbar.colors = colors;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
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
