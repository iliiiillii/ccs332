using UnityEngine;
using UnityEngine.Tilemaps;

// [CreateAssetMenu]를 사용하여 유니티 Create 메뉴에 항목을 추가합니다.
[CreateAssetMenu(fileName = "Simple Tile", menuName = "2D/Tiles/Simple Tile")]
public class SimpleTileAsset : TileBase
{
    // Inspector에서 연결할 스프라이트 변수
    public Sprite tileSprite;

    // 타일 데이터가 요청될 때 호출되는 필수 오버라이드 함수
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // 1. 스프라이트 설정 (가장 중요)
        tileData.sprite = tileSprite;

        // 2. 기타 설정 (충돌체, 색상 등)
        tileData.colliderType = Tile.ColliderType.None; // 충돌체를 사용하지 않음 (배경 타일이므로)
        tileData.color = Color.white; // 기본 색상
    }

    // TileBase를 상속받을 때 구현해야 하는 나머지 필수 메서드들 (빈 함수로 구현)
    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        return true;
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        tilemap.RefreshTile(position);
    }
}