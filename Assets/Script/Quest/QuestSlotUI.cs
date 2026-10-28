using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게시판의 의뢰 한 장. 양피지 의뢰서 위에 스테이지 봉인, 제목·설명, 조건 꼬리표, 보상, 상태 버튼을 그린다.
/// 대상 스테이지가 잠겨 있으면 의뢰도 잠기고, 스테이지가 열리면 수락할 수 있다.
/// </summary>
public class QuestSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button rewardButton;
    [SerializeField] private Image buttonFill;
    [SerializeField] private Image stateAccent;
    [SerializeField] private CanvasGroup canvasGroup;

    private static readonly Color Ink = new Color32(86, 58, 32, 255);
    private static readonly Color InkSoft = new Color32(124, 92, 58, 255);
    private static readonly Color DarkLabel = new Color32(46, 30, 16, 255);
    private static readonly Color LightLabel = new Color32(240, 230, 208, 255);

    private string currentQuestId;
    private QuestState currentState;
    private bool currentLocked;

    // 도트 의뢰서(처음 Setup 때 한 번 만든다)
    private RectTransform card;
    private Image note, seal, sealLock, stamp, buttonImage, buttonLock, gem;
    private TextMeshProUGUI sealText, title, description, rewardAmount, rewardLabel, actionLabel, stampText;
    private RectTransform tagRow;
    private readonly List<GameObject> tags = new List<GameObject>();

    public void Setup(QuestData data, QuestState state, bool canSave) => Setup(data, state, canSave, false);

    public void Setup(QuestData data, QuestState state, bool canSave, bool locked)
    {
        currentQuestId = data.questId;
        currentState = state;
        currentLocked = locked && state == QuestState.None;
        BuildCard();

        string stageLabel = LastOfTheTower.Progression.StageIds.GetLabel(data.targetStageId) ?? "";
        title.text = string.IsNullOrWhiteSpace(data.questTitle) ? data.questDescription : data.questTitle;
        description.text = currentLocked ? $"{stageLabel} 해금 시 열립니다" : data.questDescription;
        rewardAmount.text = "+" + data.rewardAmount;
        sealText.text = ShortStage(data.targetStageId);
        sealText.gameObject.SetActive(!currentLocked);
        seal.sprite = TowerPanelArt.Sprite(currentLocked ? "quest_seal_locked" : "quest_seal");
        sealLock.gameObject.SetActive(currentLocked);
        BuildTags(data, stageLabel);

        string label, sprite;
        Color labelColor;
        bool interactable;
        switch (state)
        {
            case QuestState.InProgress:
                label = "진행 중"; sprite = "quest_btn_stone"; labelColor = DarkLabel; interactable = false;
                break;
            case QuestState.Completed:
                label = "보상 받기"; sprite = "quest_btn_green"; labelColor = DarkLabel; interactable = true;
                break;
            case QuestState.Rewarded:
                label = "완료"; sprite = "quest_btn_dark"; labelColor = new Color32(170, 160, 140, 255); interactable = false;
                break;
            default:
                label = currentLocked ? "잠김" : "수락";
                sprite = currentLocked ? "quest_btn_dark" : "quest_btn_gold";
                labelColor = currentLocked ? new Color32(150, 140, 128, 255) : DarkLabel;
                interactable = !currentLocked;
                break;
        }
        buttonImage.sprite = TowerPanelArt.Sprite(sprite);
        actionLabel.text = label;
        actionLabel.color = labelColor;
        buttonLock.gameObject.SetActive(currentLocked);
        actionLabel.rectTransform.anchoredPosition = new Vector2((currentLocked ? 6f : 0f) * TowerPanelArt.Dot, 0f);
        rewardButton.interactable = interactable && canSave;

        bool done = state == QuestState.Rewarded;
        stamp.gameObject.SetActive(done);
        note.color = currentLocked ? new Color(0.72f, 0.7f, 0.68f, 1f) : done ? new Color(0.9f, 0.88f, 0.84f, 1f) : Color.white;
        if (canvasGroup != null)
            canvasGroup.alpha = done ? 0.8f : currentLocked ? 0.85f : 1f;
    }

    private static string ShortStage(string stageId)
    {
        string label = LastOfTheTower.Progression.StageIds.GetLabel(stageId) ?? "";
        if (label.StartsWith("하드코어"))
            return "H" + label.Replace("하드코어", "").Trim();
        return label.Replace("스테이지", "");
    }

    // 조건을 짧은 꼬리표로(스테이지, 체력, 타워 수, 타워 제한)
    private void BuildTags(QuestData data, string stageLabel)
    {
        foreach (GameObject tag in tags) Destroy(tag);
        tags.Clear();
        var texts = new List<string> { stageLabel };
        if (data.noBaseDamage) texts.Add("기지 무피해");
        else if (data.minRemainingBaseHealth > 0) texts.Add($"기지 체력 {data.minRemainingBaseHealth}+");
        if (data.maxTowers > 0) texts.Add($"타워 {data.maxTowers}개 이하");
        if (data.bannedTowerIds != null && data.bannedTowerIds.Length > 0)
            texts.Add(data.bannedTowerIds.Length >= 4 ? AllowedOnly(data.bannedTowerIds) : BannedText(data.bannedTowerIds));

        float x = 0f;
        foreach (string text in texts)
        {
            var label = TowerPanelArt.Text("Tag", tagRow, 0f, 0f, 10f, 9f, 14f, LightLabel, TextAlignmentOptions.Center, false);
            label.text = text;
            float width = label.GetPreferredValues(text).x / TowerPanelArt.Dot + 6f;
            var tag = TowerPanelArt.Picture("TagBack", tagRow, x, 0f, width, 9f, false);
            tag.sprite = TowerPanelArt.Sprite("quest_tag");
            tag.type = Image.Type.Sliced;
            tag.pixelsPerUnitMultiplier = 1f / TowerPanelArt.Dot;
            label.transform.SetParent(tag.transform, false);
            TowerPanelArt.Place(label.rectTransform, 0f, 0f, width, 9f);
            tags.Add(tag.gameObject);
            x += width + 3f;
        }
    }

    private static readonly string[] AllTowerIds =
        { "arrow_tower", "cannon_tower", "magic_tower", "laser_tower", "sniper_tower", "shock_tower" };

    private static string TowerShortName(string id) => id switch
    {
        "arrow_tower" => "화살", "cannon_tower" => "캐논", "magic_tower" => "마법",
        "laser_tower" => "레이저", "sniper_tower" => "저격", "shock_tower" => "감전", _ => id
    };

    private static string AllowedOnly(string[] banned)
    {
        var allowed = new List<string>();
        foreach (string id in AllTowerIds)
            if (System.Array.IndexOf(banned, id) < 0) allowed.Add(TowerShortName(id));
        return string.Join("·", allowed) + " 타워만";
    }

    private static string BannedText(string[] banned)
    {
        var names = new List<string>();
        foreach (string id in banned) names.Add(TowerShortName(id));
        return string.Join("·", names) + " 금지";
    }

    private void BuildCard()
    {
        if (card != null)
            return;
        // 기존 가죽 띠 모양은 숨기고 같은 자리에 도트 의뢰서를 그린다.
        var rootImage = GetComponent<Image>();
        if (rootImage != null)
            rootImage.color = new Color(1f, 1f, 1f, 0f);
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        card = TowerPanelArt.Group("Card", transform, 0f, 0f, 312f, 41f);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;

        note = TowerPanelArt.Image("Note", card, "quest_note", 0f, 0f);
        seal = TowerPanelArt.Image("Seal", card, "quest_seal", 8f, 7.5f);
        sealText = TowerPanelArt.Text("Stage", seal.transform, 0f, 0f, 26f, 26f, 22f, Color.white, TextAlignmentOptions.Center);
        sealLock = TowerPanelArt.Image("Lock", seal.transform, "icon_lock", 8.5f, 7.5f);

        title = TowerPanelArt.Text("Title", card, 40f, 4.5f, 170f, 11f, 26f, Ink, TextAlignmentOptions.Left, false);
        description = TowerPanelArt.Text("Description", card, 40f, 15.5f, 168f, 9f, 18f, InkSoft, TextAlignmentOptions.Left, false);
        // 긴 조건 문장은 칸 안에 맞게 줄여 보여 준다.
        description.enableAutoSizing = true;
        description.fontSizeMin = 13f;
        description.fontSizeMax = description.fontSize;
        tagRow = TowerPanelArt.Group("Tags", card, 40f, 27f, 170f, 9f);

        gem = TowerPanelArt.Picture("Gem", card, 214f, 8f, 12f, 12f);
        gem.sprite = FindOldRewardIcon();
        rewardAmount = TowerPanelArt.Text("Amount", card, 228f, 6f, 20f, 14f, 30f, Ink, TextAlignmentOptions.Left, false);
        rewardLabel = TowerPanelArt.Text("RewardLabel", card, 212f, 23f, 36f, 8f, 14f, InkSoft, TextAlignmentOptions.Center, false);
        rewardLabel.text = "스킬 포인트";

        buttonImage = TowerPanelArt.Image("Action", card, "quest_btn_gold", 248f, 11.5f);
        rewardButton = TowerPanelArt.MakeButton(buttonImage);
        rewardButton.onClick.AddListener(OnClickRewardButton);
        actionLabel = TowerPanelArt.Text("Label", buttonImage.transform, 0f, 0f, 58f, 17f, 22f, DarkLabel, TextAlignmentOptions.Center, false);
        buttonLock = TowerPanelArt.Image("Lock", buttonImage.transform, "icon_lock", 9f, 4f);

        stamp = TowerPanelArt.Image("Stamp", card, "quest_stamp", 160f, 11f);
        stamp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        stamp.rectTransform.anchoredPosition += new Vector2(22f, -9f) * TowerPanelArt.Dot;
        stamp.rectTransform.localEulerAngles = new Vector3(0f, 0f, 10f);
        stampText = TowerPanelArt.Text("Text", stamp.transform, 0f, 0f, 44f, 18f, 26f, new Color32(200, 44, 38, 235), TextAlignmentOptions.Center, false);
        stampText.text = "완료";

        // 예전 연결은 새 그림으로 바꿔 둔다(다른 코드가 읽어도 맞게).
        buttonText = actionLabel;
        titleText = title;
        descriptionText = description;
        rewardText = rewardAmount;
    }

    private Sprite FindOldRewardIcon()
    {
        foreach (var image in GetComponentsInChildren<Image>(true))
            if (image.sprite != null && image.sprite.name.Contains("SkillPoint"))
                return image.sprite;
        return TowerPanelArt.Sprite("dia_a");
    }

    // 버튼 하나가 상태에 따라 수락 또는 보상 받기를 처리한다.
    public void OnClickRewardButton()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null || rewardButton == null || !rewardButton.interactable || currentLocked)
            return;

        // 처리 중 연속 클릭을 막는다. 성공하면 게시판이 새 상태로 다시 그린다.
        rewardButton.interactable = false;
        bool handled = currentState == QuestState.None ? manager.AcceptQuest(currentQuestId) :
            currentState == QuestState.Completed && manager.ClaimReward(currentQuestId);

        if (handled && AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
        if (!handled)
            rewardButton.interactable = true;
    }
}
