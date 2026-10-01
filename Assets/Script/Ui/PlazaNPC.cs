using UnityEngine;
using UnityEngine.EventSystems;

public class PlazaNPC : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public enum NPCType
    {
        Skill,
        Quest,
        Stage
    }

    [Header("NPC 종류")]
    [SerializeField] private NPCType npcType;

    [Header("NPC 정보")]
    [SerializeField] private string npcName;
    [SerializeField] private Sprite portrait;

    [Header("NPC 대사")]
    [TextArea(2, 4)]
    [SerializeField] private string[] dialogueLines;

    [Header("마우스 오버")]
    [SerializeField] private float hoverScale = 1.08f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// NPC에 마우스를 올렸을 때
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale =
            originalScale * hoverScale;
    }

    /// <summary>
    /// NPC에서 마우스를 뗐을 때
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale =
            originalScale;
    }

    /// <summary>
    /// NPC를 클릭했을 때
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 대화창 관리자가 없으면 종료
        if (DialogueUI.Instance == null)
        {
            Debug.LogWarning(
                "DialogueUI.Instance를 찾을 수 없습니다."
            );

            return;
        }

        // 대화창 시작
        DialogueUI.Instance.StartDialogue(
            npcType,
            npcName,
            portrait,
            dialogueLines
        );
    }
}
