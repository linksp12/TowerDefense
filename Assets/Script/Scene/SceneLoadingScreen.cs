using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 출전/다시하기/복귀가 공유하는 런타임 UI. 씬·진행 기록·보상은 소유하지 않는다.
public class SceneLoadingScreen : MonoBehaviour
{
    private static SceneLoadingScreen instance;
    public static bool IsLoading => instance != null;

    private CanvasGroup group;
    private RectTransform progressFill;
    private TextMeshProUGUI loadingLabel;
    private string message;
    private int lastDots = -1;
    private float previousTimeScale;
    private bool previousAudioPause;

    public static bool TryLoad(string scenePath, string message, Action onFailure = null)
    {
        if (!Application.isPlaying || IsLoading)
            return false;
        if (string.IsNullOrWhiteSpace(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError($"SceneLoadingScreen: 이동할 씬을 찾을 수 없습니다: {scenePath}");
            return false;
        }

        GameObject root = new GameObject("SceneLoadingScreen", typeof(RectTransform),
            typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup), typeof(SceneLoadingScreen));
        instance = root.GetComponent<SceneLoadingScreen>();
        instance.previousTimeScale = Time.timeScale;
        instance.previousAudioPause = AudioListener.pause;
        DontDestroyOnLoad(root);
        try
        {
            instance.Build(string.IsNullOrWhiteSpace(message) ? "화면을 준비하고 있습니다" : message);
            instance.StartCoroutine(instance.Load(scenePath, onFailure));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            instance.Release(true);
            return false;
        }
    }

    private IEnumerator Load(string scenePath, Action onFailure)
    {
        // 비동기 로드를 시작하기 전에 로딩 화면을 먼저 그린다.
        yield return null;
        yield return Fade(0f, 1f, 0.12f);

        AsyncOperation operation = null;
        try
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (operation != null)
                operation.allowSceneActivation = false;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (operation == null)
        {
            Release(true);
            onFailure?.Invoke();
            yield break;
        }

        // Unity의 0.9는 데이터 로딩 완료이며 씬 활성화 완료(100%)는 아니다.
        while (operation.progress < 0.9f)
        {
            SetProgress(operation.progress);
            yield return null;
        }
        SetProgress(0.9f);
        operation.allowSceneActivation = true;
        while (!operation.isDone)
            yield return null;

        // 목적지 Awake/Start 및 첫 화면 갱신을 기다린 뒤 입력 차단을 해제한다.
        yield return null;
        yield return null;
        SetProgress(1f);
        loadingLabel.text = "준비 완료";
        yield return Fade(1f, 0f, 0.18f);
        Release(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
    }

    private void Update()
    {
        if (loadingLabel == null || loadingLabel.text == "준비 완료")
            return;
        int dots = (int)(Time.unscaledTime * 2.5f) % 3 + 1;
        if (dots == lastDots)
            return;
        lastDots = dots;
        loadingLabel.text = message + new string('.', dots);
    }

    private void SetProgress(float value)
    {
        progressFill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    private void Release(bool restorePreviousState)
    {
        if (restorePreviousState)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
        }
        if (group != null)
            group.blocksRaycasts = false;
        if (instance == this)
            instance = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void Build(string content)
    {
        message = content;
        gameObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = true;

        var background = Image("Background", transform, new Color(0.035f, 0.05f, 0.065f, 1f));
        Stretch(background.rectTransform);
        background.raycastTarget = true;
        Color gold = new Color(0.86f, 0.64f, 0.28f, 1f);
        Place(Image("TopBorder", transform, gold).rectTransform, new Vector2(0f, 250f), new Vector2(900f, 2f));
        Place(Image("BottomBorder", transform, gold).rectTransform, new Vector2(0f, -250f), new Vector2(900f, 2f));
        var ornament = Image("Crest", transform, gold).rectTransform;
        Place(ornament, new Vector2(0f, 172f), new Vector2(16f, 16f));
        ornament.localRotation = Quaternion.Euler(0f, 0f, 45f);

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            if (text.font != null && text.font.name == "Maplestory Bold SDF")
            {
                font = text.font;
                break;
            }
        Text("GameTitle", "Last of the Tower", new Vector2(0f, 80f), new Vector2(1000f, 100f), 64f, gold, font);
        loadingLabel = Text("LoadingMessage", message + "...", new Vector2(0f, -60f),
            new Vector2(1000f, 60f), 30f, new Color(0.9f, 0.92f, 0.9f, 1f), font);

        var frame = Image("ProgressFrame", transform, gold);
        Place(frame.rectTransform, new Vector2(0f, -145f), new Vector2(626f, 18f));
        var track = Image("ProgressTrack", frame.transform, new Color(0.12f, 0.14f, 0.15f, 1f));
        Stretch(track.rectTransform);
        track.rectTransform.sizeDelta = new Vector2(-6f, -6f);
        progressFill = Image("ProgressFill", track.transform, gold).rectTransform;
        Stretch(progressFill);
        SetProgress(0f);
    }

    private UnityEngine.UI.Image Image(string objectName, Transform parent, Color color)
    {
        var child = new GameObject(objectName, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        child.layer = gameObject.layer;
        child.transform.SetParent(parent, false);
        var image = child.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI Text(string objectName, string content, Vector2 position,
        Vector2 size, float fontSize, Color color, TMP_FontAsset font)
    {
        var child = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);
        var text = child.GetComponent<TextMeshProUGUI>();
        Place(text.rectTransform, position, size);
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
