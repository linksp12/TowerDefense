using System.Collections;
using System.Collections.Generic;
using LastOfTheTower.Progression;
using UnityEngine;
using UnityEngine.UI;

// 지도 위 스테이지 표지판 사이를 점선 길로 잇는다. 진행 기록은 조회만 한다.
// 클리어해서 열린 구간은 밝게, 아직 못 간 구간은 흐리게, 지금 도전할 곳으로 가는 구간은 빛이 흐르게 표시한다.
public class StageSelectionRoute : MonoBehaviour
{
    [Tooltip("길이 이어지는 순서대로 넣는다. 표지판 중앙끼리 이어 표지판 사이 구간만 보이게 한다.")]
    [SerializeField] private RectTransform[] markers;
    [Tooltip("markers와 같은 순서의 스테이지 ID")]
    [SerializeField] private string[] stageIds;
    [SerializeField] private Color litColor = new Color32(255, 214, 120, 255);
    [SerializeField] private Color dimColor = new Color32(232, 222, 196, 130);
    [SerializeField] private Color pulseColor = new Color32(255, 248, 220, 255);
    [SerializeField] private Color outlineColor = new Color32(30, 20, 12, 150);
    [SerializeField] private float dotSize = 8f;
    [SerializeField] private float dotSpacing = 15f;
    [Tooltip("구간 길이에 대한 휘어짐 비율. 구간마다 방향을 번갈아 굽힌다.")]
    [SerializeField] private float bend = 0.12f;
    [SerializeField] private float pulseStepSeconds = 0.05f;

    private readonly List<List<Image>> segments = new List<List<Image>>();
    private Coroutine routine;

    private void OnEnable()
    {
        routine = StartCoroutine(BuildAndAnimate());
    }

    private void OnDisable()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator BuildAndAnimate()
    {
        // 표지판이 같은 프레임에 켜질 수 있으므로 한 프레임 뒤 배치가 끝난 위치를 쓴다.
        yield return null;

        if (markers == null || stageIds == null || markers.Length != stageIds.Length)
        {
            Debug.LogError("StageSelectionRoute: 표지판과 스테이지 ID 연결을 확인하세요.", this);
            yield break;
        }

        if (segments.Count == 0)
        {
            Canvas.ForceUpdateCanvases();
            Build();
        }

        int pulseSegment = ApplyProgress();
        if (pulseSegment < 0)
            yield break;

        // 지금 도전할 스테이지로 가는 구간에 밝은 점이 길을 따라 흐른다.
        List<Image> dots = segments[pulseSegment];
        var wait = new WaitForSecondsRealtime(pulseStepSeconds);
        while (true)
        {
            for (int i = 0; i < dots.Count + 3; i++)
            {
                for (int d = 0; d < dots.Count; d++)
                {
                    int distance = i - d;
                    dots[d].color = distance >= 0 && distance < 3
                        ? Color.Lerp(pulseColor, litColor, distance / 3f)
                        : litColor;
                }
                yield return wait;
            }
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    private void Build()
    {
        var container = (RectTransform)transform;
        for (int i = 0; i + 1 < markers.Length; i++)
        {
            Vector2 from = LocalPoint(container, markers[i]);
            Vector2 to = LocalPoint(container, markers[i + 1]);
            Vector2 delta = to - from;
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
            Vector2 control = (from + to) * 0.5f + normal * delta.magnitude * bend * (i % 2 == 0 ? 1f : -1f);
            segments.Add(CreateDots(container, from, control, to));
        }
    }

    private static Vector2 LocalPoint(RectTransform container, RectTransform marker)
    {
        Vector3 world = marker.TransformPoint(marker.rect.center);
        return container.InverseTransformPoint(world);
    }

    private List<Image> CreateDots(RectTransform container, Vector2 a, Vector2 b, Vector2 c)
    {
        // 곡선을 잘게 나눠 길이를 재고, 같은 간격으로 점을 찍는다.
        const int samples = 64;
        var points = new Vector2[samples + 1];
        var lengths = new float[samples + 1];
        for (int s = 0; s <= samples; s++)
        {
            float t = s / (float)samples;
            points[s] = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;
            if (s > 0)
                lengths[s] = lengths[s - 1] + Vector2.Distance(points[s - 1], points[s]);
        }

        var dots = new List<Image>();
        float total = lengths[samples];
        int count = Mathf.Max(1, Mathf.FloorToInt(total / dotSpacing));
        int index = 1;
        for (int n = 1; n < count; n++)
        {
            float target = total * n / count;
            while (index < samples && lengths[index] < target)
                index++;
            float span = lengths[index] - lengths[index - 1];
            float k = span > 0f ? (target - lengths[index - 1]) / span : 0f;
            Vector2 position = Vector2.Lerp(points[index - 1], points[index], k);

            var dot = new GameObject("RouteDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            var rect = (RectTransform)dot.transform;
            rect.SetParent(container, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(dotSize, dotSize);
            rect.anchoredPosition = position - container.rect.center;
            var image = dot.GetComponent<Image>();
            image.raycastTarget = false;
            var outline = dot.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
            dots.Add(image);
        }
        return dots;
    }

    // 구간 색을 정하고, 빛이 흐를 구간 번호를 돌려준다(없으면 -1).
    private int ApplyProgress()
    {
        var progress = new StageProgressService(new PlayerPrefsStageProgressStorage());
        progress.Load();

        int pulseSegment = -1;
        for (int i = 0; i < segments.Count; i++)
        {
            bool lit = progress.CanWrite && progress.IsCleared(stageIds[i]);
            if (lit && !progress.IsCleared(stageIds[i + 1]))
                pulseSegment = i;
            foreach (Image dot in segments[i])
                dot.color = lit ? litColor : dimColor;
        }
        return pulseSegment;
    }
}
