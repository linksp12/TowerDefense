
using System.Collections.Generic;
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

    // 현재 씬에 존재하는 NPC 관리
    private static readonly HashSet<PlazaNPC> allNPCs =
        new HashSet<PlazaNPC>();

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
        // NPC의 원래 크기 저장
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        allNPCs.Add(this);
        ResetHoverScale();
    }

    private void OnDisable()
    {
        // NPC가 비활성화되더라도 확대 상태가 남지 않도록 처리
        ResetHoverScale();
        allNPCs.Remove(this);
    }

    /// <summary>
    /// NPC에 마우스를 올렸을 때
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 대화 중에는 NPC 확대 방지
        if (DialogueUI.IsOpen)
            return;

        transform.localScale = originalScale * hoverScale;
    }

    /// <summary>
    /// NPC에서 마우스를 뗐을 때
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverScale();
    }

    /// <summary>
    /// NPC를 클릭했을 때
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 마우스 왼쪽 클릭만 처리
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // 대화 중 다른 NPC를 클릭해도 대화가 바뀌지 않도록 차단
        if (DialogueUI.IsOpen)
            return;

        // 대화 관리자 확인
        if (DialogueUI.Instance == null)
        {
            Debug.LogWarning(
                "DialogueUI.Instance를 찾을 수 없습니다."
            );

            return;
        }

        // 대사가 없으면 대화 및 효과음 실행 방지
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning(
                $"{npcName}: 등록된 대사가 없습니다."
            );

            return;
        }

        // NPC 대화 시작
        DialogueUI.Instance.StartDialogue(
            npcType,
            npcName,
            portrait,
            dialogueLines
        );

        // 기존 AudioManager를 사용하여 효과음 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNPCInteraction();
        }
    }

    /// <summary>
    /// NPC의 원래 크기로 복구
    /// </summary>
    private void ResetHoverScale()
    {
        transform.localScale = originalScale;
    }

    /// <summary>
    /// 모든 NPC의 확대 상태 초기화
    /// 대화 시작 및 종료 시 DialogueUI에서 호출한다.
    /// </summary>
    public static void ResetAllHoverScales()
    {
        foreach (PlazaNPC npc in allNPCs)
        {
            if (npc != null)
            {
                npc.ResetHoverScale();
            }
        }
    }
}
