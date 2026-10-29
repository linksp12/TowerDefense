using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>버튼 확대·흔들림 애니메이션과 쿨타임 안내(버튼 위 도트 판)를 표시합니다.</summary>
internal sealed class SkillButtonFeedback
{
    private static readonly Dictionary<Canvas, CooldownNotice> Notices = new Dictionary<Canvas, CooldownNotice>();
    private readonly MonoBehaviour owner;
    private readonly Transform animatedTransform;
    private readonly Vector3 originalScale;
    private readonly Vector3 originalPosition;
    private Coroutine animation;
    private CooldownNotice currentNotice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetNotices() => Notices.Clear();

    public SkillButtonFeedback(MonoBehaviour owner, Transform animatedTransform)
    {
        this.owner = owner;
        this.animatedTransform = animatedTransform;
        originalScale = animatedTransform.localScale;
        originalPosition = animatedTransform.localPosition;
    }

    public void Punch(float scale, float duration) => Animate(Pulse(scale, Mathf.Max(0.001f, duration * 0.5f)));
    public void Ready() => Animate(Pulse(1.15f, 0.2f));

    private void Animate(IEnumerator routine)
    {
        if (animatedTransform == null) return;
        if (animation != null) owner.StopCoroutine(animation);
        animatedTransform.localScale = originalScale;
        animatedTransform.localPosition = originalPosition;
        animation = owner.StartCoroutine(routine);
    }

