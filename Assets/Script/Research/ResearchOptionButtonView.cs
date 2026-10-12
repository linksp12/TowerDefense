using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>연구 항목 버튼의 선택 표시와 클릭 전달을 담당합니다.</summary>
public sealed class ResearchOptionButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;
    [SerializeField] private Outline outline;
    [SerializeField] private ButtonEffect buttonEffect;
    private Action clicked;
    private ResearchPalette palette;
    public bool IsConfigured => button != null && label != null && background != null && outline != null && buttonEffect != null;

    public void Initialize(string title, ResearchPalette colors, AudioSource audio, Action onClick)
    {
        label.text = title;
        palette = colors;
        buttonEffect.fallbackAudioSource = audio;
        clicked = onClick;
        button.onClick.RemoveListener(HandleClick);
        button.onClick.AddListener(HandleClick);
    }

    public void Render(bool visible, bool selected)
    {
        gameObject.SetActive(visible);
        background.color = selected ? palette.SelectedBackground : palette.CardBackground;
        outline.effectColor = selected ? palette.Accent : palette.Edge;
    }

    private void HandleClick() => clicked?.Invoke();
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(HandleClick); }
}
