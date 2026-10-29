using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 스테이지(씬 또는 스테이지 오브젝트)에 붙여서 이 스테이지의 환경을 지정한다.
// 진입 시 환경을 적용하고, 종료(파괴) 시 해제한다.
public class StageEnvironment : MonoBehaviour
{
    [SerializeField] private EnvironmentType environmentType = EnvironmentType.Default;

    public EnvironmentType Type => environmentType;

    private void Start()
    {
        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.Enter(environmentType);
            StartCoroutine(ShowModifierBanner(EnvironmentManager.Instance.Current));
        }
    }

    private void OnDestroy()
    {
        if (EnvironmentManager.Instance != null)
            EnvironmentManager.Instance.Exit();
    }

    // 전투 시작 안내: "설원 지대 — 불 속성(캐논·레이저) 피해 +20% · 얼음 속성(마법) 피해 -20%"
    private IEnumerator ShowModifierBanner(EnvironmentData data)
    {
        if (data == null || data.modifiers == null || data.modifiers.Length == 0)
            yield break;
        yield return null;   // 보스전 앞 대사가 먼저 뜰 수 있게 한 프레임 기다린다
        while (StageDialogue.IsOpen)
            yield return null;
        yield return new WaitForSeconds(1.2f);

        var parts = new List<string>();
        foreach (EnvironmentData.ElementModifier modifier in data.modifiers)
        {
            if (modifier.element == ElementType.None || Mathf.Approximately(modifier.rate, 0f))
                continue;
            string towers = TowersOf(modifier.element);
            string color = modifier.rate > 0f ? "#8CE878" : "#FF8A6A";
            parts.Add($"{ElementName(modifier.element)} 속성{(towers.Length > 0 ? $"({towers})" : "")} " +
                      $"<color={color}>{(modifier.rate > 0f ? "+" : "")}{modifier.rate * 100f:0}%</color>");
        }
        if (parts.Count == 0)
            yield break;
        string title = data.type == EnvironmentType.Ice ? "<color=#9FD8FF>설원 지대</color>"
            : data.type == EnvironmentType.Fire ? "<color=#FF8A4C>화산 지대</color>" : data.displayName;
        var ui = FindAnyObjectByType<UIManager>();
        if (ui != null)
            ui.ShowHazardBanner(title, string.Join("  ·  ", parts));
    }

    public static string ElementName(ElementType element) =>
        element == ElementType.Fire ? "불" : element == ElementType.Ice ? "얼음" : "";

    // 이 전투에서 지을 수 있는 타워 가운데 해당 속성 타워 이름(“ 타워” 생략)
    private static string TowersOf(ElementType element)
    {
        if (TowerBuildManager.Instance == null)
            return "";
        var names = new List<string>();
        foreach (GameObject prefab in TowerBuildManager.Instance.GetBuildOptions())
        {
            var tower = prefab.GetComponent<TowerAttack>();
            if (tower != null && tower.towerData != null && tower.towerData.element == element)
                names.Add(tower.towerData.towerName.Replace(" 타워", ""));
        }
        return string.Join("·", names);
    }
}
