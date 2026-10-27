using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>버튼 확대 애니메이션과 쿨타임 안내를 표시합니다.</summary>
internal sealed class SkillButtonFeedback
{
    private static readonly Dictionary<Canvas, CooldownNotice> Notices = new Dictionary<Canvas, CooldownNotice>();
    private readonly MonoBehaviour owner;
    private readonly Transform animatedTransform;
    private readonly Vector3 originalScale;
    private Coroutine animation;
    private CooldownNotice currentNotice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetNotices() => Notices.Clear();

    public SkillButtonFeedback(MonoBehaviour owner, Transform animatedTransform)
    {
        this.owner = owner;
        this.animatedTransform = animatedTransform;
        originalScale = animatedTransform.localScale;
    }

    public void Punch(float scale, float duration) => Animate(scale, Mathf.Max(0.001f, duration * 0.5f));
    public void Ready() => Animate(1.15f, 0.2f);

    private void Animate(float scale, float halfDuration)
    {
        if (animatedTransform == null) return;
        if (animation != null) owner.StopCoroutine(animation);
        animatedTransform.localScale = originalScale;
        animation = owner.StartCoroutine(Pulse(scale, halfDuration));
    }

    private IEnumerator Pulse(float scale, float duration)
    {
        Vector3 large = originalScale * scale;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (animatedTransform == null) yield break;
            animatedTransform.localScale = Vector3.Lerp(originalScale, large, elapsed / duration);
            yield return null;
        }
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (animatedTransform == null) yield break;
            animatedTransform.localScale = Vector3.Lerp(large, originalScale, elapsed / duration);
            yield return null;
        }
        if (animatedTransform != null) animatedTransform.localScale = originalScale;
        animation = null;
    }

    public void ShowCooldown(Canvas canvas, TMP_FontAsset font)
    {
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
        if (!Notices.TryGetValue(canvas, out CooldownNotice notice) || notice.Text == null)
        {
            notice = new CooldownNotice(canvas);
            Notices[canvas] = notice;
        }
        currentNotice = notice;
        notice.Show(owner, font);
    }

    public void Cancel()
    {
        if (animation != null && owner != null) owner.StopCoroutine(animation);
        animation = null;
        if (animatedTransform != null) animatedTransform.localScale = originalScale;
        currentNotice?.Hide(owner);
    }

    private sealed class CooldownNotice
    {
        public readonly TextMeshProUGUI Text;
        private MonoBehaviour owner;
        private Coroutine hideRoutine;

        public CooldownNotice(Canvas canvas)
        {
            GameObject root = new GameObject("SkillCooldownMessage", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(700f, 100f);
            Text = root.GetComponent<TextMeshProUGUI>();
            Text.fontSize = 42f;
            Text.alignment = TextAlignmentOptions.Center;
            Text.color = new Color(1f, 0.82f, 0.25f, 1f);
            Text.outlineColor = Color.black;
            Text.outlineWidth = 0.22f;
            Text.overflowMode = TextOverflowModes.Overflow;
            Text.raycastTarget = false;
            root.SetActive(false);
        }

        public void Show(MonoBehaviour requester, TMP_FontAsset font)
        {
            if (owner != null && hideRoutine != null) owner.StopCoroutine(hideRoutine);
            owner = requester;
            Text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            Text.text = "스킬 쿨타임입니다";
            Text.gameObject.SetActive(true);
            Text.transform.SetAsLastSibling();
            hideRoutine = owner.StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            Hide(owner);
        }

        public void Hide(MonoBehaviour requester)
        {
            if (owner != requester) return;
            if (owner != null && hideRoutine != null) owner.StopCoroutine(hideRoutine);
            if (Text != null) Text.gameObject.SetActive(false);
            hideRoutine = null;
            owner = null;
        }
    }
}
