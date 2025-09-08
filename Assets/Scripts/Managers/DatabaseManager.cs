using UnityEngine;
using System.Collections.Generic;
using MySql.Data.MySqlClient; // MySQL Ŀ���� ����� ���� �߰�
using System; // Exception ����� ���� �߰�

// DB�� tower_data ���̺� ������ ��Ī�� Ŭ����
[System.Serializable]
public class TowerDataRecord
{
    public int id;
    public string towerName;
    public string towerType;
    public string towerGrade;
    public string description;
    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public string unlockGrade;
    public bool mythicOnly;
    public float specialAbilityValue1;
    public float specialAbilityValue2;
    public string prefabPath;
    public string iconPath;
}

// DB�� monster_data ���̺� ������ ��Ī�� Ŭ����
[System.Serializable]
public class MonsterDataRecord
{
    public int id;
    public string monsterName;
    public string monsterType;
    public string description;
    public float baseHp;
    public float baseSpeed;
    public int rewardGold;
    public bool isBoss;
    public string prefabPath;
    public string iconPath;
}

// DB�� wave_definitions ���̺� ������ ��Ī�� Ŭ����
[System.Serializable]
public class WaveDefinitionRecord
{
    public int waveNumber;
    public int monsterDataId;
    public int quantity;
    public float spawnInterval;
    public float hpMultiplier;
    public float speedMultiplier;
    public float goldMultiplier;
}

// DB�� achievement_definitions ���̺� ������ ��Ī�� Ŭ����
[System.Serializable]
public class AchievementDefinitionRecord
{
    public string achievementIdText;
    public string achievementName;
    public string description;
    public int rewardGold;
    public string conditionType;
    public string conditionValue1;
    public string conditionValue2;
}

// DB�� grade_probability ���̺� ������ ��Ī�� Ŭ����
[System.Serializable]
public class GradeProbabilityRecord
{
    public string towerGrade;
    public float summonRate;
}


