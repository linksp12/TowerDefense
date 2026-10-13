using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 화면에서 런타임으로 만드는 UI가 같은 픽셀 스타일을 쓰도록 스프라이트·폰트를 한곳에서 제공합니다.
/// 스프라이트는 Resources/UI/Plaza·UI/Battle 에 있으며 없으면 기존 단색 표시로 대체합니다.
/// </summary>
public static class BattleUiSkin
{
    public const float PixelScale = 4f / 3f;

    private static Sprite frame;
    private static Sprite plate;
    private static Sprite darkButton;
    private static Sprite goldButton;
    private static Sprite inset;
    private static Sprite starFull;
    private static Sprite starEmpty;
    private static Sprite coin;
    private static Sprite banner;
    private static Sprite hpFrame;
    private static Sprite hpFill;
    private static TMP_FontAsset font;

    public static Sprite Frame => frame != null ? frame : frame = Resources.Load<Sprite>("UI/Plaza/plaza_ui_frame");
    public static Sprite Plate => plate != null ? plate : plate = Resources.Load<Sprite>("UI/Plaza/plaza_ui_plate");
    public static Sprite DarkButton => darkButton != null ? darkButton : darkButton = Resources.Load<Sprite>("UI/Plaza/plaza_ui_button_dark");
    public static Sprite Inset => inset != null ? inset : inset = Resources.Load<Sprite>("UI/Plaza/plaza_ui_inset");
    public static Sprite StarFull => starFull != null ? starFull : starFull = Resources.Load<Sprite>("UI/Battle/battle_star_full");
    public static Sprite StarEmpty => starEmpty != null ? starEmpty : starEmpty = Resources.Load<Sprite>("UI/Battle/battle_star_empty");
    public static Sprite Coin => coin != null ? coin : coin = Resources.Load<Sprite>("UI/Battle/battle_coin");
    public static Sprite GoldButton => goldButton != null ? goldButton : goldButton = Resources.Load<Sprite>("UI/Plaza/plaza_ui_button_gold");
    public static Sprite Banner => banner != null ? banner : banner = Resources.Load<Sprite>("UI/Battle/battle_banner");
    public static Sprite HpFrame => hpFrame != null ? hpFrame : hpFrame = Resources.Load<Sprite>("UI/Battle/battle_hp_frame");
    public static Sprite HpFill => hpFill != null ? hpFill : hpFill = Resources.Load<Sprite>("UI/Battle/battle_hp_fill");

    // 씬에 배치된 한글 폰트를 찾아 재사용한다. (전투 씬은 모두 Maplestory Bold SDF 사용)
    public static TMP_FontAsset Font
    {
        get
        {
            if (font != null)
                return font;
            foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            {
                if (text.font != null && text.font.name == "Maplestory Bold SDF")
                    return font = text.font;
            }
            return TMP_Settings.defaultFontAsset;
        }
    }

    public static bool Skin(Image image, Sprite sprite, float pixelScale)
    {
        if (image == null || sprite == null)
            return false;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = pixelScale;
        image.color = Color.white;
        return true;
    }

    // ──────── 몬스터 체력바 ────────

    private static readonly Color HpHigh = new Color32(111, 211, 106, 255);
    private static readonly Color HpMid = new Color32(242, 200, 75, 255);
    private static readonly Color HpLow = new Color32(229, 83, 61, 255);

    /// <summary>
    /// 몬스터 프리팹의 기본 Slider 체력바를 픽셀 테두리·명암 채움으로 바꾸고 남은 비율에 따라 색을 바꾼다.
    /// </summary>
    public static void ApplyMonsterHpBar(Slider slider)
    {
        if (slider == null || HpFrame == null || HpFill == null)
            return;

        var background = slider.transform.Find("Background")?.GetComponent<Image>();
        var fillArea = slider.fillRect != null ? slider.fillRect.parent as RectTransform : null;
        var fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (background == null || fillArea == null || fill == null)
            return;

        // 월드 캔버스(가로 200) 막대를 조금 두껍게 하고 테두리 8 안쪽에 채움을 맞춘다.
        if (slider.GetComponentInParent<Canvas>() is Canvas barCanvas &&
            barCanvas.transform is RectTransform canvasRect && canvasRect.sizeDelta.y < 40f)
            canvasRect.sizeDelta = new Vector2(canvasRect.sizeDelta.x, 40f);
        Skin(background, HpFrame, 1f);
        Skin(fill, HpFill, 2f);
        fillArea.offsetMin = new Vector2(8f, 8f);
        fillArea.offsetMax = new Vector2(-8f, -8f);

        // 몬스터마다 Start에서 한 번만 호출된다.
        slider.onValueChanged.AddListener(_ => UpdateHpColor(slider, fill));
        UpdateHpColor(slider, fill);
    }

    private static void UpdateHpColor(Slider slider, Image fill)
    {
        if (slider == null || fill == null)
            return;
        float ratio = slider.maxValue > 0f ? slider.value / slider.maxValue : 0f;
        fill.color = ratio > 0.5f ? HpHigh : ratio > 0.25f ? HpMid : HpLow;
    }
}
