using System.Collections;
using UnityEngine;

public class TowerEffectAnimator : MonoBehaviour
{
    [Header("Scale Animation")]
    public float animationTime = 0.18f;
    public Vector3 startScale = new Vector3(0.75f, 0.75f, 1f);
    public Vector3 punchScale = new Vector3(1.18f, 1.18f, 1f);
    public Vector3 endScale = Vector3.one;

    [Header("Optional Effect")]
    public GameObject installEffectPrefab;
    public GameObject upgradeEffectPrefab;

    private Coroutine scaleCoroutine;

    public void PlayInstallEffect()
    {
        PlayEffectPrefab(installEffectPrefab);
        PlayScaleAnimation();
    }

    public void PlayUpgradeEffect()
    {
        PlayEffectPrefab(upgradeEffectPrefab);
        PlayScaleAnimation();
    }

    public void PlayScaleAnimation()
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleRoutine());
    }

    private IEnumerator ScaleRoutine()
    {
        float halfTime = animationTime * 0.5f;
        float timer = 0f;

        transform.localScale = startScale;

        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            float t = timer / halfTime;

            transform.localScale = Vector3.Lerp(startScale, punchScale, t);
            yield return null;
        }

        timer = 0f;

        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            float t = timer / halfTime;

            transform.localScale = Vector3.Lerp(punchScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
        scaleCoroutine = null;
    }

    private void PlayEffectPrefab(GameObject effectPrefab)
    {
        if (effectPrefab == null)
            return;

        GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
        Destroy(effect, 1.2f);
    }
}
