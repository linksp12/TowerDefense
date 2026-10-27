using UnityEngine;

/// <summary>
/// SpriteRenderer 그림을 정해 둔 프레임으로 계속 넘긴다(광장 NPC 숨쉬기, 분수 물결, 불빛 일렁임).
/// 프레임마다 머무는 시간을 따로 줄 수 있고, 같은 그림이 여럿이어도 똑같이 움직이지 않게 시작 위치를 흩는다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [Tooltip("프레임별 머무는 시간(초). 비어 있으면 모든 프레임이 defaultDuration")]
    [SerializeField] private float[] durations;
    [SerializeField] private float defaultDuration = 0.12f;
    [SerializeField] private bool randomStart = true;

    private SpriteRenderer spriteRenderer;
    private int index;
    private float timer;

    public void Setup(Sprite[] newFrames, float[] newDurations, float fallbackDuration)
    {
        frames = newFrames;
        durations = newDurations;
        defaultDuration = fallbackDuration;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames == null || frames.Length == 0) { enabled = false; return; }
        if (randomStart)
        {
            index = Random.Range(0, frames.Length);
            timer = Random.value * Duration(index);
        }
        spriteRenderer.sprite = frames[index];
    }

    private float Duration(int i) =>
        durations != null && i < durations.Length && durations[i] > 0f ? durations[i] : Mathf.Max(0.02f, defaultDuration);

    private void Update()
    {
        // 광장은 일시정지가 없어 실제 시간 기준으로 돌린다.
        timer += Time.unscaledDeltaTime;
        float current = Duration(index);
        if (timer < current) return;
        timer -= current;
        index = (index + 1) % frames.Length;
        spriteRenderer.sprite = frames[index];
    }
}
