using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 손맛 연출(발사·타격 효과음, 픽셀 이펙트, 화면 흔들림, 처치 골드 연출)을 한곳에서 처리합니다.
/// 전투 코드는 아래 정적 함수만 호출하고, 소리·그림·수치는 CombatFeelSettings 에셋에서 바꿉니다.
/// 씬마다 처음 쓰일 때 만들어지며 씬과 함께 정리됩니다. 효과음과 이펙트는 재사용(풀링)합니다.
/// </summary>
public sealed class CombatFeedback : MonoBehaviour
{
    private const string SettingsPath = "Combat/CombatFeelSettings";
    private static CombatFeedback instance;
    private static CombatFeelSettings settings;
    private static bool settingsMissingLogged;

    private readonly List<AudioSource> voices = new List<AudioSource>();
    private readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();
    private readonly Stack<PixelEffect> effectPool = new Stack<PixelEffect>();
    private readonly Stack<Image> coinPool = new Stack<Image>();
    private int nextVoice;
    private int flyingCoins;
    private Canvas coinCanvas;
    private Transform effectRoot;

    // ──────── 전투 코드에서 호출하는 함수 ────────

    public static void TowerFired(TowerData.AttackFeel feel, Vector3 muzzlePosition)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        switch (feel)
        {
            case TowerData.AttackFeel.Cannon:
                fx.Play(settings.cannonShot);
                fx.Spawn(settings.muzzleSmokeFx, muzzlePosition, 1f);
                break;
            case TowerData.AttackFeel.Magic:
                fx.Play(settings.magicShot);
                break;
            default:
                fx.Play(settings.arrowShot);
                break;
        }
    }

    // 투사체가 맞은 순간. 폭발하는 공격은 폭발 반경에 맞춰 크게 터진다.
    public static void ProjectileImpact(TowerData.AttackFeel feel, Vector3 position, bool exploded,
        float explosionRadius, bool burns, bool chills)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;

        if (exploded)
        {
            // 폭발 그림(32px, PPU 16 = 지름 2)을 실제 폭발 지름에 맞춘다.
            float scale = Mathf.Max(0.6f, explosionRadius) * settings.explosionFx.scale;
            fx.Spawn(settings.explosionFx, position, scale, false);
            fx.Play(settings.cannonExplode);
            fx.Shake(settings.explosionShake);
            return;
        }

        if (burns)
        {
            fx.Spawn(settings.fireHitFx, position, 1f);
            fx.Play(settings.fireHit);
        }
        else if (chills)
        {
            fx.Spawn(settings.frostHitFx, position, 1f);
            fx.Play(settings.frostHit);
        }
        else if (feel == TowerData.AttackFeel.Magic)
        {
            fx.Spawn(settings.magicHitFx, position, 1f);
            fx.Play(settings.magicHit);
        }
        else if (feel == TowerData.AttackFeel.Cannon)
        {
            // 폭발하지 않는 포탄(공성 루트)도 무거운 한 방이 느껴지게 작은 폭발과 흔들림을 준다.
            fx.Spawn(settings.explosionFx, position, 0.6f * settings.explosionFx.scale, false);
            fx.Play(settings.cannonExplode, 0.85f);
            fx.Shake(settings.explosionShake * 0.7f);
        }
        else
        {
            fx.Spawn(settings.arrowHitFx, position, 1f);
            fx.Play(settings.arrowHit);
        }
    }

    public static void CriticalHit()
    {
        CombatFeedback fx = Get();
        if (fx != null)
            fx.Play(settings.critical);
    }

    // 처치 순간: 연기, 처치음, 골드 숫자와 동전이 왼쪽 위 골드 칸으로 날아가는 연출.
    public static void MonsterKilled(Vector3 position, int goldReward, bool boss)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.deathPuffFx, position, boss ? 3f : 1f);
        fx.Play(settings.killPop);
        if (boss)
            fx.Shake(settings.bossDeathShake);
        if (goldReward > 0)
        {
            DamagePopup.ShowGold(position, goldReward);
            fx.FlyCoin(position);
        }
    }

    // 기지가 맞은 순간: 충격파·피격음·흔들림. 금이 갈 때는 돌 파편·균열음·더 큰 흔들림을 더한다.
    public static void BaseHit(Vector3 position, bool cracked)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.baseShockwaveFx, position, 1f);
        fx.Play(settings.baseHit);
        fx.Shake(settings.baseHitShake);
        if (cracked)
        {
            fx.Spawn(settings.baseDebrisFx, position, 1.2f);
            fx.Play(settings.baseCrack);
            fx.Shake(settings.baseCrackShake);
        }
    }

    // 위험 상태 기지에서 피어오르는 연기
    public static void BaseSmoke(Vector3 position)
    {
        CombatFeedback fx = Get();
        if (fx != null)
            fx.Spawn(settings.muzzleSmokeFx, position, 1.6f);
    }

    // ──────── 새 타워(레이저·저격·감전)와 화상 ────────

    // 지속 레이저 한 번 타격. 오래 맞힐수록(strength 0~1) 소리가 높아지고 불꽃이 커진다.
    public static void LaserTick(Vector3 hitPosition, float strength)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.laserSparkFx, hitPosition, Mathf.Lerp(0.8f, 1.35f, strength));
        fx.Play(settings.laserTick, Mathf.Lerp(0.92f, 1.25f, strength));
    }

    public static void LaserChargeFired(Vector3 muzzlePosition)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.laserSparkFx, muzzlePosition, 1.3f);
        fx.Play(settings.laserChargeShot);
    }

    // 충전 레이저 폭발. 그림(32px, PPU 16 = 지름 2)을 폭발 반경에 맞춘다.
    public static void EnergyBlast(Vector3 position, float radius)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.energyBlastFx, position, Mathf.Max(0.7f, radius) * settings.energyBlastFx.scale, false);
        fx.Play(settings.energyBlast);
        fx.Shake(settings.energyBlastShake);
    }

    public static void SniperFired(Vector3 muzzlePosition, bool critical)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.sniperMuzzleFx, muzzlePosition, critical ? 1.4f : 1f);
        fx.Play(settings.sniperShot);
        if (critical)
            fx.Shake(settings.sniperCriticalShake);
    }

    // 저격탄이 몬스터를 뚫은 순간. 파편(A 경로)은 작게, 폭발 타격(B 경로)은 작은 폭발로,
    // 화상이 붙으면 불꽃도 함께 터진다. 치명타는 타격 효과를 크게 키운다.
    public static void SniperHit(Vector3 position, bool fragment, bool explosion, bool burns, bool critical)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.sniperHitFx, position, fragment ? 0.55f : critical ? 1.6f : 1f);
        fx.Play(settings.sniperPierce, fragment ? 1.25f : 1f);
        if (explosion)
        {
            fx.Spawn(settings.explosionFx, position, 0.5f * settings.explosionFx.scale, false);
            fx.Play(settings.cannonExplode, 1.15f);
        }
        if (burns)
            fx.Spawn(settings.fireHitFx, position, 1f);
    }

    public static void ShockHit(Vector3 position)
    {
        CombatFeedback fx = Get();
        if (fx != null)
            fx.Spawn(settings.shockHitFx, position, 1f);
    }

    public static void ShockFired()
    {
        CombatFeedback fx = Get();
        if (fx != null)
            fx.Play(settings.shockZap);
    }

    public static void BurnTick()
    {
        CombatFeedback fx = Get();
        if (fx != null)
            fx.Play(settings.burnTick);
    }

    public static CombatFeelSettings Settings
    {
        get
        {
            if (settings == null)
                settings = Resources.Load<CombatFeelSettings>(SettingsPath);
            return settings;
        }
    }

    public static void BossAppeared()
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Play(settings.bossRoar);
        fx.Shake(settings.bossAppearShake);
    }

    // 맵 기믹: 용암 분출. 반경에 맞춘 큰 폭발과 흔들림(소리는 기믹 쪽에서 낸다).
    public static void LavaBurst(Vector3 position, float radius)
    {
        CombatFeedback fx = Get();
        if (fx == null)
            return;
        fx.Spawn(settings.explosionFx, position, Mathf.Max(0.8f, radius) * settings.explosionFx.scale, false);
        fx.Spawn(settings.fireHitFx, position + Vector3.up * 0.3f, 1.4f);
        fx.Shake(settings.explosionShake * 1.3f);
    }

    // 결과창이 뜰 때 남은 연출을 모두 정리한다.
    public static void ClearAll()
    {
        if (instance == null)
            return;
        instance.StopAllCoroutines();
        foreach (Transform child in instance.effectRoot)
        {
            if (child.TryGetComponent(out PixelEffect effect))
                effect.Finish();
        }
        if (instance.coinCanvas != null)
        {
            foreach (Transform child in instance.coinCanvas.transform)
            {
                child.DOKill();
                if (child.gameObject.activeSelf)
                    instance.ReleaseCoin(child.GetComponent<Image>());
            }
        }
        instance.flyingCoins = 0;
    }

    // ──────── 내부 처리 ────────

    private static CombatFeedback Get()
    {
        if (instance != null)
            return instance;
        if (settings == null)
            settings = Resources.Load<CombatFeelSettings>(SettingsPath);
        if (settings == null)
        {
            if (!settingsMissingLogged)
                Debug.LogWarning($"CombatFeedback: Resources/{SettingsPath} 설정이 없어 전투 연출을 건너뜁니다.");
            settingsMissingLogged = true;
            return null;
        }

        // 현재 전투 씬에 속한 오브젝트로 만들어 씬이 바뀌면 함께 정리된다.
        var go = new GameObject("CombatFeedback");
        instance = go.AddComponent<CombatFeedback>();
        instance.Build();
        return instance;
    }

    private void Build()
    {
        for (int i = 0; i < settings.maxVoices; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            voices.Add(source);
        }

        effectRoot = new GameObject("Effects").transform;
        effectRoot.SetParent(transform, false);

        var canvasObject = new GameObject("CoinCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);
        coinCanvas = canvasObject.GetComponent<Canvas>();
        coinCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 전투 UI(0) 바로 위: 동전이 골드 칸 위로 날아가 보이게 한다.
        coinCanvas.sortingOrder = 5;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Play(CombatFeelSettings.Sound sound, float pitch = 1f)
    {
        if (sound == null || sound.clip == null)
            return;
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(sound.clip, out float last) && now - last < sound.minInterval)
            return;
        lastPlayed[sound.clip] = now;

        // 쉬고 있는 채널을 먼저 쓰고, 모두 바쁘면 가장 오래된 채널을 끊는다.
        AudioSource voice = null;
        for (int i = 0; i < voices.Count; i++)
        {
            AudioSource candidate = voices[(nextVoice + i) % voices.Count];
            if (!candidate.isPlaying)
            {
                voice = candidate;
                break;
            }
        }
        if (voice == null)
            voice = voices[nextVoice];
        nextVoice = (voices.IndexOf(voice) + 1) % voices.Count;

        voice.clip = sound.clip;
        voice.volume = sound.volume * AudioManager.SfxGroupVolume;
        voice.pitch = pitch * (1f + Random.Range(-sound.pitchJitter, sound.pitchJitter));
        voice.Play();
    }

    private void Spawn(CombatFeelSettings.Effect effect, Vector3 position, float scale, bool applyEffectScale = true)
    {
        if (effect == null || effect.frames == null || effect.frames.Length == 0)
            return;
        PixelEffect instanceEffect = effectPool.Count > 0 ? effectPool.Pop() : CreateEffect();
        instanceEffect.Play(effect.frames, effect.fps, position, applyEffectScale ? scale * effect.scale : scale,
            done => effectPool.Push(done));
    }

    private PixelEffect CreateEffect()
    {
        var go = new GameObject("PixelEffect", typeof(SpriteRenderer), typeof(PixelEffect));
        go.transform.SetParent(effectRoot, false);
        go.SetActive(false);
        return go.GetComponent<PixelEffect>();
    }

    private void Shake(float amount)
    {
        Camera camera = Camera.main;
        if (camera == null || amount <= 0f)
            return;
        if (!camera.TryGetComponent(out CameraShake shake))
            shake = camera.gameObject.AddComponent<CameraShake>();
        shake.Add(amount, settings.maxShakeOffset);
    }

    private void FlyCoin(Vector3 worldPosition)
    {
        if (settings.coinIcon == null || flyingCoins >= settings.maxFlyingCoins || Camera.main == null)
            return;
        RectTransform target = GameManager.Instance != null && GameManager.Instance.moneyText != null
            ? GameManager.Instance.moneyText.rectTransform
            : null;
        if (target == null)
            return;

        Vector3 screen = Camera.main.WorldToScreenPoint(worldPosition);
        if (screen.z < 0f)
            return;
        StartCoroutine(FlyCoinRoutine(screen, target));
    }

    private IEnumerator FlyCoinRoutine(Vector3 start, RectTransform target)
    {
        flyingCoins++;
        Image coin = coinPool.Count > 0 ? coinPool.Pop() : CreateCoin();
        RectTransform rect = coin.rectTransform;
        coin.gameObject.SetActive(true);
        rect.position = start;
        rect.localScale = Vector3.zero;

        // 톡 튀어나온 뒤 골드 칸 왼쪽(아이콘 쪽)으로 포물선을 그리며 빨려 들어간다.
        Vector3 pop = start + new Vector3(Random.Range(-14f, 14f), 26f, 0f);
        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.16f);
            rect.position = Vector3.Lerp(start, pop, 1f - (1f - p) * (1f - p));
            rect.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, p);
            yield return null;
        }

        t = 0f;
        const float flight = 0.5f;
        while (t < flight)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / flight);
            Vector3 end = TargetPoint(target);
            Vector3 control = new Vector3(pop.x, Mathf.Max(pop.y, end.y) + 60f, 0f);
            float eased = p * p;
            Vector3 a = Vector3.Lerp(pop, control, eased);
            Vector3 b = Vector3.Lerp(control, end, eased);
            rect.position = Vector3.Lerp(a, b, eased);
            rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.75f, p);
            yield return null;
        }

        ReleaseCoin(coin);
        flyingCoins = Mathf.Max(0, flyingCoins - 1);
        Play(settings.coin);
        target.DOKill(true);
        target.localScale = Vector3.one;
        target.DOPunchScale(Vector3.one * 0.22f, 0.22f, 6, 0.6f).SetUpdate(true);
    }

    private static Vector3 TargetPoint(RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // 골드 글자의 왼쪽 가운데(동전 아이콘 바로 옆)
        return (corners[0] + corners[1]) * 0.5f;
    }

    private Image CreateCoin()
    {
        var go = new GameObject("FlyingCoin", typeof(RectTransform), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(coinCanvas.transform, false);
        var image = go.GetComponent<Image>();
        image.sprite = settings.coinIcon;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(26f, 26f);
        return image;
    }

    private void ReleaseCoin(Image coin)
    {
        if (coin == null)
            return;
        coin.gameObject.SetActive(false);
        coinPool.Push(coin);
    }
}
