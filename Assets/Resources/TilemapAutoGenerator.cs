using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapAutoGenerator : MonoBehaviour
{
    // Rule Tile (잔디, 경계가 필요한 타일)
    public RuleTile groundTile;

    // 기본 채움 타일 (돌/벽, Rule Tile이 없는 영역을 채움)
    public TileBase defaultFillTile;

    // 맵의 크기 설정
    public int mapWidth = 50;
    public int mapHeight = 50;

    // 맵이 생성될 시작 위치 (타일맵 좌표계 기준)
    public Vector3Int startPosition = new Vector3Int(0, 0, 0);

    // Perlin Noise 관련 변수는 이제 사용하지 않습니다.
    public float scale = 10f;
    public float threshold = 0.5f;

    private Tilemap tilemap;
    private float offsetX;
    private float offsetY;

    void Start()
    {
        tilemap = GetComponent<Tilemap>();

        // Perlin Noise 초기화 코드는 이제 필요 없지만, 에러 방지를 위해 유지
        offsetX = Random.Range(0f, 99999f);
        offsetY = Random.Range(0f, 99999f);

        GenerateMap();
    }

    [ContextMenu("Regenerate Map")]
    void GenerateMap()
    {
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
            if (tilemap == null) return;
        }
        tilemap.ClearAllTiles();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int localTilePosition = new Vector3Int(x, y, 0);
                Vector3Int finalPosition = localTilePosition + startPosition;

                // 1. 맵의 외곽 테두리 1줄은 무조건 Default Fill Tile로 강제 채우기
                if (x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1)
                {
                    tilemap.SetTile(finalPosition, defaultFillTile);
                }
                // 2. 맵 내부 영역 (외곽 테두리 제외)
                else
                {
                    // <--- 수정된 핵심: 내부 전체를 Rule Tile (잔디)로 채웁니다.
                    tilemap.SetTile(finalPosition, groundTile);
                }
            }
        }
    }

    // Perlin Noise 함수는 사용되지 않지만 코드 완성도를 위해 유지
    float CalculatePerlinNoise(int x, int y)
    {
        float xCoord = (float)x / mapWidth * scale + offsetX;
        float yCoord = (float)y / mapHeight * scale + offsetY;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}