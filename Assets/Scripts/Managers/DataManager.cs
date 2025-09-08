using UnityEngine;
using System.IO; // ���� ������� ���� �ʿ�
using System.Collections.Generic; // List ����� ���� �ʿ�

// <<< �߰�: ����� ���� Ÿ���� ���� ����ü >>>
[System.Serializable]
public class SavedTowerData
{
    public string towerTypeString; // TowerType enum ���� ���ڿ��� ����
    public string towerGradeString; // TowerGrade enum ���� ���ڿ��� ����
    public float positionX;
    public float positionY;
    public float positionZ;
    // �ʿ��ϴٸ� public float currentHealth; �� �߰� ����

    // �⺻ ������ (JsonUtility�� ���)
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
// <<< ������� SavedTowerData Ŭ���� ���� >>>


// �÷��̾��� ����� ������ ����
[System.Serializable]
public class PlayerData
{
    public int gold;
    public int currentWave;
    public List<string> achievedAchievementIds; // �޼��� ���� ID ���

    // AchievementManager�� �����ϱ� ���� ���� ��� �ʵ��
    public int totalMonstersKilled;
    public int totalEliteMonstersKilled;
    public int totalBossesKilled;
    public List<string> killedBossNames;
    public int synthesisCount;
    public float accumulatedGold;
    public List<string> collectedTowerSignatures;

    // <<< �߰�: ��ġ�� Ÿ�� ���� ����Ʈ >>>
    public List<SavedTowerData> placedTowersData;

    // ������: �� ���� ���� �� �⺻���� �����մϴ�.
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

        placedTowersData = new List<SavedTowerData>(); // <<< �ʱ�ȭ >>>
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
                    Debug.LogWarning("�÷��̾� ������ ���� �ε� ���� �Ǵ� ���� ������ ����ֽ��ϴ�. �� ������ ����.");
                    CurrentPlayerData = new PlayerData();
                }
                else
                {
                    // ���� ���� ���� ���ϰ��� ȣȯ���� ���� List Ÿ�� �ʵ���� null�̸� �ʱ�ȭ
                    if (CurrentPlayerData.achievedAchievementIds == null) CurrentPlayerData.achievedAchievementIds = new List<string>();
                    if (CurrentPlayerData.killedBossNames == null) CurrentPlayerData.killedBossNames = new List<string>();
                    if (CurrentPlayerData.collectedTowerSignatures == null) CurrentPlayerData.collectedTowerSignatures = new List<string>();
                    if (CurrentPlayerData.placedTowersData == null) CurrentPlayerData.placedTowersData = new List<SavedTowerData>(); // <<< �߰��� �ʵ� null üũ >>>

                    Debug.Log("�÷��̾� ������ �ε� �Ϸ�. Gold: " + CurrentPlayerData.gold + ", Wave: " + CurrentPlayerData.currentWave);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"�÷��̾� ������ �ε� �� ���� �߻�: {ex.Message}. �� ������ ����.");
                CurrentPlayerData = new PlayerData();
            }
        }
        else
        {
            Debug.Log("����� �÷��̾� ������ ����. �� ���� ������ ����.");
            CurrentPlayerData = new PlayerData(); // �����ڿ��� ��� �ʵ尡 �ʱ�ȭ��
        }
    }

    public void SavePlayerData()
    {
        if (CurrentPlayerData == null)
        {
            Debug.LogWarning("������ �÷��̾� �����Ͱ� �����ϴ�. �� �����͸� �����Ͽ� �����մϴ�.");
            CurrentPlayerData = new PlayerData();
        }

        try
        {
            string jsonPlayerData = JsonUtility.ToJson(CurrentPlayerData, true);
            File.WriteAllText(saveFilePath, jsonPlayerData);
            Debug.Log("�÷��̾� ������ ���� �Ϸ�: " + saveFilePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"�÷��̾� ������ ���� �� ���� �߻�: {ex.Message}");
        }
    }

    public void ResetPlayerData()
    {
        CurrentPlayerData = new PlayerData(); // �����ڿ��� ��� �ʵ尡 �ʱ�ȭ��
        SavePlayerData();
        Debug.Log("�÷��̾� �����Ͱ� �ʱ�ȭ�Ǿ�����, �ʱⰪ���� ����Ǿ����ϴ�.");
    }
}