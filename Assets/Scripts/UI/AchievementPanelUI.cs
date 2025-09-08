// AchievementPanelUI.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // VerticalLayoutGroup, ContentSizeFitter 등 사용 시
using TMPro;          // TextMeshProUGUI를 사용한다면 추가

public class AchievementPanelUI : MonoBehaviour
{
    public GameObject achievementEntryPrefab; // Inspector에서 업적 항목 UI 프리팹 연결
    public Transform contentParent;          // ScrollView의 Content 오브젝트 연결

    void OnEnable() // 패널이 활성화될 때마다 목록을 새로고침
    {
        Debug.Log("[AchievementPanelUI] OnEnable 호출됨, PopulateAchievements() 실행 시도.");
        PopulateAchievements();
    }

    public void PopulateAchievements()
    {
        Debug.Log("[AchievementPanelUI] PopulateAchievements() 함수 시작됨.");

        if (contentParent == null)
        {
            Debug.LogError("[AchievementPanelUI] CRITICAL: contentParent가 Inspector에 연결되지 않았습니다! 업적 항목을 생성할 위치가 없습니다.");
            return;
        }
        Debug.Log($"[AchievementPanelUI] contentParent 확인됨: {contentParent.name}");

        // 기존 항목들 삭제 (업데이트 시 중복 방지)
        Debug.Log($"[AchievementPanelUI] 기존 업적 항목 삭제 시작... 현재 자식 수: {contentParent.childCount}");
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("[AchievementPanelUI] 기존 업적 항목 삭제 완료.");

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[AchievementPanelUI] CRITICAL: DatabaseManager.Instance 가 null입니다. 업적 정의를 가져올 수 없습니다.");
            return;
        }
        if (DatabaseManager.Instance.achievementDefinitionList == null)
        {
            Debug.LogError("[AchievementPanelUI] CRITICAL: DatabaseManager.Instance.achievementDefinitionList 가 null입니다.");
            return;
        }
        Debug.Log("[AchievementPanelUI] DatabaseManager 및 achievementDefinitionList 확인됨.");

        if (AchievementManager.Instance == null)
        {
            Debug.LogError("[AchievementPanelUI] CRITICAL: AchievementManager.Instance 가 null입니다. 달성 정보를 가져올 수 없습니다.");
            return;
        }
        Debug.Log("[AchievementPanelUI] AchievementManager 확인됨.");

        List<AchievementDefinitionRecord> allDefinitions = DatabaseManager.Instance.achievementDefinitionList;
        HashSet<string> achievedIds = new HashSet<string>(AchievementManager.Instance.GetAchievedIdsForSave()); // 이 함수가 null을 반환하지 않는다고 가정

        Debug.Log($"[AchievementPanelUI] 로드된 총 업적 정의 수: {allDefinitions.Count}");
        Debug.Log($"[AchievementPanelUI] 현재까지 달성된 업적 ID 수: {achievedIds.Count}");

        if (allDefinitions.Count == 0)
        {
            Debug.LogWarning("[AchievementPanelUI] 표시할 업적 정의가 DatabaseManager에 없습니다.");
            // 여기에 "표시할 업적이 없습니다" 같은 안내 메시지를 UI에 표시하는 로직을 추가할 수 있습니다.
            // 예: if (noAchievementsText != null) noAchievementsText.SetActive(true);
            return;
        }

        if (achievementEntryPrefab == null)
        {
            Debug.LogError("[AchievementPanelUI] CRITICAL: achievementEntryPrefab이 Inspector에 할당되지 않았습니다! 업적 항목을 생성할 수 없습니다.");
            return; // 프리팹 없으면 진행 불가
        }
        Debug.Log($"[AchievementPanelUI] achievementEntryPrefab 확인됨: {achievementEntryPrefab.name}");

