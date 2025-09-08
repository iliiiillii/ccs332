using UnityEngine;
// using System.Collections; // 현재 코드에서 사용되지 않음
// using System.Collections.Generic; // 현재 코드에서 사용되지 않음

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))] // 타워 설치 공간 및 클릭 감지를 위해 유지
public class TileScript : MonoBehaviour
{
    public static TileScript selectedTile = null;

    public TileType tileType; // MapGenerator가 Init()을 통해 설정
    public bool isOccupied = false;
    public GameObject placedTower; // 이 타일에 배치된 타워의 참조

    // 각 프리팹이 이미 고유한 스프라이트를 가지고 있으므로, TileScript에서 스프라이트를 직접 바꿀 필요는 없음.
    // 하지만, 타입에 따라 추가적인 시각적 조정(예: 미세한 색조 변경, 특정 효과 켜고 끄기)이 필요하다면 Init에서 처리 가능.
    public void Init(TileType type)
    {
        this.tileType = type;
        // var sr = GetComponent<SpriteRenderer>(); // SpriteRenderer는 Awake에서 이미 설정했거나, 각 프리팹에 이미 설정됨

        // 각 프리팹이 이미 고유한 스프라이트를 가지고 있으므로, 아래 색상 변경 로직은 제거하거나 주석 처리합니다.
        // 만약 각 프리팹의 스프라이트가 기본 흰색이고, 여기서 색상을 입히고 싶다면 남겨둘 수 있습니다.
        /*
        switch (tileType)
        {
            case TileType.Background:
                sr.color = Color.gray; // 또는 backgroundPrefab 자체가 회색 스프라이트를 가짐
                break;
            case TileType.Path:
                sr.color = Color.white; // 또는 pathPrefab 자체가 흰색(또는 길 모양) 스프라이트를 가짐
                break;
            case TileType.TowerPlace:
                sr.color = Color.yellow; // 또는 towerPlacePrefab 자체가 노란색 스프라이트를 가짐
                break;
        }
        */
        // Debug.Log($"{gameObject.name} initialized as {tileType}");
    }

    private void OnMouseDown()
    {
        Debug.Log($"✅ 타일 클릭됨: {gameObject.name}, 타입: {tileType}");
        if (tileType == TileType.TowerPlace)
        {
            if (!isOccupied)
            {
                // 이전에 선택된 타일이 있다면 선택 해제 효과 (선택적)
                if (selectedTile != null && selectedTile != this)
                {
                    // selectedTile.DeselectEffect(); // DeselectEffect 같은 함수가 있다면
                }
                selectedTile = this;
                // 선택 효과 (선택적)
                // SelectEffect(); 

                if (UIManager.Instance != null) UIManager.Instance.ShowSummonButton(true);
            }
            else // 이미 타워가 있는 타일 클릭 시
            {
                // 여기에 현재 배치된 타워를 선택하거나 정보를 보여주는 로직 추가 가능
                // 예: if (placedTower != null) UpgradeManager.Instance.SelectTower(placedTower.GetComponent<TowerScript>());
                // 현재는 타워 자체의 OnMouseDown에서 UpgradeManager.SelectTower를 호출하므로,
                // 여기서는 특별히 할 일이 없거나, 소환 버튼을 끄는 정도로만.
                selectedTile = null; // 다른 타워 설치를 위해 기존 선택 해제
                if (UIManager.Instance != null) UIManager.Instance.ShowSummonButton(false);
                if (UpgradeManager.Instance != null && placedTower != null) // 타워가 있다면 해당 타워 선택 시도
                {
                    TowerScript towerOnThisTile = placedTower.GetComponent<TowerScript>();
                    if (towerOnThisTile != null)
                    {
                        // UpgradeManager가 선택/해제를 관리하도록 위임
                        // 클릭된 타워의 타워를 선택하거나, 이미 선택된 타워면 해제하는 로직 필요
                        // 지금은 타워의 OnMouseDown에서 처리되므로, 여기서는 UI만 닫음
                    }
                }
            }
        }
        else // 타워 설치 불가능한 타일 클릭 시
        {
            if (selectedTile != null)
            {
                // selectedTile.DeselectEffect();
            }
            selectedTile = null;
            if (UIManager.Instance != null) UIManager.Instance.ShowSummonButton(false);
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.ClearSelection();
        }
    }

    public void PlaceTower(GameObject towerInstance)
    {
        if (isOccupied)
        {
            Debug.LogWarning("여기는 이미 타워가 설치된 타일입니다.");
            Destroy(towerInstance); // 새로 소환된 타워 파괴
            return;
        }

        if (towerInstance != null)
        {
            placedTower = towerInstance;
            isOccupied = true;
            // selectedTile = null; // 타워 설치 후에는 이 타일이 더 이상 "소환을 위해 선택된 타일"은 아님
            if (UIManager.Instance != null) UIManager.Instance.ShowSummonButton(false); // 소환 버튼 숨김
        }
        else
        {
            Debug.LogWarning("PlaceTower 호출 시 타워 인스턴스가 null입니다.");
        }
    }

    public void RemoveTower()
    {
        // placedTower의 Destroy는 UpgradeManager 또는 다른 곳에서 처리
        isOccupied = false;
        placedTower = null;
        Debug.Log($"{gameObject.name}에서 타워 정보 제거됨 (isOccupied = false)");
    }
}