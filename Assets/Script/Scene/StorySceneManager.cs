using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 스토리 씬 매니저 (업데이트 버전)
/// - 스토리 BGM 자동 재생
/// - 버튼 클릭 사운드는 ButtonEffect 컴포넌트가 처리
///   (Next, Skip 버튼에 ButtonEffect 컴포넌트를 추가하면 됨)
/// </summary>
public class StorySceneManager : MonoBehaviour
{
    [Header("배경 이미지 (Story 1, 2, 3 순서)")]
    public Image backgroundImage;
    public Sprite[] backgroundSprites;

    [Header("스토리 UI 패널 이미지 (Story 01, 02, 03 프레임)")]
    public Image storyFrameImage;
    public Sprite[] storyFrameSprites;

    [Header("자막 텍스트")]
    public TextMeshProUGUI storyText;

    [Header("버튼")]
    public Button nextButton;
    public Button skipButton;

    [Header("설정")]
    public float autoAdvanceTime = 7f;

    // ───────── 스토리 텍스트 ─────────
    private readonly string[] stories = new string[]
    {
        "인류는 신소재인 X의 발견과 AI 폭발적인 성장으로 엄청난 발전을 이루었다.\n" +
        "하늘에는 공중도시가 떠오르고, 세계는 거대한 산업 문명으로 뒤덮인다.\n" +
        "사람들은 끝없는 번영이 시작되었다고 믿었다.",

        "하지만 인간의 발전이 계속될수록 세계 곳곳에서 이상 현상이 발생하기 시작한다.\n" +
        "붉게 물든 바다, 거대한 지진, 엄청난 일교차\n" +
        "사람들은 원인을 알 수 없는 재앙 앞에서 불안에 휩싸인다.",

        "어느 날, 정체불명의 생명체들이 도시 외곽부터 출현해 도시 중앙까지 나타나는 지경에 이르렀고,\n" +
        "발전소와 도시 주요시설들을 공격하기 시작한다.\n" +
        "인류는 살아남기 위해 거대한 방어탑을 건설하고 전면전에 돌입한다.\n" +
        "그리고 끝없는 방어 전쟁이 시작된다."
    };

    private int currentIndex = 0;
    private Coroutine autoAdvanceCoroutine;

    // ───────── 초기화 ─────────
    private void Start()
    {
        // 스토리 BGM 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGMForScene("StoryScene");
        }

        // 버튼 이벤트 연결
        nextButton.onClick.AddListener(OnNextButtonClicked);
        skipButton.onClick.AddListener(OnSkipButtonClicked);

        ShowStory(0);
    }

    // ───────── 스토리 표시 ─────────
    private void ShowStory(int index)
    {
        currentIndex = index;

        if (backgroundSprites != null && index < backgroundSprites.Length)
            backgroundImage.sprite = backgroundSprites[index];

        if (storyFrameSprites != null && index < storyFrameSprites.Length)
            storyFrameImage.sprite = storyFrameSprites[index];

        if (storyText != null)
            storyText.text = stories[index];

        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);
        autoAdvanceCoroutine = StartCoroutine(AutoAdvance());
    }

    // ───────── 자동 넘김 ─────────
    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceTime);
        AdvanceStory();
    }

    // ───────── 다음 스토리 ─────────
    private void AdvanceStory()
    {
        int nextIndex = currentIndex + 1;

        if (nextIndex < stories.Length)
            ShowStory(nextIndex);
        else
            LoadGameScene();
    }

    // ───────── 버튼 콜백 ─────────
    public void OnNextButtonClicked()
    {
        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);

        AdvanceStory();
    }

    public void OnSkipButtonClicked()
    {
        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);

        LoadGameScene();
    }

    // ───────── 게임씬 전환 ─────────
    private void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }
}
