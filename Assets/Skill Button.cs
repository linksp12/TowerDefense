using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    public AudioClip skillSound;
    private AudioSource audioSource;

    [Header("애니메이션")]
    public float punchScale = 1.2f;
    public float punchDuration = 0.1f;

    private bool wasOnCooldown = false; // 쿨타임 끝남 감지용

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        button.onClick.AddListener(OnSkillButtonClick);
        cooldownOverlay.fillAmount = 0f;
    }

    void Update()
    {
        UpdateCooldownUI();
    }

    void OnSkillButtonClick()
    {
        if (skillSound != null)
            audioSource.PlayOneShot(skillSound);

        // 클릭 시 펀치 애니메이션
        StartCoroutine(PunchAnim());

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
            wasOnCooldown = true; // 쿨타임 중 기록
        }
        else
        {
            // 쿨타임이 방금 끝났을 때 반짝 애니메이션
            if (wasOnCooldown)
            {
                wasOnCooldown = false;
                StartCoroutine(ReadyAnim());
            }

            cooldownOverlay.fillAmount = 0f;
            cooldownText.gameObject.SetActive(false);
        }
    }

    // 클릭 시 커졌다 작아지는 애니메이션
    IEnumerator PunchAnim()
    {
        Vector3 original = transform.localScale;
        Vector3 big = original * punchScale;
        float half = punchDuration * 0.5f;

        for (float t = 0; t < half; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(original, big, t / half);
            yield return null;
        }
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(big, original, t / half);
            yield return null;
        }
        transform.localScale = original;
    }

    // 쿨타임 끝났을 때 반짝이는 애니메이션
    IEnumerator ReadyAnim()
    {
        Vector3 original = transform.localScale;
        Vector3 big = original * 1.15f;
        float dur = 0.2f;

        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(original, big, t / dur);
            yield return null;
        }
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(big, original, t / dur);
            yield return null;
        }
        transform.localScale = original;
    }
}
