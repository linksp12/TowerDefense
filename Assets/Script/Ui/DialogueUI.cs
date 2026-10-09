
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    public static bool IsOpen =>
        Instance != null &&
        Instance.dialoguePanel != null &&
        Instance.dialoguePanel.activeSelf;

    public event Action<PlazaNPC.NPCType> ActionRequested;

    [Header("기본 UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("일반 대화 버튼")]
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;

    [Header("마지막 대화 버튼")]
    [SerializeField] private GameObject lastButtons;
    [SerializeField] private Button stopButton;
    [SerializeField] private TMP_Text stopButtonText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    private string[] currentLines;
    private int currentIndex;
    private PlazaNPC.NPCType currentNpcType;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("DialogueUI가 중복으로 존재합니다.", this);
            enabled = false;
            return;
        }

        Instance = this;

        if (nextButton != null)
            nextButton.onClick.AddListener(NextLine);

        if (stopButton != null)
            stopButton.onClick.AddListener(CloseDialogue);

        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);

        ResetDialogueState();
    }

    private void OnEnable()
    {
        if (Instance == this)
            ResetDialogueState();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextLine);

        if (stopButton != null)
            stopButton.onClick.RemoveListener(CloseDialogue);

        if (actionButton != null)
            actionButton.onClick.RemoveListener(OnActionButtonClicked);

        if (Instance == this)
            Instance = null;
    }

    public void StartDialogue(
        PlazaNPC.NPCType npcType,
        string npcName,
        Sprite npcPortrait,
        string[] lines)
    {
        // 이미 대화 중이면 새 대화를 시작하지 않는다.
        if (IsOpen)
            return;

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning($"{npcName}: 대사가 없습니다.");
            return;
        }

        currentNpcType = npcType;
        currentLines = lines;
        currentIndex = 0;

        // 클릭으로 확대된 NPC가 있다면 원래 크기로 복구
        PlazaNPC.ResetAllHoverScales();

        if (nameText != null)
            nameText.text = npcName;

        if (portrait != null)
            portrait.sprite = npcPortrait;

        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null ||
            currentIndex < 0 ||
            currentIndex >= currentLines.Length)
            return;

        if (dialogueText != null)
            dialogueText.text = currentLines[currentIndex];

        bool isLastLine = currentIndex == currentLines.Length - 1;

        if (nextButton != null)
            nextButton.gameObject.SetActive(!isLastLine);

        if (lastButtons != null)
            lastButtons.SetActive(isLastLine);

        if (nextButtonText != null)
            nextButtonText.text = "다음";

        if (stopButtonText != null)
            stopButtonText.text = "대화그만하기";

        if (actionButtonText != null)
            actionButtonText.text = GetActionButtonText();
    }

    private void NextLine()
    {
        if (!IsOpen || currentLines == null)
            return;

        if (currentIndex >= currentLines.Length - 1)
            return;

        currentIndex++;
        ShowCurrentLine();
    }

    private string GetActionButtonText()
    {
        switch (currentNpcType)
        {
            case PlazaNPC.NPCType.Skill:
                return "업그레이드";

            case PlazaNPC.NPCType.Quest:
                return "퀘스트 받기";

            case PlazaNPC.NPCType.Stage:
                return "스테이지 진입하기";

            default:
                return "";
        }
    }

    private void OnActionButtonClicked()
    {
        if (!IsOpen ||
            currentLines == null ||
            currentIndex != currentLines.Length - 1)
            return;

        PlazaNPC.NPCType requestedType = currentNpcType;

        // 기능 실행 전에 대화창을 닫아 다음 UI와 겹치지 않게 한다.
        CloseDialogue();

        // 업그레이드, 퀘스트, 스테이지 기능은 외부 연결 코드가 처리한다.
        ActionRequested?.Invoke(requestedType);
    }

    public void CloseDialogue()
    {
        ResetDialogueState();
    }

    private void ResetDialogueState()
    {
        currentLines = null;
        currentIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (lastButtons != null)
            lastButtons.SetActive(false);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = "";

        if (dialogueText != null)
            dialogueText.text = "";

        if (portrait != null)
            portrait.sprite = null;

        PlazaNPC.ResetAllHoverScales();
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseDialogue();
    }
}
