using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>대상 카드의 표시와 클릭 전달만 담당합니다. 구매 기록은 읽지 않습니다.</summary>
public sealed class ResearchTargetCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Outline outline;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Image[] rankPips = Array.Empty<Image>();
    [SerializeField] private ButtonEffect buttonEffect;
    private Action clicked;
    private Color accent;
    private ResearchPalette palette;

    public bool IsConfigured => button != null && background != null && outline != null && icon != null &&
        nameLabel != null && statusLabel != null && buttonEffect != null && rankPips != null && Array.TrueForAll(rankPips, x => x != null);

    public void Initialize(string title, Sprite sprite, Color accentColor, ResearchPalette colors, AudioSource audio, Action onClick)
    {
        nameLabel.text = title;
        icon.sprite = sprite;
        icon.color = sprite != null ? Color.white : accentColor;
        accent = accentColor;
        palette = colors;
        statusLabel.color = accent;
        buttonEffect.fallbackAudioSource = audio;
        clicked = onClick;
        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    public void Render(bool selected, string status, int level, int maxLevel)
    {
        background.color = selected ? palette.SelectedBackground : palette.CardBackground;
        outline.effectColor = selected ? accent : palette.Edge;
        statusLabel.text = status;
        for (int i = 0; i < rankPips.Length; i++)
            rankPips[i].color = maxLevel > 0 && (long)level * rankPips.Length >= (long)(i + 1) * maxLevel ? accent : palette.RankOff;
    }

    private void HandleClick() => clicked?.Invoke();
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(HandleClick); }
}
