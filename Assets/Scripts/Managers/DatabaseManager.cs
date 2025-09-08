using UnityEngine;
using System.Collections.Generic;
using MySql.Data.MySqlClient; // MySQL 커넥터 사용을 위해 추가
using System; // Exception 사용을 위해 추가

// DB의 tower_data 테이블 구조와 매칭될 클래스
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

// DB의 monster_data 테이블 구조와 매칭될 클래스
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

// DB의 wave_definitions 테이블 구조와 매칭될 클래스
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

// DB의 achievement_definitions 테이블 구조와 매칭될 클래스
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

// DB의 grade_probability 테이블 구조와 매칭될 클래스
[System.Serializable]
public class GradeProbabilityRecord
{
    public string towerGrade;
    public float summonRate;
}


public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    // DB 연결 정보 (주의: 실제 비밀번호 등은 안전하게 관리해야 합니다)
    // MySQL Workbench에서 사용하는 접속 정보와 동일하게 입력합니다.
    // 예: 서버 주소가 localhost(내 컴퓨터), 사용자 ID가 root, 비밀번호가 1234, 데이터베이스명이 towerdefenselocal
    private string connectionString = "Server=127.0.0.1;Database=TowerDefenseLocal;Uid=root;Pwd=1234;";
    // 중요!!! 위 Uid (사용자 ID) 와 Pwd (비밀번호)는 본인의 MySQL 설정에 맞게 꼭 수정해주세요.

    // DB에서 로드한 데이터를 저장할 리스트
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
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 유지되도록 설정

            // DB 연결 문자열을 실제 환경에 맞게 수정하세요.
            // 예: 실제 DB 서버 IP, 사용자 ID, 암호 등
            // connectionString = "Server=YOUR_DB_IP;Database=towerdefenselocal;Uid=YOUR_DB_USER;Pwd=YOUR_DB_PASSWORD;";

            LoadAllDataFromDB(); // 게임 시작 시 모든 설정 데이터를 DB에서 로드
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadAllDataFromDB()
    {
        Debug.Log("DB에서 모든 설정 데이터 로드를 시작합니다...");
        LoadTowerData();
        LoadMonsterData();
        LoadWaveDefinitions();
        LoadAchievementDefinitions();
        LoadGradeProbabilities();
        Debug.Log("모든 설정 데이터 로드 시도 완료.");
    }

    // tower_data 테이블에서 데이터 로드
    public void LoadTowerData()
    {
        towerDataList.Clear(); // 기존 리스트 초기화
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("tower_data 로드 중: DB 연결 성공.");
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
                Debug.Log(towerDataList.Count + "개의 타워 데이터 로드 완료.");
            }
            catch (Exception ex)
            {
                Debug.LogError("tower_data 로드 중 DB 오류: " + ex.Message);
            }
            finally // 성공하든 실패하든 항상 연결을 닫도록 finally 사용
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }
    }

    // monster_data 테이블에서 데이터 로드
    public void LoadMonsterData()
    {
        monsterDataList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("monster_data 로드 중: DB 연결 성공.");
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
                Debug.Log(monsterDataList.Count + "개의 몬스터 데이터 로드 완료.");
            }
            catch (Exception ex)
            {
                Debug.LogError("monster_data 로드 중 DB 오류: " + ex.Message);
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

    // wave_definitions 테이블에서 데이터 로드
    public void LoadWaveDefinitions()
    {
        waveDefinitionList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("wave_definitions 로드 중: DB 연결 성공.");
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
                Debug.Log(waveDefinitionList.Count + "개의 웨이브 정의 데이터 로드 완료.");
            }
            catch (Exception ex)
            {
                Debug.LogError("wave_definitions 로드 중 DB 오류: " + ex.Message);
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

    // achievement_definitions 테이블에서 데이터 로드
    public void LoadAchievementDefinitions()
    {
        achievementDefinitionList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("achievement_definitions 로드 중: DB 연결 성공.");
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
                Debug.Log(achievementDefinitionList.Count + "개의 업적 정의 데이터 로드 완료.");
            }
            catch (Exception ex)
            {
                Debug.LogError("achievement_definitions 로드 중 DB 오류: " + ex.Message);
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

    // grade_probability 테이블에서 데이터 로드
    public void LoadGradeProbabilities()
    {
        gradeProbabilityList.Clear();
        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                Debug.Log("grade_probability 로드 중: DB 연결 성공.");
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
                Debug.Log(gradeProbabilityList.Count + "개의 등급 확률 데이터 로드 완료.");
            }
            catch (Exception ex)
            {
                Debug.LogError("grade_probability 로드 중 DB 오류: " + ex.Message);
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

    // 특정 TowerDataRecord를 찾는 함수 (예시)
    public TowerDataRecord GetTowerData(string type, string grade)
    {
        return towerDataList.Find(t => t.towerType == type && t.towerGrade == grade);
    }
    public TowerDataRecord GetTowerDataByName(string name)
    {
        return towerDataList.Find(t => t.towerName == name);
    }

    // (향후 추가) 다른 데이터 찾는 함수들
    public MonsterDataRecord GetMonsterDataByName(string name)
    {
        return monsterDataList.Find(m => m.monsterName == name);
    }
    public List<WaveDefinitionRecord> GetWaveDefinitionsByWaveNumber(int waveNumber) // 함수 이름 명확화
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