using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillButton : MonoBehaviour
{
    [Header("연결할 스킬 이름")]
    public string skillName;

    [Header("UI 컴포넌트")]
    public Button button;
    public Image iconImage;
    public Image cooldownOverlay;   // 쿨타임 어두운 오버레이 (fillAmount 사용)
    public TextMeshProUGUI cooldownText;  // 남은 시간 텍스트

    void Start()
    {
        // 버튼 클릭 이벤트 등록
        button.onClick.AddListener(OnSkillButtonClick);
        cooldownOverlay.fillAmount = 0f;
    }

    void Update()
    {
        UpdateCooldownUI();
    }

    void OnSkillButtonClick()
    {
        SkillManager.Instance.UseSkill(skillName);
    }

    void UpdateCooldownUI()
    {
        bool onCooldown = !SkillManager.Instance.CanUseSkill(skillName);
        float remaining = SkillManager.Instance.GetCooldownRemaining(skillName);
        float normalized = SkillManager.Instance.GetCooldownNormalized(skillName);

        // 오버레이 fillAmount 업데이트 (시계 방향으로 줄어듦)
        cooldownOverlay.fillAmount = normalized;

        // 버튼 활성화/비활성화
        button.interactable = !onCooldown;

        // 남은 시간 텍스트
        if (onCooldown)
        {
            cooldownText.text = remaining > 1f
                ? Mathf.CeilToInt(remaining).ToString()
                : remaining.ToString("F1");
            cooldownText.gameObject.SetActive(true);
        }
        else
        {
            cooldownText.gameObject.SetActive(false);
        }
    }
}
