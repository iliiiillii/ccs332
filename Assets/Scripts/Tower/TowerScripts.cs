using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TowerScript : MonoBehaviour
{
    // 기본 데이터
    private TowerDataRecord dbData;
    public TowerDataRecord DbData { get { return dbData; } }
    public TowerGrade grade;
    public TowerType towerType;

    // 능력치
    public float attackRange;
    public float attackCooldown;
    public float attackDamage;
    private float specialValue1;
    private float specialValue2;
    private float areaOfEffectRange;
    private int chainTargets;

    // 내부 상태 변수
    private float currentCooldown = 0f;
    private bool isSelected = false;

    // 컴포넌트 참조
    private SpriteRenderer sr;
    private Animator anim; // 타워 자체 애니메이션을 위한 변수

    // --- Inspector 설정 변수들 ---

    [Header("Targeting Settings")]
    public LayerMask monsterLayerMask;

    [Header("Common Effects")]
    public GameObject attackEffectPrefab; // 모든 타워 공용 공격 발생 이펙트
    public GameObject projectilePrefab;   // 투사체 프리팹
    public Transform muzzlePoint;         // 발사 위치

    [Header("Splash Attack Effects")]
    public GameObject splashAreaEffectPrefab;
    public GameObject splashHitEffectPrefab;

    [Header("Slow Attack Effects")]
    public GameObject slowEffectPrefab;

    [Header("Poison Attack Effects")]
    public GameObject poisonEffectPrefab;

    [Header("Fire Attack Effects")]
    public GameObject fireAreaEffectPrefab;

    [Header("Lightning Attack Effects")]
    public GameObject lightningEffectPrefab;

    [Header("Freeze Attack Effects")]
    public GameObject freezeEffectPrefab;

    [Header("Buff Settings")]
    public LayerMask towerLayerMask;
    private List<Coroutine> activeBuffCoroutines = new List<Coroutine>();

    // --- 초기화 및 설정 함수 ---

    public void InitializeFromDB(TowerDataRecord dataFromDB)
    {
        dbData = dataFromDB;
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); // Animator 컴포넌트 가져오기

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
    }

    void SetTowerColorByGrade()
    {
        switch (grade)
        {
            case TowerGrade.Normal: sr.color = new Color(0.5f, 0.8f, 1f); break; // 연한 하늘색
            case TowerGrade.Rare: sr.color = new Color(0f, 0.2f, 1f); break; // 파란색
            case TowerGrade.Unique: sr.color = new Color(0f, 1f, 0.2f); break; // 초록색
            case TowerGrade.Legendary: sr.color = new Color(0.6f, 0f, 1f); break; // 보라색
            case TowerGrade.Epic: sr.color = new Color(1f, 0.3f, 0f); break; // 주황색
            case TowerGrade.Mythic: sr.color = new Color(1f, 0f, 0f); break; // 빨간색
            default: sr.color = Color.white; break;
        }
    }

    // --- 유니티 생명주기 함수 ---

    private void Update()
    {
        // 1. 버프 타워 로직
        if (towerType == TowerType.Buff)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f)
            {
                ApplyBuffToNearbyTowers();
                currentCooldown = attackCooldown;
            }
            return; // 버프 타워는 공격 로직을 실행하지 않음
        }

        // 2. 공격 타워 로직
        if (dbData == null) return;

        currentCooldown -= Time.deltaTime;
        if (currentCooldown <= 0f)
        {
            MonsterScript targetMonster = FindTarget();
            if (targetMonster != null)
            {
                PerformAttack(targetMonster);
                currentCooldown = attackCooldown;
            }
        }
    }

    // --- 공격 실행 및 보조 함수 ---

    void PerformAttack(MonsterScript monster)
    {
        // 1. 타워 자체 공격 애니메이션 실행
        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        // 2. 공용 공격 발생 이펙트 생성
        if (attackEffectPrefab != null)
        {
            Transform spawnPoint = muzzlePoint != null ? muzzlePoint : transform;
            Instantiate(attackEffectPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        // 3. 타입별 실제 공격 로직 실행
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
                if (slowEffectPrefab != null) Instantiate(slowEffectPrefab, monster.transform.position, Quaternion.identity, monster.transform);
                monster.ApplySlowEffect(specialValue1 > 0 ? specialValue1 : 0.3f, specialValue2 > 0 ? specialValue2 : 3.0f);
                monster.TakeDamage(attackDamage, this.towerType);
                break;
            case TowerType.Poison:
                if (poisonEffectPrefab != null) Instantiate(poisonEffectPrefab, monster.transform.position, Quaternion.identity);
                monster.ApplyPoisonEffect(specialValue1 > 0 ? specialValue1 : attackDamage, specialValue2 > 0 ? specialValue2 : 5.0f);
                break;
            case TowerType.Fire:
                FireAttack(monster.transform.position);
                break;
            case TowerType.Lightning:
                ChainLightningAttack(monster);
                break;
            case TowerType.Freeze:
                if (freezeEffectPrefab != null) Instantiate(freezeEffectPrefab, monster.transform.position, Quaternion.identity);
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

    MonsterScript FindTarget()
    {
        MonsterScript closestMonster = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 position = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(position, attackRange, monsterLayerMask);

        foreach (var hit in hits)
        {
            MonsterScript monster = hit.GetComponent<MonsterScript>();
            if (monster != null)
            {
                Vector3 directionToTarget = monster.transform.position - position;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    closestMonster = monster;
                }
            }
        }
        return closestMonster;
    }

    // --- 각 타워 타입별 공격 함수들 ---

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
        }
        else
        {
            monster.TakeDamage(attackDamage);
        }
    }

    private void SplashAttack(Vector3 targetPosition)
    {
        if (splashAreaEffectPrefab != null) Instantiate(splashAreaEffectPrefab, transform.position, Quaternion.identity);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, areaOfEffectRange, monsterLayerMask);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<MonsterScript>() is MonsterScript monsterInRange)
            {
                float splashDamage = attackDamage * (specialValue2 > 0 ? specialValue2 : 0.8f);
                monsterInRange.TakeDamage(splashDamage);
                if (splashHitEffectPrefab != null) Instantiate(splashHitEffectPrefab, monsterInRange.transform.position, Quaternion.identity);
            }
        }
    }

    private void FireAttack(Vector3 targetPosition)
    {
        if (fireAreaEffectPrefab != null) Instantiate(fireAreaEffectPrefab, targetPosition, Quaternion.identity);
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, areaOfEffectRange, monsterLayerMask);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<MonsterScript>() is MonsterScript monsterInRange)
            {
                monsterInRange.TakeDamage(attackDamage);
            }
        }
    }

    private void ChainLightningAttack(MonsterScript initialTarget)
    {
        if (lightningEffectPrefab != null) Instantiate(lightningEffectPrefab, initialTarget.transform.position, Quaternion.identity);
        initialTarget.TakeDamage(attackDamage * (specialValue2 > 0 ? specialValue2 : 0.7f));

        int targetsHit = 1;
        int maxTargets = chainTargets > 0 ? chainTargets : 3;
        Collider2D[] hits = Physics2D.OverlapCircleAll(initialTarget.transform.position, attackRange, monsterLayerMask);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<MonsterScript>() is MonsterScript monsterInRange && monsterInRange != initialTarget && targetsHit < maxTargets)
            {
                if (lightningEffectPrefab != null) Instantiate(lightningEffectPrefab, monsterInRange.transform.position, Quaternion.identity);
                monsterInRange.TakeDamage(attackDamage * (specialValue2 > 0 ? specialValue2 : 0.7f) * 0.8f);
                targetsHit++;
            }
        }
    }

    private void RocketAttack(MonsterScript monster)
    {
        // SingleTargetAttack과 유사하게 구현
        SingleTargetAttack(monster);
    }

    // --- 버프 관련 함수들 ---

    void ApplyBuffToNearbyTowers()
    {
        Collider2D[] foundTowers = Physics2D.OverlapCircleAll(transform.position, attackRange, towerLayerMask);
        foreach (var towerCollider in foundTowers)
        {
            if (towerCollider.GetComponent<TowerScript>() is TowerScript targetTower && targetTower != this && targetTower.towerType != TowerType.Buff)
            {
                targetTower.ReceiveBuff(specialValue1, specialValue2);
            }
        }
    }

    public void ReceiveBuff(float damageMultiplier, float duration)
    {
        foreach (var co in activeBuffCoroutines)
        {
            StopCoroutine(co);
        }
        activeBuffCoroutines.Clear();
        activeBuffCoroutines.Add(StartCoroutine(BuffCoroutine(damageMultiplier, duration)));
    }

    private IEnumerator BuffCoroutine(float damageMultiplier, float duration)
    {
        float originalDamage = dbData.attackDamage;
        attackDamage = originalDamage * damageMultiplier;

        yield return new WaitForSeconds(duration);

        attackDamage = originalDamage;
    }

    // --- 기타 유틸리티 함수 ---

    private void OnMouseDown()
    {
        Debug.Log($"[타워 클릭됨] {gameObject.name}");
        if (!isSelected)
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.SelectTower(this);
            }
        }
        else
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.DeselectTower(this);
            }
        }
    }
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selected)
        {
            // 선택되면 시안(cyan) 색으로 변경
            sr.color = Color.cyan;
        }
        else
        {
            // 선택이 해제되면 원래 등급에 맞는 색으로 복원
            if (dbData != null)
            {
                SetTowerColorByGrade();
            }
            else
            {
                sr.color = Color.gray;
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        // Scene 뷰에서 선택했을 때 노란색으로 공격 범위를 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}