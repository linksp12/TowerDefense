using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;

    private string[] currentLines;
    private int currentIndex;

    private void Awake()
    {
        Instance = this;

        dialoguePanel.SetActive(false);

        nextButton.onClick.AddListener(NextLine);
    }

    public void StartDialogue(
        string npcName,
        Sprite npcPortrait,
        string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        dialoguePanel.SetActive(true);

        nameText.text = npcName;
        portrait.sprite = npcPortrait;

        currentLines = lines;
        currentIndex = 0;

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        dialogueText.text = currentLines[currentIndex];

        if (currentIndex == currentLines.Length - 1)
        {
            nextButtonText.text = "닫기";
        }
        else
        {
            nextButtonText.text = "다음";
        }
    }

    private void NextLine()
    {
        if (currentLines == null || currentLines.Length == 0)
            return;

        if (currentIndex >= currentLines.Length - 1)
        {
            CloseDialogue();
            return;
        }

        currentIndex++;
        ShowCurrentLine();
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);

        currentLines = null;
        currentIndex = 0;
    }

    private void Update()
    {
        if (dialoguePanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDialogue();
        }
    }
}
