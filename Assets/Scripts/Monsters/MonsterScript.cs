using UnityEngine;
using System.Collections;
using System;

public class MonsterScript : MonoBehaviour
{
    public static event Action OnMonsterDeath;

    private MonsterDataRecord dbData;
    public MonsterDataRecord DbData { get { return dbData; } }

    public float currentHp;
    private float initialHp;
    private int rewardGold;
    private MonsterMovement movement;

    // MonsterScript.cs 파일의 클래스 선언 바로 아래, 다른 변수들 있는 곳에 추가
    [Header("Directional Sprites")] // Inspector에서 보기 좋게 그룹 이름 설정
    public Sprite frontSprite;  // 몬스터 정면 이미지 할당용
    public Sprite backSprite;   // 몬스터 뒷면 이미지 할당용
    public Sprite sideSprite;   // 몬스터 옆면 이미지 할당용 (기본적으로 오른쪽을 보고 있는 스프라이트로 준비하시면 flipX로 왼쪽도 표현 가능)

    [Header("Effects")]
    public GameObject hitEffectPrefab;

    [Header("Slow Effect Prefabs")]
    [SerializeField] private GameObject slowAuraPrefab;
    [SerializeField] private GameObject frostDustPrefab;
    // <<< 추가: 스프라이트 렌더러 참조 >>>
    private SpriteRenderer spriteRenderer; // 몬스터의 스프라이트를 제어하기 위함

