using UnityEngine;

/// <summary>
/// 씬 전환에도 유지되는 오디오 매니저 (싱글톤)
/// - BGM, 효과음(SFX), UI 사운드 각각 볼륨 조절 가능
/// - PlayerPrefs로 볼륨 설정 자동 저장/불러오기
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;    // BGM 전용 (Loop)
    public AudioSource sfxSource;    // 효과음 전용 (OneShot)
    public AudioSource uiSource;     // UI 사운드 전용 (OneShot)

    [Header("BGM Clips")]
    public AudioClip mainMenuBGM;    // 메인 화면 BGM
    public AudioClip storyBGM;       // 스토리 씬 BGM
    public AudioClip gameBGM;        // 게임 씬 BGM
    public AudioClip resultBGM;      // 결과 화면 BGM

    [Header("UI Clips")]
    public AudioClip buttonClickSFX; // 버튼 클릭 사운드

    // ──────── 볼륨 (0 ~ 1) ────────
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private float uiVolume = 1f;

    // ──────── PlayerPrefs 키 ────────
    private const string KEY_MASTER = "Volume_Master";
    private const string KEY_BGM    = "Volume_BGM";
    private const string KEY_SFX    = "Volume_SFX";
    private const string KEY_UI     = "Volume_UI";

    // ──────── 초기화 ────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource가 비어있으면 자동 생성
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.loop = false;
            uiSource.playOnAwake = false;
        }

        LoadVolumeSettings();
    }

    // ──────── BGM 재생 ────────
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        // 이미 같은 BGM 재생 중이면 무시
        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>
    /// 씬 이름에 맞는 BGM 자동 재생
    /// </summary>
    public void PlayBGMForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainScene":
                PlayBGM(mainMenuBGM);
                break;
            case "StoryScene":
                PlayBGM(storyBGM);
                break;
            case "GameScene":
                PlayBGM(gameBGM);
                break;
            case "ResultScene":
                PlayBGM(resultBGM);
                break;
        }
    }

    // ──────── 효과음 재생 ────────
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }

    // ──────── UI 사운드 재생 ────────
    public void PlayUISound(AudioClip clip)
    {
        if (clip == null || uiSource == null) return;
        uiSource.PlayOneShot(clip, uiVolume * masterVolume);
    }

    /// <summary>
    /// 버튼 클릭 사운드 (간편 호출용)
    /// </summary>
    public void PlayButtonClick()
    {
        PlayUISound(buttonClickSFX);
    }

    // ──────── 볼륨 설정 ────────
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyVolumes();
            PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
        }
    }

    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            ApplyVolumes();
            PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplyVolumes();
            PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        }
    }

    public float UIVolume
    {
        get => uiVolume;
        set
        {
            uiVolume = Mathf.Clamp01(value);
            ApplyVolumes();
            PlayerPrefs.SetFloat(KEY_UI, uiVolume);
        }
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = bgmVolume * masterVolume;

        // SFX와 UI는 PlayOneShot에서 볼륨 곱해서 재생하므로
        // source 자체 볼륨은 1로 유지
    }

    // ──────── 저장 / 불러오기 ────────
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        bgmVolume    = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        sfxVolume    = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        uiVolume     = PlayerPrefs.GetFloat(KEY_UI, 1f);

        ApplyVolumes();
    }

    public void SaveAllSettings()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
        PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.SetFloat(KEY_UI, uiVolume);
        PlayerPrefs.Save();
    }
}
