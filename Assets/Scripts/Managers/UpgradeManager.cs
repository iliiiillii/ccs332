using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // Linq 사용을 위해 추가

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    private List<TowerScript> selectedTowers = new List<TowerScript>();
    public Button upgradeButton;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (upgradeButton != null) // 버튼 null 체크 추가
            upgradeButton.gameObject.SetActive(false);
    }

    public void SelectTower(TowerScript tower)
    {
        if (selectedTowers.Contains(tower))
            return;

        if (selectedTowers.Count >= 2)
        {
            Debug.Log("⚠️ 이미 두 개 선택되어 있음. 추가 선택 불가.");
            return;
        }

        selectedTowers.Add(tower);
        tower.SetSelected(true);

        if (selectedTowers.Count == 2 && upgradeButton != null)
        {
            upgradeButton.gameObject.SetActive(true);  // 두 타워 선택 시 합성 버튼 활성화
        }
    }

    public void DeselectTower(TowerScript tower)
    {
        if (selectedTowers.Contains(tower))
        {
            selectedTowers.Remove(tower);
            tower.SetSelected(false);
            if (upgradeButton != null)
                upgradeButton.gameObject.SetActive(false);
        }
    }

    public void ClearSelection()
    {
        foreach (var tower in selectedTowers)
        {
            if (tower != null)
                tower.SetSelected(false);
        }
        selectedTowers.Clear();
        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(false);
    }

    private TileScript FindTileUnderTower(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(position);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<TileScript>(out var tile))
            {
                return tile;
            }
        }
        return null;
    }

    public void TryUpgrade()
    {
        if (selectedTowers.Count != 2)
            return;

        TowerScript t1 = selectedTowers[0];
        TowerScript t2 = selectedTowers[1];

        if (t1 == null || t2 == null || t1.DbData == null || t2. DbData == null) // dbData null 체크 추가
        {
            Debug.LogError("❌ 선택된 타워 또는 타워 데이터가 없습니다! 합성을 진행할 수 없습니다.");
            ClearSelection();
            return;
        }

        // 합성 조건: 다른 타워여야 하고, 등급과 타입이 같아야 하며, 최고 등급(Mythic)이 아니어야 함.
        // TowerScript의 grade와 towerType은 DB 데이터로 초기화된 enum 값을 사용.
        if (t1 == t2 || t1.grade != t2.grade || t1.towerType != t2.towerType || t1.grade == TowerGrade.Mythic)
        {
            Debug.LogError("❌ 합성 조건 불일치: 동일 타워 선택, 등급 다름, 종류 다름, 또는 이미 신화 등급입니다.");
            ClearSelection();
            return;
        }

        // 다음 등급 결정 (TowerGrade enum 순서에 의존)
        // TowerGrade enum이 Normal, Rare, Unique, Legendary, Epic, Mythic 순으로 정의되어 있다고 가정
        TowerGrade newGradeEnum = t1.grade + 1;
        if (newGradeEnum > TowerGrade.Mythic) // Enum의 마지막 값보다 커지면 Mythic으로 고정 (또는 오류 처리)
        {
            newGradeEnum = TowerGrade.Mythic;
            // 또는 Debug.LogError("더 이상 업그레이드할 수 없는 등급입니다."); ClearSelection(); return;
        }
        string newGradeString = newGradeEnum.ToString().ToUpper();


        // 새로운 타워의 타입 결정 로직 (DB 데이터 기반)
        List<TowerType> availableTypesForNewGrade = new List<TowerType>();
        if (DatabaseManager.Instance != null && DatabaseManager.Instance.towerDataList != null)
        {
            foreach (var towerDef in DatabaseManager.Instance.towerDataList)
            {
                // 새로운 등급(newGradeString)이고, 신화 전용이 아니거나 신화 등급인 경우
                // 또한, 해당 타워의 unlock_grade 조건을 만족하는지 확인 (선택적, DB 설계에 따라)
                // 여기서는 간단히 newGradeString과 일치하는 타워의 타입을 가져옴
                if (towerDef.towerGrade.ToUpper() == newGradeString)
                {
                    // DB의 tower_type 문자열을 TowerType enum으로 변환
                    if (System.Enum.TryParse<TowerType>(towerDef.towerType, true, out TowerType typeEnum))
                    {
                        // 특정 조건 (예: Buff 타워는 합성으로 안 나옴 등)을 추가할 수 있음
                        // if (typeEnum != TowerType.Buff)
                        availableTypesForNewGrade.Add(typeEnum);
                    }
                }
            }
        }

        TowerType newTypeEnum = t1.towerType; // 기본적으로는 원래 타입을 유지 시도 (선택적 규칙)
        if (availableTypesForNewGrade.Count > 0)
        {
            // 새로운 등급에서 사용 가능한 타입 중 랜덤 선택
            newTypeEnum = availableTypesForNewGrade[Random.Range(0, availableTypesForNewGrade.Count)];
        }
        else
        {
            // 만약 newGrade에서 가능한 타입이 없다면 (DB 데이터 부족 등), 합성을 실패 처리하거나
            // 원래 타입과 newGrade로 특정 소환을 시도해볼 수 있음 (SummonSpecificTower가 알아서 처리)
            Debug.LogWarning($"DB에 {newGradeString} 등급으로 합성 가능한 타워 타입 정의가 부족합니다. 기존 타입({t1.towerType})으로 합성을 시도합니다.");
            // newTypeEnum은 t1.towerType으로 유지됨
        }


        TileScript tile1 = FindTileUnderTower(t1.transform.position);
        TileScript tile2 = FindTileUnderTower(t2.transform.position);

        if (tile1 == null || tile2 == null)
        {
            Debug.LogError("❌ 타워의 타일을 찾을 수 없습니다. 합성을 중단합니다.");
            ClearSelection();
            return;
        }

        // 기존 타워 제거
        Vector3 positionForNewTower = tile1.transform.position; // 새 타워는 첫 번째 타워 위치에 생성

        // 첫 번째 타워가 있던 타일에서 타워 제거
        if (tile1.placedTower != null)
        {
            Destroy(tile1.placedTower);
            tile1.isOccupied = false;
            tile1.placedTower = null;
        }
        // 두 번째 타워가 있던 타일에서 타워 제거
        if (tile2.placedTower != null)
        {
            // 두 번째 타워가 첫 번째 타워와 다른 타일에 있는지 확인 (같은 타일 위 중첩 방지)
            if (tile1 != tile2)
            {
                Destroy(tile2.placedTower);
                tile2.isOccupied = false;
                tile2.placedTower = null;
            }
            else // 만약 두 타워가 같은 타일 위에 있었다면(이런 경우는 없어야 함), 이미 위에서 처리됨
            {
                Debug.LogWarning("두 개의 선택된 타워가 같은 타일 위에 있습니다.");
            }
        }


        // 새로운 타워 소환 (SummonManager는 내부적으로 DB 데이터를 사용)
        GameObject newTower = SummonManager.Instance.SummonSpecificTower(positionForNewTower, newTypeEnum, newGradeEnum);
        if (newTower != null)
        {
            // 새 타워를 원래 첫 번째 타워가 있던 타일에 배치
            tile1.PlaceTower(newTower);
            Debug.Log($"✅ 합성 성공! [{newTower.name}] 새 타워 생성 완료");
        }
        else
        {
            Debug.LogError($"❌ 새 타워 소환 실패! ({newTypeEnum} / {newGradeEnum}) DB에 해당 정의가 있는지, 프리팹 경로가 올바른지 확인하세요.");
            // 중요: 합성 실패 시, 제거했던 타워들을 복구하거나 사용자에게 골드 등을 반환하는 로직이 필요할 수 있음
            // 간단하게는 합성을 취소하고 선택을 해제
        }

        ClearSelection();
        // Debug.Log($"✅ 합성 시도 완료. 결과는 로그를 확인하세요."); // 성공/실패 메시지는 SummonSpecificTower 결과에 따라 위에서 로깅
    }
}