    // Start 또는 Awake에서 SpriteRenderer 컴포넌트를 가져옵니다.
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponent<MonsterMovement>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] SpriteRenderer 컴포넌트를 찾을 수 없습니다!");
        }
    }

    public void InitializeFromDB(MonsterDataRecord baseStats, float currentWaveHpMultiplier, float currentWaveGoldMultiplier)
    {
        dbData = baseStats;

        if (dbData == null)
        {
            Debug.LogError($"{gameObject.name}: DB로부터 몬스터 데이터를 받지 못했습니다! 기본값 사용.");
            initialHp = 60f;
            currentHp = initialHp;
            rewardGold = 5;
            gameObject.name = "DefaultMonster";
            return;
        }

        initialHp = dbData.baseHp * currentWaveHpMultiplier;
        currentHp = initialHp;
        rewardGold = (int)(dbData.rewardGold * currentWaveGoldMultiplier);
        gameObject.name = dbData.monsterName;

        Debug.Log($"몬스터 초기화 (DB): [{dbData.monsterName}] HP: {currentHp}, Gold: {rewardGold}");
    }

    public void SetHp(float newMaxHp)
    {
        initialHp = newMaxHp;
        currentHp = newMaxHp;
    }

    public void ApplySlowEffect(float slowAmount, float duration)
    {
        Debug.Log($"{name} 슬로우 효과 적용! {slowAmount * 100}% 감소, {duration}초 동안");

        // <<< 수정: 실제 슬로우 로직 호출 >>>
        if (movement != null)
        {
            movement.ApplySlow(slowAmount, duration);
        }
        else
        {
            Debug.LogWarning($"[{name}] MonsterMovement 스크립트가 없어서 슬로우 효과를 적용할 수 없습니다.");
        }
    }

    public void ApplyPoisonEffect(float damagePerTick, float duration)
    {
        Debug.Log($"{name} 독 효과 적용! {duration}초 동안 초당 {damagePerTick} 피해");
        // 실제 독 데미지 로직 구현 필요 (코루틴 사용 등)
    }

    public void ApplyFreezeEffect(float duration)
    {
        Debug.Log($"{name} 얼림 효과 적용! {duration}초 동안 스턴");
        // 실제 이동 정지 로직 구현 필요 (MonsterMovement의 이동 중단 등)
    }

    private IEnumerator SlowEffectCoroutine(float duration)
    {
        // 1) 오라 이펙트 붙이기
        var aura = Instantiate(slowAuraPrefab, transform.position, Quaternion.identity, transform);
        var psAura = aura.GetComponent<ParticleSystem>();
        if (psAura != null) psAura.Play();

        // 2) 잔설 이펙트 붙이기
        var dust = Instantiate(frostDustPrefab, transform.position, Quaternion.identity, transform);
        var psDust = dust.GetComponent<ParticleSystem>();
        if (psDust != null) psDust.Play();

        // 3) duration 만큼 대기
        yield return new WaitForSeconds(duration);

        // 4) 이펙트 제거
        Destroy(aura);
        Destroy(dust);

        // TODO: MonsterMovement 쪽에서 속도 복원 로직 호출
    }

    public void TakeDamage(float dmg, TowerType attackType = TowerType.Normal) // 기본값은 Normal로 설정
    {
        if (currentHp <= 0) return;

        currentHp -= dmg;
        Debug.Log($"{name}이(가) {attackType} 타입 공격으로 {dmg} 피해 받음! 남은 체력: {currentHp}");

        // <<< 여기에 공격 타입에 따라 다른 이펙트를 생성하는 로직 추가 >>>
        if (attackType == TowerType.Slow)
        {
            // 슬로우 공격을 받았을 때의 피격 이펙트 생성 로직
            // 예: 얼음 파편이 튀는 이펙트 등
            // 만약 슬로우 효과 자체가 몬스터 몸에 붙어있는 오라 형태라면,
            // 이펙트 생성은 TakeDamage가 아닌 ApplySlowEffect에서 처리하는 것이 더 적절할 수 있습니다.
            // 현재는 '피격' 순간의 이펙트만 처리한다고 가정합니다.
            Debug.Log("슬로우 타입 피격 이펙트 생성!");
            if (frostDustPrefab != null) Instantiate(frostDustPrefab);
        }
        else // 슬로우가 아닌 다른 모든 공격
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] 일반 피격 이펙트(hitEffectPrefab)가 할당되지 않았습니다!");
            }
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} 사망!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(rewardGold);
            GameManager.Instance.MonsterKilled(this.dbData);
        }

        OnMonsterDeath?.Invoke();
        Destroy(gameObject);
    }

    // <<< 여기에 UpdateSpriteDirection 함수 추가! >>>
    /// <summary>
    /// 몬스터의 이동 방향에 따라 스프라이트 방향을 업데이트합니다.
    /// </summary>
    /// <param name="direction">몬스터가 이동하는 방향 (예: Vector2)</param>
    public void UpdateSpriteDirection(Vector2 direction)
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[{gameObject.name}] SpriteRenderer가 없습니다! Awake에서 초기화되었는지 확인해주세요.");
            return;
        }

        // 방향 스프라이트들이 Inspector에서 모두 할당되었는지 확인 (안전을 위해)
        if (frontSprite == null || backSprite == null || sideSprite == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Inspector에서 frontSprite, backSprite, sideSprite 중 하나 이상이 할당되지 않았습니다! 스프라이트 변경이 제대로 작동하지 않을 수 있습니다.");
            // 이 경우, 기본적인 flipX 로직만 수행하거나, 아예 아무것도 안 할 수도 있습니다.
            // 여기서는 일단 기본적인 flipX만 남겨두고 함수를 종료하거나, 또는 기본 스프라이트를 표시할 수 있습니다.
            // 아래는 예시로, 기존 flipX 로직만 수행하고 넘어가는 코드입니다. (이 부분은 게임 디자인에 맞게 수정하세요)
            if (direction.x > 0.01f) { spriteRenderer.flipX = false; }
            else if (direction.x < -0.01f) { spriteRenderer.flipX = true; }
            return; // 중요한 스프라이트가 없으면 더 이상 진행하지 않음
        }

        // Y축(상하) 움직임의 절대값이 X축(좌우) 움직임의 절대값보다 클 때 (상하 이동을 우선으로 침)
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            if (direction.y > 0.01f) // 위로 이동 (화면 위쪽, 보통 캐릭터의 뒷모습)
            {
                spriteRenderer.sprite = backSprite;
                spriteRenderer.flipX = false; // 뒷모습은 보통 좌우반전이 필요 없습니다.
            }
            else if (direction.y < -0.01f) // 아래로 이동 (화면 아래쪽, 보통 캐릭터의 앞모습)
            {
                spriteRenderer.sprite = frontSprite;
                spriteRenderer.flipX = false; // 앞모습도 보통 좌우반전이 필요 없습니다.
            }
            // 만약 상하 이동 중에도 좌우를 약간 틀고 싶다면 여기서 flipX를 추가로 고려할 수 있지만, 보통은 고정.
        }
        // X축(좌우) 움직임의 절대값이 Y축 움직임의 절대값보다 크거나 같을 때 (좌우 이동을 우선으로 침, 또는 대각선 이동 시 옆모습으로 처리)
        else if (Mathf.Abs(direction.x) > 0.01f) // 좌우로 이동 중일 때
        {
            spriteRenderer.sprite = sideSprite; // 옆모습 스프라이트로 변경
            if (direction.x > 0.01f) // 오른쪽으로 이동
            {
                spriteRenderer.flipX = false; // sideSprite가 기본적으로 오른쪽을 보고 있다고 가정
            }
            else // 왼쪽으로 이동 (direction.x < -0.01f)
            {
                spriteRenderer.flipX = true;  // sideSprite를 좌우반전하여 왼쪽을 보게 함
            }
        }
        // else: 움직임이 거의 없을 때 (제자리). 이 때는 어떤 모습을 보여줄지 결정합니다.
        // 예를 들어, 마지막 움직였던 방향을 유지하거나, 기본적으로 정면(frontSprite)을 보도록 할 수 있습니다.
        // 아래는 예시로, 움직임이 없으면 정면을 보도록 하는 코드입니다. (선택 사항)
        /*
        else 
        {
            spriteRenderer.sprite = frontSprite;
            spriteRenderer.flipX = false;
        }
        */
        // 현재는 움직임이 없으면 마지막 상태를 유지하도록 아무것도 하지 않습니다. 필요에 따라 위 주석처럼 추가하세요.
    }
}