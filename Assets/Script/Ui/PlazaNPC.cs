using UnityEngine;
using UnityEngine.EventSystems;

public class PlazaNPC : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("NPC 정보")]
    [SerializeField] private string npcName;
    [SerializeField] private Sprite portrait;

    [Header("대사")]
    [TextArea(2, 4)]
    [SerializeField] private string[] dialogueLines;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * 1.08f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DialogueUI.Instance == null)
            return;

        DialogueUI.Instance.StartDialogue(
            npcName,
            portrait,
            dialogueLines
        );
    }
}
