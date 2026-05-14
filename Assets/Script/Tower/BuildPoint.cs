using UnityEngine;

public class BuildPoint : MonoBehaviour
{
    private bool hasTower = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        if (hasTower)
        {
            Debug.Log("이미 타워가 설치된 위치입니다.");
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

        // 타워 설치 후 설치 위치 표시 이미지 숨기기
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }
}
