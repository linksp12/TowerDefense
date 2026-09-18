using UnityEngine;
using UnityEngine.EventSystems;

public class SkillAimController : MonoBehaviour
{
    public static SkillAimController Instance;


    // =========================================================
    // 마법진 표시 오브젝트
    // =========================================================

    [Header("마법진 표시 오브젝트")]
    [Tooltip("기존 MagicCircleVisual을 연결하세요.")]
    public GameObject magicCircleObject;


    // =========================================================
    // 스킬 범위 표시
    // =========================================================

    [Header("스킬 범위 표시 - LineRenderer")]

    [Tooltip("빨간/파란/노란 원을 그릴 RangeIndicator 오브젝트")]
    public GameObject rangeIndicatorObject;

    [Tooltip("불 스킬 범위 색상")]
    public Color fireRangeColor =
        new Color(1f, 0f, 0f, 1f);

    [Tooltip("얼음 스킬 범위 색상")]
    public Color iceRangeColor =
        new Color(0.2f, 0.8f, 1f, 1f);

    [Tooltip("번개 스킬 범위 색상")]
    public Color lightningRangeColor =
        new Color(1f, 0.85f, 0f, 1f);

    [Tooltip("범위 원 선 두께")]
    [Min(0.001f)]
    public float rangeIndicatorWidth = 0.05f;

    [Tooltip("원 정밀도")]
    [Range(32, 128)]
    public int rangeIndicatorSegments = 64;

    private LineRenderer rangeLineRenderer;


    // =========================================================
    // 스킬별 범위 보정
    // =========================================================

    [Header("스킬별 범위 보정")]

    [Tooltip("Fireball 실제 공격 범위 보정")]
    [Range(0.5f, 1.5f)]
    public float fireRangeRadiusMultiplier = 0.75f;

    [Tooltip("Ice Attack 실제 공격 범위 보정")]
    [Range(0.5f, 1.5f)]
    public float iceRangeRadiusMultiplier = 0.75f;

    [Tooltip("Lightning 실제 공격 범위 보정")]
    [Range(0.5f, 1.5f)]
    public float lightningRangeRadiusMultiplier = 0.75f;


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

    private Vector3 targetPosition;

    private Vector3 originalScale;

    // 현재 선택된 스킬의 실제 공격 반경
    private float currentSkillRadius = 0f;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        Instance = this;

        mainCamera = Camera.main;

        FindSpriteRenderer();


        // -----------------------------------------------------
        // 기존 마법진
        // -----------------------------------------------------

        if (magicCircleObject != null)
        {
            magicCircleObject.SetActive(false);

            originalScale =
                magicCircleObject.transform.localScale;
        }


        // -----------------------------------------------------
        // 범위 표시 준비
        // -----------------------------------------------------

        SetupRangeIndicator();
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

