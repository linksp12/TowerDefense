using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환에도 유지되는 오디오 매니저 (싱글톤)
/// - BGM, 효과음(SFX), UI 사운드 각각 볼륨 조절 가능
/// - PlayerPrefs로 볼륨 설정 자동 저장/불러오기
/// - 씬 변경 시 BGM 자동 교체
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

    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private float uiVolume = 1f;

    private const string KEY_MASTER = "Volume_Master";
    private const string KEY_BGM = "Volume_BGM";
    private const string KEY_SFX = "Volume_SFX";
    private const string KEY_UI = "Volume_UI";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        LoadVolumeSettings();

        // 씬이 바뀔 때마다 자동으로 BGM 교체
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 처음 시작한 씬의 BGM 재생
        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        // 파괴될 때 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
        }

        uiSource.loop = false;
        uiSource.playOnAwake = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null)
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

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

            default:
                Debug.Log("AudioManager: 등록되지 않은 씬입니다. BGM 변경 없음: " + sceneName);
                break;
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }

    public void PlayUISound(AudioClip clip)
    {
        if (clip == null || uiSource == null)
            return;

        uiSource.PlayOneShot(clip, uiVolume * masterVolume);
    }

    public void PlayButtonClick()
    {
        PlayUISound(buttonClickSFX);
    }

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
            PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        }
    }

    public float UIVolume
    {
        get => uiVolume;
        set
        {
            uiVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_UI, uiVolume);
        }
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = bgmVolume * masterVolume;
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        uiVolume = PlayerPrefs.GetFloat(KEY_UI, 1f);

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
