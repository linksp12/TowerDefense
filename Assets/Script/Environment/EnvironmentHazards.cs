using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵 기믹. 화산 맵은 가끔 용암이 분출해(예고 후) 주변 타워가 잠깐 과열로 멈추고 몬스터도 피해를 입는다.
/// 설원 맵은 가끔 눈보라가 불어 타워 사거리와 몬스터 이동 속도가 잠시 줄어든다.
/// 웨이브가 시작된 뒤부터 동작하고, 배속·일시정지를 따른다.
/// </summary>
public sealed class EnvironmentHazards : MonoBehaviour
{
    [Header("용암 분출(화산)")]
    public Vector2 eruptionInterval = new Vector2(22f, 32f);
    public float eruptionWarning = 2.2f;
    public float eruptionRadius = 1.7f;
    public float overheatDuration = 3f;
    [Range(0f, 1f)] public float eruptionMonsterDamage = 0.25f;   // 일반 몬스터 최대 체력 비율
    [Range(0f, 1f)] public float eruptionBossDamage = 0.02f;

    [Header("눈보라(설원)")]
    public Vector2 blizzardInterval = new Vector2(34f, 44f);
    public float blizzardWarning = 2.5f;
    public float blizzardDuration = 8f;
    [Range(0.3f, 1f)] public float blizzardRangeMultiplier = 0.8f;
    [Range(0.3f, 1f)] public float blizzardSpeedMultiplier = 0.85f;

    private EnvironmentType type;
    private WaveManager waves;
    private Coroutine routine;
    private bool explainedEruption;
    private bool explainedBlizzard;
    private Canvas overlayCanvas;
    private CanvasGroup snowGroup;
    private AudioClip warningClip, burstClip, windClip;
    private static Sprite pixel;

    public void Begin(EnvironmentType environment)
    {
        Stop();
        type = environment;
        if (type != EnvironmentType.Fire && type != EnvironmentType.Ice)
            return;
        waves = FindAnyObjectByType<WaveManager>();
        warningClip = Resources.Load<AudioClip>("Audio/Hazard/hazard_rumble");
        burstClip = Resources.Load<AudioClip>("Audio/Hazard/hazard_lava_burst");
        windClip = Resources.Load<AudioClip>("Audio/Hazard/hazard_blizzard");
        routine = StartCoroutine(type == EnvironmentType.Fire ? EruptionLoop() : BlizzardLoop());
    }

