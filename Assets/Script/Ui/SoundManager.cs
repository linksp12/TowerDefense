using UnityEngine;

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

    public void ShowMoneyWarning()
    {
        CancelInvoke("HideMoneyWarning");

        if (moneyWarningText != null) moneyWarningText.SetActive(true);

        if (uiAudioSource != null && errorSound != null)
        {
            uiAudioSource.PlayOneShot(errorSound);
        }

        Invoke("HideMoneyWarning", 1.5f);
    }

    void HideMoneyWarning()
    {
        if (moneyWarningText != null) moneyWarningText.SetActive(false);
    }
}
