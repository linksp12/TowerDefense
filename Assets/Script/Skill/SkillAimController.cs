using UnityEngine;
using UnityEngine.EventSystems;

public class SkillAimController : MonoBehaviour
{
    public static SkillAimController Instance;

    // =========================================================
    // 마법진 표시 오브젝트
    // =========================================================

    [Header("마법진 표시 오브젝트")]
    [Tooltip("MagicCircleVisual을 연결하세요.")]
    public GameObject magicCircleObject;


    // =========================================================
    // 마법진 이미지
    // =========================================================

    [Header("마법진 이미지")]
    public Sprite fireMagicCircle;
    public Sprite iceMagicCircle;
    public Sprite lightningMagicCircle;


    // =========================================================
    // 마법진 크기
    // =========================================================

    [Header("마법진 크기")]
    [Tooltip("전체 마법진 크기")]
    public float magicCircleScale = 1f;

    [Tooltip("불 마법진 크기 보정")]
    public float fireCircleScaleMultiplier = 1f;

    [Tooltip("얼음 마법진 크기 보정")]
    public float iceCircleScaleMultiplier = 1f;

    [Tooltip("번개 마법진 크기 보정")]
    public float lightningCircleScaleMultiplier = 1f;


    // =========================================================
    // 마우스 이동
    // =========================================================

    [Header("마우스 이동")]
    [Tooltip("마법진이 마우스를 따라가는 속도")]
    public float followSpeed = 15f;

    [Tooltip("카메라와 월드 위치 계산용 거리")]
    public float zDistanceFromCamera = 10f;


    // =========================================================
    // 회전
    // =========================================================

    [Header("마법진 회전")]
    [Tooltip("마법진 회전 사용")]
    public bool rotateMagicCircle = true;

    [Tooltip("초당 회전 각도")]
    public float rotationSpeed = 35f;


    // =========================================================
    // 펄스
    // =========================================================

    [Header("마법진 펄스 효과")]
    [Tooltip("마법진이 살짝 커졌다 작아집니다.")]
    public bool usePulseEffect = true;

    [Tooltip("펄스 크기")]
    [Range(0f, 0.3f)]
    public float pulseAmount = 0.04f;

    [Tooltip("펄스 속도")]
    public float pulseSpeed = 3f;


    // =========================================================
    // 몬스터 Layer
    // =========================================================

    [Header("몬스터 감지")]
    [Tooltip("몬스터가 사용하는 Layer")]
    public LayerMask monsterLayer;


    // =========================================================
    // 범위 내 몬스터 강조
    // =========================================================

    [Header("범위 내 몬스터 강조")]
    public bool highlightWhenEnemyInside = true;

    [Tooltip("평소 마법진 투명도")]
    [Range(0f, 1f)]
    public float normalAlpha = 0.30f;

    [Tooltip("몬스터가 범위 안에 있을 때 투명도")]
    [Range(0f, 1f)]
    public float enemyHighlightAlpha = 0.45f;

    [Tooltip("밝기 변화 속도")]
    public float highlightSpeed = 8f;


    // =========================================================
    // 내부 변수
    // =========================================================

    private Camera mainCamera;

    private SpriteRenderer spriteRenderer;

    private bool isAiming = false;

    private string selectedSkillName = "";

    // 마우스가 향하는 위치
    private Vector3 targetPosition;

    // 마법진 기본 크기
    private Vector3 originalScale;

    // 실제 공격 반경
    // 스킬 선택 순간에 계산해서 고정
    private float currentSkillRadius = 0f;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        Instance = this;

        mainCamera = Camera.main;

        FindSpriteRenderer();


        if (magicCircleObject != null)
        {
            magicCircleObject.SetActive(false);

            originalScale =
                magicCircleObject.transform.localScale;
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (spriteRenderer != null)
        {
            SetMagicCircleAlpha(
                normalAlpha
            );
        }
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isAiming)
            return;


        // -----------------------------------------------------
        // 마우스 위치 갱신
        // -----------------------------------------------------

        UpdateTargetPosition();


        // -----------------------------------------------------
        // 마우스를 따라 이동
        // -----------------------------------------------------

        FollowMouseSmooth();


        // -----------------------------------------------------
        // 회전
        // -----------------------------------------------------

        RotateMagicCircle();


        // -----------------------------------------------------
        // 펄스
        // -----------------------------------------------------

        PulseMagicCircle();


