using UnityEngine;

/// <summary>잠시 보였다가 서서히 사라지는 바닥 자국.</summary>
public class FadeAndDestroy : MonoBehaviour
{
    private float hold;
    private float fade;
    private float age;
    private SpriteRenderer sr;

    public void Init(float holdTime, float fadeTime)
    {
        hold = holdTime;
        fade = fadeTime;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (sr != null && age > hold)
        {
            Color c = sr.color;
            c.a = Mathf.Clamp01(1f - (age - hold) / fade);
            sr.color = c;
        }

        if (age >= hold + fade)
            Destroy(gameObject);
    }
}
