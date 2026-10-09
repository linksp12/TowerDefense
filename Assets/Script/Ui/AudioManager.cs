
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환에도 유지되는 오디오 매니저 (싱글톤)
/// - BGM, 효과음(SFX), UI 사운드 각각 볼륨 조절 가능
/// - PlayerPrefs로 볼륨 설정 자동 저장/불러오기
/// - 씬 변경 시 BGM 자동 교체
/// - 광장 전용 BGM 및 NPC 상호작용 효과음 지원
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
    public AudioClip plazaBGM;       // 광장 BGM

    [Header("UI Clips")]
    public AudioClip buttonClickSFX; // 버튼 클릭 사운드

    [Header("Interaction Clips")]
    public AudioClip npcInteractionSFX; // NPC 상호작용 효과음

    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private float uiVolume = 1f;
    private float sfxSourceBaseVolume = 1f;
    private float uiSourceBaseVolume = 1f;

    private struct ExternalSource
    {
        public float baseVolume;
        public bool isUI;
    }

    private readonly Dictionary<AudioSource, ExternalSource> externalSources =
        new Dictionary<AudioSource, ExternalSource>();

    private readonly List<AudioSource> destroyedSources =
        new List<AudioSource>();

    private const string KEY_MASTER = "Volume_Master";
    private const string KEY_BGM = "Volume_BGM";
    private const string KEY_SFX = "Volume_SFX";
    private const string KEY_UI = "Volume_UI";
    private const string RESULT_POPUP_SFX_PATH = "Audio/StageResultPopup";

    private AudioClip resultPopupSFX;

    // NPC 효과음의 빠른 중복 재생 방지
    private float lastNpcInteractionTime = -1f;
    private const float NPC_INTERACTION_COOLDOWN = 0.08f;

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

        resultPopupSFX =
            Resources.Load<AudioClip>(RESULT_POPUP_SFX_PATH);

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

        if (Instance == this)
            Instance = null;
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
        sfxSourceBaseVolume = sfxSource.volume;

        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
        }

        uiSource.loop = false;
        uiSource.playOnAwake = false;
        uiSourceBaseVolume = uiSource.volume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyVolumes();
        PlayBGMForScene(scene.name);
    }

    /// <summary>
    /// 지정된 BGM을 반복 재생한다.
    /// 이미 같은 음악이 재생 중이면 다시 시작하지 않는다.
    /// </summary>
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

    /// <summary>
    /// 현재 BGM을 정지한다.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.time = 0f;
        }
    }

    /// <summary>
    /// 씬 이름에 따라 알맞은 BGM을 재생한다.
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

            case "PlazaScene":
                // 광장 전용 BGM 재생
                if (plazaBGM != null)
                {
                    PlayBGM(plazaBGM);
                }
                else
                {
                    // 클립 미지정 시 이전 씬 음악이 계속 재생되지 않도록 한다.
                    StopBGM();

                    Debug.LogWarning(
                        "AudioManager: PlazaScene의 plazaBGM이 지정되지 않았습니다."
                    );
                }
                break;

            case "Stage1Scene":
            case "Stage2Scene":
            case "Stage3Scene":
            case "Stage4Scene":
                PlayBGM(gameBGM);
                break;

            case "ResultScene":
                PlayBGM(resultBGM);
                break;

            default:
                Debug.Log(
                    "AudioManager: 등록되지 않은 씬입니다. BGM 변경 없음: "
                    + sceneName
                );
                break;
        }
    }

    /// <summary>
    /// 일반 효과음을 재생한다.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(
            clip,
            Mathf.Max(0f, volumeScale)
        );
    }

    /// <summary>
    /// UI 효과음을 재생한다.
    /// </summary>
    public void PlayUISound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || uiSource == null)
            return;

        uiSource.PlayOneShot(
            clip,
            Mathf.Max(0f, volumeScale)
        );
    }

    // 기존 컴포넌트의 AudioSource와 피치/볼륨 설정은 유지하고,
    // 모든 효과음의 최종 음량만 이 매니저에서 관리한다.
    public static void PlaySFXOn(
        AudioSource source,
        AudioClip clip,
        float volumeScale = 1f)
    {
        PlayOn(source, clip, volumeScale, false);
    }

    public static void PlayUISoundOn(
        AudioSource source,
        AudioClip clip,
        float volumeScale = 1f)
    {
        PlayOn(source, clip, volumeScale, true);
    }

    public static void PlaySFXAtPoint(
        AudioClip clip,
        Vector3 position,
        float volumeScale = 1f)
    {
        if (clip == null)
            return;

        float groupVolume = Instance != null
            ? Instance.masterVolume * Instance.sfxVolume
            : GetSavedGroupVolume(false);

        AudioSource.PlayClipAtPoint(
            clip,
            position,
            Mathf.Max(0f, volumeScale) * groupVolume
        );
    }

    private static void PlayOn(
        AudioSource source,
        AudioClip clip,
        float volumeScale,
        bool isUI)
    {
        if (clip == null)
            return;

        if (Instance != null)
        {
            if (source == null ||
                source == Instance.sfxSource ||
                source == Instance.uiSource)
            {
                if (isUI)
                    Instance.PlayUISound(clip, volumeScale);
                else
                    Instance.PlaySFX(clip, volumeScale);

                return;
            }

            Instance.PlayOnExternalSource(
                source,
                clip,
                volumeScale,
                isUI
            );
        }
        else if (source != null)
        {
            // 스테이지 씬을 에디터에서 직접 실행한 경우에도
            // 저장된 음량 설정을 적용한다.
            source.PlayOneShot(
                clip,
                Mathf.Max(0f, volumeScale)
                * GetSavedGroupVolume(isUI)
            );
        }
    }

    private void PlayOnExternalSource(
        AudioSource source,
        AudioClip clip,
        float volumeScale,
        bool isUI)
    {
        if (!externalSources.TryGetValue(
            source,
            out ExternalSource settings))
        {
            settings = new ExternalSource
            {
                baseVolume = source.volume,
                isUI = isUI
            };
        }
        else
        {
            settings.isUI = isUI;
        }

        externalSources[source] = settings;

        source.volume = settings.baseVolume
            * masterVolume
            * (isUI ? uiVolume : sfxVolume);

        source.PlayOneShot(
            clip,
            Mathf.Max(0f, volumeScale)
        );
    }

    private static float GetSavedGroupVolume(bool isUI)
    {
        float savedMaster = Mathf.Clamp01(
            PlayerPrefs.GetFloat(KEY_MASTER, 1f)
        );

        float savedGroup = Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                isUI ? KEY_UI : KEY_SFX,
                1f
            )
        );

        return savedMaster * savedGroup;
    }

    /// <summary>
    /// 일반 버튼 클릭 효과음을 재생한다.
    /// </summary>
    public void PlayButtonClick()
    {
        PlayUISound(buttonClickSFX);
    }

    /// <summary>
    /// NPC 상호작용 효과음을 재생한다.
    /// 빠르게 중복 호출되는 경우 재생 간격을 제한한다.
    /// </summary>
    public void PlayNPCInteraction()
    {
        if (npcInteractionSFX == null)
            return;

        if (Time.unscaledTime - lastNpcInteractionTime
            < NPC_INTERACTION_COOLDOWN)
        {
            return;
        }

        lastNpcInteractionTime = Time.unscaledTime;

        // SFX 볼륨 설정을 적용하여 재생한다.
        PlaySFX(npcInteractionSFX);
    }

    /// <summary>
    /// 스테이지 결과창 효과음을 재생한다.
    /// </summary>
    public void PlayStageResultSound()
    {
        if (resultPopupSFX == null)
        {
            resultPopupSFX =
                Resources.Load<AudioClip>(RESULT_POPUP_SFX_PATH);
        }

        if (resultPopupSFX == null)
        {
            Debug.LogWarning(
                "AudioManager: 결과창 효과음을 찾지 못했습니다."
            );
            return;
        }

        PlayUISound(resultPopupSFX);
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

    /// <summary>
    /// 모든 AudioSource에 현재 볼륨 설정을 적용한다.
    /// </summary>
    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume * masterVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume =
                sfxSourceBaseVolume * sfxVolume * masterVolume;
        }

        if (uiSource != null)
        {
            uiSource.volume =
                uiSourceBaseVolume * uiVolume * masterVolume;
        }

        destroyedSources.Clear();

        foreach (KeyValuePair<AudioSource, ExternalSource> entry
            in externalSources)
        {
            if (entry.Key == null)
            {
                destroyedSources.Add(entry.Key);
                continue;
            }

            entry.Key.volume =
                entry.Value.baseVolume
                * masterVolume
                * (entry.Value.isUI ? uiVolume : sfxVolume);
        }

        foreach (AudioSource source in destroyedSources)
        {
            externalSources.Remove(source);
        }
    }

    /// <summary>
    /// 저장된 볼륨 설정을 불러온다.
    /// </summary>
    private void LoadVolumeSettings()
    {
        masterVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(KEY_MASTER, 1f)
        );

        bgmVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(KEY_BGM, 1f)
        );

        sfxVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(KEY_SFX, 1f)
        );

        uiVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(KEY_UI, 1f)
        );

        ApplyVolumes();
    }

    /// <summary>
    /// 전체 볼륨 설정을 PlayerPrefs에 저장한다.
    /// </summary>
    public void SaveAllSettings()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, masterVolume);
        PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.SetFloat(KEY_UI, uiVolume);

        PlayerPrefs.Save();
    }
}