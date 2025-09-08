using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public bool isGameStarted = false;
    private bool isWaveRunning = false;
    private bool isGameOver = false;

    [Header("Game Speed Control")]
    private float[] speedSettings = { 1f, 2f, 3f };
    private int currentSpeedIndex = 0;

    [Header("Wave Management")]
    public float waveTimer = 30f;
    public int currentWave = 1;
    public int aliveMonsterCount = 0;
    public int monsterLimit = 50;

    [Header("Player Stats")]
    public int gold = 100;
    public UnityEvent<int> OnGoldChanged;

    [Header("Cameras")]
    public Camera menuCamera;
    public Camera gameCamera;

    [Header("HUD Elements")]
    public TextMeshProUGUI waveInfoText;
    public TextMeshProUGUI waveTimerText;
    public TextMeshProUGUI monstersAliveText;

    [Header("Boss Challenge")]
    [Tooltip("보스를 처음 만난 라운드 번호")]
    private int bossSpawnWave = -1;
    [Tooltip("보스를 이 라운드 수 이내에 잡아야 함")]
    public int bossKillDeadline = 3;

    private bool bossInCurrentWave;
    private bool bossKilled;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Time.timeScale = 0f;
        Debug.Log("GameManager Awake: Time.timeScale set to 0 (Paused)");
    }

    void Start()
    {
        // 초기 카메라 설정
        if (menuCamera != null) menuCamera.enabled = true;
        if (gameCamera != null) gameCamera.enabled = false;

        // 메인 메뉴 버튼 연결
        UIManager.Instance?.SetupMainMenuButtons();

        // HUD 초기화
        UpdateAllHUD();
    }

    void Update()
    {
        if (!isGameStarted || isGameOver) return;

        // 배속 토글 (스페이스)
        if (Input.GetKeyDown(KeyCode.Space))
            CycleGameSpeed();

        // 웨이브 대기 카운트다운
        if (!isWaveRunning)
        {
            waveTimer -= Time.deltaTime;
            UpdateWaveTimerUIOnly();
            if (waveTimer <= 0f)
                StartWave();
        }

        if (aliveMonsterCount >= monsterLimit)
        {
            GameOver("몬스터수 초과!");
            return;
        }


        // 게임 오버 체크
        if (aliveMonsterCount >= monsterLimit)
            GameOver();

        // 디버그 키
        if (Input.GetKeyDown(KeyCode.M))
            Debug.Log($"[Debug] Monsters: {aliveMonsterCount}/{monsterLimit}");
        if (Input.GetKeyDown(KeyCode.F12))
        {
            DataManager.Instance?.ResetPlayerData();
            InitializeGameManagerWithPlayerData(DataManager.Instance?.CurrentPlayerData);
            Debug.Log("GameManager: Player data reset via F12");
        }
    }

    /// <summary>
    /// 1x → 2x → 3x → 1x 배속 순환
    /// </summary>
    public void CycleGameSpeed()
    {
        if (isGameOver) return;

        currentSpeedIndex = (currentSpeedIndex + 1) % speedSettings.Length;
        Time.timeScale = speedSettings[currentSpeedIndex];
        Debug.Log($"Game Speed: {Time.timeScale}x");

        UIManager.Instance?.UpdateGameSpeedUI(Time.timeScale);
    }

    /// <summary>
    /// HUD의 모든 정보를 갱신
    /// </summary>
    public void UpdateAllHUD()
    {
        if (waveInfoText != null)
            waveInfoText.text = $"WAVE {currentWave}";

        UpdateWaveTimerUIOnly();
        UpdateMonstersAliveUI();
    }

    private void UpdateWaveTimerUIOnly()
    {
        if (waveTimerText == null) return;

        if (!isGameStarted || isGameOver)
            waveTimerText.text = string.Empty;
        else if (!isWaveRunning && waveTimer > 0f)
            waveTimerText.text = $"다음 웨이브: {waveTimer:F0}초";
        else if (isWaveRunning)
            waveTimerText.text = "진행 중!";
        else
            waveTimerText.text = "웨이브 시작!";
    }

    public void UpdateMonstersAliveUI()
    {
        if (monstersAliveText == null) return;
        monstersAliveText.text = (isGameStarted && !isGameOver)
            ? $"남은 몬스터: {aliveMonsterCount}" : string.Empty;
    }

    /// <summary>
    /// 웨이브 시작
    /// </summary>
    public void StartWave()
    {
        if (!isGameStarted || isGameOver || isWaveRunning) return;

        // 1) 이번 웨이브에 보스가 있는지 검사
        bool hasBossThisWave = DatabaseManager.Instance
            .GetWaveDefinitionsByWaveNumber(currentWave)
            .Any(w => DatabaseManager.Instance.monsterDataList
                         .First(m => m.id == w.monsterDataId).isBoss);

        // 2) 웨이브 상태 초기화
        isWaveRunning = true;
        waveTimer = 0f;
        UpdateAllHUD();
        Debug.Log($"Wave {currentWave} 시작");

        // 3) 보스 첫 등장 라운드 기록
        if (hasBossThisWave && bossSpawnWave < 0)
        {
            bossSpawnWave = currentWave;
            Debug.Log($"Boss first appeared at wave {bossSpawnWave}");
        }

        // 4) 보스 플래그 세팅
        bossInCurrentWave = hasBossThisWave;
        bossKilled = false;

        // 5) 실제 스폰 시작
        if (MonsterSpawner.Instance != null)
            MonsterSpawner.Instance.StartWave(currentWave);
        else
            OnWaveEnd();
    }

    /// <summary>
    /// 웨이브 종료
    /// </summary>
    public void OnWaveEnd()
    {
        if (isGameOver) return;

        isWaveRunning = false;
        if (bossSpawnWave >= 0 &&
        !bossKilled &&
        currentWave > bossSpawnWave + bossKillDeadline)
        {
            GameOver($"보스를 시간내에 처치하지 못했습니다");
            return;
        }
        currentWave++;
        waveTimer = 30f;
        UpdateAllHUD();
        Debug.Log($"Wave {currentWave - 1} 종료, 다음 웨이브: {currentWave}");
    }

    private void GameOver()
    {
        GameOver("패배");
    }

    private void GameOver(string reason)
    {
        if (isGameOver) return;

        isGameOver = true;
        isWaveRunning = false;
        Time.timeScale = 0f;
        Debug.Log("🛑 GameOver");

        UIManager.Instance?.ShowGameOverPanel(reason);
    }

    // ------------------ 골드 및 업적 ------------------

    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        AchievementManager.Instance?.NotifyGoldAccumulated(amount);
    }

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            return true;
        }
        UIManager.Instance?.ShowSystemMessage("골드가 부족합니다!", 1.5f);
        return false;
    }

    // ------------------ 몬스터 관리 ------------------

    public void MonsterSpawned()
    {
        if (isGameOver) return;
        aliveMonsterCount++;
        UpdateMonstersAliveUI();
    }

    public void MonsterKilled(MonsterDataRecord record)
    {
        if (isGameOver) return;
        if (aliveMonsterCount > 0) aliveMonsterCount--;
        UpdateMonstersAliveUI();

        if (record.isBoss)
            bossKilled = true;

        AchievementManager.Instance?.NotifyMonsterKilled(record);
    }

    // ------------------ 데이터 저장·로드 ------------------

    public void ManualSavePlayerData()
    {
        if (!isGameStarted || isGameOver) return;
        var dm = DataManager.Instance;
        if (dm == null || dm.CurrentPlayerData == null) return;

        var pd = dm.CurrentPlayerData;
        pd.gold = gold;
        pd.currentWave = currentWave;

        AchievementManager.Instance?.UpdatePlayerDataForSave(pd);

        pd.placedTowersData.Clear();
        foreach (var tower in FindObjectsOfType<TowerScript>())
        {
            if (tower.DbData != null)
                pd.placedTowersData.Add(new SavedTowerData(tower.towerType, tower.grade, tower.transform.position));
        }
        dm.SavePlayerData();
        UIManager.Instance?.ShowSystemMessage("게임이 저장되었습니다!", 1.5f);
    }

    public void StartNewGame()
    {
        DataManager.Instance?.ResetPlayerData();
        InitializeGameManagerWithPlayerData(DataManager.Instance?.CurrentPlayerData);
        StartGameLogic();
    }

    public void ContinueGame()
    {
        InitializeGameManagerWithPlayerData(DataManager.Instance?.CurrentPlayerData);
        StartGameLogic();
    }

    private void InitializeGameManagerWithPlayerData(PlayerData pd)
    {
        if (pd == null)
        {
            gold = 100;
            currentWave = 1;
            AchievementManager.Instance?.LoadAchievedListFromPlayerData(new List<string>(), new PlayerData());
        }
        else
        {
            gold = pd.gold;
            currentWave = pd.currentWave;
            AchievementManager.Instance?.LoadAchievedListFromPlayerData(pd.achievedAchievementIds, pd);
        }
        UpdateAllHUD();
        OnGoldChanged?.Invoke(gold);
    }

    private void StartGameLogic()
    {
        Time.timeScale = 1f;
        isGameStarted = true;
        isGameOver = false;

        menuCamera.enabled = false;
        gameCamera.enabled = true;
        UIManager.Instance?.HideStartUI();

        ClearExistingTowersOnMap();
        MapGenerator.Instance?.GenerateMap();

        aliveMonsterCount = 0;
        waveTimer = 30f;
        isWaveRunning = false;

        StartCoroutine(InitializeSpawnerAfterMapAndRestoration());
        UpdateAllHUD();
    }

    private IEnumerator InitializeSpawnerAfterMapAndRestoration()
    {
        yield return null;
        MonsterSpawner.Instance?.InitializeSpawnPoint();
        RestorePlacedTowers();
    }

    private void ClearExistingTowersOnMap()
    {
        foreach (var tower in FindObjectsOfType<TowerScript>())
        {
            var tile = FindTileScriptUnderTower(tower.transform.position);
            tile?.RemoveTower();
            Destroy(tower.gameObject);
        }
        UpgradeManager.Instance?.ClearSelection();
    }

    private void RestorePlacedTowers()
    {
        var dm = DataManager.Instance;
        if (dm?.CurrentPlayerData?.placedTowersData == null) return;

        foreach (var saved in dm.CurrentPlayerData.placedTowersData)
        {
            try
            {
                var type = (TowerType)System.Enum.Parse(typeof(TowerType), saved.towerTypeString, true);
                var grade = (TowerGrade)System.Enum.Parse(typeof(TowerGrade), saved.towerGradeString, true);
                var pos = saved.GetPosition();

                var obj = SummonManager.Instance?.SummonSpecificTower(pos, type, grade);
                if (obj != null)
                {
                    var tile = FindTileScriptUnderTower(pos);
                    if (tile != null && !tile.isOccupied)
                        tile.PlaceTower(obj);
                    else
                        Destroy(obj);
                }
            }
            catch { /* enum parse 에러 등 무시 */ }
        }
    }

    private TileScript FindTileScriptUnderTower(Vector3 pos)
    {
        var hits = Physics2D.RaycastAll(pos, Vector2.zero);
        foreach (var hit in hits)
            if (hit.collider.GetComponent<TileScript>() is TileScript t)
                return t;
        return null;
    }
}
