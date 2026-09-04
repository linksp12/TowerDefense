using UnityEngine;
using System.Collections;

public class StealthMonster : MonoBehaviour
{
    [Header("은신 설정")]
    [SerializeField] private bool isStealthed = false;
    [SerializeField] private float hideDelay = 1f;
    [SerializeField] private float revealDuration = 3f;

    public bool IsStealthed => isStealthed;

    private Animator animator;
    private MonsterHealth monsterHealth;

    private static readonly int HideTrigger = Animator.StringToHash("Hide");
    private static readonly int RevealTrigger = Animator.StringToHash("Reveal");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        monsterHealth = GetComponent<MonsterHealth>();
        isStealthed = false;
    }

    private void Start()
    {
        UIManager uiManager = FindFirstObjectByType<UIManager>();

        if (uiManager != null)
        {
            uiManager.ShowStealthMonsterTip();
        }

        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        EnterStealth();
    }

    private void EnterStealth()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
            return;

        isStealthed = true;

        if (animator != null)
        {
            animator.SetTrigger(HideTrigger);
        }
    }

    public void Reveal()
    {
        if (!isStealthed)
            return;

        isStealthed = false;

        if (animator != null)
        {
            animator.SetTrigger(RevealTrigger);
        }

        StartCoroutine(RehideAfterDelay());
    }

    private IEnumerator RehideAfterDelay()
    {
        yield return new WaitForSeconds(revealDuration);

        EnterStealth();
    }
}
