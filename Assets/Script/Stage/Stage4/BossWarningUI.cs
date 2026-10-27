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
    private bool hasShownWarning = false;


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
        ShowBossWarning(
            "<size=48><color=#FF4A3D>보스 웨이브 시작!</color></size>\n" +
            "<size=28><color=#FFE3B0>강력한 적이 다가옵니다.</color></size>"
        );
    }


    public void ShowBossWarning(string message)
    {
        if (warningText == null || hasShownWarning)
            return;

        hasShownWarning = true;

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
