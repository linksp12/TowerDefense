using UnityEngine;

/// <summary>
/// 프레임 그림을 계속 반복하는 픽셀 표시(화상 불꽃, 감전 표시, 에너지 구슬 등)입니다.
/// 게임 시간 기준이라 일시정지 중에는 멈추고 배속에서는 함께 빨라집니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PixelLoop : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float fps;
    private float elapsed;

    public SpriteRenderer Renderer => spriteRenderer;

    // 부모 아래에 반복 표시를 만든다. 효과 설정이 비어 있으면 null을 돌려준다.
    public static PixelLoop Create(string name, Transform parent, CombatFeelSettings.Effect effect, int sortingOrder)
    {
        if (effect == null || effect.frames == null || effect.frames.Length == 0)
            return null;
        var go = new GameObject(name, typeof(SpriteRenderer), typeof(PixelLoop));
        go.transform.SetParent(parent, false);
        PixelLoop loop = go.GetComponent<PixelLoop>();
        loop.spriteRenderer = go.GetComponent<SpriteRenderer>();
        loop.spriteRenderer.sortingLayerName = "Effects";
        loop.spriteRenderer.sortingOrder = sortingOrder;
        loop.frames = effect.frames;
        loop.fps = Mathf.Max(1f, effect.fps);
        // 여러 개가 함께 있을 때 똑같이 움직이지 않게 시작 프레임을 흩는다.
        loop.elapsed = Random.value * effect.frames.Length / loop.fps;
        loop.spriteRenderer.sprite = effect.frames[0];
        return loop;
    }

    // 부모 크기와 상관없이 월드 크기를 맞춘다(부모 몬스터 프리팹 크기가 제각각이라).
    public void SetWorldScale(float scale)
    {
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        spriteRenderer.sprite = frames[Mathf.FloorToInt(elapsed * fps) % frames.Length];
    }
}
