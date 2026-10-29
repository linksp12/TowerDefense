using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 스테이지를 처음 시작할 때(아직 클리어 전) 전장 지휘관·보스의 짧은 대사를 보여 준다.
/// 대사 중에는 전투가 멈추고, 클릭·Space·Enter로 넘기며 Esc로 모두 건너뛴다. 끝나면 고른 배속으로 돌아간다.
/// </summary>
public sealed class StageDialogue : MonoBehaviour
{
    private const string CommanderName = "전장 지휘관";
    private const float CharactersPerSecond = 38f;

    private struct Line
    {
        public bool Boss;
        public string Text;
        public Line(bool boss, string text) { Boss = boss; Text = text; }
    }

    private static readonly Dictionary<string, Line[]> Scripts = new Dictionary<string, Line[]>
    {
        ["stage_four"] = new[]
        {
            new Line(false, "숲 깊은 곳에서 땅이 울리고 있다.\n숲의 골렘이 깨어났다는 보고다."),
            new Line(true, "……돌아가라. 숲을 어지럽힌 자들은\n한 걸음도 지나가지 못한다."),
            new Line(false, "녀석은 무척 단단하다.\n화력을 길목 한곳에 모아 막아 내라!"),
        },
        ["stage_six"] = new[]
        {
            new Line(false, "설원의 얼음 마녀가 결계를 펼치고 있다.\n결계 안의 마물은 쉽게 쓰러지지 않는다."),
            new Line(true, "차가운 침묵 속에서… 영원히 잠들어라."),
            new Line(false, "결계가 걷히는 순간을 노려라.\n이번에도 탑을 지켜 낸다!"),
        },
        ["stage_eight"] = new[]
        {
            new Line(false, "화산이 깨어났다. 마지막 마물,\n화염 드래곤이 직접 이곳으로 온다."),
            new Line(true, "작은 탑 하나로 이 불길을 막겠다고?\n모두 재가 되어 흩어져라!"),
            new Line(false, "여기서 막으면 긴 밤이 끝난다.\n모든 것을 걸어라, 수호자!"),
        },
        ["hardcore_four"] = new[]
        {
            new Line(false, "꺼지지 않은 불씨들이 모여\n더 강한 마물이 되었다. 보스가 둘이다."),
            new Line(false, "이 싸움이 정말 마지막이다.\n탑의 전설이 되어라, 수호자!"),
        },
    };

    private static StageDialogue current;
    private static int closedFrame = -1;
    public static bool IsOpen => current != null;
    /// <summary>대사 중이거나 방금(같은 프레임) 닫혔으면 true. 같은 Esc로 일시정지 창이 열리지 않게 한다.</summary>
    public static bool BlocksInput => current != null || Time.frameCount == closedFrame;

    private Line[] lines;
    private MonsterData boss;
    private Sprite commanderPortrait;
    private AudioClip nextSound;
    private int index = -1;
    private float shown;
    private bool typing;
    private float openedAt;

    private CanvasGroup group;
    private RectTransform box;
    private Image portrait, hint;
    private TextMeshProUGUI speaker, body;

