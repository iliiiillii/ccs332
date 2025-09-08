using System.Collections;
using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine;
using System.Linq; // Linq 사용을 위해 추가

public class MonsterSpawner : MonoBehaviour
{
    public static MonsterSpawner Instance;

    public float defaultSpawnInterval = 0.5f;

    private Transform startTile; // 몬스터 스폰 위치

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("MonsterSpawner: Start() 완료. InitializeSpawnPoint() 호출 대기 중.");
    }

    public void InitializeSpawnPoint()
    {
        if (MapGenerator.Instance != null && MapGenerator.Instance.monsterStartTileTransform != null)
        {
            startTile = MapGenerator.Instance.monsterStartTileTransform;
            Debug.Log($"MonsterSpawner: 시작 타일 '{startTile.name}' (MapGenerator로부터 참조 받음) 설정 완료. 위치: {startTile.position}");
        }
        else
        {
            Debug.LogError("MonsterSpawner: MapGenerator로부터 시작 타일 참조를 가져올 수 없습니다!");
            if (MapGenerator.Instance == null) Debug.LogError(" - MapGenerator.Instance is null");
            else if (MapGenerator.Instance.monsterStartTileTransform == null) Debug.LogError(" - MapGenerator.Instance.monsterStartTileTransform is null");
        }
    }

    public void StartWave(int waveNumber)
    {
        Debug.Log($"MonsterSpawner: StartWave({waveNumber}) 호출됨.");
        if (startTile == null)
        {
            Debug.LogError("MonsterSpawner: 시작 타일이 설정되지 않아 웨이브를 시작할 수 없습니다.");
            if (GameManager.Instance != null) GameManager.Instance.OnWaveEnd();
            return;
        }

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("MonsterSpawner: DatabaseManager 인스턴스를 찾을 수 없습니다!");
            if (GameManager.Instance != null) GameManager.Instance.OnWaveEnd();
            return;
        }
        StartCoroutine(SpawnWaveCoroutine(waveNumber));
    }

    private IEnumerator SpawnWaveCoroutine(int waveNumber)
    {
        if (GameManager.Instance != null) GameManager.Instance.currentWave = waveNumber;
        Debug.Log($"MonsterSpawner: [Wave {waveNumber}] SpawnWaveCoroutine 시작!");

        if (DatabaseManager.Instance == null || DatabaseManager.Instance.waveDefinitionList == null || DatabaseManager.Instance.monsterDataList == null)
        {
            Debug.LogError("MonsterSpawner: DatabaseManager 또는 내부 리스트가 null입니다!");
            if (GameManager.Instance != null) GameManager.Instance.OnWaveEnd();
            yield break;
        }

        List<WaveDefinitionRecord> waveCompositions = DatabaseManager.Instance.GetWaveDefinitionsByWaveNumber(waveNumber);

        if (waveCompositions == null || waveCompositions.Count == 0)
        {
            Debug.LogWarning($"MonsterSpawner: Wave {waveNumber}에 대한 몬스터 구성 정보가 DB에 없습니다. OnWaveEnd() 호출.");
            if (GameManager.Instance != null) GameManager.Instance.OnWaveEnd();
            yield break;
        }

        Debug.Log($"MonsterSpawner: Wave {waveNumber} - {waveCompositions.Count} 종류의 몬스터 구성 로드됨.");

        // <<< 경로 정보는 웨이브 시작 시 한 번만 가져옴 >>>
        List<Transform> pathWaypointsForThisWave = null;
        if (MapGenerator.Instance != null)
        {
            Debug.Log($"MonsterSpawner: Wave {waveNumber}의 경로 설정을 위해 MapGenerator.GetPathWaypoints() 호출.");
            pathWaypointsForThisWave = MapGenerator.Instance.GetPathWaypoints();
            if (pathWaypointsForThisWave == null || pathWaypointsForThisWave.Count == 0)
            {
                Debug.LogError($"MonsterSpawner: MapGenerator로부터 Wave {waveNumber}의 경로를 가져오지 못했거나 경로가 비어있습니다. 웨이브 진행 불가.");
                if (GameManager.Instance != null) GameManager.Instance.OnWaveEnd();
                yield break; // 경로 없으면 웨이브 진행 불가
            }
            Debug.Log($"MonsterSpawner: MapGenerator로부터 {pathWaypointsForThisWave.Count}개의 웨이포인트 받음 (Wave {waveNumber}).");
        }
        else
        {
            Debug.LogError("MonsterSpawner: MapGenerator.Instance가 null이라 경로를 설정할 수 없습니다. 웨이브 진행 불가.");
            if (GameManager.Instance != null) GameManager.Instance.OnWaveEnd();
            yield break; // 경로 없으면 웨이브 진행 불가
        }
        // <<< 여기까지 경로 정보 가져오기 >>>


        for (int waveCompIndex = 0; waveCompIndex < waveCompositions.Count; waveCompIndex++)
        {
            WaveDefinitionRecord waveDef = waveCompositions[waveCompIndex];
            MonsterDataRecord monsterBaseData = DatabaseManager.Instance.monsterDataList.FirstOrDefault(m => m.id == waveDef.monsterDataId);

            if (monsterBaseData == null) { /* ... (오류 처리) ... */ Debug.LogError($"MonsterSpawner: DB에 ID {waveDef.monsterDataId} 몬스터 없음."); continue; }
            GameObject monsterPrefab = Resources.Load<GameObject>(monsterBaseData.prefabPath);
            if (monsterPrefab == null) { /* ... (오류 처리) ... */ Debug.LogError($"MonsterSpawner: 프리팹 로드 실패 {monsterBaseData.prefabPath}."); continue; }

            for (int i = 0; i < waveDef.quantity; i++)
            {
                if (startTile == null) { /* ... (오류 처리) ... */ Debug.LogError("MonsterSpawner: 시작 타일 null (스폰 루프 내부)."); yield break; }
                GameObject monsterObj = Instantiate(monsterPrefab, startTile.position, Quaternion.identity);
                if (GameManager.Instance != null) GameManager.Instance.MonsterSpawned();
                // Debug.Log($"MonsterSpawner: Wave {waveNumber} - '{monsterBaseData.monsterName}' #{i + 1} 스폰됨.");

                if (monsterBaseData.isBoss)
                {
                    BossMonsterScript bossScript = monsterObj.GetComponent<BossMonsterScript>();
                    if (bossScript != null) bossScript.InitializeFromDB(monsterBaseData, waveDef.hpMultiplier, waveDef.goldMultiplier);
                    else Debug.LogError($"'{monsterBaseData.monsterName}' 프리팹에 BossMonsterScript가 없습니다!");
                }
                else
                {
                    MonsterScript monsterScript = monsterObj.GetComponent<MonsterScript>();
                    if (monsterScript != null) monsterScript.InitializeFromDB(monsterBaseData, waveDef.hpMultiplier, waveDef.goldMultiplier);
                    else Debug.LogError($"'{monsterBaseData.monsterName}' 프리팹에 MonsterScript가 없습니다!");
                }

                MonsterMovement movement = monsterObj.GetComponent<MonsterMovement>();
                if (movement != null)
                {
                    movement.moveSpeed = monsterBaseData.baseSpeed * waveDef.speedMultiplier;
                    // <<< 수정된 부분: 가져온 경로 정보 전달 >>>
                    movement.InitializePath(pathWaypointsForThisWave);
                }
                else
                {
                    Debug.LogError($"'{monsterBaseData.monsterName}' 프리팹에 MonsterMovement 컴포넌트가 없습니다!");
                }

                float interval = waveDef.spawnInterval > 0 ? waveDef.spawnInterval : defaultSpawnInterval;
                yield return new WaitForSeconds(interval);
            }
        }

        Debug.Log($"MonsterSpawner: Wave {waveNumber} 모든 몬스터 스폰 루프 완료. GameManager.OnWaveEnd() 호출 시도.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveEnd();
            Debug.Log("MonsterSpawner: GameManager.OnWaveEnd() 호출 완료.");
        }
        else
        {
            Debug.LogError("MonsterSpawner: GameManager.Instance가 null이라 OnWaveEnd() 호출 실패!");
        }
    }
}