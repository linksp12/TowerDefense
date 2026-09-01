using UnityEngine;
using UnityEngine.EventSystems;

public class BuildPoint : MonoBehaviour
{
    private bool hasTower = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D clickCollider;

    [Header("Preview")]
    public GameObject previewObject;
    public SpriteRenderer previewRenderer;

    [Header("Preview Color")]
    public Color canBuildColor = new Color(0f, 1f, 0f, 0.45f);
    public Color cannotBuildColor = new Color(1f, 0f, 0f, 0.45f);

    [Header("Cost Check")]
    public int minimumBuildCost = 50;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        clickCollider = GetComponent<Collider2D>();

        if (previewObject == null)
        {
            Transform previewTransform = transform.Find("BuildPreview");

            if (previewTransform != null)
                previewObject = previewTransform.gameObject;
        }

        if (previewRenderer == null && previewObject != null)
        {
            previewRenderer = previewObject.GetComponent<SpriteRenderer>();
        }

        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
    }

    private void OnMouseOver()
    {
        // UI 위에 마우스가 있으면 빌드포인트 미리보기 표시 안 함
        if (IsPointerOverUI())
        {
            HidePreview();
            return;
        }

        ShowPreview();
    }

    private void OnMouseExit()
    {
        HidePreview();
    }

    private void OnMouseDown()
    {
        // UI 버튼/패널 위를 클릭한 경우 빌드포인트 클릭 무시
        if (IsPointerOverUI())
        {
            return;
        }

        if (hasTower)
        {
            Debug.Log("이미 타워가 설치된 위치입니다.");
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.CanAfford(minimumBuildCost))
        {
            Debug.Log("돈이 부족해서 타워를 설치할 수 없습니다.");
            return;
        }

        if (TowerBuildManager.Instance != null)
        {
            TowerBuildManager.Instance.OpenBuildPanel(this);
        }
    }

    public void BuildTower(GameObject towerPrefab)
    {
        if (hasTower)
        {
            Debug.Log("이미 타워가 있어서 설치할 수 없습니다.");
            return;
        }

        GameObject tower = Instantiate(towerPrefab, transform.position, Quaternion.identity);

        TowerUpgrade towerUpgrade = tower.GetComponent<TowerUpgrade>();
        if (towerUpgrade != null)
        {
            towerUpgrade.SetOwnerBuildPoint(this);
        }

        TowerEffectAnimator effectAnimator = tower.GetComponent<TowerEffectAnimator>();
        if (effectAnimator != null)
        {
            effectAnimator.PlayInstallEffect();
        }

        hasTower = true;

        // 설치된 타워와 빌드 포인트의 콜라이더가 겹쳐 클릭을 가로채지 않게 한다.
        if (clickCollider != null)
        {
            clickCollider.enabled = false;
        }

        HidePreview();

        // 타워 설치 후 설치 위치 표시 이미지 숨기기
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    public void ClearTower()
    {
        hasTower = false;

        if (clickCollider != null)
        {
            clickCollider.enabled = true;
        }

        // 판매 후 다시 설치 위치 표시 이미지 보이게 하기
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        HidePreview();
    }

    private void ShowPreview()
    {
        if (previewObject == null || previewRenderer == null)
            return;

        previewObject.SetActive(true);

        if (CanBuildHere())
        {
            previewRenderer.color = canBuildColor;
        }
        else
        {
            previewRenderer.color = cannotBuildColor;
        }
    }

    private void HidePreview()
    {
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
    }

    private bool CanBuildHere()
    {
        if (hasTower)
            return false;

        if (GameManager.Instance == null)
            return true;

        return GameManager.Instance.CanAfford(TowerBuildManager.Instance.GetCheapestTowerCost());
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
