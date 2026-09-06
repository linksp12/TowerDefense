using System.Collections;
using UnityEngine;

public class TowerFreeze : MonoBehaviour
{
    [Header("덩굴 이펙트")]
    [SerializeField] private GameObject vineEffectPrefab;

    private TowerAttack towerAttack;

    private GameObject currentVine;
    private Coroutine freezeCoroutine;

    private void Awake()
    {
        towerAttack = GetComponent<TowerAttack>();
    }

    // =========================================================
    // 타워 정지
    // =========================================================
    public void Freeze(float duration, GameObject overrideVinePrefab = null)
    {
        if (towerAttack == null)
        {
            towerAttack = GetComponent<TowerAttack>();
        }

        // 사용 가능한 덩굴 프리팹 결정
        GameObject prefab = overrideVinePrefab;

        if (prefab == null)
        {
            prefab = vineEffectPrefab;
        }

        // -----------------------------------------------------
        // 이미 정지 중이라면
        // 기존 코루틴을 중지하고 다시 3초 시작
        // -----------------------------------------------------
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }

        freezeCoroutine = StartCoroutine(
            FreezeCoroutine(duration, prefab)
        );
    }

    // =========================================================
    // 실제 정지 처리
    // =========================================================
    private IEnumerator FreezeCoroutine(
        float duration,
        GameObject vinePrefab)
    {
        // -----------------------------------------------------
        // 타워 공격 정지
        // -----------------------------------------------------
        if (towerAttack != null)
        {
            towerAttack.enabled = false;
        }

        // -----------------------------------------------------
        // 기존 덩굴 제거
        // -----------------------------------------------------
        if (currentVine != null)
        {
            Destroy(currentVine);
            currentVine = null;
        }

        // -----------------------------------------------------
        // 새 덩굴 생성
        // -----------------------------------------------------
        if (vinePrefab != null)
        {
            currentVine = Instantiate(
                vinePrefab,
                transform.position,
                Quaternion.identity,
                transform
            );
        }

        // -----------------------------------------------------
        // 지정된 시간만큼 정지
        // -----------------------------------------------------
        yield return new WaitForSeconds(duration);

        // -----------------------------------------------------
        // 덩굴 제거
        // -----------------------------------------------------
        if (currentVine != null)
        {
            Destroy(currentVine);
            currentVine = null;
        }

        // -----------------------------------------------------
        // 타워 공격 재개
        // -----------------------------------------------------
        if (towerAttack != null)
        {
            towerAttack.enabled = true;
        }

        freezeCoroutine = null;
    }
}
