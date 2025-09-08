using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Button 사용을 위해 추가
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject achievementPanel; // Inspector에서 Achievement_Panel 오브젝트를 연결할 변수

    [Header("In-Game UI")]
    public TMP_Text waveTimerText;
    public TMP_Text waveCountText;
    public TMP_Text goldText;
    public Button summonButton; // 타워 소환 버튼

    [Header("Main Menu UI Elements")]
    public GameObject startPanel; // 새 게임, 이어하기, 저장하기 버튼이 있는 패널
    public Button newGameButton; // 인스펙터에서 연결
    public Button continueButton; // 인스펙터에서 연결
    public Button saveGameButton; // 인스펙터에서 연결 (게임 플레이 중에만 보이도록 할 수 있음)

    [Header("Canvas Groups")]
    public GameObject canvasMenu; // 메인 메뉴 전체 캔버스 또는 패널
    public GameObject canvasGame; // 인게임 UI 전체 캔버스 또는 패널

    [Header("Notification UI")]
    [SerializeField] private TMP_Text achievementText;
    private Coroutine achievementNotifyRoutine;
    [SerializeField] private TMP_Text systemMessageText;
    private Coroutine systemMessageRoutine;

    [Header("Game Control UI")]
    public Button gameSpeedButton;            // Inspector에서 연결
    public TextMeshProUGUI gameSpeedText;     // Inspector에서 연결

    [Header("Game Over / Victory UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverReasonText;
    public GameObject victoryPanel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // UIManager가 씬 전환 시 유지되어야 한다면
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (summonButton != null) ShowSummonButton(false); // 게임 시작 전에는 타워 소환 버튼 숨김
        if (canvasGame != null) canvasGame.SetActive(false); // 초기에는 게임 UI 숨김
        if (canvasMenu != null) canvasMenu.SetActive(true); // 초기에는 메뉴 UI 표시
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (achievementText != null) achievementText.gameObject.SetActive(false);
        if (systemMessageText != null) systemMessageText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 배속 버튼 클릭 시 호출되어 텍스트를 x1, x2, x3으로 바꿉니다.
    /// </summary>
    public void UpdateGameSpeedUI(float speed)
    {
        if (gameSpeedText != null)
            gameSpeedText.text = $"x{speed:F0}";
    }

    void Start()
    {
        Debug.Log($"[🧩] UIManager Start 실행됨");
        SetupMainMenuButtons(); // 메인 메뉴 버튼 상태 설정
        LinkButtonEvents(); // 버튼 이벤트 연결

        if (gameSpeedButton != null)
            gameSpeedButton.onClick.AddListener(GameManager.Instance.CycleGameSpeed);
        else
            Debug.LogWarning("GameSpeedButton이 UIManager에 연결되지 않았습니다.");

        UpdateGameSpeedUI(1f);

        if (GameManager.Instance != null && GameManager.Instance.OnGoldChanged != null)
        {
            GameManager.Instance.OnGoldChanged.AddListener(UpdateGoldUI);
            // 초기 골드 업데이트는 GameManager.StartGameLogic() 또는 LoadPlayerData() 후 호출되는 OnGoldChanged에 의해 처리됨
            // UpdateGoldUI(GameManager.Instance.gold); // 여기서 호출하면 GameManager.Awake보다 빠를 수 있음
        }
        else
        {
            Debug.LogWarning("UIManager Start: GameManager.Instance 또는 OnGoldChanged가 null입니다.");
        }
    }

    void LinkButtonEvents()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnClickNewGameButton);
        else
            Debug.LogWarning("New Game Button이 UIManager에 연결되지 않았습니다.");

        if (continueButton != null)
            continueButton.onClick.AddListener(OnClickContinueButton);
        else
            Debug.LogWarning("Continue Button이 UIManager에 연결되지 않았습니다.");

        if (saveGameButton != null)
            saveGameButton.onClick.AddListener(OnClickSaveButton);
        else
            Debug.LogWarning("Save Game Button이 UIManager에 연결되지 않았습니다.");

        // 타워 소환 버튼 연결 (기존 OnSummonButtonClick 함수가 있다면 그것을 사용)
        if (summonButton != null)
            summonButton.onClick.AddListener(OnSummonButtonClick); // OnSummonButtonClick 함수는 이미 public으로 존재

        if (gameSpeedButton != null)
            gameSpeedButton.onClick.AddListener(GameManager.Instance.CycleGameSpeed);
        else
            Debug.LogWarning("GameSpeedButton이 UIManager에 연결되지 않았습니다.");
    }

    public void SetupMainMenuButtons()
    {
        // "이어하기" 버튼 활성화/비활성화 로직
        if (continueButton != null && DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            // 저장된 데이터가 있는지 (예: currentWave가 1보다 큰지, 또는 별도의 플래그 확인)
            bool hasSaveData = DataManager.Instance.CurrentPlayerData.currentWave > 1 ||
                               (DataManager.Instance.CurrentPlayerData.currentWave == 1 && DataManager.Instance.CurrentPlayerData.gold != 100); // 새 게임 기본값과 다른지
            // 또는 DataManager에 File.Exists(saveFilePath)를 확인하는 public 함수를 만들어 사용
            // bool hasSaveData = DataManager.Instance.CheckIfSaveFileExists();

            continueButton.interactable = hasSaveData;
            if (hasSaveData) Debug.Log("UIManager: 이어하기 버튼 활성화.");
            else Debug.Log("UIManager: 이어하기 버튼 비활성화 (저장 데이터 없음 또는 새 게임 상태).");
        }
        else if (continueButton != null)
        {
            continueButton.interactable = false; // DataManager가 준비되지 않았으면 비활성화
            Debug.LogWarning("UIManager: DataManager.Instance 또는 CurrentPlayerData가 null이므로 이어하기 버튼 비활성화.");
        }

        // "게임 저장" 버튼은 게임 플레이 중에만 활성화 (예: canvasGame이 활성화될 때)
        if (saveGameButton != null)
        {
            saveGameButton.gameObject.SetActive(canvasGame != null && canvasGame.activeSelf);
        }
    }

    // "새 게임 시작" 버튼 클릭 시 호출될 함수
    public void OnClickNewGameButton()
    {
        Debug.Log("UI - 새 게임 시작 버튼 클릭됨");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }

    // "이어하기" 버튼 클릭 시 호출될 함수
    public void OnClickContinueButton()
    {
        Debug.Log("UI - 이어하기 버튼 클릭됨");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ContinueGame();
        }
    }

    // "게임 저장" 버튼 클릭 시 호출될 함수
    public void OnClickSaveButton()
    {
        Debug.Log("UI - 게임 저장 버튼 클릭됨");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ManualSavePlayerData();
        }
    }


    public void ShowSummonButton(bool show)
    {
        if (summonButton != null)
            summonButton.gameObject.SetActive(show);
    }

    // 게임 시작 시 호출 (메인 메뉴 UI 숨기고 게임 UI 표시)
    public void HideStartUI() // 이 함수는 GameManager.StartGameLogic에서 호출
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (canvasMenu != null) canvasMenu.SetActive(false);
        if (canvasGame != null) canvasGame.SetActive(true);

        // 게임이 시작되면 "게임 저장" 버튼을 활성화할 수 있음
        if (saveGameButton != null)
        {
            saveGameButton.gameObject.SetActive(true);
        }
        // 게임 시작 시 타워 소환 버튼 상태는 게임 로직에 따라 결정
        // ShowSummonButton(true); // 예시: 게임 시작 시 바로 소환 가능하게
    }

    // 게임 종료 또는 메인 메뉴로 돌아갈 때 호출될 수 있는 함수 (선택적)
    public void ShowStartUI() // 예: 게임오버 후 "메인으로" 버튼 클릭 시
    {
        if (startPanel != null) startPanel.SetActive(true);
        if (canvasMenu != null) canvasMenu.SetActive(true);
        if (canvasGame != null) canvasGame.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        SetupMainMenuButtons(); // 메인 메뉴로 돌아오면 버튼 상태 다시 설정
    }


    public void ShowGameOverPanel(string reason)
    {
        Debug.Log($"✅ ShowGameOverPanel() called with reason: {reason}");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else
            Debug.LogWarning("❌ gameOverPanel이 연결되지 않았습니다!");

        if (gameOverReasonText != null)
            gameOverReasonText.text = reason;
        else
            Debug.LogWarning("❌ gameOverReasonText가 연결되지 않았습니다!");
    }

    public void OnSummonButtonClick()
    {
        if (TileScript.selectedTile == null || TileScript.selectedTile.isOccupied)
            return;

        // 1) 비용 차감 로직 제거
        // int cost = 50;
        // if (!GameManager.Instance.SpendGold(cost))
        // {
        //     Debug.Log("골드 부족 또는 GameManager.Instance 없음!");
        //     return;
        // }

        // 2) 바로 SummonManager에 소환 요청
        if (SummonManager.Instance == null)
        {
            Debug.LogWarning("SummonManager.Instance가 null입니다.");
            return;
        }

        Vector3 spawnPos = TileScript.selectedTile.transform.position;
        GameObject summonedTower = SummonManager.Instance.SummonRandomTower(spawnPos);

        if (summonedTower != null)
        {
            TileScript.selectedTile.PlaceTower(summonedTower);
        }
        else
        {
            Debug.Log("타워 소환 실패");
        }
    }


    public void UpdateGoldUI(int gold)
    {
        if (goldText != null)
            goldText.text = $"Gold: {gold}";
    }

    public void UpdateWaveTimerUI(float time)
    {
        if (waveTimerText == null) return;
        int seconds = Mathf.CeilToInt(time);
        waveTimerText.text = $"NEXT WAVE START : {seconds}s";
    }

    public void UpdateWaveUI(int wave)
    {
        if (waveCountText != null)
            waveCountText.text = $"Wave {wave}";
    }

    public void ShowAchievementMessage(string msg)
    {
        if (achievementText == null)
        {
            Debug.LogWarning("achievementText가 UIManager에 연결되지 않았습니다.");
            return;
        }
        achievementText.text = msg;
        achievementText.gameObject.SetActive(true);
        if (achievementNotifyRoutine != null) StopCoroutine(achievementNotifyRoutine);
        achievementNotifyRoutine = StartCoroutine(HideAchievementText());
    }

    private IEnumerator HideAchievementText()
    {
        yield return new WaitForSeconds(2f);
        if (achievementText != null)
            achievementText.gameObject.SetActive(false);
        achievementNotifyRoutine = null;
    }

    public void ShowSystemMessage(string msg, float duration = 2f)
    {
        if (systemMessageText == null)
        {
            Debug.LogWarning("systemMessageText가 UIManager에 연결되지 않았습니다. 메시지: " + msg);
            return;
        }
        systemMessageText.text = msg;
        systemMessageText.gameObject.SetActive(true);
        if (systemMessageRoutine != null) StopCoroutine(systemMessageRoutine);
        systemMessageRoutine = StartCoroutine(HideSystemMessage(duration));
    }

    private IEnumerator HideSystemMessage(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (systemMessageText != null)
            systemMessageText.gameObject.SetActive(false);
        systemMessageRoutine = null;
    }

    public void ToggleAchievementPanel()
    {
        if (achievementPanel == null)
        {
            Debug.LogError("UIManager: Achievement Panel이 연결되지 않았습니다!");
            return;
        }

        bool isActive = achievementPanel.activeSelf;
        achievementPanel.SetActive(!isActive);

        if (!isActive) // 패널이 방금 활성화 되었다면 (열렸다면)
        {
            AchievementPanelUI panelUI = achievementPanel.GetComponent<AchievementPanelUI>();
            if (panelUI != null)
            {
                panelUI.PopulateAchievements(); // 목록 새로고침
                Debug.Log("Achievement Panel이 열리고 목록이 업데이트되었습니다.");
            }
        }
        else // 패널이 방금 비활성화 되었다면 (닫혔다면)
        {
            Debug.Log("Achievement Panel이 닫혔습니다.");
        }
    }

    public void OnClickQuitButton()
    {
        Debug.Log("UI - 게임 종료 버튼 클릭됨");

        // Unity 에디터에서 실행 중일 때와 실제 빌드에서 실행될 때를 구분하여 처리
#if UNITY_EDITOR
        // 에디터에서 플레이 중일 경우, 플레이 모드를 중지합니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 실제 빌드된 게임에서는 어플리케이션을 종료합니다.
        Application.Quit();
#endif
    }
}