    private IEnumerator Pulse(float scale, float duration)
    {
        Vector3 large = originalScale * scale;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            if (animatedTransform == null) yield break;
            animatedTransform.localScale = Vector3.Lerp(originalScale, large, elapsed / duration);
            yield return null;
        }
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            if (animatedTransform == null) yield break;
            animatedTransform.localScale = Vector3.Lerp(large, originalScale, elapsed / duration);
            yield return null;
        }
        if (animatedTransform != null) animatedTransform.localScale = originalScale;
        animation = null;
    }

    // 아직 못 쓰는 스킬을 눌렀을 때 좌우로 짧게 흔든다.
    private IEnumerator Shake()
    {
        for (float elapsed = 0f; elapsed < 0.28f; elapsed += Time.unscaledDeltaTime)
        {
            if (animatedTransform == null) yield break;
            float k = 1f - elapsed / 0.28f;
            animatedTransform.localPosition = originalPosition + new Vector3(Mathf.Sin(elapsed * 70f) * 7f * k, 0f, 0f);
            yield return null;
        }
        if (animatedTransform != null) animatedTransform.localPosition = originalPosition;
        animation = null;
    }

    /// <summary>"화염구 재사용까지 32초" 판을 버튼 위에 띄우고 버튼을 흔든다.</summary>
    public void ShowCooldown(Canvas canvas, TMP_FontAsset font, RectTransform anchor, string skillName, float remaining)
    {
        Animate(Shake());
        if (UISoundManager.Instance != null) UISoundManager.Instance.PlayFail();
        if (canvas == null) return;
        List<Canvas> expired = null;
        foreach (Canvas key in Notices.Keys)
        {
            if (key != null) continue;
            if (expired == null) expired = new List<Canvas>();
            expired.Add(key);
        }
        if (expired != null)
            foreach (Canvas key in expired) Notices.Remove(key);
        if (!Notices.TryGetValue(canvas, out CooldownNotice notice) || notice.Root == null)
        {
            notice = new CooldownNotice(canvas);
            Notices[canvas] = notice;
        }
        currentNotice = notice;
        string time = remaining > 1f ? $"{Mathf.CeilToInt(remaining)}초" : $"{remaining:0.0}초";
        notice.Show(owner, font, anchor,
            $"<color=#FFD678>{skillName}</color>  재사용까지 <color=#FF9A6A>{time}</color>");
    }

    public void Cancel()
    {
        if (animation != null && owner != null) owner.StopCoroutine(animation);
        animation = null;
        if (animatedTransform != null)
        {
            animatedTransform.localScale = originalScale;
            animatedTransform.localPosition = originalPosition;
        }
        currentNotice?.Hide(owner);
    }

    private sealed class CooldownNotice
    {
        public readonly GameObject Root;
        private readonly Canvas canvas;
        private readonly RectTransform rect;
        private readonly CanvasGroup group;
        private readonly TextMeshProUGUI text;
        private readonly Vector3[] corners = new Vector3[4];
        private MonoBehaviour owner;
        private Coroutine routine;

        public CooldownNotice(Canvas canvas)
        {
            this.canvas = canvas;
            Root = new GameObject("SkillCooldownMessage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup));
            Root.transform.SetParent(canvas.transform, false);
            rect = (RectTransform)Root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var plate = Root.GetComponent<Image>();
            plate.raycastTarget = false;
            if (!BattleUiSkin.Skin(plate, BattleUiSkin.Plate, 1f))
                plate.color = new Color(0.08f, 0.06f, 0.05f, 0.95f);
            group = Root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(rect, false);
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(18f, 0f);
            iconRect.sizeDelta = new Vector2(26f, 26f);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = Resources.Load<Sprite>("UI/TowerPanel/icon_hourglass");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = icon.sprite != null;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(rect, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(52f, 0f);
            textRect.offsetMax = new Vector2(-18f, 0f);
            text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = BattleUiSkin.Font;
            text.fontSize = 22f;
            text.color = new Color32(244, 236, 220, 255);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.richText = true;
            text.raycastTarget = false;
            text.outlineWidth = 0.15f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            Root.SetActive(false);
        }

        public void Show(MonoBehaviour requester, TMP_FontAsset font, RectTransform anchor, string message)
        {
            if (owner != null && routine != null) owner.StopCoroutine(routine);
            owner = requester;
            Root.SetActive(true);
            if (font != null) text.font = font;
            text.text = message;
            float width = text.GetPreferredValues(message, 1000f, 40f).x + 72f;
            rect.sizeDelta = new Vector2(width, 54f);
            Root.transform.SetAsLastSibling();

            // 누른 버튼 바로 위(화면 밖으로 나가지 않게)
            var canvasRect = (RectTransform)canvas.transform;
            Vector2 position = Vector2.zero;
            if (anchor != null)
            {
                anchor.GetWorldCorners(corners);
                Vector3 top = canvasRect.InverseTransformPoint((corners[1] + corners[2]) * 0.5f);
                position = new Vector2(top.x, top.y + 46f);
            }
            Rect area = canvasRect.rect;
            position.x = Mathf.Clamp(position.x, area.xMin + width * 0.5f + 8f, area.xMax - width * 0.5f - 8f);
            routine = owner.StartCoroutine(Run(position));
        }

        // 톡 튀어나와 잠깐 머문 뒤 위로 떠오르며 사라진다.
        private IEnumerator Run(Vector2 position)
        {
            const float total = 1.3f;
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < total)
            {
                float t = Time.unscaledTime - start;
                float pop = t < 0.12f ? Mathf.Lerp(0.8f, 1.06f, t / 0.12f) : Mathf.Lerp(1.06f, 1f, Mathf.Clamp01((t - 0.12f) / 0.1f));
                float leave = Mathf.Clamp01((t - (total - 0.35f)) / 0.35f);
                rect.localScale = Vector3.one * pop;
                rect.localPosition = new Vector3(position.x, position.y + 16f * leave, 0f);
                group.alpha = Mathf.Min(1f, t / 0.08f) * (1f - leave);
                yield return null;
            }
            Hide(owner);
        }

        public void Hide(MonoBehaviour requester)
        {
            if (owner != requester) return;
            if (owner != null && routine != null) owner.StopCoroutine(routine);
            if (Root != null) Root.SetActive(false);
            routine = null;
            owner = null;
        }
    }
}