public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    // DB ���� ���� (����: ���� ��й�ȣ ���� �����ϰ� �����ؾ� �մϴ�)
    // MySQL Workbench���� ����ϴ� ���� ������ �����ϰ� �Է��մϴ�.
    // ��: ���� �ּҰ� localhost(�� ��ǻ��), ����� ID�� root, ��й�ȣ�� 1234, �����ͺ��̽����� towerdefenselocal
    private string connectionString = "Server=127.0.0.1;Database=TowerDefenseLocal;Uid=root;Pwd=1234;";
    // �߿�!!! �� Uid (����� ID) �� Pwd (��й�ȣ)�� ������ MySQL ������ �°� �� �������ּ���.

    // DB���� �ε��� �����͸� ������ ����Ʈ
    public List<TowerDataRecord> towerDataList = new List<TowerDataRecord>();
    public List<MonsterDataRecord> monsterDataList = new List<MonsterDataRecord>();
    public List<WaveDefinitionRecord> waveDefinitionList = new List<WaveDefinitionRecord>();
    public List<AchievementDefinitionRecord> achievementDefinitionList = new List<AchievementDefinitionRecord>();
    public List<GradeProbabilityRecord> gradeProbabilityList = new List<GradeProbabilityRecord>();


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� ����Ǿ �����ǵ��� ����

            // DB ���� ���ڿ��� ���� ȯ�濡 �°� �����ϼ���.
            // ��: ���� DB ���� IP, ����� ID, ��ȣ ��
            // connectionString = "Server=YOUR_DB_IP;Database=towerdefenselocal;Uid=YOUR_DB_USER;Pwd=YOUR_DB_PASSWORD;";

            LoadAllDataFromDB(); // ���� ���� �� ��� ���� �����͸� DB���� �ε�
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadAllDataFromDB()
    {
        Debug.Log("DB���� ��� ���� ������ �ε带 �����մϴ�...");
        LoadTowerData();
        LoadMonsterData();
        LoadWaveDefinitions();
        LoadAchievementDefinitions();
        LoadGradeProbabilities();
        Debug.Log("��� ���� ������ �ε� �õ� �Ϸ�.");
    }

    // tower_data ���̺��� ������ �ε�
    public void LoadTowerData()
    {
        towerDataList.Clear(); // ���� ����Ʈ �ʱ�ȭ
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("tower_data �ε� ��: DB ���� ����.");
                string query = "SELECT id, tower_name, tower_type, tower_grade, description, attack_damage, attack_range, attack_cooldown, unlock_grade, mythic_only, special_ability_value1, special_ability_value2, prefab_path, icon_path FROM tower_data;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TowerDataRecord data = new TowerDataRecord
                            {
                                id = reader.GetInt32("id"),
                                towerName = reader.GetString("tower_name"),
                                towerType = reader.GetString("tower_type"),
                                towerGrade = reader.GetString("tower_grade"),
                                description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                attackDamage = reader.GetFloat("attack_damage"),
                                attackRange = reader.GetFloat("attack_range"),
                                attackCooldown = reader.GetFloat("attack_cooldown"),
                                unlockGrade = reader.IsDBNull(reader.GetOrdinal("unlock_grade")) ? "" : reader.GetString("unlock_grade"),
                                mythicOnly = reader.GetBoolean("mythic_only"),
                                specialAbilityValue1 = reader.IsDBNull(reader.GetOrdinal("special_ability_value1")) ? 0f : reader.GetFloat("special_ability_value1"),
                                specialAbilityValue2 = reader.IsDBNull(reader.GetOrdinal("special_ability_value2")) ? 0f : reader.GetFloat("special_ability_value2"),
                                prefabPath = reader.IsDBNull(reader.GetOrdinal("prefab_path")) ? "" : reader.GetString("prefab_path"),
                                iconPath = reader.IsDBNull(reader.GetOrdinal("icon_path")) ? "" : reader.GetString("icon_path")
                            };
                            towerDataList.Add(data);
                        }
                    }
                }
                Debug.Log(towerDataList.Count + "���� Ÿ�� ������ �ε� �Ϸ�.");
            }
            catch (Exception ex)
            {
                Debug.LogError("tower_data �ε� �� DB ����: " + ex.Message);
            }
            finally // �����ϵ� �����ϵ� �׻� ������ �ݵ��� finally ���
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    // monster_data ���̺��� ������ �ε�
    public void LoadMonsterData()
    {
        monsterDataList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("monster_data �ε� ��: DB ���� ����.");
                string query = "SELECT id, monster_name, monster_type, description, base_hp, base_speed, reward_gold, is_boss, prefab_path, icon_path FROM monster_data;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MonsterDataRecord data = new MonsterDataRecord
                            {
                                id = reader.GetInt32("id"),
                                monsterName = reader.GetString("monster_name"),
                                monsterType = reader.IsDBNull(reader.GetOrdinal("monster_type")) ? "" : reader.GetString("monster_type"),
                                description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString("description"),
                                baseHp = reader.GetFloat("base_hp"),
                                baseSpeed = reader.GetFloat("base_speed"),
                                rewardGold = reader.GetInt32("reward_gold"),
                                isBoss = reader.GetBoolean("is_boss"),
                                prefabPath = reader.IsDBNull(reader.GetOrdinal("prefab_path")) ? "" : reader.GetString("prefab_path"),
                                iconPath = reader.IsDBNull(reader.GetOrdinal("icon_path")) ? "" : reader.GetString("icon_path")
                            };
                            monsterDataList.Add(data);
                        }
                    }
                }
                Debug.Log(monsterDataList.Count + "���� ���� ������ �ε� �Ϸ�.");
            }
            catch (Exception ex)
            {
                Debug.LogError("monster_data �ε� �� DB ����: " + ex.Message);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    // wave_definitions ���̺��� ������ �ε�
    public void LoadWaveDefinitions()
    {
        waveDefinitionList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("wave_definitions �ε� ��: DB ���� ����.");
                string query = "SELECT wave_number, monster_data_id, quantity, spawn_interval, hp_multiplier, speed_multiplier, gold_multiplier FROM wave_definitions;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            WaveDefinitionRecord data = new WaveDefinitionRecord
                            {
                                waveNumber = reader.GetInt32("wave_number"),
                                monsterDataId = reader.GetInt32("monster_data_id"),
                                quantity = reader.GetInt32("quantity"),
                                spawnInterval = reader.GetFloat("spawn_interval"),
                                hpMultiplier = reader.GetFloat("hp_multiplier"),
                                speedMultiplier = reader.GetFloat("speed_multiplier"),
                                goldMultiplier = reader.GetFloat("gold_multiplier")
                            };
                            waveDefinitionList.Add(data);
                        }
                    }
                }
                Debug.Log(waveDefinitionList.Count + "���� ���̺� ���� ������ �ε� �Ϸ�.");
            }
            catch (Exception ex)
            {
                Debug.LogError("wave_definitions �ε� �� DB ����: " + ex.Message);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    // achievement_definitions ���̺��� ������ �ε�
    public void LoadAchievementDefinitions()
    {
        achievementDefinitionList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("achievement_definitions �ε� ��: DB ���� ����.");
                string query = "SELECT achievement_id_text, achievement_name, description, reward_gold, condition_type, condition_value1, condition_value2 FROM achievement_definitions;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AchievementDefinitionRecord data = new AchievementDefinitionRecord
                            {
                                achievementIdText = reader.GetString("achievement_id_text"),
                                achievementName = reader.GetString("achievement_name"),
                                description = reader.GetString("description"),
                                rewardGold = reader.GetInt32("reward_gold"),
                                conditionType = reader.IsDBNull(reader.GetOrdinal("condition_type")) ? "" : reader.GetString("condition_type"),
                                conditionValue1 = reader.IsDBNull(reader.GetOrdinal("condition_value1")) ? "" : reader.GetString("condition_value1"),
                                conditionValue2 = reader.IsDBNull(reader.GetOrdinal("condition_value2")) ? "" : reader.GetString("condition_value2")
                            };
                            achievementDefinitionList.Add(data);
                        }
                    }
                }
                Debug.Log(achievementDefinitionList.Count + "���� ���� ���� ������ �ε� �Ϸ�.");
            }
            catch (Exception ex)
            {
                Debug.LogError("achievement_definitions �ε� �� DB ����: " + ex.Message);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    // grade_probability ���̺��� ������ �ε�
    public void LoadGradeProbabilities()
    {
        gradeProbabilityList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("grade_probability �ε� ��: DB ���� ����.");
                string query = "SELECT tower_grade, summon_rate FROM grade_probability;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            GradeProbabilityRecord data = new GradeProbabilityRecord
                            {
                                towerGrade = reader.GetString("tower_grade"),
                                summonRate = reader.GetFloat("summon_rate")
                            };
                            gradeProbabilityList.Add(data);
                        }
                    }
                }
                Debug.Log(gradeProbabilityList.Count + "���� ��� Ȯ�� ������ �ε� �Ϸ�.");
            }
            catch (Exception ex)
            {
                Debug.LogError("grade_probability �ε� �� DB ����: " + ex.Message);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    // Ư�� TowerDataRecord�� ã�� �Լ� (����)
    public TowerDataRecord GetTowerData(string type, string grade)
    {
        return towerDataList.Find(t => t.towerType == type && t.towerGrade == grade);
    }
    public TowerDataRecord GetTowerDataByName(string name)
    {
        return towerDataList.Find(t => t.towerName == name);
    }

    // (���� �߰�) �ٸ� ������ ã�� �Լ���
    public MonsterDataRecord GetMonsterDataByName(string name)
    {
        return monsterDataList.Find(m => m.monsterName == name);
    }
    public List<WaveDefinitionRecord> GetWaveDefinitionsByWaveNumber(int waveNumber) // �Լ� �̸� ��Ȯȭ
    {
        return waveDefinitionList.FindAll(w => w.waveNumber == waveNumber);
    }
    public AchievementDefinitionRecord GetAchievementDefinition(string achievementId)
    {
        return achievementDefinitionList.Find(a => a.achievementIdText == achievementId);
    }
    public GradeProbabilityRecord GetGradeProbability(string grade)
    {
        return gradeProbabilityList.Find(p => p.towerGrade == grade);
    }
}