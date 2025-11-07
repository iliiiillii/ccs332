using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;

    // [추가됨]: 맵 전체를 이동시킬 기준 오프셋 (Inspector에서 조정)
    [Header("Map Position Offset")]
    public Vector3Int mapOffset = new Vector3Int(0, 0, 0);

    // [추가됨]: 상호작용 프리팹 (TileScript와 Collider를 가진 투명 GO)
    [Header("Interaction Prefabs")]
    public GameObject tileInteractionPrefab;

    [Header("Tile Assets")]
    public TileBase pathTileAsset;
    public TileBase towerPlaceTileAsset;

    [Header("Tilemap Layers")]
    public Tilemap objectTilemapLayer;

    public Transform tileParent;

    [HideInInspector]
    public Transform monsterStartTileTransform;

    private Dictionary<Vector2Int, Vector3> tileWorldPositions = new Dictionary<Vector2Int, Vector3>();
    private Dictionary<Vector2Int, Transform> tileObjects = new Dictionary<Vector2Int, Transform>();

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

    private readonly int monsterSpawnTileX = 1;
    private readonly int monsterSpawnTileY = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GenerateMap()
    {
        if (tileParent != null)
        {
            Transform oldWaypoints = transform.Find("Waypoints_Temp");
            if (oldWaypoints != null) DestroyImmediate(oldWaypoints.gameObject);

            // 기존 상호작용 GO가 있을 경우 제거 (DestroyImmediate는 에디터 모드에서 사용)
            // Safety check: 모든 TileInteraction GameObject를 찾아서 제거 (선택적)
            TileScript[] oldInteractions = FindObjectsOfType<TileScript>();
            foreach (var ts in oldInteractions)
            {
                DestroyImmediate(ts.gameObject);
            }

            foreach (Transform child in tileParent)
                Destroy(child.gameObject);
        }

        if (objectTilemapLayer != null)
        {
            objectTilemapLayer.ClearAllTiles();
        }

        tileObjects.Clear();
        tileWorldPositions.Clear();
        monsterStartTileTransform = null;

        for (int y = 0; y < mapData.GetLength(0); y++)
        {
            for (int x = 0; x < mapData.GetLength(1); x++)
            {
                int tileValue = mapData[y, x];

                if (tileValue == 2 || tileValue == 3)
                {
                    // [수정됨]: mapOffset을 타일 위치에 더합니다.
                    Vector3Int tilePos = new Vector3Int(x, y, 0) + mapOffset;

                    TileBase tileToSet = (tileValue == 2) ? pathTileAsset : towerPlaceTileAsset;

                    if (objectTilemapLayer != null)
                    {
                        objectTilemapLayer.SetTile(tilePos, tileToSet);

                        // 몬스터 경로 관리를 위해 World Position 저장
                        Vector3 worldPos = objectTilemapLayer.GetCellCenterWorld(tilePos);
                        tileWorldPositions[new Vector2Int(tilePos.x, tilePos.y)] = worldPos;

                        // [핵심 추가]: 타워 지역(3)일 경우 상호작용 GO 생성
                        if (tileValue == 3 && tileInteractionPrefab != null)
                        {
                            GameObject interactionInstance = Instantiate(tileInteractionPrefab, worldPos, Quaternion.identity, transform);
                            interactionInstance.name = $"TileInteraction_{tilePos.x}_{tilePos.y}";

                            // TileScript 초기화 (TileType.TowerPlace는 Enum 값이 2임을 가정)
                            if (interactionInstance.TryGetComponent<TileScript>(out var tileScript))
                            {
                                // TileType enum을 직접 참조해야 하지만, 안전을 위해 숫자로 전달한다고 가정합니다.
                                // 실제 코드에서는 tileScript.Init(TileType.TowerPlace)를 사용해야 합니다.
                                tileScript.Init((TileType)2);
                            }
                        }
                    }
                }
            }
        }
    }

    public List<Transform> GetPathWaypoints()
    {
        List<Transform> waypoints = new List<Transform>();

        Vector2Int[] hardcodedPath = new Vector2Int[]
        {
            new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3), new Vector2Int(1, 4),
            new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(4, 4), new Vector2Int(5, 4),
            new Vector2Int(6, 4), new Vector2Int(7, 4), new Vector2Int(8, 4), new Vector2Int(9, 4), new Vector2Int(10, 4),
            new Vector2Int(10, 3), new Vector2Int(10, 2), new Vector2Int(10, 1),
            new Vector2Int(9, 1), new Vector2Int(8, 1), new Vector2Int(7, 1),
            new Vector2Int(7, 2), new Vector2Int(7, 3), new Vector2Int(7, 4), new Vector2Int(7, 5), new Vector2Int(7, 6),
            new Vector2Int(7, 7), new Vector2Int(7, 8), new Vector2Int(7, 9), new Vector2Int(7, 10),
            new Vector2Int(8, 10), new Vector2Int(9, 10), new Vector2Int(10, 10),
            new Vector2Int(10, 9), new Vector2Int(10, 8), new Vector2Int(10, 7),
            new Vector2Int(9, 7), new Vector2Int(8, 7), new Vector2Int(7, 7), new Vector2Int(6, 7), new Vector2Int(5, 7),
            new Vector2Int(4, 7), new Vector2Int(3, 7), new Vector2Int(2, 7), new Vector2Int(1, 7),
            new Vector2Int(1, 8), new Vector2Int(1, 9), new Vector2Int(1, 10),
            new Vector2Int(2, 10), new Vector2Int(3, 10), new Vector2Int(4, 10),
            new Vector2Int(4, 9), new Vector2Int(4, 8), new Vector2Int(4, 7), new Vector2Int(4, 6), new Vector2Int(4, 5),
            new Vector2Int(4, 4), new Vector2Int(4, 3), new Vector2Int(4, 2), new Vector2Int(4, 1),
            new Vector2Int(3, 1), new Vector2Int(2, 1), new Vector2Int(1, 1)
        };

        if (objectTilemapLayer == null)
        {
            Debug.LogError("ObjectTilemapLayer가 연결되지 않았습니다. 경로를 생성할 수 없습니다.");
            return waypoints;
        }

        // 임시 Waypoint 부모 오브젝트 생성 및 이전 오브젝트 제거
        Transform waypointParent = transform.Find("Waypoints_Temp");
        if (waypointParent != null) DestroyImmediate(waypointParent.gameObject);
        waypointParent = new GameObject("Waypoints_Temp").transform;
        waypointParent.SetParent(transform);


        foreach (var coord in hardcodedPath)
        {
            Vector2Int offsetCoord = new Vector2Int(coord.x, coord.y) + new Vector2Int(mapOffset.x, mapOffset.y);

            Vector3 pos;
            if (tileWorldPositions.TryGetValue(offsetCoord, out pos))
            {
                GameObject waypointGO = new GameObject($"Waypoint_{offsetCoord.x}_{offsetCoord.y}");

                waypointGO.transform.position = pos;
                waypointGO.transform.SetParent(waypointParent);

                waypoints.Add(waypointGO.transform);

                if (coord.x == monsterSpawnTileX && coord.y == monsterSpawnTileY)
                {
                    monsterStartTileTransform = waypointGO.transform;
                }
            }
            else
            {
                Debug.LogError($"GetPathWaypoints: 매핑된 타일이 없습니다! coord=({coord.x},{coord.y})");
            }
        }

        return waypoints;
    }
}