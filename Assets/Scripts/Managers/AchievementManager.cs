using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    private float checkInterval = 1f;
    private float checkTimer = 0f;

    public int GetTotalMonstersKilled() { return totalMonstersKilled; }
    public float GetAccumulatedGold() { return accumulatedGold; }
    public int GetSynthesisCount() { return synthesisCount; }

    private HashSet<string> achievedIdListForSave = new HashSet<string>();

    // 게임 상태를 추적하기 위한 변수들 (PlayerData에서 로드/저장되어야 함)
    private int totalMonstersKilled = 0;
    private int totalEliteMonstersKilled = 0;
    // private int totalBossesKilled = 0; // 모든 보스 처치 수 (개별 보스 처치와 구분)
    private Dictionary<string, int> specificBossKillsCount = new Dictionary<string, int>(); // 보스 이름(또는 ID)별 처치 횟수
    private int synthesisCount = 0;
    private float accumulatedGold = 0;

    // 플레이어가 "획득한 적 있는" 타워 목록 (등급과 타입을 조합한 문자열 등)
    // 이 데이터도 PlayerData에 저장하고 로드해야 TOWER_COLLECT_GRADE_ALL_TYPES 업적을 정확히 판별 가능
    private HashSet<string> collectedTowerSignatures = new HashSet<string>();


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // GameManager 등에서 PlayerData 로드 후 이 함수를 호출하여 달성 목록 및 누적 데이터 초기화
    public void LoadAchievedListFromPlayerData(List<string> loadedAchievedIds, PlayerData playerData)
    {
        if (loadedAchievedIds != null)
        {
            achievedIdListForSave = new HashSet<string>(loadedAchievedIds);
            Debug.Log($"AchievementManager: 플레이어의 달성 업적 {achievedIdListForSave.Count}개 로드 완료.");
        }
        else
        {
            achievedIdListForSave = new HashSet<string>();
            Debug.LogWarning("AchievementManager: 플레이어의 달성 업적 목록을 로드하지 못했습니다. 새로 시작합니다.");
        }

        // PlayerData에서 누적 데이터 로드 (PlayerData에 해당 필드가 정의되어 있다고 가정)
        if (playerData != null)
        {
            totalMonstersKilled = playerData.totalMonstersKilled;
            totalEliteMonstersKilled = playerData.totalEliteMonstersKilled;
            // specificBossKills는 PlayerData에서 리스트 등으로 변환하여 저장/로드 필요
            // 여기서는 PlayerData에 List<BossKillEntry> bossKills 같은 형태로 저장하고 변환한다고 가정
            // LoadSpecificBossKills(playerData.bossKillsData); // 별도 함수로 처리
            synthesisCount = playerData.synthesisCount;
            accumulatedGold = playerData.accumulatedGold;
            // collectedTowerSignatures = new HashSet<string>(playerData.collectedTowerSignatures ?? new List<string>());
            Debug.Log("AchievementManager: 누적 데이터 로드 완료.");
        }
        else
        {
            Debug.LogWarning("AchievementManager: 누적 데이터를 담은 PlayerData가 null입니다. 초기값 사용.");
            // 누적 변수들 초기화
            totalMonstersKilled = 0;
            totalEliteMonstersKilled = 0;
            specificBossKillsCount.Clear();
            synthesisCount = 0;
            accumulatedGold = 0f;
            collectedTowerSignatures.Clear();
        }
    }

    // 업적 달성 시 호출. 실제 저장은 GameManager.ManualSavePlayerData에서 일괄 처리.
    private void RecordAchievementProgress(string achievementId)
    {
        if (achievedIdListForSave.Add(achievementId))
        {
            Debug.Log($"업적 [{achievementId}] 달성! (저장은 수동 저장 시 반영됩니다)");
            // UI 알림은 CheckAllAchievements에서 직접 호출
        }
    }

    // GameManager가 ManualSavePlayerData 호출 시, 현재 달성 목록과 누적 통계를 PlayerData에 반영하기 위해 호출
    public void UpdatePlayerDataForSave(PlayerData playerDataToSave)
    {
        if (playerDataToSave == null) return;

        playerDataToSave.achievedAchievementIds = achievedIdListForSave.ToList();
        playerDataToSave.totalMonstersKilled = totalMonstersKilled;
        playerDataToSave.totalEliteMonstersKilled = totalEliteMonstersKilled;
        // playerDataToSave.bossKillsData = ConvertSpecificBossKillsToList(); // 딕셔너리를 저장 가능한 형태로 변환
        playerDataToSave.synthesisCount = synthesisCount;
        playerDataToSave.accumulatedGold = accumulatedGold;
        // playerDataToSave.collectedTowerSignatures = collectedTowerSignatures.ToList();
    }

    // GameManager의 ManualSavePlayerData에서 호출될 함수
    public List<string> GetAchievedIdsForSave()
    {
        return achievedIdListForSave.ToList();
    }


    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted) return;

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            CheckAllAchievements();
            checkTimer = checkInterval;
        }
    }

    public void NotifyMonsterKilled(MonsterDataRecord monsterData)
    {
        if (monsterData == null) return;
        totalMonstersKilled++;
        if (monsterData.monsterType?.ToUpper() == "ELITE")
        {
            totalEliteMonstersKilled++;
        }
        if (monsterData.isBoss)
        {
            if (!specificBossKillsCount.ContainsKey(monsterData.monsterName))
            {
                specificBossKillsCount[monsterData.monsterName] = 0;
            }
            specificBossKillsCount[monsterData.monsterName]++;
            Debug.Log($"Boss Killed: {monsterData.monsterName}, Count: {specificBossKillsCount[monsterData.monsterName]}");
        }
    }

    public void NotifySynthesisCompleted(TowerGrade synthesizedGrade) // 어떤 등급 타워가 합성되었는지 정보 받기
    {
        synthesisCount++;
        // "SYNTHESIZE_TOWER_GRADE_FIRST" 업적을 위해, 합성된 타워 등급 기록 (PlayerData에 저장 필요)
        // 예: PlayerData.Instance.MarkSynthesizedGrade(synthesizedGrade);
    }

    public void NotifyGoldAccumulated(float amount)
    {
        accumulatedGold += amount;
    }

    // 타워 획득/생성 시 호출되어 collectedTowerSignatures 업데이트
    public void NotifyTowerAcquired(TowerDataRecord towerData)
    {
        if (towerData == null) return;
        string signature = $"{towerData.towerGrade.ToUpper()}_{towerData.towerType.ToUpper()}";
        collectedTowerSignatures.Add(signature);
        // Debug.Log($"Tower Acquired for Achievement Tracking: {signature}");
    }


    public void CheckAllAchievements()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.achievementDefinitionList.Count == 0)
        {
            return;
        }

        // 현재 게임 상태 수집 (타워 현황 등)
        Dictionary<TowerGrade, int> currentGradeCount = new Dictionary<TowerGrade, int>();
        Dictionary<TowerGrade, HashSet<TowerType>> currentGradeTypes = new Dictionary<TowerGrade, HashSet<TowerType>>();
        List<TowerScript> allPlacedTowers = new List<TowerScript>(FindObjectsOfType<TowerScript>());
        HashSet<string> ownedTowerNames = new HashSet<string>();

        foreach (var towerScript in allPlacedTowers)
        {
            if (towerScript.isActiveAndEnabled && towerScript.DbData != null)
            {
                TowerGrade grade = towerScript.grade;
                TowerType type = towerScript.towerType;

                if (!currentGradeCount.ContainsKey(grade)) currentGradeCount[grade] = 0;
                currentGradeCount[grade]++;

                if (!currentGradeTypes.ContainsKey(grade)) currentGradeTypes[grade] = new HashSet<TowerType>();
                currentGradeTypes[grade].Add(type);

                ownedTowerNames.Add(towerScript.DbData.towerName);
            }
        }

        // DB 업적 정의 순회 및 달성 조건 확인
        foreach (AchievementDefinitionRecord achDef in DatabaseManager.Instance.achievementDefinitionList)
        {
            if (achievedIdListForSave.Contains(achDef.achievementIdText))
            {
                continue; // 이미 달성한 업적
            }

            bool conditionMet = false;

            switch (achDef.conditionType?.ToUpper())
            {
                case "TOWER_COUNT_GRADE":
                    if (System.Enum.TryParse<TowerGrade>(achDef.conditionValue1, true, out TowerGrade targetGrade_TCG) &&
                        int.TryParse(achDef.conditionValue2, out int requiredCount_TCG))
                    {
                        if (currentGradeCount.TryGetValue(targetGrade_TCG, out int count) && count >= requiredCount_TCG)
                            conditionMet = true;
                    }
                    break;

                case "TOWER_COLLECT_GRADE_ALL_TYPES":
                    // 이 업적은 '플레이어가 해당 등급의 모든 타입을 한 번이라도 획득했는가'를 봐야 함.
                    // collectedTowerSignatures 와 GetAvailableTypesForGradeFromDB(grade, true) 를 비교해야 함.
                    if (System.Enum.TryParse<TowerGrade>(achDef.conditionValue1, true, out TowerGrade targetGrade_TCGAT))
                    {
                        List<TowerType> availableTypes = GetAvailableTypesForGradeFromDB(targetGrade_TCGAT, true);
                        if (availableTypes.Count > 0) // 해당 등급에 정의된 타입이 있을 경우에만
                        {
                            bool allCollected = true;
                            foreach (TowerType type in availableTypes)
                            {
                                if (!collectedTowerSignatures.Contains($"{targetGrade_TCGAT.ToString().ToUpper()}_{type.ToString().ToUpper()}"))
                                {
                                    allCollected = false;
                                    break;
                                }
                            }
                            if (allCollected) conditionMet = true;
                        }
                    }
                    break;

                case "OWN_TOWER_GRADE_FIRST":
                    // 이 업적은 특정 등급 타워를 "처음" 보유하는 순간에 달성되어야 함.
                    // CheckAllAchievements에서 매번 체크하는 것보다, 타워 생성/합성 시점에 판단하는 것이 더 정확.
                    // 여기서는 단순히 해당 등급 타워 보유 여부로 임시 체크.
                    if (System.Enum.TryParse<TowerGrade>(achDef.conditionValue1, true, out TowerGrade targetGrade_OTGF))
                    {
                        if (currentGradeCount.ContainsKey(targetGrade_OTGF) && currentGradeCount[targetGrade_OTGF] > 0)
                            conditionMet = true;
                    }
                    break;

                case "OWN_SPECIFIC_TOWER":
                    if (ownedTowerNames.Contains(achDef.conditionValue1))
                        conditionMet = true;
                    break;

                case "OWN_TOWER_GRADE_COUNT": // TOWER_COUNT_GRADE와 동일 로직
                    if (System.Enum.TryParse<TowerGrade>(achDef.conditionValue1, true, out TowerGrade targetGrade_OTGC) &&
                        int.TryParse(achDef.conditionValue2, out int requiredCount_OTGC))
                    {
                        if (currentGradeCount.TryGetValue(targetGrade_OTGC, out int count_otgc) && count_otgc >= requiredCount_OTGC)
                            conditionMet = true;
                    }
                    break;

                case "CLEAR_WAVE":
                    if (int.TryParse(achDef.conditionValue1, out int targetWave_CW))
                    {
                        if (GameManager.Instance != null && GameManager.Instance.currentWave > targetWave_CW)
                            conditionMet = true;
                    }
                    break;

                case "KILL_SPECIFIC_BOSS":
                    if (specificBossKillsCount.TryGetValue(achDef.conditionValue1, out int killCount) && killCount > 0)
                        conditionMet = true;
                    break;

                case "REACH_WAVE":
                    if (int.TryParse(achDef.conditionValue1, out int targetWave_RW))
                    {
                        if (GameManager.Instance != null && GameManager.Instance.currentWave >= targetWave_RW)
                            conditionMet = true;
                    }
                    break;

                case "HOLD_GOLD":
                    if (int.TryParse(achDef.conditionValue1, out int requiredGold_HG))
                    {
                        if (GameManager.Instance != null && GameManager.Instance.gold >= requiredGold_HG)
                            conditionMet = true;
                    }
                    break;

                case "ACCUMULATE_GOLD":
                    if (float.TryParse(achDef.conditionValue1, out float requiredGold_AG))
                    {
                        if (accumulatedGold >= requiredGold_AG)
                            conditionMet = true;
                    }
                    break;

                case "KILL_MONSTER_TYPE_COUNT":
                    if (int.TryParse(achDef.conditionValue2, out int requiredKills_KMTC))
                    {
                        if (achDef.conditionValue1?.ToUpper() == "ELITE" && totalEliteMonstersKilled >= requiredKills_KMTC)
                            conditionMet = true;
                        // else if (achDef.conditionValue1?.ToUpper() == "BOSS" && totalBossesKilled >= requiredKills_KMTC) conditionMet = true; // totalBossesKilled는 모든 보스 합산
                    }
                    break;

                case "KILL_ANY_MONSTER_COUNT":
                    if (int.TryParse(achDef.conditionValue1, out int requiredKills_KAMC))
                    {
                        if (totalMonstersKilled >= requiredKills_KAMC)
                            conditionMet = true;
                    }
                    break;

                case "SYNTHESIZE_TOWER_GRADE_FIRST":
                    // 이 업적은 UpgradeManager에서 NotifySynthesisCompleted(TowerGrade grade)를 호출할 때,
                    // 해당 grade가 처음 합성된 것인지 PlayerData에 저장된 정보와 비교하여 판단해야 함.
                    // 지금은 해당 등급 타워 보유 여부로만 임시 판단.
                    if (System.Enum.TryParse<TowerGrade>(achDef.conditionValue1, true, out TowerGrade targetGrade_STGF))
                    {
                        // PlayerData에 Set<string> synthesizedGrades 같은 것을 만들어서 관리 필요.
                        // if (playerData.HasSynthesizedGradeFirstTime(targetGrade_STGF)) conditionMet = true;
                        if (currentGradeCount.ContainsKey(targetGrade_STGF) && currentGradeCount[targetGrade_STGF] > 0) // 임시
                            conditionMet = true;
                    }
                    break;

                case "SYNTHESIZE_COUNT":
                    if (int.TryParse(achDef.conditionValue1, out int requiredSynCount))
                    {
                        if (synthesisCount >= requiredSynCount)
                            conditionMet = true;
                    }
                    break;
            }

            if (conditionMet)
            {
                if (GameManager.Instance != null) GameManager.Instance.AddGold(achDef.rewardGold);
                RecordAchievementProgress(achDef.achievementIdText); // 파일 저장은 안 함. 상태만 업데이트.

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowAchievementMessage(
                        $"{achDef.achievementName} 달성! 골드 +{achDef.rewardGold}"
                    );
                }
                Debug.Log($"업적 달성: {achDef.achievementName} ({achDef.achievementIdText}) → 골드 +{achDef.rewardGold}");
            }
        }
    }

    public List<TowerType> GetAvailableTypesForGradeFromDB(TowerGrade grade, bool onlyExactGrade = false) // <<< public으로 변경
    {
        HashSet<TowerType> types = new HashSet<TowerType>();
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.towerDataList == null)
        {
            return types.ToList();
        }

        foreach (var towerDef in DatabaseManager.Instance.towerDataList)
        {
            TowerGrade currentTowerDefGrade;
            if (System.Enum.TryParse<TowerGrade>(towerDef.towerGrade, true, out currentTowerDefGrade))
            {
                if (onlyExactGrade)
                {
                    if (currentTowerDefGrade == grade)
                    {
                        if (System.Enum.TryParse<TowerType>(towerDef.towerType, true, out TowerType typeEnum))
                        {
                            types.Add(typeEnum);
                        }
                    }
                }
                else
                {
                    TowerGrade unlockGradeEnum;
                    bool parsedUnlockGrade = System.Enum.TryParse<TowerGrade>(towerDef.unlockGrade, true, out unlockGradeEnum);
                    if (!parsedUnlockGrade)
                    {
                        unlockGradeEnum = currentTowerDefGrade;
                    }
                    if (!towerDef.mythicOnly && (int)unlockGradeEnum <= (int)grade)
                    {
                        if (System.Enum.TryParse<TowerType>(towerDef.towerType, true, out TowerType typeEnum))
                        {
                            types.Add(typeEnum);
                        }
                    }
                }
            }
        }
        return types.ToList();
    }


    /// <summary>
    /// 현재까지 처치한 총 엘리트 몬스터 수를 반환합니다.
    /// </summary>
    /// <returns>총 엘리트 몬스터 처치 수</returns>
    public int GetTotalEliteMonstersKilled()
    {
        return totalEliteMonstersKilled;
    }

    /// <summary>
    /// 현재 맵에 배치된 특정 등급의 타워 개수를 반환합니다.
    /// </summary>
    public int GetCurrentTowerCountByGrade(TowerGrade grade)
    {
        // CheckAllAchievements() 내부의 currentGradeCount 계산 로직을 참고하거나,
        // 또는 CheckAllAchievements()가 호출될 때 이 정보를 미리 계산하여 별도 변수에 저장 후 반환할 수 있습니다.
        // 여기서는 CheckAllAchievements()와 유사하게 실시간으로 계산하는 예시를 보여드립니다.
        int count = 0;
        List<TowerScript> allPlacedTowers = new List<TowerScript>(FindObjectsOfType<TowerScript>());
        foreach (var towerScript in allPlacedTowers)
        {
            if (towerScript.isActiveAndEnabled && towerScript.grade == grade)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 특정 등급의 모든 종류 타워를 수집했는지 여부를 판단하기 위해,
    /// 현재까지 플레이어가 획득한 해당 등급의 타워 타입 시그니처 개수를 반환합니다.
    /// GetAvailableTypesForGradeFromDB()와 함께 사용하여 진행률을 계산할 수 있습니다.
    /// </summary>
    public int GetCollectedTowerTypeCountForGrade(TowerGrade grade)
    {
        int count = 0;
        string gradeStr = grade.ToString().ToUpper() + "_";
        foreach (string signature in collectedTowerSignatures)
        {
            if (signature.StartsWith(gradeStr))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 특정 이름을 가진 타워를 현재 보유하고 있는지 확인합니다.
    /// </summary>
    public bool IsSpecificTowerOwned(string towerName)
    {
        // CheckAllAchievements() 내부의 ownedTowerNames 계산 로직 참고
        List<TowerScript> allPlacedTowers = new List<TowerScript>(FindObjectsOfType<TowerScript>());
        foreach (var towerScript in allPlacedTowers)
        {
            if (towerScript.isActiveAndEnabled && towerScript.DbData != null && towerScript.DbData.towerName == towerName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 보스 몬스터를 처치한 횟수를 반환합니다.
    /// </summary>
    public int GetSpecificBossKillCount(string bossNameOrId)
    {
        if (specificBossKillsCount.TryGetValue(bossNameOrId, out int killCount))
        {
            return killCount;
        }
        return 0;
    }
}