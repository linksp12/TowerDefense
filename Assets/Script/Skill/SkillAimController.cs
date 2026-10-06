using UnityEngine;
using UnityEngine.EventSystems;

public class SkillAimController : MonoBehaviour
{
    public static SkillAimController Instance;

    [Header("마법진 표시 오브젝트")]
    [Tooltip("기존 MagicCircleVisual을 연결하세요.")]
    public GameObject magicCircleObject;

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

    [Tooltip("타워 가속 스킬 범위 색상")]
    public Color hasteRangeColor =
        new Color(0.25f, 1f, 0.55f, 1f);

    [Tooltip("범위 원 선 두께")]
    [Min(0.001f)]
    public float rangeIndicatorWidth = 0.05f;

    [Tooltip("원 정밀도")]
    [Range(32, 128)]
    public int rangeIndicatorSegments = 64;

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

    [Header("마법진 이미지")]

    public Sprite fireMagicCircle;

    public Sprite iceMagicCircle;

    public Sprite lightningMagicCircle;

    [Header("마법진 크기")]

    [Tooltip("전체 마법진 크기")]
    public float magicCircleScale = 1f;

    [Tooltip("불 마법진 크기 보정")]
    public float fireCircleScaleMultiplier = 1f;

    [Tooltip("얼음 마법진 크기 보정")]
    public float iceCircleScaleMultiplier = 1f;

    [Tooltip("번개 마법진 크기 보정")]
    public float lightningCircleScaleMultiplier = 1f;

    [Header("마우스 이동")]

    [Tooltip("마법진이 마우스를 따라가는 속도")]
    public float followSpeed = 15f;

    [Tooltip("카메라와 월드 위치 계산용 거리")]
    public float zDistanceFromCamera = 10f;

    [Header("마법진 회전")]

    [Tooltip("마법진 회전 사용")]
    public bool rotateMagicCircle = true;

    [Tooltip("초당 회전 각도")]
    public float rotationSpeed = 35f;

    [Header("몬스터 감지")]

    [Tooltip("몬스터가 사용하는 Layer")]
    public LayerMask monsterLayer;

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

    private Camera mainCamera;
    private SkillAimVisual visual;
    private SkillData selectedSkillData;
    private Vector3 targetPosition;
    private float currentSkillRadius;
    private bool isAiming;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        visual = new SkillAimVisual(magicCircleObject, rangeIndicatorObject);
    }

    private void Update()
    {
        if (!isAiming) return;
        if (visual == null || !visual.IsReady)
        {
            FinishAiming();
            return;
        }
        UpdateTargetPosition();
        visual.Tick(targetPosition, currentSkillRadius, GetVisualSettings());
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelAiming();
            return;
        }
        if (Input.GetMouseButtonDown(0)) TryUseSkill();
    }

    public void StartAiming(string skillName)
    {
        SkillManager manager = SkillManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("SkillManager가 씬에 없습니다.", this);
            return;
        }
        if (!manager.TryGetSkill(skillName, out SkillData skill) || !manager.CanUseSkill(skill.SkillId)) return;
        if (magicCircleObject == null || skill.MagicCircle == null)
        {
            Debug.LogWarning($"{skill.DisplayName}: 마법진 오브젝트 또는 이미지가 연결되지 않았습니다.", this);
            return;
        }
        if (visual == null) visual = new SkillAimVisual(magicCircleObject, rangeIndicatorObject);
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("스킬 조준용 Main Camera를 찾을 수 없습니다.", this);
            return;
        }
        UpdateTargetPosition();
        if (!visual.Show(skill, targetPosition, skill.Range, GetVisualSettings()))
        {
            Debug.LogWarning("마법진 SpriteRenderer를 찾을 수 없습니다.", this);
            return;
        }
        selectedSkillData = skill;
        currentSkillRadius = skill.Range;
        isAiming = true;
    }

    private void UpdateTargetPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        Vector3 mouse = Input.mousePosition;
        targetPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, zDistanceFromCamera));
        targetPosition.z = 0f;
    }

    private void TryUseSkill()
    {
        SkillManager manager = SkillManager.Instance;
        if (manager == null || selectedSkillData == null || currentSkillRadius <= 0f) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        UpdateTargetPosition();
        visual.SetPosition(targetPosition);
        if (manager.UseSkillAtPosition(selectedSkillData.SkillId, targetPosition, currentSkillRadius)) FinishAiming();
    }

    private void FinishAiming()
    {
        isAiming = false;
        selectedSkillData = null;
        currentSkillRadius = 0f;
        visual?.Hide();
    }

    public void CancelAiming() => FinishAiming();
    public bool IsAiming() => isAiming;
    public string GetSelectedSkillName() => selectedSkillData != null ? selectedSkillData.SkillId : string.Empty;
    public float GetMagicCircleRadius() => currentSkillRadius;

    private SkillAimVisualSettings GetVisualSettings()
    {
        return new SkillAimVisualSettings
        {
            FireColor = fireRangeColor, IceColor = iceRangeColor, LightningColor = lightningRangeColor,
            HasteColor = hasteRangeColor,
            CircleScale = magicCircleScale, FireScale = fireCircleScaleMultiplier,
            IceScale = iceCircleScaleMultiplier, LightningScale = lightningCircleScaleMultiplier,
            FollowSpeed = followSpeed, Rotate = rotateMagicCircle, RotationSpeed = rotationSpeed,
            Highlight = highlightWhenEnemyInside, NormalAlpha = normalAlpha,
            HighlightAlpha = enemyHighlightAlpha, HighlightSpeed = highlightSpeed,
            MonsterLayer = monsterLayer, LineWidth = rangeIndicatorWidth, Segments = rangeIndicatorSegments
        };
    }

    private void OnDisable() => FinishAiming();

    private void OnDestroy()
    {
        visual?.Dispose();
        if (Instance == this) Instance = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (isAiming && magicCircleObject != null && currentSkillRadius > 0f)
            Gizmos.DrawWireSphere(magicCircleObject.transform.position, currentSkillRadius);
    }
}
