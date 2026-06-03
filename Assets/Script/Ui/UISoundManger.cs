using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("UI Sounds")]
    public AudioClip openPanelSound;
    public AudioClip closePanelSound;
    public AudioClip clickSound;
    public AudioClip buildSuccessSound;
    public AudioClip upgradeSuccessSound;
    public AudioClip sellSound;
    public AudioClip failSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayOpenPanel()
    {
        Play(openPanelSound);
    }

    public void PlayClosePanel()
    {
        Play(closePanelSound);
    }

    public void PlayClick()
    {
        Play(clickSound);
    }

    public void PlayBuildSuccess()
    {
        Play(buildSuccessSound);
    }

    public void PlayUpgradeSuccess()
    {
        Play(upgradeSuccessSound);
    }

    public void PlaySell()
    {
        Play(sellSound);
    }

    public void PlayFail()
    {
        Play(failSound);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
