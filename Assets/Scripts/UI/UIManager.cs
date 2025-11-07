using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Button 사용을 위해 추가
using UnityEngine.Audio;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Audio Settings")]
    public AudioMixer gameAudioMixer;   // 믹서 에셋 연결용
    public GameObject settingsPanel;    // 설정 패널 (Slider를 담을 팝업창)
    public Button soundToggleButton;    // 소리 아이콘 버튼
    public Slider bgmSlider;            // BGM 슬라이더
    public Slider sfxSlider;

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

        // [추가]: 설정 패널 초기 상태 설정
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
        LinkAudioControls();

        if (gameSpeedButton != null)
            gameSpeedButton.onClick.AddListener(GameManager.Instance.CycleGameSpeed);
        else
            Debug.LogWarning("GameSpeedButton이 UIManager에 연결되지 않았습니다.");

        UpdateGameSpeedUI(1f);

        if (GameManager.Instance != null && GameManager.Instance.OnGoldChanged != null)
        {
            GameManager.Instance.OnGoldChanged.AddListener(UpdateGoldUI);
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

        if (summonButton != null)
            summonButton.onClick.AddListener(OnSummonButtonClick);

        if (gameSpeedButton != null)
            gameSpeedButton.onClick.AddListener(GameManager.Instance.CycleGameSpeed);
        else
            Debug.LogWarning("GameSpeedButton이 UIManager에 연결되지 않았습니다.");

        // [추가]: 사운드 토글 버튼 연결
        if (soundToggleButton != null)
            soundToggleButton.onClick.AddListener(ToggleSettingsPanel);
        else
            Debug.LogWarning("Sound Toggle Button이 UIManager에 연결되지 않았습니다.");

        LinkAudioControls(); // Start 함수로 이동했으므로 여기서 제거 가능 (Start에서 한 번만 호출됨)
    }

    public void SetupMainMenuButtons()
    {
        // "이어하기" 버튼 활성화/비활성화 로직
        if (continueButton != null && DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            bool hasSaveData = DataManager.Instance.CurrentPlayerData.currentWave > 1 ||
                               (DataManager.Instance.CurrentPlayerData.currentWave == 1 && DataManager.Instance.CurrentPlayerData.gold != 100);

            continueButton.interactable = hasSaveData;
            if (hasSaveData) Debug.Log("UIManager: 이어하기 버튼 활성화.");
            else Debug.Log("UIManager: 이어하기 버튼 비활성화 (저장 데이터 없음 또는 새 게임 상태).");
        }
        else if (continueButton != null)
        {
            continueButton.interactable = false;
            Debug.LogWarning("UIManager: DataManager.Instance 또는 CurrentPlayerData가 null이므로 이어하기 버튼 비활성화.");
        }

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

    // [추가]: 설정 패널을 켜고 끄는 토글 함수
    public void ToggleSettingsPanel()
    {
        if (settingsPanel == null)
        {
            Debug.LogError("SettingsPanel이 UIManager에 연결되지 않아 토글할 수 없습니다!");
            return;
        }

        // 패널 상태를 반전시킵니다.
        bool isActive = settingsPanel.activeSelf;
        settingsPanel.SetActive(!isActive);
    }

    public void ShowSummonButton(bool show)
    {
        if (summonButton != null)
            summonButton.gameObject.SetActive(show);
    }

    // 게임 시작 시 호출 (메인 메뉴 UI 숨기고 게임 UI 표시)
    public void HideStartUI()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (canvasMenu != null) canvasMenu.SetActive(false);
        if (canvasGame != null) canvasGame.SetActive(true);

        if (saveGameButton != null)
        {
            saveGameButton.gameObject.SetActive(true);
        }
    }

    // 게임 종료 또는 메인 메뉴로 돌아갈 때 호출될 수 있는 함수 (선택적)
    public void ShowStartUI()
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

    void LinkAudioControls()
    {
        if (bgmSlider != null)
        {
            // 슬라이더 값이 변경될 때 SetBGMVolume 함수 호출
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            // 초기값 설정 (저장된 값이 있다면 로드, 없으면 1f)
            SetBGMVolume(bgmSlider.value);
        }
        if (sfxSlider != null)
        {
            // 슬라이더 값이 변경될 때 SetSFXVolume 함수 호출
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            SetSFXVolume(sfxSlider.value);
        }
    }
    public void SetBGMVolume(float volume) // volume은 0.0 ~ 1.0 사이의 슬라이더 값입니다.
    {
        if (gameAudioMixer == null) return;

        // 로그 스케일을 사용하여 슬라이더 값(0~1)을 믹서 값(-80dB~0dB)으로 변환합니다.
        // volume이 0일 때 -80dB (음소거), volume이 1일 때 0dB (최대)
        float mixerVolume = Mathf.Log10(volume) * 20;

        // Mathf.Log10(0)은 무한대가 되므로, volume이 0일 때 -80dB로 강제 설정합니다.
        if (volume == 0)
        {
            mixerVolume = -80f;
        }

        gameAudioMixer.SetFloat("BGMVolume", mixerVolume);
    }

    // SFX 볼륨 조절 함수
    public void SetSFXVolume(float volume)
    {
        if (gameAudioMixer == null) return;

        float mixerVolume = Mathf.Log10(volume) * 20;

        if (volume == 0)
        {
            mixerVolume = -80f;
        }

        gameAudioMixer.SetFloat("SFXVolume", mixerVolume);
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