using UnityEngine;

public class BuildPoint : MonoBehaviour
{
    private bool hasTower = false;
    private SpriteRenderer spriteRenderer;

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

    private void OnMouseEnter()
    {
        ShowPreview();
    }

    private void OnMouseExit()
    {
        HidePreview();
    }

    private void OnMouseDown()
    {
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

        TowerBuildManager.Instance.OpenBuildPanel(this);
    }

    public void BuildTower(GameObject towerPrefab)
    {
        if (hasTower)
        {
            Debug.Log("이미 타워가 있어서 설치할 수 없습니다.");
            return;
        }

        Instantiate(towerPrefab, transform.position, Quaternion.identity);
        hasTower = true;

        HidePreview();

        // 타워 설치 후 설치 위치 표시 이미지 숨기기
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
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

        return GameManager.Instance.CanAfford(minimumBuildCost);
    }
}
