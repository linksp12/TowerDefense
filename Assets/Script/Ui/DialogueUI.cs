using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    public event System.Action<PlazaNPC.NPCType> ActionRequested;

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
        Instance = this;

        // 게임 시작 시 대화창 닫기
        dialoguePanel.SetActive(false);

        // 마지막 버튼도 숨기기
        lastButtons.SetActive(false);

        // 버튼 이벤트 연결
        nextButton.onClick.AddListener(NextLine);
        stopButton.onClick.AddListener(CloseDialogue);
        actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    /// <summary>
    /// NPC 대화를 시작한다.
    /// </summary>
    public void StartDialogue(
        PlazaNPC.NPCType npcType,
        string npcName,
        Sprite npcPortrait,
        string[] lines)
    {
        // 대사가 없으면 실행하지 않음
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("대사가 없습니다.");
            return;
        }

        currentNpcType = npcType;
        currentLines = lines;
        currentIndex = 0;

        // UI 표시
        dialoguePanel.SetActive(true);

        // NPC 정보 표시
        nameText.text = npcName;
        portrait.sprite = npcPortrait;

        // 첫 대사 표시
        ShowCurrentLine();
    }

    /// <summary>
    /// 현재 대사를 화면에 표시한다.
    /// </summary>
    private void ShowCurrentLine()
    {
        dialogueText.text = currentLines[currentIndex];

        bool isLastLine =
            currentIndex == currentLines.Length - 1;

        // 마지막 대사가 아니면 "다음"
        if (!isLastLine)
        {
            nextButton.gameObject.SetActive(true);
            lastButtons.SetActive(false);

            nextButtonText.text = "다음";
        }
        // 마지막 대사면 두 개의 버튼 표시
        else
        {
            nextButton.gameObject.SetActive(false);
            lastButtons.SetActive(true);

            stopButtonText.text = "대화 그만하기";
            actionButtonText.text = GetActionButtonText();
        }
    }

    /// <summary>
    /// 다음 대사로 이동한다.
    /// </summary>
    private void NextLine()
    {
        if (currentLines == null || currentLines.Length == 0)
            return;

        // 마지막이면 더 이상 다음으로 가지 않음
        if (currentIndex >= currentLines.Length - 1)
            return;

        currentIndex++;

        ShowCurrentLine();
    }

    /// <summary>
    /// NPC 종류에 따라 마지막 기능 버튼 이름을 반환한다.
    /// </summary>
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

    /// <summary>
    /// 마지막 대화의 기능 버튼을 눌렀을 때 실행된다.
    /// NPC 기능 연결을 요청한다. 씬 이동은 외부 연결 컴포넌트가 담당한다.
    /// </summary>
    private void OnActionButtonClicked()
    {
        if (!dialoguePanel.activeSelf || currentLines == null ||
            currentIndex != currentLines.Length - 1)
            return;

        ActionRequested?.Invoke(currentNpcType);
    }

    /// <summary>
    /// 대화를 닫는다.
    /// </summary>
    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        lastButtons.SetActive(false);
        nextButton.gameObject.SetActive(true);

        currentLines = null;
        currentIndex = 0;
    }

    private void Update()
    {
        // ESC로 대화 종료
        if (dialoguePanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDialogue();
        }
    }
}
