using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Linq 사용을 위해 추가

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance;

    // 기존 TowerPool 리스트는 이제 사용하지 않습니다.
    // public List<TowerPool> towerPools = new List<TowerPool>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public GameObject SummonRandomTower(Vector3 spawnPosition)
    {
        // 0) DB 검사
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.towerDataList.Count == 0)
        {
            Debug.LogWarning("DB에 등록된 타워 데이터가 없습니다!");
            return null;
        }

        // 1) 랜덤 등급 결정 & 후보 리스트 필터링
        TowerGrade randomGrade = GetRandomGradeFromDB();
        string gradeStr = randomGrade.ToString().ToUpper();
        var candidates = DatabaseManager.Instance.towerDataList
            .Where(td => td.towerGrade.ToUpper() == gradeStr && !td.mythicOnly)
            .ToList();

        // 1.1) 폴백 로직: Normal 등급으로
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"{gradeStr} 등급 후보가 없어 Normal 등급으로 폴백합니다.");
            candidates = DatabaseManager.Instance.towerDataList
                .Where(td => td.towerGrade.ToUpper() == "NORMAL" && !td.mythicOnly)
                .ToList();
            if (candidates.Count == 0)
            {
                Debug.LogError("Normal 등급 후보조차 없습니다. 소환 취소.");
                return null;
            }
        }

        // 2) 실제 선택된 타워 데이터
        var selected = candidates[Random.Range(0, candidates.Count)];

        // 3) 소환 비용 결정 & 차감 (고정 비용 50으로 단순화)
        int cost = 30;
        if (!GameManager.Instance.SpendGold(cost))
        {
            Debug.Log("골드 부족으로 소환 불가!");
            return null;
        }

        // 4) 프리팹 로드 및 생성
        GameObject prefab = Resources.Load<GameObject>(selected.prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"프리팹 로드 실패: {selected.prefabPath}");
            GameManager.Instance.AddGold(cost); // 환불
            return null;
        }

        GameObject tower = Instantiate(prefab, spawnPosition, Quaternion.identity);
        var ts = tower.GetComponent<TowerScript>();
        if (ts != null)
        {
            ts.InitializeFromDB(selected);
            tower.name = $"{selected.towerName}_{selected.towerGrade}_{System.Guid.NewGuid():N}".Substring(0, 20);
        }
        else
        {
            Debug.LogError("TowerScript 컴포넌트 누락!");
            GameManager.Instance.AddGold(cost);
            Destroy(tower);
            return null;
        }

        Debug.Log($"[{selected.towerGrade}] {selected.towerName} 소환 완료! 비용: {cost}");
        return tower;
    }


    public GameObject SummonSpecificTower(Vector3 position, TowerType typeEnum, TowerGrade gradeEnum)
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.towerDataList.Count == 0)
        {
            Debug.LogError("❌ 타워 데이터가 DB에 없습니다: Summon 실패");
            return null;
        }

        string typeString = typeEnum.ToString().ToUpper();
        string gradeString = gradeEnum.ToString().ToUpper();

        // DB에서 요청된 타입과 등급에 맞는 타워 데이터 검색
        TowerDataRecord selectedTowerData = DatabaseManager.Instance.towerDataList
            .FirstOrDefault(td => td.towerType.ToUpper() == typeString && td.towerGrade.ToUpper() == gradeString);

        if (selectedTowerData == null)
        {
            Debug.LogError($"❌ 요청한 타입({typeString}) 및 등급({gradeString})에 해당하는 타워 데이터가 DB에 없습니다.");
            return null;
        }

        GameObject towerPrefab = Resources.Load<GameObject>(selectedTowerData.prefabPath);
        if (towerPrefab == null)
        {
            Debug.LogError($"프리팹 로드 실패: {selectedTowerData.prefabPath}. DB의 prefab_path를 확인하세요.");
            return null;
        }

        GameObject newTower = Instantiate(towerPrefab, position, Quaternion.identity);
        TowerScript towerScript = newTower.GetComponent<TowerScript>();

        if (towerScript != null)
        {
            towerScript.InitializeFromDB(selectedTowerData);
            newTower.name = $"{selectedTowerData.towerName}_{selectedTowerData.towerGrade}"; // 합성 시 이름
        }
        else
        {
            Debug.LogError($"소환된 특정 타워에 TowerScript 컴포넌트가 없습니다! 프리팹: {selectedTowerData.prefabPath}");
            Destroy(newTower);
            return null;
        }
        return newTower;
    }

    // DB의 grade_probability 테이블을 사용하여 등급 결정
    private TowerGrade GetRandomGradeFromDB()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.gradeProbabilityList.Count == 0)
        {
            Debug.LogWarning("DB에 등급 확률 데이터가 없습니다. Normal 등급으로 고정합니다.");
            return TowerGrade.Normal;
        }

        List<GradeProbabilityRecord> probabilities = DatabaseManager.Instance.gradeProbabilityList;
        float totalRate = probabilities.Sum(p => p.summonRate); // 확률 총합 (보통 100이어야 함)
        float randomPoint = Random.Range(0f, totalRate);
        float currentRateSum = 0f;

        // 확률에 따라 등급 정렬 (선택적이지만, 낮은 등급부터 높은 등급 순으로 정렬되어 있다고 가정)
        // probabilities.Sort((a, b) => Enum.Parse<TowerGrade>(a.towerGrade).CompareTo(Enum.Parse<TowerGrade>(b.towerGrade)));


        foreach (var prob in probabilities)
        {
            currentRateSum += prob.summonRate;
            if (randomPoint <= currentRateSum)
            {
                try
                {
                    // DB의 tower_grade 문자열을 TowerGrade enum으로 변환
                    return (TowerGrade)System.Enum.Parse(typeof(TowerGrade), prob.towerGrade, true);
                }
                catch (System.ArgumentException ex)
                {
                    Debug.LogError($"DB의 등급 문자열 ({prob.towerGrade})을 TowerGrade enum으로 변환 실패: {ex.Message}. Normal 등급으로 대체합니다.");
                    return TowerGrade.Normal;
                }
            }
        }
        // 모든 확률을 지나도 결정되지 않으면 (오류 상황), 가장 낮은 등급 또는 기본 등급 반환
        Debug.LogWarning("등급 확률 계산 오류. Normal 등급으로 고정합니다.");
        return TowerGrade.Normal;
    }

    // 기존 GetRandomGrade() 함수는 GetRandomGradeFromDB()로 대체됨
    // private TowerGrade GetRandomGrade()
    // {
    //     // ... 기존 확률 로직 ...
    // }
}