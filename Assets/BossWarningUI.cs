using System.Collections;
using TMPro;
using UnityEngine;

public class BossWarningUI : MonoBehaviour
{
    public static BossWarningUI Instance;

    [Header("경고 텍스트")]
    [SerializeField]
    private TMP_Text warningText;

    [Header("표시 시간")]
    [SerializeField]
    private float showTime = 2.5f;

    [Header("페이드 시간")]
    [SerializeField]
    private float fadeTime = 0.5f;

    private Coroutine warningCoroutine;


    private void Awake()
    {
        Instance = this;

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }


    public void ShowBossWarning()
    {
        ShowBossWarning("⚠ 경고 ⚠\n보스가 출현합니다!");
    }


    public void ShowBossWarning(string message)
    {
        if (warningText == null)
            return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(WarningCoroutine(message));
    }


    private IEnumerator WarningCoroutine(string message)
    {
        warningText.gameObject.SetActive(true);

        warningText.text = message;

        Color color = warningText.color;
        color.a = 1f;
        warningText.color = color;

        // 텍스트 표시
        yield return new WaitForSeconds(showTime);

        // 페이드 아웃
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float alpha = 1f - (timer / fadeTime);

            color.a = alpha;
            warningText.color = color;

            yield return null;
        }

        color.a = 0f;
        warningText.color = color;

        warningText.gameObject.SetActive(false);

        warningCoroutine = null;
    }
}
