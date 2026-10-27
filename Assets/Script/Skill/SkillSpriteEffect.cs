using UnityEngine;

/// <summary>스프라이트 프레임 재생과 수명만 처리하는 표시 전용 컴포넌트.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SkillSpriteEffect : MonoBehaviour
{
    [SerializeField] private SkillVisualProfile towerBuffProfile;
    private SpriteRenderer visual;
    private SpriteRenderer followedTower;
    private Sprite[] frames;
    private float start, duration, fps;
    private bool loop;
    private int frame = -1;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        if (towerBuffProfile == null) return;
        followedTower = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
        Initialize(towerBuffProfile.statusFrames, 0, towerBuffProfile.statusFps, true);
    }

    public void Initialize(Sprite[] sequence, float lifetime, float framesPerSecond = 8, bool repeat = false)
    {
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        frames = sequence; duration = Mathf.Max(0.01f, lifetime); fps = framesPerSecond;
        loop = repeat; start = Time.time; frame = -1;
        SetFrame(0);
    }

    private void SetFrame(int index)
    {
        if (frames == null || frames.Length == 0 || index == frame) return;
        frame = index; visual.sprite = frames[index];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;
        float elapsed = Time.time - start;
        if (!loop && elapsed >= duration) { Destroy(gameObject); return; }
        int index = loop ? (int)(elapsed * fps) % frames.Length
            : Mathf.Min(frames.Length - 1, (int)(elapsed / duration * frames.Length));
        SetFrame(index);
        if (!loop)
        {
            Color color = visual.color;
            color.a = 1 - Mathf.InverseLerp(0.75f, 1, elapsed / duration);
            visual.color = color;
        }
    }

    private void LateUpdate()
    {
        if (followedTower == null || visual.sprite == null) return;
        // 업그레이드로 그림 높이가 달라져도 표식은 타워 상단을 따라간다.
        SkillVisuals.FitWidth(visual, followedTower.bounds.size.x * 1.2f);
        Bounds bounds = followedTower.bounds;
        transform.position = new Vector3(bounds.center.x,
            bounds.max.y + 0.45f - visual.sprite.bounds.max.y * Mathf.Abs(transform.lossyScale.y), bounds.center.z);
    }
}
