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

    [Header("사운드")]
    public AudioClip skillSound;        // 스킬별 사운드 클립
    private AudioSource audioSource;

    void Start()
    {
        // AudioSource 자동으로 가져오기 (없으면 추가)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;  // 시작하자마자 재생 방지

        button.onClick.AddListener(OnSkillButtonClick);
        cooldownOverlay.fillAmount = 0f;
    }

    void Update()
    {
        UpdateCooldownUI();
    }

    void OnSkillButtonClick()
    {
        // 사운드 재생
        if (skillSound != null)
            audioSource.PlayOneShot(skillSound);

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
