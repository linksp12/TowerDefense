using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StageSelectionButtonMotion : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Button button;
    private Coroutine animation;
    private bool hovered;
    private Vector3 restingScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        restingScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData data) { hovered = true; MoveTo(1.035f); }
    public void OnPointerExit(PointerEventData data) { hovered = false; MoveTo(1f); }
    public void OnPointerDown(PointerEventData data) { MoveTo(0.975f); }
    public void OnPointerUp(PointerEventData data) { MoveTo(hovered ? 1.035f : 1f); }

    private void MoveTo(float scale)
    {
        if (!button.IsInteractable())
            return;
        if (animation != null)
            StopCoroutine(animation);
        animation = StartCoroutine(Animate(scale));
    }

    private IEnumerator Animate(float scale)
    {
        Vector3 from = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / 0.1f);
            transform.localScale = Vector3.Lerp(from, restingScale * scale,
                1f - (1f - progress) * (1f - progress));
            yield return null;
        }
        transform.localScale = restingScale * scale;
        animation = null;
    }

    private void OnDisable()
    {
        if (animation != null)
            StopCoroutine(animation);
        animation = null;
        hovered = false;
        transform.localScale = restingScale;
    }
}
