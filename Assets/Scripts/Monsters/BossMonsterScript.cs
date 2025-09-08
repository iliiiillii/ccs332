using UnityEngine;
using System.Collections; // 필요에 따라

public class BossMonsterScript : MonoBehaviour
{
    private MonsterDataRecord dbData;
    public MonsterDataRecord DbData { get { return dbData; } }

    public float currentHp;
    private float initialHp;
    private int rewardGold;

    [Header("Effects")]
    public GameObject hitEffectPrefab;

    // <<< 추가: 방향별 스프라이트 변수 >>>
    [Header("Directional Sprites")]
    public Sprite frontSprite;  // 보스의 정면 이미지
    public Sprite backSprite;   // 보스의 뒷면 이미지
    public Sprite sideSprite;   // 보스의 옆면 이미지 (기본적으로 오른쪽을 본다고 가정)

    // <<< 추가: 스프라이트 렌더러 참조 >>>
    private SpriteRenderer spriteRenderer;

    public static event System.Action OnBossDeath;

    // <<< 추가: Awake 함수 >>>
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] Boss에게 SpriteRenderer 컴포넌트가 없습니다!");
        }
    }

    public void InitializeFromDB(MonsterDataRecord baseStats, float currentWaveHpMultiplier, float currentWaveGoldMultiplier)
    {
        dbData = baseStats;

        if (dbData == null)
        {
            Debug.LogError($"{gameObject.name}: DB로부터 보스 몬스터 데이터를 받지 못했습니다! 기본값 사용.");
            initialHp = 500f;
            currentHp = initialHp;
            rewardGold = 100;
            gameObject.name = "DefaultBossMonster";
            return;
        }

        initialHp = dbData.baseHp * currentWaveHpMultiplier;
        currentHp = initialHp;
        rewardGold = (int)(dbData.rewardGold * currentWaveGoldMultiplier);
        gameObject.name = dbData.monsterName;

        Debug.Log($"보스 몬스터 초기화 (DB): [{dbData.monsterName}] HP: {currentHp}, Gold: {rewardGold}");
    }

    private void Update()
    {
        // TODO: 보스 이동 로직 (MonsterMovement 사용 또는 자체 구현)
        // TODO: 보스 공격 패턴 로직
    }

    public void TakeDamage(float dmg)
    {
        if (currentHp <= 0) return;

        currentHp -= dmg;
        Debug.Log($"{name} (보스) 피해 받음! 남은 체력: {currentHp}");

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} (보스) 사망!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(rewardGold);
            GameManager.Instance.MonsterKilled(this.dbData);
        }

        OnBossDeath?.Invoke();
        Destroy(gameObject);
    }

    // <<< 추가: 방향에 따른 스프라이트 업데이트 함수 >>>
    public void UpdateSpriteDirection(Vector2 direction)
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[{gameObject.name}] SpriteRenderer가 없습니다! 방향 전환 불가.");
            return;
        }

        // 방향 스프라이트들이 모두 할당되었는지 확인 (선택적이지만 안전함)
        if (frontSprite == null || backSprite == null || sideSprite == null)
        {
            // Debug.LogWarning($"[{gameObject.name}] Boss의 방향별 스프라이트가 모두 할당되지 않았습니다.");
            // 스프라이트가 없으면 기본 flipX만이라도 작동하도록 할 수 있음
            if (direction.x > 0.01f) spriteRenderer.flipX = false;
            else if (direction.x < -0.01f) spriteRenderer.flipX = true;
            return;
        }

        // 상하 이동이 우선인지, 좌우 이동이 우선인지에 따라 스프라이트 변경
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            if (direction.y > 0) // 위로 이동 -> 뒷모습
            {
                spriteRenderer.sprite = backSprite;
                spriteRenderer.flipX = false;
            }
            else // 아래로 이동 -> 앞모습
            {
                spriteRenderer.sprite = frontSprite;
                spriteRenderer.flipX = false;
            }
        }
        else // 좌우 이동이 더 크거나 같을 때
        {
            spriteRenderer.sprite = sideSprite;
            if (direction.x > 0) // 오른쪽으로 이동
            {
                spriteRenderer.flipX = false; // sideSprite가 오른쪽을 보고 있다고 가정
            }
            else // 왼쪽으로 이동
            {
                spriteRenderer.flipX = true; // sideSprite를 좌우 반전
            }
        }
    }

    // TODO: 보스 몬스터만의 특수 스킬 함수들 추가 (예: 광역 공격, 부하 소환 등)
}