        // -----------------------------------------------------
        // 범위 안 몬스터 확인
        // -----------------------------------------------------

        UpdateEnemyHighlight();


        // -----------------------------------------------------
        // ESC 취소
        // -----------------------------------------------------

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAiming();
            return;
        }


        // -----------------------------------------------------
        // 우클릭 취소
        // -----------------------------------------------------

        if (Input.GetMouseButtonDown(1))
        {
            CancelAiming();
            return;
        }


        // -----------------------------------------------------
        // ★ 좌클릭 = 현재 마법진 위치에 스킬 발동
        // -----------------------------------------------------

        if (Input.GetMouseButtonDown(0))
        {
            TryUseSkill();
        }
    }


    // =========================================================
    // SpriteRenderer 찾기
    // =========================================================

    private void FindSpriteRenderer()
    {
        if (magicCircleObject == null)
            return;


        // 자기 자신
        spriteRenderer =
            magicCircleObject.GetComponent<SpriteRenderer>();


        // 자식까지 검색
        if (spriteRenderer == null)
        {
            spriteRenderer =
                magicCircleObject.GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }
    }


    // =========================================================
    // 스킬 조준 시작
    // =========================================================

    public void StartAiming(
        string skillName
    )
    {
        Debug.Log(
            "StartAiming 실행 : " +
            skillName
        );


        // -----------------------------------------------------
        // Magic Circle 확인
        // -----------------------------------------------------

        if (magicCircleObject == null)
        {
            Debug.LogError(
                "SkillAimController : " +
                "Magic Circle Object가 연결되지 않았습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // SkillManager 확인
        // -----------------------------------------------------

        if (SkillManager.Instance == null)
        {
            Debug.LogError(
                "SkillManager가 씬에 없습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // 쿨타임 확인
        // -----------------------------------------------------

        if (
            !SkillManager.Instance.CanUseSkill(
                skillName
            )
        )
        {
            Debug.Log(
                skillName +
                " 스킬은 현재 쿨타임 중입니다."
            );

            return;
        }


        // -----------------------------------------------------
        // Camera 확인
        // -----------------------------------------------------

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }


        // -----------------------------------------------------
        // SpriteRenderer 확인
        // -----------------------------------------------------

        if (spriteRenderer == null)
        {
            FindSpriteRenderer();
        }


        if (spriteRenderer == null)
        {
            Debug.LogError(
                "MagicCircleVisual에서 " +
                "SpriteRenderer를 찾을 수 없습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // 스킬 이름 저장
        // -----------------------------------------------------

        selectedSkillName =
            skillName;


        // -----------------------------------------------------
        // 스킬별 마법진 이미지 설정
        // -----------------------------------------------------

        if (
            !SetMagicCircle(
                skillName
            )
        )
        {
            selectedSkillName = "";
            return;
        }


        // -----------------------------------------------------
        // 마법진 표시
        // -----------------------------------------------------

        magicCircleObject.SetActive(true);

        spriteRenderer.enabled = true;


        // -----------------------------------------------------
        // 마법진 크기 설정
        // -----------------------------------------------------

        SetMagicCircleSize(
            skillName
        );


        // -----------------------------------------------------
        // ★ 실제 공격 반경 저장
        // -----------------------------------------------------

        currentSkillRadius =
            CalculateBaseMagicCircleRadius();


        // -----------------------------------------------------
        // 마우스 위치
        // -----------------------------------------------------

        UpdateTargetPosition();


        // 시작할 때 마우스 위치에 바로 배치
        magicCircleObject.transform.position =
            targetPosition;


        // -----------------------------------------------------
        // 회전 초기화
        // -----------------------------------------------------

        magicCircleObject.transform.rotation =
            Quaternion.identity;


        // -----------------------------------------------------
        // 투명도 초기화
        // -----------------------------------------------------

        SetMagicCircleAlpha(
            normalAlpha
        );


        // -----------------------------------------------------
        // 조준 상태
        // -----------------------------------------------------

        isAiming = true;


        Debug.Log(
            "스킬 선택됨 : " +
            skillName +
            " / 실제 공격 반경 : " +
            currentSkillRadius
        );
    }


    // =========================================================
    // 스킬별 마법진 이미지
    // =========================================================

    private bool SetMagicCircle(
        string skillName
    )
    {
        if (spriteRenderer == null)
            return false;


        Sprite selectedSprite = null;


        switch (skillName)
        {
            case "Fireball":

                selectedSprite =
                    fireMagicCircle;

                break;


            case "Ice Attack":

                selectedSprite =
                    iceMagicCircle;

                break;


            case "Lightning":

                selectedSprite =
                    lightningMagicCircle;

                break;


            default:

                Debug.LogError(
                    "등록되지 않은 스킬 : " +
                    skillName
                );

                return false;
        }


        if (selectedSprite == null)
        {
            Debug.LogError(
                skillName +
                "의 마법진 Sprite가 연결되지 않았습니다."
            );

            return false;
        }


        spriteRenderer.sprite =
            selectedSprite;


        spriteRenderer.enabled =
            true;


        return true;
    }


    // =========================================================
    // 마법진 크기
    // =========================================================

    private void SetMagicCircleSize(
        string skillName
    )
    {
        float scale =
            magicCircleScale;


        switch (skillName)
        {
            case "Fireball":

                scale *=
                    fireCircleScaleMultiplier;

                break;


            case "Ice Attack":

                scale *=
                    iceCircleScaleMultiplier;

                break;


            case "Lightning":

                scale *=
                    lightningCircleScaleMultiplier;

                break;
        }


        originalScale =
            Vector3.one *
            scale;


        magicCircleObject.transform.localScale =
            originalScale;
    }


    // =========================================================
    // 실제 마법진 반경 계산
    // =========================================================

    private float CalculateBaseMagicCircleRadius()
    {
        if (spriteRenderer == null)
            return 0f;


        if (spriteRenderer.sprite == null)
            return 0f;


        // Sprite의 가로 반쪽 크기
        float spriteRadius =
            spriteRenderer.sprite.bounds.extents.x;


        // 월드 스케일
        float worldScale =
            Mathf.Abs(
                magicCircleObject.transform.lossyScale.x
            );


        return
            spriteRadius *
            worldScale;
    }


    // =========================================================
    // 현재 공격 반경
    // =========================================================

    public float GetMagicCircleRadius()
    {
        return currentSkillRadius;
    }


    // =========================================================
    // 마우스 월드 좌표
    // =========================================================

    private void UpdateTargetPosition()
    {
        if (mainCamera == null)
            return;


        Vector3 mouseScreenPosition =
            Input.mousePosition;


        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    zDistanceFromCamera
                )
            );


        mouseWorldPosition.z =
            0f;


        targetPosition =
            mouseWorldPosition;
    }


    // =========================================================
    // 마우스를 부드럽게 따라가기
    // =========================================================

    private void FollowMouseSmooth()
    {
        if (magicCircleObject == null)
            return;


        float t =
            1f -
            Mathf.Exp(
                -followSpeed *
                Time.unscaledDeltaTime
            );


        magicCircleObject.transform.position =
            Vector3.Lerp(
                magicCircleObject.transform.position,
                targetPosition,
                t
            );
    }


    // =========================================================
    // 회전
    // =========================================================

    private void RotateMagicCircle()
    {
        if (!rotateMagicCircle)
            return;


        if (magicCircleObject == null)
            return;


        magicCircleObject.transform.Rotate(
            0f,
            0f,
            rotationSpeed *
            Time.unscaledDeltaTime
        );
    }


    // =========================================================
    // 펄스
    // =========================================================

    private void PulseMagicCircle()
    {
        if (!usePulseEffect)
            return;


        if (magicCircleObject == null)
            return;


        float pulse =
            Mathf.Sin(
                Time.unscaledTime *
                pulseSpeed
            );


        float multiplier =
            1f +
            pulse *
            pulseAmount;


        // ★ 공격 반경은 바뀌지 않고
        // 화면상의 마법진만 펄스됨
        magicCircleObject.transform.localScale =
            originalScale *
            multiplier;
    }


    // =========================================================
    // 범위 안 몬스터 확인
    // =========================================================

    private bool HasEnemyInsideRange()
    {
        if (magicCircleObject == null)
            return false;


        float radius =
            currentSkillRadius;


        if (radius <= 0f)
            return false;


        int layerMask =
            monsterLayer.value != 0
                ? monsterLayer.value
                : Physics2D.AllLayers;


        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                magicCircleObject.transform.position,
                radius,
                layerMask
            );


        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;


            MonsterHealth monster =
                hit.GetComponentInParent<
                    MonsterHealth
                >();


            if (
                monster != null &&
                !monster.IsDead
            )
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // 범위 안 몬스터가 있으면 밝게
    // =========================================================

    private void UpdateEnemyHighlight()
    {
        if (spriteRenderer == null)
            return;


        if (!highlightWhenEnemyInside)
        {
            SetMagicCircleAlpha(
                normalAlpha
            );

            return;
        }


        bool enemyInside =
            HasEnemyInsideRange();


        float targetAlpha =
            enemyInside
                ? enemyHighlightAlpha
                : normalAlpha;


        Color currentColor =
            spriteRenderer.color;


        float t =
            highlightSpeed *
            Time.unscaledDeltaTime;


        float alpha =
            Mathf.Lerp(
                currentColor.a,
                targetAlpha,
                t
            );


        spriteRenderer.color =
            new Color(
                currentColor.r,
                currentColor.g,
                currentColor.b,
                alpha
            );
    }


    // =========================================================
    // 마법진 투명도
    // =========================================================

    private void SetMagicCircleAlpha(
        float alpha
    )
    {
        if (spriteRenderer == null)
            return;


        Color color =
            spriteRenderer.color;


        color.a =
            Mathf.Clamp01(alpha);


        spriteRenderer.color =
            color;
    }


    // =========================================================
    // ★ 실제 스킬 사용
    // =========================================================
    // 몬스터를 클릭할 필요 없음
    // 화면 어디든 클릭하면 그 위치에서 발동
    // =========================================================

    private void TryUseSkill()
    {
        if (SkillManager.Instance == null)
            return;


        if (magicCircleObject == null)
            return;


        if (
            string.IsNullOrEmpty(
                selectedSkillName
            )
        )
        {
            return;
        }


        // -----------------------------------------------------
        // UI 클릭이면 발동하지 않음
        // -----------------------------------------------------

        if (
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject()
        )
        {
            return;
        }


        // =====================================================
        // ★ 클릭 순간 마우스 위치를 바로 확정
        // =====================================================

        UpdateTargetPosition();


        magicCircleObject.transform.position =
            targetPosition;


        // =====================================================
        // ★ 클릭 위치를 스킬 발동 위치로 사용
        // =====================================================

        Vector3 castPosition =
            targetPosition;


        // =====================================================
        // 실제 공격 반경
        // =====================================================

        float radius =
            currentSkillRadius;


        if (radius <= 0f)
        {
            Debug.LogWarning(
                "현재 스킬의 공격 반경이 없습니다."
            );

            return;
        }


        // =====================================================
        // ★ SkillManager에서 범위 스킬 발동
        // =====================================================

        bool success =
            SkillManager.Instance.UseSkillAtPosition(
                selectedSkillName,
                castPosition,
                radius
            );


        if (!success)
        {
            return;
        }


        // -----------------------------------------------------
        // 디버그
        // -----------------------------------------------------

        Debug.Log(
            "스킬 발동 : " +
            selectedSkillName +
            " / 위치 : " +
            castPosition +
            " / 반경 : " +
            radius
        );


        // =====================================================
        // 조준 종료
        // =====================================================

        FinishAiming();
    }


    // =========================================================
    // 조준 완료
    // =========================================================

    private void FinishAiming()
    {
        isAiming = false;

        selectedSkillName = "";

        currentSkillRadius = 0f;


        if (magicCircleObject != null)
        {
            magicCircleObject.SetActive(false);
        }
    }


    // =========================================================
    // 조준 취소
    // =========================================================

    public void CancelAiming()
    {
        isAiming = false;

        selectedSkillName = "";

        currentSkillRadius = 0f;


        if (magicCircleObject != null)
        {
            magicCircleObject.SetActive(false);
        }


        Debug.Log(
            "스킬 조준 취소"
        );
    }


    // =========================================================
    // 조준 여부
    // =========================================================

    public bool IsAiming()
    {
        return isAiming;
    }


    // =========================================================
    // 선택된 스킬 이름
    // =========================================================

    public string GetSelectedSkillName()
    {
        return selectedSkillName;
    }


    // =========================================================
    // Gizmo
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!isAiming)
            return;


        if (magicCircleObject == null)
            return;


        float radius =
            currentSkillRadius;


        if (radius <= 0f)
            return;


        Gizmos.DrawWireSphere(
            magicCircleObject.transform.position,
            radius
        );
    }
}
