using UnityEngine;
using TMPro;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public GameObject moneyWarningText;
    public AudioSource uiAudioSource;
    public AudioClip errorSound;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowMoneyWarning(int amount)
    {
        CancelInvoke("HideMoneyWarning");

        if (moneyWarningText != null)
        {
            moneyWarningText.SetActive(true);

            var textComponent = moneyWarningText.GetComponent<TMPro.TextMeshProUGUI>();
            
            if (textComponent != null)
            {
                textComponent.text = $"{amount}원이 부족합니다!";
            }
        }

        if (uiAudioSource != null && errorSound != null)
        {
            AudioManager.PlayUISoundOn(uiAudioSource, errorSound);
        }

        Invoke("HideMoneyWarning", 1.5f);
    }

    void HideMoneyWarning()
    {
        if (moneyWarningText != null) moneyWarningText.SetActive(false);
    }
}
