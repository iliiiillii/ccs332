using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TowerScript : MonoBehaviour
{
    private TowerDataRecord dbData;
    public TowerDataRecord DbData { get { return dbData; } }

    public TowerGrade grade;
    public TowerType towerType;

    public float attackRange;
    public float attackCooldown;
    public float attackDamage;

    private float specialValue1;
    private float specialValue2;
    private float areaOfEffectRange;
    private int chainTargets;

    private float currentCooldown = 0f;
    private bool isSelected = false;
    private SpriteRenderer sr;

    [Header("Targeting Settings")]
    public LayerMask monsterLayerMask;

    [Header("Splash Attack Effects")]
    public GameObject splashAreaEffectPrefab; // 타워 주변에 생길 '성수 물결' 이펙트 프리팹
    public GameObject splashHitEffectPrefab;  // 각 몬스터에게 맞았을 때 생길 '피격' 이펙트 프리팹

    [Header("Projectile & Effects")]
    public GameObject projectilePrefab;
    public GameObject attackEffectPrefab;
    public Transform muzzlePoint;

    [Header("Slow Attack Effects")] // 또는 기존 헤더에 추가
    public GameObject slowEffectPrefab; // 몬스터에게 적용될 슬로우 이펙트

    // <<< 추가된 헬퍼 함수: 레이어 마스크에 포함된 레이어 이름들을 문자열로 반환 >>>
    string GetLayerMaskNames(LayerMask mask)
    {
        List<string> layerNames = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) == (1 << i)) // i번 레이어가 마스크에 포함되어 있는지 확인
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layerNames.Add(layerName);
                }
                else
                {
                    layerNames.Add($"Layer {i}"); // 이름 없는 레이어 (거의 없음)
                }
            }
        }
        if (layerNames.Count == 0) return "Nothing";
        return string.Join(", ", layerNames);
    }
    // <<< 여기까지 헬퍼 함수 >>>

    public void InitializeFromDB(TowerDataRecord dataFromDB)
    {
        dbData = dataFromDB;
        sr = GetComponent<SpriteRenderer>();

        if (dbData == null)
        {
            Debug.LogError($"{gameObject.name}: DB로부터 타워 데이터를 받지 못했습니다!");
            this.towerType = TowerType.Normal;
            this.grade = TowerGrade.Normal;
            ApplyStatsFromDBData();
            return;
        }

        try
        {
            this.towerType = (TowerType)System.Enum.Parse(typeof(TowerType), dbData.towerType, true);
            this.grade = (TowerGrade)System.Enum.Parse(typeof(TowerGrade), dbData.towerGrade, true);
        }
        catch (System.ArgumentException ex)
        {
            Debug.LogError($"타워 타입 또는 등급 변환 오류: {dbData.towerType}, {dbData.towerGrade} - {ex.Message}");
            this.towerType = TowerType.Normal;
            this.grade = TowerGrade.Normal;
        }
        ApplyStatsFromDBData();

        if (muzzlePoint == null)
        {
            muzzlePoint = transform;
        }
    }

    void ApplyStatsFromDBData()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (dbData == null)
        {
            Debug.LogWarning($"{gameObject.name}: 적용할 DB 데이터가 없습니다. 기본 능력치를 사용합니다.");
            attackDamage = 1f;
            attackRange = 1f;
            attackCooldown = 2f;
            sr.color = Color.gray;
            return;
        }

        attackDamage = dbData.attackDamage;
        attackRange = dbData.attackRange;
        attackCooldown = dbData.attackCooldown;
        specialValue1 = dbData.specialAbilityValue1;
        specialValue2 = dbData.specialAbilityValue2;

        switch (this.towerType)
        {
            case TowerType.Splash:
            case TowerType.Fire:
            case TowerType.Rocket:
                areaOfEffectRange = dbData.specialAbilityValue1 > 0 ? dbData.specialAbilityValue1 : 1.5f;
                break;
            case TowerType.Lightning:
                chainTargets = dbData.specialAbilityValue1 > 0 ? (int)dbData.specialAbilityValue1 : 3;
                break;
        }
        SetTowerColorByGrade();
        Debug.Log($"🎯 타워 초기화됨 (DB): [{dbData.towerName} - {grade} ({towerType})] 공격력: {attackDamage}, 사거리: {attackRange}, 쿨타임: {attackCooldown}");
    }

    void SetTowerColorByGrade()
    {
        switch (grade)
        {
            case TowerGrade.Normal: sr.color = new Color(0.5f, 0.8f, 1f); break;
            case TowerGrade.Rare: sr.color = new Color(0f, 0.2f, 1f); break;
            case TowerGrade.Unique: sr.color = new Color(0f, 1f, 0.2f); break;
            case TowerGrade.Legendary: sr.color = new Color(0.6f, 0f, 1f); break;
            case TowerGrade.Epic: sr.color = new Color(1f, 0.3f, 0f); break;
            case TowerGrade.Mythic: sr.color = new Color(1f, 0f, 0f); break;
            default: sr.color = Color.white; break;
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"[타워 클릭됨] {gameObject.name}");
        if (!isSelected)
        {
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.SelectTower(this);
        }
        else
        {
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.DeselectTower(this);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selected)
        {
            sr.color = Color.cyan;
        }
        else
        {
            if (dbData != null) SetTowerColorByGrade();
            else sr.color = Color.gray;
        }
    }

    private void SingleTargetAttack(MonsterScript monster)
    {
        if (projectilePrefab != null)
        {
            GameObject projectileGO = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);
            ProjectileScript projectile = projectileGO.GetComponent<ProjectileScript>();
            if (projectile != null)
            {
                projectile.SetTarget(monster.transform, attackDamage);
            }
            else
            {
                Debug.LogError($"발사체 프리팹 '{projectilePrefab.name}'에 ProjectileScript가 없습니다!");
                Destroy(projectileGO);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} ({towerType}): projectilePrefab이 설정되지 않아 직접 데미지를 줍니다.");
            monster.TakeDamage(attackDamage);
        }
    }

    private void SplashAttack(Vector3 targetPosition)
    {
        // 1. 타워 위치에 '성수 물결' 같은 범위 공격 이펙트를 생성합니다.
        if (splashAreaEffectPrefab != null)
        {
            // 타워 자기 자신의 위치에 이펙트 생성
            GameObject areaEffect = Instantiate(splashAreaEffectPrefab, transform.position, Quaternion.identity);

            // (선택 사항) 만약 이펙트의 크기를 타워의 공격 범위에 맞춰야 한다면, 아래 코드의 주석을 해제하고 조절합니다.
            // float effectScale = (areaOfEffectRange > 0 ? areaOfEffectRange : 1.5f) * 2f;
            // areaEffect.transform.localScale = new Vector3(effectScale, effectScale, 1f);
        }

        // 2. 타워 주변의 모든 몬스터를 찾습니다.
        // targetPosition (목표 몬스터 위치) 대신 타워 자신의 위치(transform.position)를 중심으로 범위를 찾는 것이
        // "주변에 뿌린다"는 컨셉에 더 잘 맞습니다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, areaOfEffectRange > 0 ? areaOfEffectRange : 1.5f, monsterLayerMask);
        Debug.Log($"SplashAttack at {transform.position}: {hits.Length} colliders hit in range {areaOfEffectRange}");

        // 3. 범위 내의 각 몬스터에게 데미지와 피격 이펙트를 적용합니다.
        foreach (var hit in hits)
        {
            MonsterScript monsterInRange = hit.GetComponent<MonsterScript>();
            if (monsterInRange != null)
            {
                // 3-1. 데미지 계산 및 적용 (기존 로직 유지)
                float splashDamage = attackDamage * (dbData != null && dbData.specialAbilityValue2 > 0 ? dbData.specialAbilityValue2 : 0.8f);
                monsterInRange.TakeDamage(splashDamage);
                Debug.Log($"{gameObject.name} splashed {monsterInRange.name} for {splashDamage} damage.");

                // 3-2. 각 몬스터 위치에 '피격 이펙트'를 생성합니다.
                if (splashHitEffectPrefab != null)
                {
                    Instantiate(splashHitEffectPrefab, monsterInRange.transform.position, Quaternion.identity);
                }
            }
        }
    }
    private void FireAttack(Vector3 targetPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, areaOfEffectRange > 0 ? areaOfEffectRange : 1.5f, monsterLayerMask);
        foreach (var hit in hits)
        {
            MonsterScript monsterInRange = hit.GetComponent<MonsterScript>();
            if (monsterInRange != null)
            {
                monsterInRange.TakeDamage(attackDamage);
            }
        }
    }

    private void ChainLightningAttack(MonsterScript initialTarget)
    {
        initialTarget.TakeDamage(attackDamage * (dbData != null && dbData.specialAbilityValue2 > 0 ? dbData.specialAbilityValue2 : 0.7f));
        int targetsHit = 1;
        int maxTargets = chainTargets > 0 ? chainTargets : 3;

        // List<MonsterScript> potentialNextTargets = new List<MonsterScript>(); // 이 변수는 현재 사용되지 않으므로 주석 처리하거나 필요한 로직 추가
        Collider2D[] hits = Physics2D.OverlapCircleAll(initialTarget.transform.position, attackRange, monsterLayerMask);

        foreach (var hit in hits)
        {
            MonsterScript monsterInRange = hit.GetComponent<MonsterScript>();
            if (monsterInRange != null && monsterInRange != initialTarget && targetsHit < maxTargets)
            {
                monsterInRange.TakeDamage(attackDamage * (dbData != null && dbData.specialAbilityValue2 > 0 ? dbData.specialAbilityValue2 : 0.7f) * 0.8f);
                targetsHit++;
                Debug.Log($"{gameObject.name} chained to {monsterInRange.name}");
            }
        }
    }

    private void RocketAttack(MonsterScript monster)
    {
        if (projectilePrefab != null)
        {
            GameObject rocketGO = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);
            ProjectileScript projectile = rocketGO.GetComponent<ProjectileScript>();
            if (projectile != null)
            {
                projectile.SetTarget(monster.transform, attackDamage);
            }
            else
            {
                Debug.LogError($"로켓 프리팹 '{projectilePrefab.name}'에 ProjectileScript가 없습니다!");
                Destroy(rocketGO);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} (ROCKET): projectilePrefab이 설정되지 않았습니다.");
            monster.TakeDamage(attackDamage * (dbData != null && dbData.specialAbilityValue2 > 0 ? dbData.specialAbilityValue2 : 1.5f));
        }
    }

    private void Update()
    {
        if (towerType == TowerType.Buff || dbData == null) return;

        currentCooldown -= Time.deltaTime;

        if (currentCooldown <= 0f)
        {
            MonsterScript targetMonster = FindTarget(); // 수정된 FindTarget 호출
            if (targetMonster != null)
            {
                PerformAttack(targetMonster);
                currentCooldown = attackCooldown;
            }
        }
    }

    // <<< 수정된 FindTarget 함수 >>>
    MonsterScript FindTarget()
    {
        MonsterScript closestMonster = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 position = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, attackRange, monsterLayerMask);

        // LayerMask에 포함된 레이어 이름을 출력하도록 수정 (GetLayerMaskNames 헬퍼 함수 사용)
       
        foreach (var hit in hits)
        {
            // 감지된 각 콜라이더의 게임 오브젝트에서 MonsterScript 컴포넌트를 가져옵니다.
            MonsterScript monster = hit.GetComponent<MonsterScript>();

            if (monster != null) // MonsterScript 컴포넌트가 있는 경우
            {
                // Debug.Log($"[{gameObject.name}] MonsterScript 찾음: {monster.name} (레이어: {LayerMask.LayerToName(hit.gameObject.layer)})"); // 필요시 상세 로그

                Vector3 directionToTarget = monster.transform.position - position;
                float dSqrToTarget = directionToTarget.sqrMagnitude; // 최적화를 위해 제곱 거리 사용

                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    closestMonster = monster;
                }
            }
        }

        if (closestMonster != null)
        {
            Debug.Log($"[{gameObject.name}] 최종 타겟: {closestMonster.name}");
        }
        // 타겟을 찾지 못한 경우 별도 로그는 생략 (위에서 hits.Length로 확인 가능)

        return closestMonster;
    }
    // <<< 여기까지 수정된 FindTarget 함수 >>>


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    void PerformAttack(MonsterScript monster)
    {
        if (attackEffectPrefab != null)
        {
            Transform spawnPoint = muzzlePoint != null ? muzzlePoint : transform;
            Instantiate(attackEffectPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        switch (towerType)
        {
            case TowerType.Normal:
            case TowerType.Sniper:
                SingleTargetAttack(monster);
                break;
            case TowerType.Splash:
                SplashAttack(monster.transform.position);
                break;
            case TowerType.Slow:
                if (slowEffectPrefab != null)
                {
                    // 몬스터에게 이펙트가 따라다니게 하려면 몬스터의 자식으로 생성
                    Instantiate(slowEffectPrefab, monster.transform.position, Quaternion.identity, monster.transform);
                }

                monster.ApplySlowEffect(specialValue1 > 0 ? specialValue1 : 0.3f, specialValue2 > 0 ? specialValue2 : 3.0f);
                monster.TakeDamage(attackDamage, this.towerType); // 슬로우 타워도 약간의 데미지는 줄 수 있습니다.
                break;
            case TowerType.Poison:
                monster.ApplyPoisonEffect(specialValue1 > 0 ? specialValue1 : attackDamage, specialValue2 > 0 ? specialValue2 : 5.0f);
                break;
            case TowerType.Fire:
                FireAttack(monster.transform.position);
                break;
            case TowerType.Lightning:
                ChainLightningAttack(monster);
                break;
            case TowerType.Freeze:
                monster.ApplyFreezeEffect(specialValue1 > 0 ? specialValue1 : 2.0f);
                break;
            case TowerType.Rocket:
                RocketAttack(monster);
                break;
            default:
                SingleTargetAttack(monster);
                break;
        }
    }
}