using TMPro;
using UnityEngine;

/// <summary>능력치 한 행의 표시입니다. 복제할 때 라벨 참조도 함께 복제됩니다.</summary>
public sealed class ResearchStatRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text baseLabel;
    [SerializeField] private TMP_Text currentLabel;
    public bool IsConfigured => nameLabel != null && baseLabel != null && currentLabel != null;

    public void Render(string name, string baseValue, string currentValue, Color unchanged, Color changed)
    {
        gameObject.SetActive(true);
        nameLabel.text = name;
        baseLabel.text = baseValue;
        currentLabel.text = currentValue;
        currentLabel.color = baseValue == currentValue ? unchanged : changed;
    }
}
