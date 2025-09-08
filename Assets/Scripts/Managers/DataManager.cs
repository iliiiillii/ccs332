using UnityEngine;
using System.IO; // 파일 입출력을 위해 필요
using System.Collections.Generic; // List 사용을 위해 필요

// <<< 추가: 저장될 개별 타워의 정보 구조체 >>>
[System.Serializable]
public class SavedTowerData
{
    public string towerTypeString; // TowerType enum 값을 문자열로 저장
    public string towerGradeString; // TowerGrade enum 값을 문자열로 저장
    public float positionX;
    public float positionY;
    public float positionZ;
    // 필요하다면 public float currentHealth; 등 추가 가능

    // 기본 생성자 (JsonUtility가 사용)
    public SavedTowerData() { }

    public SavedTowerData(TowerType type, TowerGrade grade, Vector3 position)
    {
        towerTypeString = type.ToString();
        towerGradeString = grade.ToString();
        positionX = position.x;
        positionY = position.y;
        positionZ = position.z;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(positionX, positionY, positionZ);
    }
}
// <<< 여기까지 SavedTowerData 클래스 정의 >>>


// 플레이어의 저장될 데이터 구조
[System.Serializable]
public class PlayerData
{
    public int gold;
    public int currentWave;
    public List<string> achievedAchievementIds; // 달성한 업적 ID 목록

    // AchievementManager와 연동하기 위한 누적 통계 필드들
    public int totalMonstersKilled;
    public int totalEliteMonstersKilled;
    public int totalBossesKilled;
    public List<string> killedBossNames;
    public int synthesisCount;
    public float accumulatedGold;
    public List<string> collectedTowerSignatures;

    // <<< 추가: 배치된 타워 정보 리스트 >>>
    public List<SavedTowerData> placedTowersData;

    // 생성자: 새 게임 시작 시 기본값을 설정합니다.
    public PlayerData()
    {
        gold = 100;
        currentWave = 1;
        achievedAchievementIds = new List<string>();

        totalMonstersKilled = 0;
        totalEliteMonstersKilled = 0;
        totalBossesKilled = 0;
        killedBossNames = new List<string>();
        synthesisCount = 0;
        accumulatedGold = 0f;
        collectedTowerSignatures = new List<string>();

        placedTowersData = new List<SavedTowerData>(); // <<< 초기화 >>>
    }
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerData CurrentPlayerData { get; private set; }

    private string saveFileName = "playerData.json";
    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
            LoadPlayerData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadPlayerData()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonPlayerData = File.ReadAllText(saveFilePath);
                CurrentPlayerData = JsonUtility.FromJson<PlayerData>(jsonPlayerData);

                if (CurrentPlayerData == null)
                {
                    Debug.LogWarning("플레이어 데이터 파일 로드 실패 또는 파일 내용이 비어있습니다. 새 데이터 생성.");
                    CurrentPlayerData = new PlayerData();
                }
                else
                {
                    // 이전 버전 저장 파일과의 호환성을 위해 List 타입 필드들이 null이면 초기화
                    if (CurrentPlayerData.achievedAchievementIds == null) CurrentPlayerData.achievedAchievementIds = new List<string>();
                    if (CurrentPlayerData.killedBossNames == null) CurrentPlayerData.killedBossNames = new List<string>();
                    if (CurrentPlayerData.collectedTowerSignatures == null) CurrentPlayerData.collectedTowerSignatures = new List<string>();
                    if (CurrentPlayerData.placedTowersData == null) CurrentPlayerData.placedTowersData = new List<SavedTowerData>(); // <<< 추가된 필드 null 체크 >>>

                    Debug.Log("플레이어 데이터 로드 완료. Gold: " + CurrentPlayerData.gold + ", Wave: " + CurrentPlayerData.currentWave);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"플레이어 데이터 로드 중 오류 발생: {ex.Message}. 새 데이터 생성.");
                CurrentPlayerData = new PlayerData();
            }
        }
        else
        {
            Debug.Log("저장된 플레이어 데이터 없음. 새 게임 데이터 생성.");
            CurrentPlayerData = new PlayerData(); // 생성자에서 모든 필드가 초기화됨
        }
    }

    public void SavePlayerData()
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogWarning("저장할 플레이어 데이터가 없습니다. 새 데이터를 생성하여 저장합니다.");
            CurrentPlayerData = new PlayerData();
        }

        try
        {
            string jsonPlayerData = JsonUtility.ToJson(CurrentPlayerData, true);
            File.WriteAllText(saveFilePath, jsonPlayerData);
            Debug.Log("플레이어 데이터 저장 완료: " + saveFilePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"플레이어 데이터 저장 중 오류 발생: {ex.Message}");
        }
    }

    public void ResetPlayerData()
    {
        CurrentPlayerData = new PlayerData(); // 생성자에서 모든 필드가 초기화됨
        SavePlayerData();
        Debug.Log("플레이어 데이터가 초기화되었으며, 초기값으로 저장되었습니다.");
    }
}