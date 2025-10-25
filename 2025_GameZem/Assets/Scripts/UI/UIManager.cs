using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Spine.Unity;

public class UIManager : Singleton<UIManager>
{
    [Header("Game UI")]
    public TextMeshProUGUI dateText; // 날짜 표시 (스코어 대신)
    public GameObject[] lifeIcons; // 하트 아이콘 배열 (3개)
    public SkeletonGraphic[] heartSkeletons; // 하트 Spine SkeletonGraphic 배열 (3개)
    public TextMeshProUGUI comboText;
    public Slider comboSlider;
    
    [Header("Progress UI")]
    public Slider progressSlider; // 게임 진행도 슬라이더
    
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI bestScoreText;
    public Button restartButton;
    public Button mainMenuButton;
    
    [Header("Pause UI")]
    public GameObject pausePanel;
    public Button pauseButton;
    public Button resumeButton;
    public Button pauseTitleButton;
    
    [Header("Settings UI")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Toggle soundToggle;
    public Toggle musicToggle;
    
    [Header("Effects")]
    public GameObject scorePopupPrefab;
    public Transform scorePopupParent;
    public float popupDuration = 1f;
    
    private int currentCombo = 0;
    private int maxCombo = 0;
    private float comboTimeLeft = 0f;
    private float maxComboTime = 3f;
    private int previousLives = 3; // 이전 생명 수를 추적하기 위한 변수
    
    private void Start()
    {
        Debug.Log("[UIManager] Start"); 
        InitializeUI();
        SubscribeToEvents();
    }
    
    private void InitializeUI()
    {
        // GameManager를 안전하게 가져오기
        GameManager gameManager = null;
        try
        {
            gameManager = GameManager.Instance;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] GameManager.Instance 접근 중 오류: {e.Message}");
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        // 초기 UI 상태 설정
        UpdateLivesDisplay(gameManager.GetCurrentLives());
        UpdateDateDisplay(gameManager.GetCurrentDate());
        UpdateComboDisplay(0);
        
        // 버튼 이벤트 연결
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
            
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
            
        if (pauseTitleButton != null)
            pauseTitleButton.onClick.AddListener(OnPauseTitleClicked);
        
        // 패널 초기 상태
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        if (pausePanel != null)
            pausePanel.SetActive(false);
            
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
    
    private void SubscribeToEvents()
    {
        GameManager gameManager = null;
        try
        {
            gameManager = GameManager.Instance;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] GameManager.Instance 접근 중 오류: {e.Message}. 직접 찾기를 시도합니다.");
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager == null)
        {
            // GameManager.Instance가 null이면 직접 찾기
            gameManager = FindFirstObjectByType<GameManager>();
            Debug.Log($"[UIManager] GameManager.Instance가 null이어서 직접 찾기: {(gameManager != null ? "찾음" : "없음")}");
        }
        
        if (gameManager != null)
        {
            Debug.Log("[UIManager] GameManager 이벤트 구독 시작");
            gameManager.OnDateChanged += UpdateDateDisplay;
            gameManager.OnLivesChanged += UpdateLivesDisplay;
            gameManager.OnGameOver += ShowGameOverScreen;
            gameManager.OnGameCleared += ShowGameClearedScreen;
            gameManager.OnComboAdded += AddCombo;
            gameManager.OnProgressChanged += UpdateProgressDisplay;
            Debug.Log("[UIManager] GameManager 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager를 찾을 수 없어서 이벤트 구독 실패!");
        }
    }
    
    private void Update()
    {
        UpdateComboTimer();
    }
    
    private void UpdateLivesDisplay(int lives)
    {
        Debug.Log($"[UIManager] UpdateLivesDisplay 호출됨 - lives: {lives}, previousLives: {previousLives}");
        
        // 하트 아이콘으로 생명 표시
        if (lifeIcons != null && lifeIcons.Length > 0 && heartSkeletons != null)
        {
            // 하트 아이콘 활성/비활성 상태 업데이트
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                {
                    bool shouldBeActive = i < lives;
                    bool wasActive = lifeIcons[i].activeSelf;
                    
                    // 생명이 줄어든 경우 (하트가 사라지는 경우)
                    if (!shouldBeActive && wasActive && i >= lives && i < previousLives)
                    {
                        // heartSkeletons 배열에서 SkeletonGraphic 가져오기
                        if (i < heartSkeletons.Length && heartSkeletons[i] != null)
                        {
                            // 하트를 활성화 (애니메이션 재생을 위해)
                            //lifeIcons[i].SetActive(true);
                            heartSkeletons[i].gameObject.SetActive(true);
                            
                                                         // "appear" 애니메이션 재생
                             var trackEntry = heartSkeletons[i].AnimationState.SetAnimation(0, "appear", false);
                             
                             if (trackEntry != null && trackEntry.Animation != null)
                             {
                                 // 정상 재생
                                 trackEntry.TrackTime = 0f;
                                 trackEntry.TimeScale = 1f;
                                
                                // 애니메이션 완료 후 오브젝트 비활성화
                                StartCoroutine(DisableAfterAnimation(heartSkeletons[i], lifeIcons[i]));
                                
                                                                 Debug.Log($"[UIManager] 하트 {i}에 appear 애니메이션 재생 - Duration: {trackEntry.Animation.Duration}");
                                continue; // 이 하트는 비활성화하지 않음 (애니메이션 후 처리)
                            }
                        }
                    }
                    
                    // 생명이 늘어난 경우 (처음 시작하거나 생명을 회복한 경우)
                    if (shouldBeActive && !wasActive)
                    {
                        // heartSkeletons 배열에서 SkeletonGraphic 가져오기
                        if (i < heartSkeletons.Length && heartSkeletons[i] != null)
                        {
                            heartSkeletons[i].AnimationState.SetAnimation(0, "appear", false);
                            Debug.Log($"[UIManager] 하트 {i}에 appear 애니메이션 재생");
                        }
                    }
                    
                    // 일반적인 활성/비활성 처리 (애니메이션 중이 아닌 경우에만)
                    if (!shouldBeActive && wasActive && i < lives)
                    {
                        // 이미 애니메이션 처리된 경우 건너뜀
                        continue;
                    }
                    
                    lifeIcons[i].SetActive(shouldBeActive);
                    Debug.Log($"[UIManager] LifeIcon {i}: {(shouldBeActive ? "활성화" : "비활성화")}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[UIManager] lifeIcons 또는 heartSkeletons가 null이거나 비어있습니다!");
        }
        
        // 이전 생명 수 업데이트
        previousLives = lives;
    }
    
    private System.Collections.IEnumerator DisableAfterAnimation(SkeletonGraphic skeletonGraphic, GameObject lifeIcon)
    {
        // 애니메이션 재생 시간 대기
        if (skeletonGraphic != null && skeletonGraphic.AnimationState != null)
        {
            var animation = skeletonGraphic.Skeleton.Data.FindAnimation("appear");
            if (animation != null)
            {
                yield return new WaitForSeconds(animation.Duration);
            }
        }
        
        // 애니메이션 완료 후 오브젝트 비활성화
        if (lifeIcon != null)
        {
            lifeIcon.SetActive(false);
            Debug.Log($"[UIManager] 하트 애니메이션 완료 후 비활성화");
        }
    }
    
    private void UpdateDateDisplay(System.DateTime date)
    {
        if (dateText != null)
        {
            dateText.text = date.ToString("yyyy. MM");
        }
    }
    
    private void UpdateProgressDisplay(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }
    }
    
    private void UpdateComboDisplay(int combo)
    {
        if (comboText != null)
        {
            if (combo > 0)
            {
                comboText.text = $"Combo x{combo}";
                comboText.gameObject.SetActive(true);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
        
        if (comboSlider != null)
        {
            comboSlider.value = comboTimeLeft / maxComboTime;
        }
    }
    
    private void UpdateComboTimer()
    {
        if (currentCombo > 0)
        {
            comboTimeLeft -= Time.deltaTime;
            
            if (comboTimeLeft <= 0)
            {
                ResetCombo();
            }
            
            UpdateComboDisplay(currentCombo);
        }
    }
    
    public void AddCombo()
    {
        currentCombo++;
        comboTimeLeft = maxComboTime;
        
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
        
        UpdateComboDisplay(currentCombo);
        
        // 콤보 효과음 재생
        // if (SoundManager.Instance != null)
        // {
        //     SoundManager.Instance.PlayComboSound(currentCombo);
        // }
    }
    
    public void ResetCombo()
    {
        currentCombo = 0;
        comboTimeLeft = 0f;
        UpdateComboDisplay(0);
    }
    
    public void ShowScorePopup(int points, Vector3 worldPosition)
    {
        if (scorePopupPrefab != null && scorePopupParent != null)
        {
            GameObject popup = Instantiate(scorePopupPrefab, scorePopupParent);
            TextMeshProUGUI popupText = popup.GetComponent<TextMeshProUGUI>();
            
            if (popupText != null)
            {
                popupText.text = $"+{points}";
                
                // 월드 좌표를 스크린 좌표로 변환
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
                popup.transform.position = screenPos;
            }
            
            // 팝업 애니메이션
            StartCoroutine(AnimateScorePopup(popup));
        }
    }
    
    private System.Collections.IEnumerator AnimateScorePopup(GameObject popup)
    {
        float elapsed = 0f;
        Vector3 startPos = popup.transform.position;
        Vector3 endPos = startPos + Vector3.up * 100f;
        
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;
            
            popup.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // 페이드 아웃
            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }
            
            yield return null;
        }
        
        Destroy(popup);
    }
    
    private void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // 최종 날짜 표시
            if (finalScoreText != null)
            {
                System.DateTime finalDate = GameManager.Instance.GetCurrentDate();
                finalScoreText.text = $"{finalDate:yyyy. MM}";
            }
            
            // 최고 기록 표시
            if (bestScoreText != null)
            {
                var bestScore = GameManager.Instance.GetBestRecord();
                bestScoreText.text = $"최고 기록 {bestScore.date}";
            }
        }
    }
    
    private void ShowGameClearedScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // 클리어 메시지
            if (finalScoreText != null)
            {
                finalScoreText.text = "Game Cleared!\n2020. 07. 27";
            }
            
            // 총 소요 일수
            if (bestScoreText != null)
            {
                var bestScore = GameManager.Instance.GetBestRecord();
                bestScoreText.text = $"최고 기록 {bestScore.date}";
            }
        }
    }
    
    private void OnRestartClicked()
    {
        
        if (GameManager.Instance == null)
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            gameManager.RestartGame();
        }
        else
        {
            GameManager.Instance.RestartGame();
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        ResetCombo();
    }
    
    private void OnMainMenuClicked()
    {
        // 메인 메뉴로 이동하는 로직
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }
    
    private void OnPauseClicked()
    {
        Time.timeScale = 0f;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }
    
    private void OnResumeClicked()
    {
        Time.timeScale = 1f;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }
    
    private void OnPauseTitleClicked()
    {
        // Time.timeScale을 1로 복원 (일시정지 해제)
        Time.timeScale = 1f;
        
        // 일시정지 패널 닫기
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        // 타이틀 씬으로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }
    
    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }
    
    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    
    protected override void OnDestroy()
    {
        // Time.timeScale을 1로 복원 (일시정지 상태 해제)
        Time.timeScale = 1f;
        
        // 이벤트 구독 해제 (GameManager가 파괴되었을 수도 있으므로 안전하게 처리)
        try
        {
            // GameManager를 직접 찾아서 이벤트 구독 해제
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnDateChanged -= UpdateDateDisplay;
                gameManager.OnLivesChanged -= UpdateLivesDisplay;
                gameManager.OnGameOver -= ShowGameOverScreen;
                gameManager.OnGameCleared -= ShowGameClearedScreen;
                gameManager.OnComboAdded -= AddCombo;
                gameManager.OnProgressChanged -= UpdateProgressDisplay;
                Debug.Log("[UIManager] 이벤트 구독 해제 완료");
            }
            else
            {
                Debug.Log("[UIManager] GameManager를 찾을 수 없어서 이벤트 구독 해제 건너뜀");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIManager] 이벤트 구독 해제 중 오류 발생: {e.Message}");
        }
        
        base.OnDestroy();
    }
}