        HideRangeIndicator();
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!isAiming)
            return;


        // -----------------------------------------------------
        // 마우스 위치
        // -----------------------------------------------------

        UpdateTargetPosition();


        // -----------------------------------------------------
        // 마법진 이동
        // -----------------------------------------------------

        FollowMouseSmooth();


        // -----------------------------------------------------
        // 범위 원 갱신
        // -----------------------------------------------------

        UpdateRangeIndicator();


        // -----------------------------------------------------
        // 마법진 회전
        // -----------------------------------------------------

        RotateMagicCircle();


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
        // 좌클릭 = 스킬 발동
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


        spriteRenderer =
            magicCircleObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer =
                magicCircleObject.GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerID = SortingLayer.NameToID("Effects");
            spriteRenderer.sortingOrder = 6;
        }
    }


    // =========================================================
    // LineRenderer 설정
    // =========================================================

    private void SetupRangeIndicator()
    {
        if (rangeIndicatorObject == null)
        {
            Debug.LogWarning(
                "SkillAimController : " +
                "Range Indicator Object가 연결되지 않았습니다."
            );

            return;
        }


        rangeLineRenderer =
            rangeIndicatorObject.GetComponent<LineRenderer>();


        if (rangeLineRenderer == null)
        {
            rangeLineRenderer =
                rangeIndicatorObject.AddComponent<LineRenderer>();
        }


        // -----------------------------------------------------
        // 기본 설정
        // -----------------------------------------------------

        rangeLineRenderer.useWorldSpace = false;

        rangeLineRenderer.loop = true;

        rangeLineRenderer.positionCount =
            Mathf.Max(
                32,
                rangeIndicatorSegments
            );


        rangeLineRenderer.startWidth =
            rangeIndicatorWidth;

        rangeLineRenderer.endWidth =
            rangeIndicatorWidth;


        rangeLineRenderer.alignment =
            LineAlignment.View;


        rangeLineRenderer.textureMode =
            LineTextureMode.Stretch;


        // -----------------------------------------------------
        // Material 생성
        // -----------------------------------------------------

        Shader shader =
            Shader.Find("Sprites/Default");


        if (shader != null)
        {
            Material material =
                new Material(shader);

            material.name =
                "SkillRangeLineMaterial";

            material.color =
                fireRangeColor;

            rangeLineRenderer.material =
                material;
        }


        // -----------------------------------------------------
        // 렌더링 순서
        // -----------------------------------------------------

        rangeLineRenderer.sortingLayerID =
            SortingLayer.NameToID("Effects");

        // 기존 MagicCircleVisual보다 뒤
        rangeLineRenderer.sortingOrder = 5;


        // -----------------------------------------------------
        // 처음에는 숨김
        // -----------------------------------------------------

        HideRangeIndicator();
    }


    // =========================================================
    // 현재 스킬의 범위 색상 가져오기
    // =========================================================

    private Color GetCurrentRangeColor()
    {
        switch (selectedSkillName)
        {
            case "Fireball":
                return fireRangeColor;

            case "Ice Attack":
                return iceRangeColor;

            case "Lightning":
                return lightningRangeColor;

            default:
                return fireRangeColor;
        }
    }


    // =========================================================
    // 현재 스킬의 범위 보정값 가져오기
    // =========================================================

    private float GetCurrentRangeMultiplier()
    {
        switch (selectedSkillName)
        {
            case "Fireball":
                return fireRangeRadiusMultiplier;

            case "Ice Attack":
                return iceRangeRadiusMultiplier;

            case "Lightning":
                return lightningRangeRadiusMultiplier;

            default:
                return 1f;
        }
    }


    // =========================================================
    // 빨간/파란/노란 원 그리기
    // =========================================================

    private void DrawRangeCircle()
    {
        if (rangeLineRenderer == null)
            return;


        int segments =
            Mathf.Clamp(
                rangeIndicatorSegments,
                32,
                128
            );


        rangeLineRenderer.positionCount =
            segments;


        float radius =
            currentSkillRadius;


        if (radius <= 0f)
            return;


        // -----------------------------------------------------
        // 현재 스킬 색상
        // -----------------------------------------------------

        Color currentColor =
            GetCurrentRangeColor();


        // -----------------------------------------------------
        // 원형 좌표 생성
        // -----------------------------------------------------

        for (int i = 0; i < segments; i++)
        {
            float angle =
                (360f / segments) * i;

            float radian =
                angle * Mathf.Deg2Rad;


            float x =
                Mathf.Cos(radian) *
                radius;


            float y =
                Mathf.Sin(radian) *
                radius;


            rangeLineRenderer.SetPosition(
                i,
                new Vector3(
                    x,
                    y,
                    0f
                )
            );
        }


        // -----------------------------------------------------
        // 선 두께
        // -----------------------------------------------------

        rangeLineRenderer.startWidth =
            rangeIndicatorWidth;

        rangeLineRenderer.endWidth =
            rangeIndicatorWidth;


        // -----------------------------------------------------
        // LineRenderer 색상
        // -----------------------------------------------------

        rangeLineRenderer.startColor =
            currentColor;

        rangeLineRenderer.endColor =
            currentColor;


        // -----------------------------------------------------
        // Material 색상
        // -----------------------------------------------------

        if (rangeLineRenderer.material != null)
        {
            rangeLineRenderer.material.color =
                currentColor;
        }
    }


    // =========================================================
    // 범위 표시 업데이트
    // =========================================================

    private void UpdateRangeIndicator()
    {
        if (rangeLineRenderer == null)
            return;


        if (magicCircleObject == null)
        {
            HideRangeIndicator();
            return;
        }


        if (currentSkillRadius <= 0f)
        {
            HideRangeIndicator();
            return;
        }


        // -----------------------------------------------------
        // 마법진과 같은 위치
        // -----------------------------------------------------

        rangeIndicatorObject.transform.position =
            magicCircleObject.transform.position;


        // -----------------------------------------------------
        // 범위 원은 회전하지 않음
        // -----------------------------------------------------

        rangeIndicatorObject.transform.rotation =
            Quaternion.identity;


        // -----------------------------------------------------
        // 원 다시 그리기
        // -----------------------------------------------------

        DrawRangeCircle();


        rangeLineRenderer.enabled = true;
    }


    // =========================================================
    // 범위 표시 숨기기
    // =========================================================

    private void HideRangeIndicator()
    {
        if (rangeLineRenderer != null)
        {
            rangeLineRenderer.enabled = false;
        }


        if (rangeIndicatorObject != null)
        {
            rangeIndicatorObject.SetActive(false);
        }
    }


    // =========================================================
    // 범위 표시 보이기
    // =========================================================

    private void ShowRangeIndicator()
    {
        if (rangeIndicatorObject == null)
            return;


        rangeIndicatorObject.SetActive(true);


        if (rangeLineRenderer != null)
        {
            rangeLineRenderer.enabled = true;
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
        // 스킬별 마법진 이미지
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
        // 마법진 크기
        // -----------------------------------------------------

        SetMagicCircleSize(
            skillName
        );


        // -----------------------------------------------------
        // 실제 공격 반경
        // -----------------------------------------------------

        currentSkillRadius =
            CalculateBaseMagicCircleRadius();


        // -----------------------------------------------------
        // 마우스 위치
        // -----------------------------------------------------

        UpdateTargetPosition();


        magicCircleObject.transform.position =
            targetPosition;


        // -----------------------------------------------------
        // 범위 원 표시
        // -----------------------------------------------------

        ShowRangeIndicator();


        rangeIndicatorObject.transform.position =
            targetPosition;


        rangeIndicatorObject.transform.rotation =
            Quaternion.identity;


        DrawRangeCircle();


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


        // -----------------------------------------------------
        // Sprite 전체 가로 반경
        // -----------------------------------------------------

        float spriteRadius =
            spriteRenderer.sprite.bounds.extents.x;


        // -----------------------------------------------------
        // 실제 월드 스케일
        // -----------------------------------------------------

        float worldScale =
            Mathf.Abs(
                magicCircleObject.transform.lossyScale.x
            );


        // -----------------------------------------------------
        // 스킬별 범위 보정
        // -----------------------------------------------------

        float rangeMultiplier =
            GetCurrentRangeMultiplier();


        // -----------------------------------------------------
        // 최종 공격 반경
        // -----------------------------------------------------

        return
            spriteRadius *
            worldScale *
            rangeMultiplier;
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
    // 범위 내 몬스터 강조
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
    // 실제 스킬 사용
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


        // -----------------------------------------------------
        // 클릭 순간 위치 확정
        // -----------------------------------------------------

        UpdateTargetPosition();


        magicCircleObject.transform.position =
            targetPosition;


        if (rangeIndicatorObject != null)
        {
            rangeIndicatorObject.transform.position =
                targetPosition;
        }


        // -----------------------------------------------------
        // 스킬 발동 위치
        // -----------------------------------------------------

        Vector3 castPosition =
            targetPosition;


        // -----------------------------------------------------
        // 실제 공격 반경
        // -----------------------------------------------------

        float radius =
            currentSkillRadius;


        if (radius <= 0f)
        {
            Debug.LogWarning(
                "현재 스킬의 공격 반경이 없습니다."
            );

            return;
        }


        // -----------------------------------------------------
        // SkillManager에서 범위 스킬 발동
        // -----------------------------------------------------

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


        Debug.Log(
            "스킬 발동 : " +
            selectedSkillName +
            " / 위치 : " +
            castPosition +
            " / 반경 : " +
            radius
        );


        // -----------------------------------------------------
        // 조준 종료
        // -----------------------------------------------------

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


        HideRangeIndicator();
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


        HideRangeIndicator();


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
