using UnityEngine;

/// <summary>
/// 카메라를 잠깐 흔듭니다. 흔들림 양(trauma)을 쌓아 두고 시간이 지나면 줄어들며, 멈추면 원래 위치로 돌아갑니다.
/// 설정의 '화면 흔들림' 강도를 곱해 적용합니다.
/// </summary>
public sealed class CameraShake : MonoBehaviour
{
    private const float DecayPerSecond = 1.8f;
    private Vector3 restPosition;
    private float trauma;
    private float seed;
    private float maxOffset = 0.35f;

    public void Add(float amount, float maxShakeOffset)
    {
        if (amount <= 0f)
            return;
        if (trauma <= 0f)
            restPosition = transform.localPosition;
        maxOffset = maxShakeOffset;
        trauma = Mathf.Min(1f, trauma + amount);
        seed = Random.value * 100f;
    }

    private void LateUpdate()
    {
        if (trauma <= 0f)
            return;

        // 일시정지 중에도 자연스럽게 가라앉도록 실제 시간을 쓴다.
        trauma = Mathf.Max(0f, trauma - DecayPerSecond * Time.unscaledDeltaTime);
        if (trauma <= 0f)
        {
            transform.localPosition = restPosition;
            return;
        }

        float power = trauma * trauma * maxOffset * SettingsPopup.ScreenShakeIntensity;
        float time = Time.unscaledTime * 28f;
        float x = (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(seed + 50f, time) - 0.5f) * 2f;
        transform.localPosition = restPosition + new Vector3(x, y, 0f) * power;
    }

    private void OnDisable()
    {
        if (trauma > 0f)
            transform.localPosition = restPosition;
        trauma = 0f;
    }
}
