using UnityEngine;

public class BossInfoTarget : MonoBehaviour
{
    private MonsterHealth monsterHealth;

    // =========================================================
    // 보스 등장 팝업은 게임 전체에서 한 번만
    // static이므로 골렘이 여러 마리 생성되어도 공유된다.
    // =========================================================
    private static bool entrancePopupShown = false;

    private void Awake()
    {
        // BossClickArea에 붙어 있어도
        // 부모 ForestGolemMonster의 MonsterHealth를 찾는다.
        monsterHealth = GetComponentInParent<MonsterHealth>();
    }

    private void Start()
    {
        // =====================================================
        // 골렘 등장 시 팝업
        // 단, 게임에서 최초 1번만 표시
        // =====================================================

        if (monsterHealth == null)
            return;

        if (!monsterHealth.isBoss)
            return;

        if (monsterHealth.IsDead)
            return;

        if (BossInfoUI.Instance == null)
            return;

        // 이미 등장 팝업을 보여줬다면 아무것도 하지 않는다.
        if (entrancePopupShown)
            return;

        // 등장 팝업을 표시했다고 기록
        entrancePopupShown = true;

        // 보스 등장 팝업 표시
        BossInfoUI.Instance.ShowBossInfo(monsterHealth);
    }

    private void OnMouseDown()
    {
        // =====================================================
        // 골렘 클릭 시 보스 정보창
        // 등장 팝업 횟수와 관계없이 언제든 표시
        // =====================================================

        ShowBossPopup();
    }

    private void ShowBossPopup()
    {
        if (monsterHealth == null)
            return;

        if (!monsterHealth.isBoss)
            return;

        if (monsterHealth.IsDead)
            return;

        if (BossInfoUI.Instance == null)
            return;

        // 클릭할 때는 항상 팝업 표시
        BossInfoUI.Instance.ShowBossInfo(monsterHealth);
    }
}