    public static void TryShow(string stageId)
    {
        if (current != null || stageId == null || !Scripts.TryGetValue(stageId, out Line[] script))
            return;
        var root = new GameObject("StageDialogue", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        current = root.AddComponent<StageDialogue>();
        current.Build(script);
    }

    // 이 스테이지 웨이브에 나오는 첫 보스(대사 초상화·이름용)
    private static MonsterData FindBoss()
    {
        var waves = FindAnyObjectByType<WaveManager>();
        if (waves == null || waves.waves == null) return null;
        foreach (WaveData wave in waves.waves)
        {
            if (wave == null || wave.spawnInfos == null) continue;
            foreach (WaveData.SpawnInfo spawn in wave.spawnInfos)
            {
                if (spawn == null || spawn.monsterPrefab == null) continue;
                var health = spawn.monsterPrefab.GetComponent<MonsterHealth>();
                if (health != null && health.monsterData != null && health.monsterData.isBoss)
                    return health.monsterData;
            }
        }
        return null;
    }

    private void Build(Line[] script)
    {
        lines = script;
        boss = FindBoss();
        commanderPortrait = Resources.Load<Sprite>("UI/Dialogue/portrait_commander");
        nextSound = Resources.Load<AudioClip>("Audio/Boss/dialogue_next");
        Time.timeScale = 0f;

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        var dim = UiFx.Image("Dim", transform, UiFx.Pixel, new Color(0f, 0f, 0f, 0.45f));
        UiFx.Stretch(dim.rectTransform);
        dim.raycastTarget = true;   // 대사 중 뒤쪽 클릭 차단

        box = UiFx.Rect("Box", transform);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0f);
        box.pivot = new Vector2(0.5f, 0f);
        box.anchoredPosition = new Vector2(0f, 60f);
        box.sizeDelta = new Vector2(1240f, 250f);
        var frame = UiFx.Image("Frame", box, BattleUiSkin.Frame, Color.white);
        BattleUiSkin.Skin(frame, BattleUiSkin.Frame, 1f);
        UiFx.Stretch(frame.rectTransform);

        var portraitFrame = UiFx.Image("PortraitFrame", box, BattleUiSkin.Inset, Color.white);
        BattleUiSkin.Skin(portraitFrame, BattleUiSkin.Inset, 1f);
        var pf = portraitFrame.rectTransform;
        pf.anchorMin = pf.anchorMax = pf.pivot = new Vector2(0f, 0.5f);
        pf.anchoredPosition = new Vector2(34f, 0f);
        pf.sizeDelta = new Vector2(190f, 190f);
        portrait = UiFx.Image("Portrait", portraitFrame.transform, null, Color.white);
        portrait.preserveAspect = true;
        UiFx.Stretch(portrait.rectTransform, 10f);

        speaker = Text("Speaker", 32f, new Vector2(250f, 78f), new Vector2(600f, 44f));
        body = Text("Body", 30f, new Vector2(250f, -10f), new Vector2(940f, 120f));
        body.textWrappingMode = TextWrappingModes.Normal;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.lineSpacing = 12f;

        hint = UiFx.Image("Next", box, UiFx.Pixel, new Color(1f, 0.84f, 0.47f, 1f));
        var hr = hint.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1f, 0f);
        hr.sizeDelta = new Vector2(14f, 14f);
        hr.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var hintText = Text("NextText", 20f, new Vector2(0f, 0f), new Vector2(330f, 30f));
        var ht = hintText.rectTransform;
        ht.anchorMin = ht.anchorMax = ht.pivot = new Vector2(1f, 0f);
        ht.anchoredPosition = new Vector2(-54f, 24f);
        hintText.alignment = TextAlignmentOptions.Right;
        hintText.color = new Color32(196, 184, 160, 255);
        hintText.text = "클릭 · Space 다음   Esc 건너뛰기";

        openedAt = Time.unscaledTime;
        Next();
    }

    private TextMeshProUGUI Text(string name, float size, Vector2 position, Vector2 area)
    {
        var rect = UiFx.Rect(name, box);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = area;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = BattleUiSkin.Font;
        text.fontSize = size;
        text.color = new Color32(244, 236, 220, 255);
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        text.outlineWidth = 0.15f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        return text;
    }

    private void Next()
    {
        index++;
        if (index >= lines.Length)
        {
            Close();
            return;
        }
        if (index > 0 && nextSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(nextSound, 0.6f);
        Line line = lines[index];
        bool bossSpeaks = line.Boss && boss != null;
        portrait.sprite = bossSpeaks ? boss.bossPortrait : commanderPortrait;
        portrait.enabled = portrait.sprite != null;
        speaker.text = bossSpeaks
            ? $"<color=#FF7A5C>{(string.IsNullOrWhiteSpace(boss.bossName) ? boss.displayName : boss.bossName)}</color>"
            : $"<color=#FFD678>{CommanderName}</color>";
        body.text = line.Text;
        body.maxVisibleCharacters = 0;
        shown = 0f;
        typing = true;
    }

    private void Update()
    {
        // 다른 스크립트가 전투 시작 때 배속을 되돌려도 대사 중에는 멈춰 둔다.
        if (Time.timeScale != 0f)
            Time.timeScale = 0f;

        float now = Time.unscaledTime;
        group.alpha = Mathf.Clamp01((now - openedAt) / 0.3f);
        box.anchoredPosition = new Vector2(0f, Mathf.Lerp(20f, 60f, UiFx.EaseOut(group.alpha)));

        if (typing)
        {
            shown += Time.unscaledDeltaTime * CharactersPerSecond;
            body.maxVisibleCharacters = Mathf.FloorToInt(shown);
            if (shown >= body.text.Length) typing = false;
        }
        hint.enabled = !typing;
        if (!typing)
            hint.rectTransform.anchoredPosition = new Vector2(-30f, 32f + 4f * Mathf.Sin(now * 6f));

        if (now - openedAt < 0.25f) return;   // 전투 시작 클릭이 바로 넘기지 않게
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (typing)
            {
                typing = false;
                body.maxVisibleCharacters = 99999;
            }
            else
            {
                Next();
            }
        }
    }

    private void Close()
    {
        if (current == this)
            current = null;
        closedFrame = Time.frameCount;
        GameSpeedController.ResumeSelectedSpeed();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