        Debug.Log("[AchievementPanelUI] 업적 항목 UI 생성 루프 시작...");
        int count = 0;
        foreach (AchievementDefinitionRecord def in allDefinitions)
        {
            count++;
            Debug.Log($"[AchievementPanelUI] 루프 {count}: 업적 ID [{def.achievementIdText}], 이름 [{def.achievementName}] 처리 시작.");

            GameObject entryObj = Instantiate(achievementEntryPrefab, contentParent);
            if (entryObj == null)
            {
                Debug.LogError($"[AchievementPanelUI] CRITICAL: 업적 항목 프리팹({achievementEntryPrefab.name}) Instantiate 실패! (루프 {count})");
                continue; // 다음 업적으로 넘어감
            }
            entryObj.name = $"AchievementEntry_{def.achievementIdText}"; // Hierarchy에서 알아보기 쉽게 이름 변경
            Debug.Log($"    - [{def.achievementName}] UI 오브젝트 생성됨: {entryObj.name}");

            AchievementEntryUI entryUI = entryObj.GetComponent<AchievementEntryUI>();
            Debug.Log($"entryObj 이름: {entryObj.name}, entryUI는 null인가?: {(entryUI == null)}"); // 이 로그 확인!
            if (entryUI != null)
            {
                bool isAchieved = achievedIds.Contains(def.achievementIdText);
                string progressString = GetProgressString(def, isAchieved); // 이 함수 내부에서도 로그를 확인해보세요.

                Debug.Log($"    - [{def.achievementName}] 데이터 설정 준비: 달성여부({isAchieved}), 진행/설명문자열='{progressString}'");
                entryUI.Setup(def, isAchieved);
                Debug.Log($"    - [{def.achievementName}] entryUI.Setup() 호출 완료.");
            }
            else
            {
                Debug.LogError($"[AchievementPanelUI] CRITICAL: 생성된 업적 항목 오브젝트({entryObj.name})에 AchievementEntryUI 스크립트가 없습니다. Prefab: {achievementEntryPrefab.name}");
            }
        }
        Debug.Log($"[AchievementPanelUI] 업적 항목 UI 생성 루프 완료. 총 {count}개 처리 시도.");
        Debug.Log("[AchievementPanelUI] PopulateAchievements() 함수 종료됨.");
    }

    // GetProgressString 함수는 이전과 동일하게 유지됩니다.
    // 필요하다면 GetProgressString 함수 내부에도 로그를 추가하여
    // 각 조건별로 어떤 값이 반환되는지 확인할 수 있습니다.
    private string GetProgressString(AchievementDefinitionRecord def, bool isAchieved)
    {
        // ... (이전 GetProgressString 함수 내용) ...
        // 예시 로그: Debug.Log($"GetProgressString: ID[{def.achievementIdText}], Type[{def.conditionType}], Achieved[{isAchieved}] -> 결과: {progressText}");
        // return progressText;

        if (isAchieved)
        {
            return "달성 완료!";
        }

        string progressText = "진행 상황 정보 없음"; // 기본값
        int requiredValue;
        float requiredFloatValue;
        int currentValue = 0;
        float currentFloatValue = 0f;

        if (AchievementManager.Instance == null)
        {
            Debug.LogError("GetProgressString: AchievementManager 인스턴스가 없습니다.");
            return "오류: AM 없음";
        }

        // (GetProgressString 함수의 나머지 switch 문은 이전 답변 내용과 동일하게 유지)
        // ... 각 case 문에서 실제 값들을 가져오고 progressText를 설정 ...
        // 예시 (KILL_ANY_MONSTER_COUNT):
        // case "KILL_ANY_MONSTER_COUNT":
        //     if (int.TryParse(def.conditionValue1, out requiredValue))
        //     {
        //         currentValue = AchievementManager.Instance.GetTotalMonstersKilled();
        //         progressText = $"몬스터 처치: {Mathf.Min(currentValue, requiredValue)} / {requiredValue}";
        //         Debug.Log($"  [Progress-{def.achievementIdText}]: {progressText}"); // 어떤 값이 계산되었는지 확인
        //     }
        //     else progressText = "조건 값 오류";
        //     break;

        // 임시로 모든 미달성 업적에 대해 설명을 반환하도록 설정 (GetProgressString 완성을 위해)
        // 실제로는 각 case를 모두 구현해야 합니다.
        if (!isAchieved && string.IsNullOrEmpty(def.description))
        {
            progressText = "업적 조건 설명 필요";
        }
        else if (!isAchieved)
        {
            progressText = def.description;
        }


        // 임시 반환 (실제 switch-case 문 완성 필요)
        // return progressText;

        // 정확한 진행 상황을 위해 이전 답변의 GetProgressString 함수 내용을 여기에 붙여넣고,
        // 각 case에 Debug.Log를 추가하여 어떤 값이 사용되는지 확인하는 것이 좋습니다.
        // 여기서는 간결성을 위해 이전 답변의 전체 switch문을 생략합니다.
        // 이전에 제공된 완성된 GetProgressString 함수를 사용해주세요.

        // 이 함수가 올바르게 완성되었다고 가정하고,
        // 아래는 해당 함수가 반환할 값에 대한 예시입니다.
        // (이전 답변에서 GetProgressString 함수 전체를 복사해서 여기에 넣어주세요)
        switch (def.conditionType?.ToUpper())
        {
            case "TOWER_COUNT_GRADE":
            case "OWN_TOWER_GRADE_COUNT":
                TowerGrade targetGrade_TCG;
                if (System.Enum.TryParse<TowerGrade>(def.conditionValue1, true, out targetGrade_TCG) &&
                    int.TryParse(def.conditionValue2, out requiredValue))
                {
                    currentValue = AchievementManager.Instance.GetCurrentTowerCountByGrade(targetGrade_TCG);
                    progressText = $"진행: {Mathf.Min(currentValue, requiredValue)} / {requiredValue}";
                }
                else progressText = "조건 값 오류";
                break;

            case "TOWER_COLLECT_GRADE_ALL_TYPES":
                TowerGrade targetGrade_TCGAT;
                if (System.Enum.TryParse<TowerGrade>(def.conditionValue1, true, out targetGrade_TCGAT))
                {
                    currentValue = AchievementManager.Instance.GetCollectedTowerTypeCountForGrade(targetGrade_TCGAT);
                    List<TowerType> allAvailableTypes = AchievementManager.Instance.GetAvailableTypesForGradeFromDB(targetGrade_TCGAT, true);
                    requiredValue = allAvailableTypes.Count;
                    if (requiredValue > 0) progressText = $"수집: {Mathf.Min(currentValue, requiredValue)} / {requiredValue} 종류";
                    else progressText = "해당 등급 타워 없음";
                }
                else progressText = "조건 값 오류";
                break;

            case "OWN_TOWER_GRADE_FIRST":
                progressText = isAchieved ? "달성 완료!" : $"최초 {def.conditionValue1} 등급 타워 제작";
                break;

            case "OWN_SPECIFIC_TOWER":
                bool owned = AchievementManager.Instance.IsSpecificTowerOwned(def.conditionValue1);
                progressText = owned ? $"{def.conditionValue1} 보유 중 (달성!)" : $"{def.conditionValue1} 미보유";
                break;

            case "CLEAR_WAVE":
                if (GameManager.Instance == null) { progressText = "오류: GM 없음"; break; }
                if (int.TryParse(def.conditionValue1, out requiredValue))
                {
                    currentValue = GameManager.Instance.currentWave - 1;
                    progressText = $"진행: {Mathf.Min(currentValue, requiredValue)} / {requiredValue} 웨이브 클리어";
                }
                else progressText = "조건 값 오류";
                break;

            case "REACH_WAVE":
                if (GameManager.Instance == null) { progressText = "오류: GM 없음"; break; }
                if (int.TryParse(def.conditionValue1, out requiredValue))
                {
                    currentValue = GameManager.Instance.currentWave;
                    progressText = $"진행: {Mathf.Min(currentValue, requiredValue)} / {requiredValue} 웨이브 도달";
                }
                else progressText = "조건 값 오류";
                break;

            case "KILL_SPECIFIC_BOSS":
                currentValue = AchievementManager.Instance.GetSpecificBossKillCount(def.conditionValue1);
                requiredValue = 1;
                progressText = $"진행: {Mathf.Min(currentValue, requiredValue)} / {requiredValue}";
                break;

            case "HOLD_GOLD":
                if (GameManager.Instance == null) { progressText = "오류: GM 없음"; break; }
                if (int.TryParse(def.conditionValue1, out requiredValue))
                {
                    currentValue = GameManager.Instance.gold;
                    progressText = $"골드: {currentValue:N0} / {requiredValue:N0}";
                }
                else progressText = "조건 값 오류";
                break;

            case "ACCUMULATE_GOLD":
                if (float.TryParse(def.conditionValue1, out requiredFloatValue))
                {
                    currentFloatValue = AchievementManager.Instance.GetAccumulatedGold();
                    progressText = $"누적 골드: {currentFloatValue:N0} / {requiredFloatValue:N0}";
                }
                else progressText = "조건 값 오류";
                break;

            case "KILL_MONSTER_TYPE_COUNT":
                if (int.TryParse(def.conditionValue2, out requiredValue))
                {
                    if (def.conditionValue1?.ToUpper() == "ELITE")
                    {
                        currentValue = AchievementManager.Instance.GetTotalEliteMonstersKilled();
                        progressText = $"엘리트: {Mathf.Min(currentValue, requiredValue)} / {requiredValue}";
                    }
                    else
                    {
                        progressText = $"{def.conditionValue1} 타입 {requiredValue}마리 처치";
                    }
                }
                else progressText = "조건 값 오류";
                break;

            case "KILL_ANY_MONSTER_COUNT":
                if (int.TryParse(def.conditionValue1, out requiredValue))
                {
                    currentValue = AchievementManager.Instance.GetTotalMonstersKilled();
                    progressText = $"모든 몬스터: {Mathf.Min(currentValue, requiredValue)} / {requiredValue}";
                }
                else progressText = "조건 값 오류";
                break;

            case "SYNTHESIZE_TOWER_GRADE_FIRST":
                progressText = isAchieved ? "달성 완료!" : $"최초 {def.conditionValue1} 등급 타워 합성";
                break;

            case "SYNTHESIZE_COUNT":
                if (int.TryParse(def.conditionValue1, out requiredValue))
                {
                    currentValue = AchievementManager.Instance.GetSynthesisCount();
                    progressText = $"합성: {Mathf.Min(currentValue, requiredValue)} / {requiredValue}회";
                }
                else progressText = "조건 값 오류";
                break;

            default:
                progressText = def.description;
                break;
        }
        return progressText;
    }
}