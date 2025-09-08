using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;

    [Header("Tile Prefabs")]
    public GameObject wallTilePrefab;       // '1'번 (벽)
    public GameObject pathTilePrefab;       // '2'번 (경로)
    public GameObject towerPlaceTilePrefab; // '3'번 (타워 건설 가능)

    public Transform tileParent;

    [HideInInspector]
    public Transform monsterStartTileTransform;

    private Dictionary<Vector2Int, Transform> tileObjects = new Dictionary<Vector2Int, Transform>();

    // <<< 12x12 크기의 맵 데이터 >>>
    // 1: 벽, 2: 몬스터 경로, 3: 타워 건설 가능
    private int[,] mapData = new int[12, 12]
    {
        {1,1,1,1,1,1,1,1,1,1,1,1},
        {1,2,2,2,2,3,3,2,2,2,2,1},
        {1,2,3,3,2,3,3,2,3,3,2,1},
        {1,2,3,3,2,3,3,2,3,3,2,1},
        {1,2,2,2,2,2,2,2,2,2,2,1},
        {1,3,3,3,2,3,3,2,3,3,3,1},
        {1,3,3,3,2,3,3,2,3,3,3,1},
        {1,2,2,2,2,2,2,2,2,2,2,1},
        {1,2,3,3,2,3,3,2,3,3,2,1},
        {1,2,3,3,2,3,3,2,3,3,2,1},
        {1,2,2,2,2,3,3,2,2,2,2,1},
        {1,1,1,1,1,1,1,1,1,1,1,1}
    };

    // <<< 몬스터 시작 타일 좌표 >>>
    private readonly int monsterSpawnTileX = 1;
    private readonly int monsterSpawnTileY = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GenerateMap()
    {
        // (1) 기존 생성된 타일 모두 삭제
        if (tileParent != null)
        {
            foreach (Transform child in tileParent)
                Destroy(child.gameObject);
        }
        tileObjects.Clear();
        monsterStartTileTransform = null;

        Debug.Log("MapGenerator: 맵 생성 시작");

        // (2) mapData에 따라서 타일 Instantiate
        for (int y = 0; y < mapData.GetLength(0); y++)
        {
            for (int x = 0; x < mapData.GetLength(1); x++)
            {
                Vector3 position = new Vector3(x, -y, 0);
                GameObject selectedPrefab = null;
                int tileValue = mapData[y, x];

                switch (tileValue)
                {
                    case 1:
                        selectedPrefab = wallTilePrefab;
                        break;
                    case 2:
                        selectedPrefab = pathTilePrefab;
                        break;
                    case 3:
                        selectedPrefab = towerPlaceTilePrefab;
                        break;
                    default:
                        Debug.LogError($"MapGenerator: 알 수 없는 타일 값: {tileValue} at ({x},{y})");
                        break;
                }

                if (selectedPrefab != null)
                {
                    GameObject tileInstance = Instantiate(selectedPrefab, position, Quaternion.identity, tileParent);
                    tileInstance.name = $"Tile_{x}_{y}";
                    tileObjects[new Vector2Int(x, y)] = tileInstance.transform;

                    if (x == monsterSpawnTileX && y == monsterSpawnTileY)
                    {
                        monsterStartTileTransform = tileInstance.transform;
                        Debug.Log($"MapGenerator: 몬스터 시작 타일 ({x},{y}) 저장됨");
                    }
                }
                else
                {
                    Debug.LogError($"MapGenerator: 타입 {tileValue}에 대한 Prefab이 없습니다.");
                }
            }
        }

        Debug.Log("MapGenerator: 맵 생성 완료");

        if (monsterStartTileTransform == null)
            Debug.LogError($"MapGenerator: 몬스터 시작 타일({monsterSpawnTileX},{monsterSpawnTileY})을 찾을 수 없습니다.");
    }

    /// <summary>
    /// 모든 '2' 타일을 다음 순서대로 하드코딩하여 반환합니다:
    /// (1,1) → (1,2) → (1,3) → (1,4)
    /// → (2,4) → (3,4) → (4,4) → (5,4) → (6,4) → (7,4) → (8,4) → (9,4) → (10,4)
    /// → (10,3) → (10,2) → (10,1)
    /// → (9,1) → (8,1) → (7,1)
    /// → (7,2) → (7,3) → (7,4) → (7,5) → (7,6) → (7,7) → (7,8) → (7,9) → (7,10)
    /// → (8,10) → (9,10) → (10,10)
    /// → (10,9) → (10,8) → (10,7)
    /// → (9,7) → (8,7) → (7,7) → (6,7) → (5,7) → (4,7) → (3,7) → (2,7) → (1,7)
    /// → (1,8) → (1,9) → (1,10)
    /// → (2,10) → (3,10) → (4,10)
    /// → (4,9) → (4,8) → (4,7) → (4,6) → (4,5) → (4,4) → (4,3) → (4,2) → (4,1)
    /// → (3,1) → (2,1) → (1,1)
    /// </summary>
    public List<Transform> GetPathWaypoints()
    {
        List<Transform> waypoints = new List<Transform>();

        Vector2Int[] hardcodedPath = new Vector2Int[]
        {
            // (1,1) → (1,4)
            new Vector2Int(1, 1),
            new Vector2Int(1, 2),
            new Vector2Int(1, 3),
            new Vector2Int(1, 4),

            // (1,4) → (10,4)
            new Vector2Int(2, 4),
            new Vector2Int(3, 4),
            new Vector2Int(4, 4),
            new Vector2Int(5, 4),
            new Vector2Int(6, 4),
            new Vector2Int(7, 4),
            new Vector2Int(8, 4),
            new Vector2Int(9, 4),
            new Vector2Int(10, 4),

            // (10,4) → (10,1)
            new Vector2Int(10, 3),
            new Vector2Int(10, 2),
            new Vector2Int(10, 1),

            // (10,1) → (7,1)
            new Vector2Int(9, 1),
            new Vector2Int(8, 1),
            new Vector2Int(7, 1),

            // (7,1) → (7,10)
            new Vector2Int(7, 2),
            new Vector2Int(7, 3),
            new Vector2Int(7, 4),
            new Vector2Int(7, 5),
            new Vector2Int(7, 6),
            new Vector2Int(7, 7),
            new Vector2Int(7, 8),
            new Vector2Int(7, 9),
            new Vector2Int(7, 10),

            // (7,10) → (10,10)
            new Vector2Int(8, 10),
            new Vector2Int(9, 10),
            new Vector2Int(10, 10),

            // (10,10) → (10,7)
            new Vector2Int(10, 9),
            new Vector2Int(10, 8),
            new Vector2Int(10, 7),

            // (10,7) → (1,7)
            new Vector2Int(9, 7),
            new Vector2Int(8, 7),
            new Vector2Int(7, 7),
            new Vector2Int(6, 7),
            new Vector2Int(5, 7),
            new Vector2Int(4, 7),
            new Vector2Int(3, 7),
            new Vector2Int(2, 7),
            new Vector2Int(1, 7),

            // (1,7) → (1,10)
            new Vector2Int(1, 8),
            new Vector2Int(1, 9),
            new Vector2Int(1, 10),

            // (1,10) → (4,10)
            new Vector2Int(2, 10),
            new Vector2Int(3, 10),
            new Vector2Int(4, 10),

            // (4,10) → (4,1)
            new Vector2Int(4, 9),
            new Vector2Int(4, 8),
            new Vector2Int(4, 7),
            new Vector2Int(4, 6),
            new Vector2Int(4, 5),
            new Vector2Int(4, 4),
            new Vector2Int(4, 3),
            new Vector2Int(4, 2),
            new Vector2Int(4, 1),

            // (4,1) → (1,1)
            new Vector2Int(3, 1),
            new Vector2Int(2, 1),
            new Vector2Int(1, 1)
        };

        foreach (var coord in hardcodedPath)
        {
            if (tileObjects.TryGetValue(coord, out Transform t))
                waypoints.Add(t);
            else
                Debug.LogError($"GetPathWaypoints: 매핑된 타일이 없습니다! coord=({coord.x},{coord.y})");
        }

        return waypoints;
    }
}
