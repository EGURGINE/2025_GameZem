using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : Singleton<UIManager>
{
    [Header("Game UI")]
    public TextMeshProUGUI dateText; // 날짜 표시 (스코어 대신)
    public GameObject[] lifeIcons; // 하트 아이콘 배열 (3개)
    public TextMeshProUGUI comboText;
    public Slider comboSlider;
    
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
    
    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }
    
    private void InitializeUI()
    {
        // 초기 UI 상태 설정
        UpdateLivesDisplay(GameManager.Instance.GetCurrentLives());
        UpdateDateDisplay(GameManager.Instance.GetCurrentDate());
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDateChanged += UpdateDateDisplay;
            GameManager.Instance.OnLivesChanged += UpdateLivesDisplay;
            GameManager.Instance.OnGameOver += ShowGameOverScreen;
            GameManager.Instance.OnGameCleared += ShowGameClearedScreen;
            GameManager.Instance.OnComboAdded += AddCombo;
        }
    }
    
    private void Update()
    {
        UpdateComboTimer();
    }
    
    private void UpdateLivesDisplay(int lives)
    {
        // 하트 아이콘으로 생명 표시
        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                {
                    lifeIcons[i].SetActive(i < lives);
                }
            }
        }
    }
    
    private void UpdateDateDisplay(System.DateTime date)
    {
        if (dateText != null)
        {
            dateText.text = date.ToString("yyyy. MM. dd");
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
                finalScoreText.text = $"Reached: {finalDate:yyyy. MM. dd}";
            }
            
            // 최고 기록 표시
            if (bestScoreText != null)
            {
                int bestScore = PlayerPrefs.GetInt("BestScore", 0);
                int currentScore = GameManager.Instance.GetScore();
                
                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    PlayerPrefs.SetInt("BestScore", bestScore);
                }
                
                bestScoreText.text = $"Best: {bestScore} days";
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
                int totalDays = GameManager.Instance.GetScore();
                bestScoreText.text = $"Total: {totalDays} days";
            }
        }
    }
    
    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDateChanged -= UpdateDateDisplay;
            GameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
            GameManager.Instance.OnGameOver -= ShowGameOverScreen;
            GameManager.Instance.OnGameCleared -= ShowGameClearedScreen;
            GameManager.Instance.OnComboAdded -= AddCombo;
        }
    }
}
