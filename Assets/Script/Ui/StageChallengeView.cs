using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 선택 화면의 도전 과제(별) 표시. 표지판 아래 별 3개와, 상세창 아래쪽 줄의 도전 과제 3칸.
/// 기록은 QuestManager에서 읽기만 한다(판정·저장은 전투 결과 때 QuestManager가 맡는다).
/// </summary>
public sealed class StageChallengeView : MonoBehaviour
{
    private readonly List<Image> markerStars = new List<Image>();
    private readonly List<Image> chipStars = new List<Image>();
    private readonly List<TextMeshProUGUI> chipTexts = new List<TextMeshProUGUI>();
    private RectTransform markerRow;
    private RectTransform chipRow;
    private TextMeshProUGUI footerHint;

    /// <summary>표지판에 붙여 한 번 만들고, 진행이 바뀔 때마다 다시 칠한다.</summary>
    public static void Refresh(StageSelectionMarker marker, TextMeshProUGUI detailStatus, string stageId, bool unlocked)
    {
        if (marker == null)
            return;
        QuestManager quests = QuestManager.Instance;
        List<QuestData> challenges = quests != null ? quests.ChallengesFor(stageId) : null;
        var view = marker.GetComponent<StageChallengeView>();
        if (challenges == null || challenges.Count < 3)
        {
            // 퀘스트 관리자가 없으면(선택 화면을 바로 실행) 표시하지 않는다.
            if (view != null) view.SetVisible(false);
            return;
        }
        if (view == null)
        {
            view = marker.gameObject.AddComponent<StageChallengeView>();
            view.BuildMarkerRow((RectTransform)marker.transform);
            view.BuildChipRow(detailStatus);
        }
        view.SetVisible(true);
        view.Paint(quests, challenges, unlocked);
    }

    private void SetVisible(bool visible)
    {
        if (markerRow != null) markerRow.gameObject.SetActive(visible);
        if (chipRow != null) chipRow.gameObject.SetActive(visible);
        if (footerHint != null) footerHint.gameObject.SetActive(!visible);
    }

    // 표지판 아래 가장자리에 걸친 작은 별 3개
    private void BuildMarkerRow(RectTransform marker)
    {
        markerRow = NewRect("ChallengeStars", marker);
        markerRow.anchorMin = markerRow.anchorMax = new Vector2(0.5f, 0f);
        markerRow.pivot = new Vector2(0.5f, 0.5f);
        markerRow.anchoredPosition = new Vector2(0f, -4f);
        markerRow.sizeDelta = new Vector2(110f, 34f);
        for (int i = 0; i < 3; i++)
        {
            Image star = NewImage("Star" + (i + 1), markerRow);
            star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            star.rectTransform.sizeDelta = new Vector2(i == 1 ? 34f : 28f, i == 1 ? 34f : 28f);
            star.rectTransform.anchoredPosition = new Vector2((i - 1) * 34f, i == 1 ? 3f : 0f);
            markerStars.Add(star);
        }
    }

    // 상세창 아래쪽 "준비가 되었다면…" 문구 자리를 도전 과제 3칸으로 바꾼다.
    private void BuildChipRow(TextMeshProUGUI detailStatus)
    {
        if (detailStatus == null)
            return;
        var details = detailStatus.GetComponentInParent<StageSelectionDetailsUI>(true);
        if (details == null)
            return;
        foreach (var text in details.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.text.StartsWith("준비가 되었다면"))
            {
                footerHint = text;
                break;
            }
        }
        if (footerHint == null)
            return;

        // 안내 문구의 왼쪽 끝·세로 가운데에 맞춘다(부모 기준 좌표).
        var hintRect = footerHint.rectTransform;
        chipRow = NewRect("ChallengeRow", hintRect.parent);
        chipRow.anchorMin = chipRow.anchorMax = new Vector2(0.5f, 0.5f);
        chipRow.pivot = new Vector2(0f, 0.5f);
        chipRow.sizeDelta = new Vector2(980f, 44f);
        Vector3 local = hintRect.localPosition;
        chipRow.localPosition = new Vector3(
            local.x - hintRect.pivot.x * hintRect.rect.width,
            local.y + (0.5f - hintRect.pivot.y) * hintRect.rect.height, 0f);

        var title = NewText("Title", chipRow, "도전 과제", 22f, new Color32(255, 214, 102, 255));
        Place(title.rectTransform, 0f, 110f);
        for (int i = 0; i < 3; i++)
        {
            float x = 112f + i * 262f;
            Image star = NewImage("Star" + (i + 1), chipRow);
            star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            star.rectTransform.pivot = new Vector2(0f, 0.5f);
            star.rectTransform.anchoredPosition = new Vector2(x, 0f);
            star.rectTransform.sizeDelta = new Vector2(30f, 30f);
            chipStars.Add(star);
            var text = NewText("Text" + (i + 1), chipRow, "", 19f, Color.white);
            text.richText = true;
            Place(text.rectTransform, x + 36f, 246f);
            chipTexts.Add(text);
        }
    }

    private void Paint(QuestManager quests, List<QuestData> challenges, bool unlocked)
    {
        if (markerRow != null)
            markerRow.gameObject.SetActive(unlocked);
        for (int i = 0; i < 3; i++)
        {
            bool earned = quests.IsChallengeEarned(challenges[i]);
            if (i < markerStars.Count)
            {
                markerStars[i].sprite = earned ? BattleUiSkin.StarFull : BattleUiSkin.StarEmpty;
                markerStars[i].color = earned ? Color.white : new Color(1f, 1f, 1f, 0.75f);
            }
            if (i < chipStars.Count)
            {
                chipStars[i].sprite = earned ? BattleUiSkin.StarFull : BattleUiSkin.StarEmpty;
                chipStars[i].color = earned ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                string condition = challenges[i].ConditionText;
                chipTexts[i].text = earned
                    ? $"<color=#FFE9B0>{condition}</color>  <color=#8FA0B4>완료</color>"
                    : $"<color=#D8DEE8>{condition}</color>  <color=#FFB25C>+{challenges[i].rewardAmount}P</color>";
            }
        }
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image NewImage(string name, Transform parent)
    {
        var image = NewRect(name, parent).gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string value, float size, Color color)
    {
        var text = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = BattleUiSkin.Font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, float x, float width)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, 40f);
    }
}
