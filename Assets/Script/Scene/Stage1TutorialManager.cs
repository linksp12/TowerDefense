using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Stage1 전용: 연습 웨이브를 실제 1웨이브와 분리한다.
public class Stage1TutorialManager : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private RectTransform topLeftHud;
    [SerializeField] private BuildPoint tutorialBuildPoint;
    [SerializeField] private TowerBuildManager towerBuildManager;
    [SerializeField] private Button bowTowerCard;
    [SerializeField] private Button cannonTowerCard;
    [SerializeField] private Button magicTowerCard;
    [SerializeField] private RectTransform skillPanel;
    [SerializeField] private WaveData practiceWave;

    private enum TutorialStep
    {
        Intro, Hud, BuildPoint, BowCard, TowerInfo,
        UpgradePanel, SkillIntro, PracticeWave, PracticeDone,
        FinishPrompt, Complete
    }
    private TutorialStep step;
    private enum CalloutSide { Right, Left, Below, Above }

    private RectTransform tutorialRect;
    private RectTransform panelRect;
    private GameObject arrowObject;
    private RectTransform calloutTarget;
    private Transform worldCalloutTarget;
    private RectTransform[] skillButtonRects;
    private RectTransform[] arrowLines;
    private RectTransform[] targetOutline;
    private readonly Vector3[] targetCorners = new Vector3[4];
    private Coroutine moveCoroutine;
    private bool isMoving;
    private TextMeshProUGUI primaryButtonLabel;
    private BuildPoint[] buildPoints;
    private Collider2D[] buildPointColliders;
    private bool[] originalColliderStates;
    private Collider2D tutorialPointCollider;
    private SpriteRenderer tutorialPointRenderer;
    private bool cannonCardOriginallyInteractable;
    private bool magicCardOriginallyInteractable;
    private bool towerCardsCached;
    private bool towerWasBuilt;
    private TowerUpgrade installedTower;
    private SkillManager skillManager;
    private Coroutine practiceCoroutine;
    private readonly List<TutorialPracticeMonster> practiceMonsters =
        new List<TutorialPracticeMonster>();
    private bool practiceSpawningDone;
    private bool practiceSkillUsed;

    private void Start()
    {
        skillManager = SkillManager.Instance;
        if (waveManager == null || tutorialRoot == null || introPanel == null ||
            guideText == null || startButton == null || skipButton == null || topLeftHud == null ||
            tutorialBuildPoint == null || towerBuildManager == null || bowTowerCard == null ||
            cannonTowerCard == null || magicTowerCard == null || skillPanel == null ||
            practiceWave == null || waveManager.spawner == null ||
            waveManager.spawner.waypoints == null ||
            waveManager.spawner.waypoints.Length == 0 || skillManager == null)
        {
            Debug.LogError("Stage1TutorialManager: 튜토리얼 UI 연결이 누락됐습니다.");
            if (tutorialRoot != null)
                tutorialRoot.SetActive(false);
            if (waveManager != null)
                waveManager.StartWaves();
            enabled = false;
            return;
        }

        tutorialRoot.transform.SetAsLastSibling();
        tutorialRoot.SetActive(true);
        introPanel.SetActive(true);
        tutorialRect = tutorialRoot.GetComponent<RectTransform>();
        panelRect = introPanel.GetComponent<RectTransform>();
        tutorialPointCollider = tutorialBuildPoint.GetComponent<Collider2D>();
        tutorialPointRenderer = tutorialBuildPoint.GetComponent<SpriteRenderer>();
        if (tutorialRect == null || panelRect == null || tutorialPointCollider == null ||
            tutorialPointRenderer == null)
        {
            Debug.LogError("Stage1TutorialManager: 안내 대상 또는 설치 지점 구성 요소가 없습니다.");
            tutorialRoot.SetActive(false);
            waveManager.StartWaves();
            enabled = false;
            return;
        }

        SkillButton[] skillButtons = skillPanel.GetComponentsInChildren<SkillButton>(true);
        skillButtonRects = new RectTransform[skillButtons.Length];
        for (int i = 0; i < skillButtons.Length; i++)
            skillButtonRects[i] = skillButtons[i].GetComponent<RectTransform>();

        CacheBuildPointColliders();
        SetBuildPointAccess(false);
        cannonCardOriginallyInteractable = cannonTowerCard.interactable;
        magicCardOriginallyInteractable = magicTowerCard.interactable;
        towerCardsCached = true;
        step = TutorialStep.Intro;

        guideText.text = "처음이신가요?\n간단한 안내를 진행할게요.";
        guideText.raycastTarget = false;
        guideText.fontSize = 32f;
        guideText.alignment = TextAlignmentOptions.Center;

        RectTransform guideRect = guideText.rectTransform;
        guideRect.anchoredPosition = new Vector2(0f, 45f);
        guideRect.sizeDelta = new Vector2(520f, 90f);

        Image panelImage = introPanel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.color = new Color(0.06f, 0.08f, 0.13f, 0.95f);

        primaryButtonLabel = CreateButtonLabel(startButton, "시작하기");
        CreateButtonLabel(skipButton, "건너뛰기");

        startButton.onClick.AddListener(AdvanceTutorial);
        skipButton.onClick.AddListener(SkipTutorial);
    }

    private void Update()
    {
        if (!tutorialRoot.activeInHierarchy)
            return;

        if (step == TutorialStep.BuildPoint && towerBuildManager.IsOpen)
        {
            step = TutorialStep.BowCard;
            tutorialRoot.transform.SetAsLastSibling();
            ShowTargetCallout(bowTowerCard.GetComponent<RectTransform>(),
                "화살 타워를 선택해 설치해보세요.\n이 타워는 빠르게 공격합니다.");
        }
        else if (step == TutorialStep.BowCard)
        {
            if (!tutorialPointCollider.enabled && !tutorialPointRenderer.enabled)
            {
                OnTowerBuilt();
            }
            else if (!towerBuildManager.IsOpen)
            {
                step = TutorialStep.BuildPoint;
                ShowWorldCallout(tutorialBuildPoint.transform,
                    "표시된 설치 지점을 클릭하세요.\n설치창이 열리면 화살 타워를 선택해보세요.");
            }
        }
        else if (step == TutorialStep.TowerInfo &&
                 TowerUpgradeUI.Instance != null && TowerUpgradeUI.Instance.IsOpen)
        {
            if (!EnsureUpgradePanelVisible())
                return;

            step = TutorialStep.UpgradePanel;
            tutorialRoot.transform.SetAsLastSibling();
            ShowTargetCallout(TowerUpgradeUI.Instance.pathAButton.GetComponent<RectTransform>(),
                "업그레이드 루트를 선택해보세요.\n강화하거나 창을 닫으면 다음으로 넘어가요.");
        }
        else if (step == TutorialStep.UpgradePanel &&
                 (installedTower != null && installedTower.level > 1 ||
                  TowerUpgradeUI.Instance == null || !TowerUpgradeUI.Instance.IsOpen))
        {
            BeginSkillIntro();
        }
    }

    private bool EnsureUpgradePanelVisible()
    {
        TowerUpgradeUI upgradeUI = TowerUpgradeUI.Instance;
        if (upgradeUI == null || upgradeUI.panel == null || installedTower == null)
            return false;

        // 일부 테스트 씬에서 패널의 부모가 비활성화되어 Open()의
        // isOpen만 true가 되고 실제 패널은 표시되지 않는 경우를 복구한다.
        if (!upgradeUI.panel.activeInHierarchy)
        {
            Transform activeCanvas = tutorialRoot.transform.parent;
            if (activeCanvas == null || !activeCanvas.gameObject.activeInHierarchy)
                return false;

            upgradeUI.panel.transform.SetParent(activeCanvas, false);
            Canvas canvas = activeCanvas.GetComponent<Canvas>();
            if (canvas != null)
                upgradeUI.canvas = canvas;
            upgradeUI.Open(installedTower);
        }

        return upgradeUI.panel.activeInHierarchy;
    }

    private void LateUpdate()
    {
        if ((calloutTarget == null && worldCalloutTarget == null) ||
            isMoving || !introPanel.activeInHierarchy)
            return;

        GetCalloutPlacement(out Vector2 panelPosition, out Rect targetBounds, out CalloutSide side);
        panelRect.anchoredPosition = panelPosition;
        UpdateArrow(targetBounds, side);
    }

    private void OnDestroy()
    {
        step = TutorialStep.Complete;
        if (skillManager != null)
            skillManager.SkillUsed -= OnPracticeSkillUsed;
        foreach (TutorialPracticeMonster monster in practiceMonsters)
        {
            if (monster != null)
                monster.SetRemovalCallback(null);
        }
        RestoreBuildPointColliders();
        RestoreTowerCards();

        if (startButton != null)
            startButton.onClick.RemoveListener(AdvanceTutorial);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipTutorial);
    }

    private void AdvanceTutorial()
    {
        switch (step)
        {
            case TutorialStep.Intro:
                BeginTutorial();
                break;
            case TutorialStep.Hud:
                BeginTowerPlacement();
                break;
            case TutorialStep.SkillIntro:
                BeginPracticeWave();
                break;
            case TutorialStep.PracticeDone:
                ShowFinishPrompt();
                break;
            case TutorialStep.FinishPrompt:
                FinishTutorial();
                break;
        }
    }

    public void BeginTutorial()
    {
        if (step != TutorialStep.Intro)
            return;

        PlayClickSound();
        step = TutorialStep.Hud;
        skipButton.gameObject.SetActive(false);
        startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -65f);
        primaryButtonLabel.text = "다음";
        ShowTargetCallout(topLeftHud,
            "왼쪽 위에는 골드·기지 체력·웨이브가 있어요.\n다음을 눌러 타워를 설치해보세요.");
    }

    private void BeginTowerPlacement()
    {
        PlayClickSound();
        step = TutorialStep.BuildPoint;
        startButton.gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            int shortfall = towerBuildManager.basicTowerCost - GameManager.Instance.GetCurrentMoney();
            if (shortfall > 0)
                GameManager.Instance.AddMoney(shortfall);
        }

        cannonTowerCard.interactable = false;
        magicTowerCard.interactable = false;
        SetBuildPointAccess(true);
        ShowWorldCallout(tutorialBuildPoint.transform,
            "표시된 설치 지점을 클릭하세요.\n설치창이 열리면 화살 타워를 선택해보세요.");
    }

    private void OnTowerBuilt()
    {
        towerWasBuilt = true;
        if (towerBuildManager.IsOpen)
            towerBuildManager.CloseBuildPanelInstantly();
        RestoreBuildPointColliders();
        RestoreTowerCards();
        tutorialRoot.transform.SetAsLastSibling();
        BeginTowerInfo();
    }

    private void BeginTowerInfo()
    {
        step = TutorialStep.TowerInfo;
        startButton.gameObject.SetActive(false);
        installedTower = null;
        TowerUpgrade[] towers = FindObjectsByType<TowerUpgrade>(FindObjectsSortMode.None);
        foreach (TowerUpgrade tower in towers)
        {
            if (tower.GetOwnerBuildPoint() == tutorialBuildPoint)
            {
                installedTower = tower;
                break;
            }
        }

        ShowWorldCallout(tutorialBuildPoint.transform,
            "화살 타워 설치 완료! 타워를 눌러보세요.\n업그레이드 창에서 강화하거나 창을 닫을 수 있어요.");
    }

    private void BeginSkillIntro()
    {
        if (towerBuildManager.IsOpen)
            towerBuildManager.CloseBuildPanelInstantly();
        if (TowerUpgradeUI.Instance != null && TowerUpgradeUI.Instance.IsOpen)
            TowerUpgradeUI.Instance.CloseInstantly();

        step = TutorialStep.SkillIntro;
        startButton.gameObject.SetActive(true);
        primaryButtonLabel.text = "연습 시작";
        tutorialRoot.transform.SetAsLastSibling();
        ShowTargetCallout(skillPanel,
            "오른쪽 아래의 스킬을 연습해볼게요.\n연습 시작을 누르면 몬스터가 나옵니다.");
    }

    private void BeginPracticeWave()
    {
        PlayClickSound();
        step = TutorialStep.PracticeWave;
        startButton.gameObject.SetActive(false);
        practiceSpawningDone = false;
        practiceSkillUsed = false;
        skillManager.ResetAllCooldowns();
        skillManager.SkillUsed += OnPracticeSkillUsed;
        ShowTargetCallout(skillPanel,
            "연습 웨이브예요!\n몬스터가 나오면 스킬을 한 번 사용해보세요.");
        practiceCoroutine = StartCoroutine(RunPracticeWave());
    }

    private IEnumerator RunPracticeWave()
    {
        yield return new WaitForSeconds(0.8f);

        // Stage 1의 경로만 빌려 쓰고, 실제 WaveManager/MonsterSpawner의
        // 웨이브 상태나 내부 스폰 카운터는 건드리지 않는다.
        foreach (WaveData.SpawnInfo info in practiceWave.spawnInfos)
        {
            if (info == null || info.monsterPrefab == null)
                continue;

            for (int i = 0; i < info.count; i++)
            {
                Transform[] route = waveManager.spawner.waypoints;
                GameObject monster = Instantiate(info.monsterPrefab,
                    route[0].position, Quaternion.identity);
                MonsterMove move = monster.GetComponent<MonsterMove>();
                if (move != null)
                    move.waypoints = route;
                OnPracticeMonsterSpawned(monster);
                yield return new WaitForSeconds(Mathf.Max(0.05f, info.interval));
            }
        }

        practiceSpawningDone = true;
        practiceCoroutine = null;
        TryCompletePractice();
    }

    private void OnPracticeMonsterSpawned(GameObject monster)
    {
        if (monster == null)
            return;

        TutorialPracticeMonster marker = monster.AddComponent<TutorialPracticeMonster>();
        marker.SetRemovalCallback(OnPracticeMonsterRemoved);
        practiceMonsters.Add(marker);
        MonsterMove move = monster.GetComponent<MonsterMove>();
        if (move != null)
            move.moveSpeed = Mathf.Min(move.moveSpeed, 1.4f);
    }

    private void OnPracticeMonsterRemoved(TutorialPracticeMonster marker)
    {
        practiceMonsters.Remove(marker);
        TryCompletePractice();
    }

    private void OnPracticeSkillUsed(string skillName)
    {
        if (step != TutorialStep.PracticeWave)
            return;

        practiceSkillUsed = true;
        guideText.text = "스킬 사용 성공!\n남은 연습 몬스터를 막아보세요.";
        TryCompletePractice();
    }

    private void TryCompletePractice()
    {
        if (step != TutorialStep.PracticeWave || !practiceSpawningDone)
            return;

        practiceMonsters.RemoveAll(monster => monster == null);
        if (practiceMonsters.Count > 0)
            return;

        if (!practiceSkillUsed)
        {
            guideText.text = "연습 몬스터 처리가 끝났어요.\n오른쪽 아래 스킬을 한 번 눌러보세요.";
            return;
        }

        skillManager.SkillUsed -= OnPracticeSkillUsed;
        step = TutorialStep.PracticeDone;
        startButton.gameObject.SetActive(true);
        primaryButtonLabel.text = "다음";
        startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -65f);
        ShowTargetCallout(topLeftHud,
            "웨이브는 몬스터가 몰려오는 한 차례의 공격이에요.\n막아내면 다음 웨이브로 넘어갑니다.");
    }

    private void ShowFinishPrompt()
    {
        PlayClickSound();
        step = TutorialStep.FinishPrompt;
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = null;
        isMoving = false;
        calloutTarget = null;
        worldCalloutTarget = null;
        if (arrowObject != null)
            arrowObject.SetActive(false);

        tutorialRoot.transform.SetAsLastSibling();
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600f, 260f);
        guideText.text = "튜토리얼 완료!\n이제 실제 1웨이브부터 플레이해보세요.";
        guideText.fontSize = 28f;
        guideText.alignment = TextAlignmentOptions.Center;
        guideText.rectTransform.anchoredPosition = new Vector2(0f, 45f);
        guideText.rectTransform.sizeDelta = new Vector2(540f, 100f);
        primaryButtonLabel.text = "튜토리얼 종료";
        startButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -65f);
    }

    public void SkipTutorial()
    {
        FinishTutorial();
    }

    private void FinishTutorial()
    {
        if (step == TutorialStep.Complete)
            return;

        PlayClickSound();
        step = TutorialStep.Complete;
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        if (practiceCoroutine != null)
            StopCoroutine(practiceCoroutine);
        foreach (TutorialPracticeMonster monster in practiceMonsters)
        {
            if (monster == null)
                continue;
            monster.SetRemovalCallback(null);
            Destroy(monster.gameObject);
        }
        practiceMonsters.Clear();
        if (skillManager != null)
        {
            skillManager.SkillUsed -= OnPracticeSkillUsed;
            skillManager.ResetAllCooldowns();
        }
        calloutTarget = null;
        worldCalloutTarget = null;
        RestoreBuildPointColliders();
        RestoreTowerCards();
        if (towerBuildManager != null && towerBuildManager.IsOpen)
            towerBuildManager.CloseBuildPanelInstantly();
        if (TowerUpgradeUI.Instance != null && TowerUpgradeUI.Instance.IsOpen)
            TowerUpgradeUI.Instance.CloseInstantly();
        if (BossInfoUI.Instance != null)
            BossInfoUI.Instance.SetSuppressedByTowerPanel(false);
        tutorialRoot.SetActive(false);
        waveManager.StartWaves();
    }

    private void CacheBuildPointColliders()
    {
        buildPoints = FindObjectsByType<BuildPoint>(FindObjectsSortMode.None);
        buildPointColliders = new Collider2D[buildPoints.Length];
        originalColliderStates = new bool[buildPoints.Length];
        for (int i = 0; i < buildPoints.Length; i++)
        {
            buildPointColliders[i] = buildPoints[i].GetComponent<Collider2D>();
            originalColliderStates[i] = buildPointColliders[i] != null &&
                                        buildPointColliders[i].enabled;
        }
    }

    private void SetBuildPointAccess(bool allowTutorialPoint)
    {
        if (buildPointColliders == null)
            return;

        for (int i = 0; i < buildPointColliders.Length; i++)
        {
            if (buildPointColliders[i] != null)
                buildPointColliders[i].enabled = allowTutorialPoint &&
                    buildPoints[i] == tutorialBuildPoint && originalColliderStates[i];
        }
    }

    private void RestoreBuildPointColliders()
    {
        if (buildPointColliders == null)
            return;

        for (int i = 0; i < buildPointColliders.Length; i++)
        {
            if (buildPointColliders[i] == null ||
                (towerWasBuilt && buildPoints[i] == tutorialBuildPoint))
                continue;

            buildPointColliders[i].enabled = originalColliderStates[i];
        }
    }

    private void RestoreTowerCards()
    {
        if (!towerCardsCached)
            return;

        if (cannonTowerCard != null)
            cannonTowerCard.interactable = cannonCardOriginallyInteractable;
        if (magicTowerCard != null)
            magicTowerCard.interactable = magicCardOriginallyInteractable;
    }

    // 다음 안내 단계에서도 대상 UI와 문구만 넘겨 재사용할 수 있다.
    public void ShowTargetCallout(RectTransform target, string message)
    {
        if (target == null || tutorialRect == null || panelRect == null)
            return;

        calloutTarget = target;
        worldCalloutTarget = null;
        StartCallout(message);
    }

    private void ShowWorldCallout(Transform target, string message)
    {
        if (target == null || Camera.main == null)
            return;

        worldCalloutTarget = target;
        calloutTarget = null;
        StartCallout(message);
    }

    private void StartCallout(string message)
    {
        guideText.text = message;
        guideText.fontSize = step == TutorialStep.UpgradePanel ? 18f : 22f;
        CreateArrowIfNeeded();
        if (arrowObject != null)
            arrowObject.SetActive(true);
        Canvas.ForceUpdateCanvases();

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCallout());
    }

    private IEnumerator MoveCallout()
    {
        isMoving = true;
        Vector2 startPosition = panelRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < 0.35f)
        {
            GetCalloutPlacement(out Vector2 destination, out Rect targetBounds, out CalloutSide side);
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / 0.35f);
            progress = progress * progress * (3f - 2f * progress);
            panelRect.anchoredPosition = Vector2.Lerp(startPosition, destination, progress);
            UpdateArrow(targetBounds, side);
            yield return null;
        }

        isMoving = false;
        moveCoroutine = null;
    }

    private void GetCalloutPlacement(out Vector2 panelPosition, out Rect bounds, out CalloutSide side)
    {
        Rect rootBounds = tutorialRect.rect;
        bool compactUpgradeGuide = step == TutorialStep.UpgradePanel;
        float panelWidth = Mathf.Min(compactUpgradeGuide ? 300f : 420f,
            rootBounds.width - 24f);
        float panelHeight = Mathf.Min(compactUpgradeGuide ? 145f : 200f,
            rootBounds.height - 24f);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        guideText.rectTransform.sizeDelta = new Vector2(panelWidth - 36f,
            compactUpgradeGuide ? 120f : 104f);
        guideText.rectTransform.anchoredPosition = new Vector2(0f,
            compactUpgradeGuide ? 0f : 37f);

        if (calloutTarget != null)
        {
            if (calloutTarget != skillPanel || !TryGetSkillBounds(out bounds))
                bounds = GetRectBounds(calloutTarget);
        }
        else
        {
            if (!TryGetInstalledTowerBounds(out bounds))
            {
                Vector2 localPoint = WorldToTutorialPoint(worldCalloutTarget.position);
                bounds = new Rect(localPoint.x - 28f, localPoint.y - 28f, 56f, 56f);
            }
        }

        const float gap = 52f;
        const float margin = 12f;
        if (compactUpgradeGuide)
        {
            // 업그레이드 패널의 버튼을 가리지 않도록 안내창을 화면 가장자리로 보낸다.
            bool targetOnLeft = bounds.center.x < rootBounds.center.x;
            side = targetOnLeft ? CalloutSide.Right : CalloutSide.Left;
            panelPosition = new Vector2(
                targetOnLeft
                    ? rootBounds.xMax - panelWidth * 0.5f - margin
                    : rootBounds.xMin + panelWidth * 0.5f + margin,
                Mathf.Clamp(bounds.center.y,
                    rootBounds.yMin + panelHeight * 0.5f + margin,
                    rootBounds.yMax - panelHeight * 0.5f - margin));
            return;
        }

        float rightX = bounds.xMax + gap + panelWidth * 0.5f;
        float leftX = bounds.xMin - gap - panelWidth * 0.5f;
        float belowY = bounds.yMin - gap - panelHeight * 0.5f;
        float aboveY = bounds.yMax + gap + panelHeight * 0.5f;

        if (rightX + panelWidth * 0.5f <= rootBounds.xMax - margin)
        {
            side = CalloutSide.Right;
            panelPosition = new Vector2(rightX,
                Mathf.Clamp(bounds.center.y,
                    rootBounds.yMin + panelHeight * 0.5f + margin,
                    rootBounds.yMax - panelHeight * 0.5f - margin));
        }
        else if (leftX - panelWidth * 0.5f >= rootBounds.xMin + margin)
        {
            side = CalloutSide.Left;
            panelPosition = new Vector2(leftX,
                Mathf.Clamp(bounds.center.y,
                    rootBounds.yMin + panelHeight * 0.5f + margin,
                    rootBounds.yMax - panelHeight * 0.5f - margin));
        }
        else if (belowY - panelHeight * 0.5f >= rootBounds.yMin + margin)
        {
            side = CalloutSide.Below;
            panelPosition = new Vector2(
                Mathf.Clamp(bounds.center.x,
                    rootBounds.xMin + panelWidth * 0.5f + margin,
                    rootBounds.xMax - panelWidth * 0.5f - margin),
                belowY);
        }
        else
        {
            side = CalloutSide.Above;
            panelPosition = new Vector2(
                Mathf.Clamp(bounds.center.x,
                    rootBounds.xMin + panelWidth * 0.5f + margin,
                    rootBounds.xMax - panelWidth * 0.5f - margin),
                Mathf.Clamp(aboveY,
                    rootBounds.yMin + panelHeight * 0.5f + margin,
                    rootBounds.yMax - panelHeight * 0.5f - margin));
        }
    }

    private Rect GetRectBounds(RectTransform target)
    {
        target.GetWorldCorners(targetCorners);
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        for (int i = 0; i < targetCorners.Length; i++)
        {
            Vector3 point = tutorialRect.InverseTransformPoint(targetCorners[i]);
            minX = Mathf.Min(minX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxX = Mathf.Max(maxX, point.x);
            maxY = Mathf.Max(maxY, point.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private bool TryGetSkillBounds(out Rect bounds)
    {
        bounds = default;
        if (skillButtonRects == null || skillButtonRects.Length == 0)
            return false;

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        bool found = false;
        foreach (RectTransform buttonRect in skillButtonRects)
        {
            if (buttonRect == null || !buttonRect.gameObject.activeInHierarchy)
                continue;

            Rect buttonBounds = GetRectBounds(buttonRect);
            minX = Mathf.Min(minX, buttonBounds.xMin);
            minY = Mathf.Min(minY, buttonBounds.yMin);
            maxX = Mathf.Max(maxX, buttonBounds.xMax);
            maxY = Mathf.Max(maxY, buttonBounds.yMax);
            found = true;
        }

        if (!found)
            return false;

        const float padding = 10f;
        bounds = Rect.MinMaxRect(minX - padding, minY - padding,
            maxX + padding, maxY + padding);
        return true;
    }

    private bool TryGetInstalledTowerBounds(out Rect bounds)
    {
        bounds = default;
        if (step != TutorialStep.TowerInfo || installedTower == null)
            return false;

        SpriteRenderer towerSprite = installedTower.GetComponent<SpriteRenderer>();
        if (towerSprite == null || towerSprite.sprite == null)
            return false;

        Bounds spriteBounds = towerSprite.bounds;
        Vector2 lowerLeft = WorldToTutorialPoint(new Vector3(
            spriteBounds.min.x, spriteBounds.min.y, spriteBounds.center.z));
        Vector2 upperRight = WorldToTutorialPoint(new Vector3(
            spriteBounds.max.x, spriteBounds.max.y, spriteBounds.center.z));
        Vector2 center = (lowerLeft + upperRight) * 0.5f;
        float width = Mathf.Max(90f, Mathf.Abs(upperRight.x - lowerLeft.x) + 20f);
        float height = Mathf.Max(115f, Mathf.Abs(upperRight.y - lowerLeft.y) + 20f);
        bounds = new Rect(center.x - width * 0.5f, center.y - height * 0.5f,
            width, height);
        return true;
    }

    private Vector2 WorldToTutorialPoint(Vector3 worldPosition)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tutorialRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private void CreateArrowIfNeeded()
    {
        if (arrowLines != null)
            return;

        arrowObject = new GameObject("GuideArrow", typeof(RectTransform));
        arrowObject.layer = tutorialRoot.layer;
        arrowObject.transform.SetParent(tutorialRoot.transform, false);
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = Vector2.zero;
        arrowRect.sizeDelta = Vector2.zero;
        arrowLines = new RectTransform[3];
        targetOutline = new RectTransform[4];

        for (int i = 0; i < arrowLines.Length; i++)
            arrowLines[i] = CreateGoldLine(arrowObject.transform, "ArrowLine" + i);
        for (int i = 0; i < targetOutline.Length; i++)
            targetOutline[i] = CreateGoldLine(arrowObject.transform, "TargetOutline" + i);
    }

    private RectTransform CreateGoldLine(Transform parent, string objectName)
    {
        GameObject lineObject = new GameObject(objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.layer = tutorialRoot.layer;
        lineObject.transform.SetParent(parent, false);
        Image image = lineObject.GetComponent<Image>();
        image.color = new Color32(255, 192, 70, 255);
        image.raycastTarget = false;
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        return lineRect;
    }

    private void UpdateArrow(Rect bounds, CalloutSide side)
    {
        Vector2 panelPosition = panelRect.anchoredPosition;
        Vector2 panelSize = panelRect.sizeDelta;
        Vector2 start;
        Vector2 tip;

        if (side == CalloutSide.Right)
        {
            start = new Vector2(panelPosition.x - panelSize.x * 0.5f - 5f,
                Mathf.Clamp(bounds.center.y,
                    panelPosition.y - panelSize.y * 0.5f + 20f,
                    panelPosition.y + panelSize.y * 0.5f - 20f));
            tip = new Vector2(bounds.xMax + 5f, bounds.center.y);
        }
        else if (side == CalloutSide.Left)
        {
            start = new Vector2(panelPosition.x + panelSize.x * 0.5f + 5f,
                Mathf.Clamp(bounds.center.y,
                    panelPosition.y - panelSize.y * 0.5f + 20f,
                    panelPosition.y + panelSize.y * 0.5f - 20f));
            tip = new Vector2(bounds.xMin - 5f, bounds.center.y);
        }
        else if (side == CalloutSide.Below)
        {
            start = new Vector2(
                Mathf.Clamp(bounds.center.x,
                    panelPosition.x - panelSize.x * 0.5f + 20f,
                    panelPosition.x + panelSize.x * 0.5f - 20f),
                panelPosition.y + panelSize.y * 0.5f + 5f);
            tip = new Vector2(bounds.center.x, bounds.yMin - 5f);
        }
        else
        {
            start = new Vector2(
                Mathf.Clamp(bounds.center.x,
                    panelPosition.x - panelSize.x * 0.5f + 20f,
                    panelPosition.x + panelSize.x * 0.5f - 20f),
                panelPosition.y - panelSize.y * 0.5f - 5f);
            tip = new Vector2(bounds.center.x, bounds.yMax + 5f);
        }

        Vector2 direction = (tip - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 headBase = tip - direction * 16f;
        SetLine(arrowLines[0], start, tip, 5f);
        SetLine(arrowLines[1], headBase + perpendicular * 9f, tip, 5f);
        SetLine(arrowLines[2], headBase - perpendicular * 9f, tip, 5f);

        const float padding = 4f;
        Vector2 lowerLeft = new Vector2(bounds.xMin - padding, bounds.yMin - padding);
        Vector2 lowerRight = new Vector2(bounds.xMax + padding, bounds.yMin - padding);
        Vector2 upperLeft = new Vector2(bounds.xMin - padding, bounds.yMax + padding);
        Vector2 upperRight = new Vector2(bounds.xMax + padding, bounds.yMax + padding);
        SetLine(targetOutline[0], lowerLeft, lowerRight, 3f);
        SetLine(targetOutline[1], lowerRight, upperRight, 3f);
        SetLine(targetOutline[2], upperRight, upperLeft, 3f);
        SetLine(targetOutline[3], upperLeft, lowerLeft, 3f);
    }

    private static void SetLine(RectTransform line, Vector2 from, Vector2 to, float thickness)
    {
        Vector2 delta = to - from;
        line.anchoredPosition = (from + to) * 0.5f;
        line.sizeDelta = new Vector2(delta.magnitude, thickness);
        line.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private TextMeshProUGUI CreateButtonLabel(Button button, string label)
    {
        GameObject labelObject = new GameObject(
            "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)
        );
        labelObject.layer = button.gameObject.layer;
        labelObject.transform.SetParent(button.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.font = guideText.font;
        labelText.fontSize = 23f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color32(255, 234, 184, 255);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
        return labelText;
    }

    private static void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
}
