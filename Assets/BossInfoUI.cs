using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BossInfoUI : MonoBehaviour
{
    public static BossInfoUI Instance;

    // =========================================================
    // Boss Info Panel
    // =========================================================
    [Header("Panel")]
    [Tooltip("보스 정보창 전체 Panel")]
    public GameObject bossInfoPanel;

    // =========================================================
    // UI References
    // =========================================================
    [Header("UI")]
    [Tooltip("보스 초상화")]
    public Image portraitImage;

    [Tooltip("보스 이름")]
    public TMP_Text bossNameText;

    [Tooltip("보스 HP Slider")]
    public Slider hpSlider;

    [Tooltip("보스 HP 숫자")]
    public TMP_Text hpText;

    [Tooltip("보스 방어력")]
    public TMP_Text defenseText;

    [Tooltip("보스 마법 저항력")]
    public TMP_Text magicResistanceText;

    // =========================================================
    // Current Selected Boss
    // =========================================================
    private MonsterHealth currentBoss;


    // =========================================================
    // Awake
    // =========================================================
    private void Awake()
    {
        // Singleton
        Instance = this;

        // 게임 시작 시 보스 정보창 숨기기
        if (bossInfoPanel != null)
        {
            bossInfoPanel.SetActive(false);
        }
    }


    // =========================================================
    // Update
    // =========================================================
    private void Update()
    {
        // 선택된 보스가 없으면 종료
        if (currentBoss == null)
            return;

        // 정보창이 없거나 꺼져 있으면 종료
        if (bossInfoPanel == null)
            return;

        if (!bossInfoPanel.activeSelf)
            return;

        // HP 실시간 갱신
        UpdateHP();

        // 보스가 죽으면 정보창 닫기
        if (currentBoss.IsDead)
        {
            HideBossInfo();
        }
    }


    // =========================================================
    // 마우스 클릭 확인
    // =========================================================
    private void LateUpdate()
    {
        // 마우스 왼쪽 버튼 클릭
        if (Input.GetMouseButtonDown(0))
        {
            CheckClickOutsideBoss();
        }
    }


    // =========================================================
    // 보스 이외의 장소 클릭 확인
    // =========================================================
    private void CheckClickOutsideBoss()
    {
        // 보스 정보창이 없으면 종료
        if (bossInfoPanel == null)
            return;

        // 보스 정보창이 꺼져 있으면 종료
        if (!bossInfoPanel.activeSelf)
            return;

        // 현재 선택된 보스가 없으면 종료
        if (currentBoss == null)
            return;


        // =====================================================
        // UI를 클릭한 경우에는 정보창을 닫지 않음
        // =====================================================
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }


        // =====================================================
        // 마우스 위치를 월드 좌표로 변환
        // =====================================================
        Vector2 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);


        // =====================================================
        // 클릭한 위치의 모든 Collider2D 확인
        // =====================================================
        Collider2D[] hits =
            Physics2D.OverlapPointAll(mouseWorldPosition);


        // =====================================================
        // 클릭한 대상이 현재 선택된 보스인지 확인
        // =====================================================
        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            MonsterHealth clickedMonster =
                hit.GetComponentInParent<MonsterHealth>();

            // 현재 선택된 보스를 다시 클릭한 경우
            // 정보창을 그대로 유지
            if (clickedMonster == currentBoss)
            {
                return;
            }
        }


        // =====================================================
        // 보스가 아닌 곳을 클릭했으면 정보창 닫기
        // =====================================================
        HideBossInfo();
    }


    // =========================================================
    // 보스 정보 표시
    // =========================================================
    public void ShowBossInfo(MonsterHealth boss)
    {
        if (boss == null)
            return;

        // 죽은 보스라면 표시하지 않음
        if (boss.IsDead)
            return;

        // 현재 선택된 보스 저장
        currentBoss = boss;


        // =====================================================
        // Panel 켜기
        // =====================================================
        if (bossInfoPanel != null)
        {
            bossInfoPanel.SetActive(true);
        }


        // =====================================================
        // 보스 이름
        // =====================================================
        if (bossNameText != null)
        {
            bossNameText.text = boss.bossName;
        }


        // =====================================================
        // 보스 초상화
        // =====================================================
        if (portraitImage != null)
        {
            portraitImage.sprite = boss.bossPortrait;
        }


        // =====================================================
        // 방어력
        // =====================================================
        if (defenseText != null)
        {
            defenseText.text =
                "방어력 : " + boss.defense;
        }


        // =====================================================
        // 마법 저항력
        // =====================================================
        if (magicResistanceText != null)
        {
            magicResistanceText.text =
                "마법 저항 : " + boss.magicResistance;
        }


        // =====================================================
        // HP
        // =====================================================
        UpdateHP();
    }


    // =========================================================
    // HP 업데이트
    // =========================================================
    private void UpdateHP()
    {
        if (currentBoss == null)
            return;


        // =====================================================
        // HP Slider
        // =====================================================
        if (hpSlider != null)
        {
            hpSlider.maxValue = currentBoss.maxHp;
            hpSlider.value = currentBoss.CurrentHp;
        }


        // =====================================================
        // HP Text
        // =====================================================
        if (hpText != null)
        {
            hpText.text =
                currentBoss.CurrentHp.ToString("N0")
                + " / "
                + currentBoss.maxHp.ToString("N0");
        }
    }


    // =========================================================
    // 정보창 닫기
    // =========================================================
    public void HideBossInfo()
    {
        // 현재 선택된 보스 초기화
        currentBoss = null;


        // Panel 숨기기
        if (bossInfoPanel != null)
        {
            bossInfoPanel.SetActive(false);
        }
    }
}
