using System;
using UnityEngine;

/// <summary>
/// 프레임 그림을 순서대로 한 번 재생하고 풀로 돌아가는 픽셀 이펙트입니다. CombatFeedback이 만들고 재사용합니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PixelEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float fps;
    private float elapsed;
    private Action<PixelEffect> release;

    public void Play(Sprite[] effectFrames, float framesPerSecond, Vector3 position, float scale,
        Action<PixelEffect> onFinished)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Effects";
            spriteRenderer.sortingOrder = 60;
        }

        frames = effectFrames;
        fps = Mathf.Max(1f, framesPerSecond);
        elapsed = 0f;
        release = onFinished;
        transform.position = new Vector3(position.x, position.y, 0f);
        transform.localScale = Vector3.one * scale;
        spriteRenderer.sprite = frames[0];
        gameObject.SetActive(true);
    }

    private void Update()
    {
        // 게임 시간 기준이라 일시정지 중에는 멈추고, 배속에서는 함께 빨라진다.
        elapsed += Time.deltaTime;
        int index = Mathf.FloorToInt(elapsed * fps);
        if (index >= frames.Length)
        {
            Finish();
            return;
        }
        spriteRenderer.sprite = frames[index];
    }

    public void Finish()
    {
        if (!gameObject.activeSelf)
            return;
        gameObject.SetActive(false);
        release?.Invoke(this);
    }
}
