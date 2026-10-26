using UnityEngine;

/// <summary>설치 패널의 추가 카드는 목록에서 한 번만 생성한다. 배치는 씬의 Grid/ScrollRect가 담당한다.</summary>
public sealed class TowerBuildCatalogView : MonoBehaviour
{
    [SerializeField] private TowerBuildManager manager;
    [SerializeField] private RectTransform content;
    [SerializeField] private TowerCardUI cardPrefab;
    [SerializeField] private Stage1TutorialManager tutorial;
    private readonly System.Collections.Generic.List<UnityEngine.UI.Button> buttons =
        new System.Collections.Generic.List<UnityEngine.UI.Button>();
    private bool restricted;

    private void Start()
    {
        if (manager == null || content == null || cardPrefab == null)
        {
            Debug.LogError("TowerBuildCatalogView의 매니저·목록·카드 프리팹 연결이 필요합니다.", this);
            return;
        }
        foreach (var prefab in manager.AdditionalTowerPrefabs)
        {
            if (prefab == null || !prefab.TryGetComponent<TowerAttack>(out var tower)) continue;
            var card = Instantiate(cardPrefab, content);
            card.name = "Card_" + (tower.towerData != null ? tower.towerData.id : prefab.name);
            card.Configure(tower);
            var button = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (button != null) buttons.Add(button);
        }
        ApplyTutorialGate();
    }

    private void Update()
    {
        if ((tutorial != null && tutorial.IsTowerChoiceRestricted) != restricted)
            ApplyTutorialGate();
    }

    private void ApplyTutorialGate()
    {
        restricted = tutorial != null && tutorial.IsTowerChoiceRestricted;
        foreach (var button in buttons) if (button != null) button.interactable = !restricted;
    }
}
