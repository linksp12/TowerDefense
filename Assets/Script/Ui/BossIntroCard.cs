using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스가 처음 나타날 때 화면 가운데를 가로지르는 등장 카드(초상화·이름·별칭)를 잠깐 보여 준다.
/// 띠가 열리고 → 초상화와 이름이 양쪽에서 들어와 타격음에 맞춰 번쩍인 뒤 → 사라진다.
/// 게임은 멈추지 않고 클릭도 막지 않는다. 같은 보스 종류는 한 전투에 한 번만 보여 준다.
/// </summary>
public sealed class BossIntroCard : MonoBehaviour
{
    private const float OpenTime = 0.35f, HitTime = 0.5f, HoldTime = 2.3f, CloseTime = 0.4f;
    private const float BandHeight = 250f;

    // 보스 ID별 별칭과 띠 안에 흩날리는 입자 색
    private static readonly Dictionary<string, (string title, Color[] palette)> Styles = new Dictionary<string, (string, Color[])>
    {
        ["forest_golem"] = ("고대 숲의 수호자", new[] { new Color(0.55f, 0.85f, 0.35f, 0.9f), new Color(0.85f, 0.75f, 0.35f, 0.8f) }),
        ["ice_witch"] = ("서리 결계의 여왕", new[] { new Color(0.75f, 0.92f, 1f, 0.95f), new Color(1f, 1f, 1f, 0.85f) }),
        ["fire_dragon"] = ("화산의 지배자", new[] { new Color(1f, 0.55f, 0.18f, 0.95f), new Color(1f, 0.32f, 0.12f, 0.9f) }),
    };

    private static readonly HashSet<MonsterData> shown = new HashSet<MonsterData>();
    private static int shownScene = -1;
    private static AudioClip sound;

    private RectTransform band, portraitGroup, textGroup;
    private Image bandImage, flash, glow, edgeTop, edgeBottom;
    private CanvasGroup group;

