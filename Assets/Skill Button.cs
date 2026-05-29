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
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;

    void Start()
    {
        // interactable 건드리지 않음
        button.onClick.AddListener(OnSkillButtonClick);
        cooldownOverlay.fillAmount = 0f;
    }

    void Update()
    {
        UpdateCooldownUI();
    }

    void OnSkillButtonClick()
    {
        // 쿨타임 체크는 SkillManager에서 처리
        // 버튼은 항상 클릭 가능하게
        SkillManager.Instance.UseSkill(skillName);
    }

    void UpdateCooldownUI()
    {
        bool onCooldown = !SkillManager.Instance.CanUseSkill(skillName);
        float remaining = SkillManager.Instance.GetCooldownRemaining(skillName);
        float normalized = SkillManager.Instance.GetCooldownNormalized(skillName);

        cooldownOverlay.fillAmount = normalized;

        if (onCooldown)
        {
            // 버튼 비활성화 제거 - UI만 업데이트
            cooldownText.text = remaining > 1f
                ? Mathf.CeilToInt(remaining).ToString()
                : remaining.ToString("F1");
            cooldownText.gameObject.SetActive(true);
        }
        else
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownText.gameObject.SetActive(false);
        }
    }
}
