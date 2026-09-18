using UnityEngine;

public class HitSoundManager : MonoBehaviour
{
    public static HitSoundManager Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    [Header("Global Hit Sound Control")]
    [Tooltip("게임 전체에서 타격음이 다시 재생되기까지의 최소 간격")]
    [SerializeField] private float hitSoundInterval = 0.08f;

    [Tooltip("타격음 볼륨")]
    [Range(0f, 1f)]
    [SerializeField] private float hitSoundVolume = 0.55f;

    [Header("Pitch Variation")]
    [SerializeField] private bool randomPitch = true;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private float lastPlayTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayHitSound()
    {
        if (audioSource == null || hitSound == null)
        {
            return;
        }

        // 게임 전체 기준으로 타격음 재생 간격 제한
        if (Time.unscaledTime - lastPlayTime < hitSoundInterval)
        {
            return;
        }

        lastPlayTime = Time.unscaledTime;

        if (randomPitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(
            hitSound,
            hitSoundVolume
        );

        // 다음 사운드를 위해 원래 피치로 복구
        audioSource.pitch = 1f;
    }
}