    /// <summary>이번 전투에서 이 보스 종류의 카드를 처음 띄웠으면 true.</summary>
    public static bool Show(MonsterHealth boss)
    {
        if (boss == null || boss.monsterData == null)
            return false;
        int scene = boss.gameObject.scene.handle;
        if (scene != shownScene)
        {
            shown.Clear();
            shownScene = scene;
        }
        if (!shown.Add(boss.monsterData))
            return false;

        var root = new GameObject("BossIntroCard", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;   // 전투 UI 위, 결과창·로딩 화면 아래
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<BossIntroCard>().Build(boss);
        return true;
    }

    private void Build(MonsterHealth boss)
    {
        MonsterData data = boss.monsterData;
        Styles.TryGetValue(data.id ?? "", out var style);
        string title = style.title ?? "강력한 적이 나타났다";
        Color[] palette = style.palette ?? new[] { new Color(1f, 0.6f, 0.3f, 0.9f) };
        Color accent = palette[0];

        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        band = UiFx.Rect("Band", transform);
        band.anchorMin = new Vector2(0f, 0.5f);
        band.anchorMax = new Vector2(1f, 0.5f);
        band.sizeDelta = new Vector2(0f, 0f);
        band.anchoredPosition = new Vector2(0f, 40f);
        bandImage = UiFx.Image("Shade", band, UiFx.Pixel, new Color(0.03f, 0.02f, 0.04f, 0.9f));
        UiFx.Stretch(bandImage.rectTransform);
        var mask = band.gameObject.AddComponent<RectMask2D>();
        mask.padding = Vector4.zero;
        edgeTop = Edge("EdgeTop", 1f, accent);
        edgeBottom = Edge("EdgeBottom", 0f, accent);

        var particles = UiParticleField.Create("Particles", band);
        UiFx.Stretch((RectTransform)particles.transform);
        particles.palette = palette;
        particles.sizeRange = new Vector2(3f, 7f);
        particles.speedMin = new Vector2(-90f, -10f);
        particles.speedMax = new Vector2(-40f, 30f);
        particles.lifeRange = new Vector2(1.5f, 3f);
        particles.rate = 26f;
        particles.spawnArea = new Rect(0.2f, 0f, 0.8f, 1f);
        particles.twinkle = 0.6f;

        // 초상화(왼쪽)
        portraitGroup = UiFx.Rect("Portrait", band);
        portraitGroup.anchorMin = portraitGroup.anchorMax = new Vector2(0.5f, 0.5f);
        portraitGroup.sizeDelta = new Vector2(220f, 220f);
        glow = UiFx.Image("Glow", portraitGroup, UiFx.SoftCircle, new Color(accent.r, accent.g, accent.b, 0.5f));
        glow.rectTransform.sizeDelta = new Vector2(380f, 380f);
        var frame = UiFx.Image("Frame", portraitGroup, BattleUiSkin.Inset, Color.white);
        BattleUiSkin.Skin(frame, BattleUiSkin.Inset, 1f);
        frame.rectTransform.sizeDelta = new Vector2(208f, 208f);
        if (data.bossPortrait != null)
        {
            var portrait = UiFx.Image("Image", portraitGroup, data.bossPortrait, Color.white);
            portrait.preserveAspect = true;
            portrait.rectTransform.sizeDelta = new Vector2(188f, 188f);
        }

        // 이름·별칭(오른쪽)
        textGroup = UiFx.Rect("Text", band);
        textGroup.anchorMin = textGroup.anchorMax = new Vector2(0.5f, 0.5f);
        textGroup.sizeDelta = new Vector2(820f, 220f);
        var bossLabel = Text("Boss", textGroup, "BOSS", 30f, new Color32(255, 90, 70, 255), new Vector2(0f, 62f));
        bossLabel.characterSpacing = 8f;
        string name = string.IsNullOrWhiteSpace(data.bossName) ? data.displayName : data.bossName;
        Text("Name", textGroup, name, 76f, new Color32(255, 222, 140, 255), new Vector2(0f, 0f));
        Text("Title", textGroup, "— " + title + " —", 28f, new Color32(236, 226, 206, 255), new Vector2(0f, -64f));

        flash = UiFx.Image("Flash", transform, UiFx.Pixel, new Color(1f, 0.95f, 0.85f, 0f));
        UiFx.Stretch(flash.rectTransform);

        if (sound == null)
            sound = Resources.Load<AudioClip>("Audio/Boss/boss_intro");
        if (sound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayUISound(sound, 0.9f);
        StartCoroutine(Run());
    }

    private Image Edge(string name, float y, Color accent)
    {
        var edge = UiFx.Image(name, band, UiFx.Pixel, new Color(accent.r, accent.g, accent.b, 0.9f));
        var rect = edge.rectTransform;
        rect.anchorMin = new Vector2(0f, y);
        rect.anchorMax = new Vector2(1f, y);
        rect.pivot = new Vector2(0.5f, y);
        rect.sizeDelta = new Vector2(0f, 4f);
        return edge;
    }

    private TextMeshProUGUI Text(string name, Transform parent, string content, float size, Color color, Vector2 position)
    {
        var rect = UiFx.Rect(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(820f, size * 1.3f);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = BattleUiSkin.Font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        return text;
    }

    private IEnumerator Run()
    {
        float start = Time.unscaledTime;
        float total = HoldTime + CloseTime;
        bool hit = false;
        while (true)
        {
            float t = Time.unscaledTime - start;
            if (t >= total) break;

            // 띠: 열렸다가 마지막에 닫힌다
            float open = UiFx.EaseOut(Mathf.Clamp01(t / OpenTime));
            float close = Mathf.Clamp01((t - HoldTime) / CloseTime);
            band.sizeDelta = new Vector2(0f, BandHeight * open * (1f - UiFx.EaseInOut(close)));

            // 초상화는 왼쪽에서, 글씨는 오른쪽에서 들어와 살짝 넘쳤다가 자리 잡는다
            float slide = Mathf.Clamp01((t - 0.1f) / 0.4f);
            float settle = slide < 1f ? UiFx.EaseOut(slide) * 1.06f : Mathf.Lerp(1.06f, 1f, Mathf.Clamp01((t - 0.5f) / 0.25f));
            float drift = Mathf.Max(0f, t - HitTime) * 8f;   // 멈춘 뒤에도 함께 아주 천천히 흐른다
            portraitGroup.anchoredPosition = new Vector2(Mathf.Lerp(-1300f, -470f, settle) + drift, 0f);
            textGroup.anchoredPosition = new Vector2(Mathf.Lerp(1200f, 70f, settle) + drift, 0f);

            if (!hit && t >= HitTime)
            {
                hit = true;
                MonsterAbilityUtil.Shake(0.18f);
            }
            float flashK = t < HitTime ? 0f : Mathf.Clamp01(1f - (t - HitTime) / 0.35f);
            flash.color = new Color(1f, 0.95f, 0.85f, 0.35f * flashK);
            float pulse = 0.4f + 0.15f * Mathf.Sin(t * 6f) + 0.4f * flashK;
            glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, pulse);
            glow.rectTransform.localScale = Vector3.one * (1f + 0.25f * flashK);
            group.alpha = 1f - close;
            yield return null;
        }
        Destroy(gameObject);
    }
}