    public void Stop()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = null;
        ResetBlizzardEffects();
    }

    private void OnDisable() => ResetBlizzardEffects();

    private void ResetBlizzardEffects()
    {
        TowerAttack.EnvironmentRangeMultiplier = 1f;
        MonsterMove.EnvironmentSpeedMultiplier = 1f;
        if (snowGroup != null)
            snowGroup.alpha = 0f;
    }

    private bool Battling =>
        waves != null && waves.Phase != WaveManager.WavePhase.NotStarted &&
        (GameManager.Instance == null || !GameManager.Instance.IsGameEnded());

    private static void PlaySfx(AudioClip clip, float volume)
    {
        if (clip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clip, volume);
    }

    private static void Banner(string title, string subtitle)
    {
        UIManager ui = FindAnyObjectByType<UIManager>();
        if (ui != null)
            ui.ShowHazardBanner(title, subtitle);
    }

    // ───────── 화산: 용암 분출 ─────────
    private IEnumerator EruptionLoop()
    {
        while (true)
        {
            yield return new WaitUntil(() => Battling);
            yield return new WaitForSeconds(Random.Range(eruptionInterval.x, eruptionInterval.y));
            if (!Battling)
                continue;

            // 타워 근처에서 터져야 의미가 있다. 타워가 없으면 이번은 건너뛴다.
            IReadOnlyList<TowerAttack> towers = TowerAttack.ActiveTowers;
            if (towers.Count == 0)
                continue;
            TowerAttack pick = towers[Random.Range(0, towers.Count)];
            Vector3 center = pick.transform.position + (Vector3)(Random.insideUnitCircle * 0.5f);
            yield return Eruption(center);
        }
    }

    private IEnumerator Eruption(Vector3 center)
    {
        if (!explainedEruption)
        {
            explainedEruption = true;
            Banner("<color=#FF8A4C>용암 분출!</color>", "붉은 원 안의 타워가 잠시 과열되어 멈춥니다");
        }
        PlaySfx(warningClip, 0.8f);

        // 예고: 붉은 원이 점점 빠르게 깜빡인다.
        var warning = new GameObject("LavaWarning").AddComponent<SpriteRenderer>();
        warning.sprite = TowerPanelArt.Sprite("range_ring");
        warning.sortingLayerName = "GroundProps";
        warning.sortingOrder = 210;
        warning.transform.position = center;
        warning.transform.localScale = Vector3.one * eruptionRadius;
        // 원 안쪽 붉은 빛(부드러운 원 그림 64px = 지름 0.64칸)
        var glow = new GameObject("LavaGlow").AddComponent<SpriteRenderer>();
        glow.sprite = TowerPanelArt.Sprite("soft_glow");
        glow.sortingLayerName = "GroundProps";
        glow.sortingOrder = 209;
        glow.transform.position = center;
        glow.transform.localScale = Vector3.one * (eruptionRadius * 2f / 0.64f);
        var mark = new GameObject("LavaMark").AddComponent<SpriteRenderer>();
        mark.sprite = TowerPanelArt.Sprite("icon_volcano");
        mark.sortingLayerName = "Effects";
        mark.sortingOrder = 120;
        mark.transform.position = center + Vector3.up * 1.5f;
        mark.transform.localScale = Vector3.one * 5f;
        float t = 0f;
        while (t < eruptionWarning)
        {
            t += Time.deltaTime;
            float blink = 0.5f + 0.5f * Mathf.Sin(t * Mathf.Lerp(6f, 22f, t / eruptionWarning));
            warning.color = new Color(1f, 0.32f, 0.12f, Mathf.Lerp(0.6f, 1f, blink));
            glow.color = new Color(1f, 0.25f, 0.05f, Mathf.Lerp(0.25f, 0.6f, blink) * Mathf.Clamp01(t / 0.4f));
            mark.color = new Color(1f, 1f, 1f, 0.7f + 0.3f * blink);
            mark.transform.position = center + Vector3.up * (1.5f + 0.08f * Mathf.Sin(t * 10f));
            yield return null;
        }
        Destroy(warning.gameObject);
        Destroy(glow.gameObject);
        Destroy(mark.gameObject);
        if (!Battling)
            yield break;

        // 분출
        PlaySfx(burstClip, 1f);
        CombatFeedback.LavaBurst(center, eruptionRadius);
        SpawnLavaSpray(center);
        float radiusSqr = eruptionRadius * eruptionRadius;
        foreach (TowerAttack tower in new List<TowerAttack>(TowerAttack.ActiveTowers))
        {
            if (tower == null || ((Vector2)(tower.transform.position - center)).sqrMagnitude > radiusSqr)
                continue;
            tower.Overheat(overheatDuration);
            StartCoroutine(OverheatMarker(tower));
        }
        foreach (MonsterHealth monster in new List<MonsterHealth>(TowerTargeting.All))
        {
            if (monster == null || monster.IsDead ||
                ((Vector2)(monster.transform.position - center)).sqrMagnitude > radiusSqr)
                continue;
            float ratio = monster.isBoss ? eruptionBossDamage : eruptionMonsterDamage;
            monster.TakeBurnDamage(Mathf.Max(1, Mathf.RoundToInt(monster.maxHp * ratio)));
        }
    }

    // 과열된 타워 위에 불꽃·연기 표시
    private IEnumerator OverheatMarker(TowerAttack tower)
    {
        var flame = new GameObject("Overheat").AddComponent<SpriteRenderer>();
        flame.sprite = TowerPanelArt.Sprite("icon_flame");
        flame.sortingLayerName = "Effects";
        flame.sortingOrder = 110;
        var body = tower.GetComponent<SpriteRenderer>();
        Vector3 top = body != null ? new Vector3(body.bounds.center.x, body.bounds.max.y + 0.15f, 0f) : tower.transform.position + Vector3.up;
        flame.transform.position = top;
        flame.transform.localScale = Vector3.one * 3.5f;
        while (tower != null && tower.IsOverheated)
        {
            float k = Mathf.Sin(Time.time * 18f);
            flame.color = new Color(1f, 0.8f + 0.2f * k, 0.6f, 0.75f + 0.25f * k);
            flame.transform.position = top + Vector3.up * (0.04f * Mathf.Sin(Time.time * 6f));
            if (body != null)
                body.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.4f), 0.5f + 0.5f * k);
            yield return null;
        }
        if (body != null)
            body.color = Color.white;
        Destroy(flame.gameObject);
    }

    // 도트 용암 방울이 튀었다가 떨어진다.
    private void SpawnLavaSpray(Vector3 center)
    {
        if (pixel == null)
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 12f);
        }
        for (int i = 0; i < 26; i++)
            StartCoroutine(LavaDrop(center));
    }

    private IEnumerator LavaDrop(Vector3 center)
    {
        var drop = new GameObject("LavaDrop").AddComponent<SpriteRenderer>();
        drop.sprite = pixel;
        drop.sortingLayerName = "Effects";
        drop.sortingOrder = 115;
        drop.transform.position = center;
        drop.transform.localScale = Vector3.one * Random.Range(0.8f, 1.6f);
        Vector2 velocity = new Vector2(Random.Range(-2.6f, 2.6f), Random.Range(3f, 6.5f));
        float life = Random.Range(0.6f, 1f);
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            velocity.y -= 14f * Time.deltaTime;
            drop.transform.position += (Vector3)(velocity * Time.deltaTime);
            float k = t / life;
            drop.color = Color.Lerp(new Color(1f, 0.85f, 0.3f), new Color(0.6f, 0.12f, 0.05f, 0f), k * k);
            yield return null;
        }
        Destroy(drop.gameObject);
    }

    // ───────── 설원: 눈보라 ─────────
    private IEnumerator BlizzardLoop()
    {
        BuildSnowOverlay();
        while (true)
        {
            yield return new WaitUntil(() => Battling);
            yield return new WaitForSeconds(Random.Range(blizzardInterval.x, blizzardInterval.y));
            if (!Battling)
                continue;

            Banner("<color=#9FD8FF>눈보라가 몰려옵니다</color>",
                explainedBlizzard ? "타워 사거리 감소 · 몬스터 이동 감소"
                    : $"잠시 타워 사거리 -{Mathf.RoundToInt((1f - blizzardRangeMultiplier) * 100)}% · 몬스터 이동 -{Mathf.RoundToInt((1f - blizzardSpeedMultiplier) * 100)}%");
            explainedBlizzard = true;
            yield return Fade(0.35f, blizzardWarning);
            PlaySfx(windClip, 0.9f);

            TowerAttack.EnvironmentRangeMultiplier = blizzardRangeMultiplier;
            MonsterMove.EnvironmentSpeedMultiplier = blizzardSpeedMultiplier;
            yield return Fade(1f, 0.8f);
            float t = 0f;
            while (t < blizzardDuration && Battling)
            {
                t += Time.deltaTime;
                yield return null;
            }
            ResetBlizzardEffects();
            snowGroup.alpha = 1f;
            yield return Fade(0f, 1.2f);
        }
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = snowGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            snowGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        snowGroup.alpha = target;
    }

    // 지도 위·전투 UI 아래에 흩날리는 눈과 하얀 가장자리
    private void BuildSnowOverlay()
    {
        var canvasObject = new GameObject("BlizzardOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = -5;
        snowGroup = canvasObject.GetComponent<CanvasGroup>();
        snowGroup.alpha = 0f;
        snowGroup.blocksRaycasts = false;
        snowGroup.interactable = false;

        // 흰 설원 위에서도 보이게 차가운 푸른 그늘을 깔고, 가장자리는 서리처럼 밝게
        var tint = UiFx.Image("Tint", canvasObject.transform, null, new Color(0.32f, 0.42f, 0.62f, 0.26f));
        UiFx.Stretch(tint.rectTransform);
        var vignette = UiFx.Image("Frost", canvasObject.transform, UiFx.Vignette, new Color(0.88f, 0.95f, 1f, 0.85f));
        UiFx.Stretch(vignette.rectTransform);

        var snow = UiParticleField.Create("Snow", canvasObject.transform);
        snow.palette = new[] { new Color(1f, 1f, 1f, 1f), new Color(0.86f, 0.94f, 1f, 0.95f) };
        snow.sizeRange = new Vector2(5f, 12f);
        snow.speedMin = new Vector2(-340f, -260f);
        snow.speedMax = new Vector2(-160f, -150f);
        snow.lifeRange = new Vector2(3f, 5f);
        snow.rate = 120f;
        snow.maxCount = 420;
        snow.sway = 30f;
        snow.spawnArea = new Rect(0.1f, 0.85f, 1.2f, 0.25f);
        snow.Prewarm(4f);
    }
}
