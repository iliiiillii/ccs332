using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 0f;
    public float lifeTime = 5f;

    private Transform targetMonster;
    private Vector3 targetDirection;
    private bool hasTarget = false;
    private SpriteRenderer spriteRenderer;
    public Vector2 spriteForwardDirection = Vector2.right;

    private bool hasHitSomething = false; // <<< 추가: 이미 충돌했는지 여부를 나타내는 플래그

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetTarget(Transform monsterTransform, float projectileDamage)
    {
        targetMonster = monsterTransform;
        damage = projectileDamage;
        hasTarget = (targetMonster != null);

        if (hasTarget)
        {
            targetDirection = (targetMonster.position - transform.position).normalized;
            RotateTowards(targetDirection);
        }
        else
        {
            Debug.LogError("Projectile target is not set! Destroying projectile.");
            Destroy(gameObject);
        }
    }

    public void SetDirection(Vector3 direction, float projectileDamage)
    {
        targetDirection = direction.normalized;
        damage = projectileDamage;
        hasTarget = false;
        RotateTowards(targetDirection);
    }

    void Update()
    {
        if (hasTarget && targetMonster == null)
        {
            Destroy(gameObject);
            return;
        }

        if (hasTarget && targetMonster != null)
        {
            Vector3 newDirection = (targetMonster.position - transform.position).normalized;
            if (newDirection != Vector3.zero)
            {
                targetDirection = newDirection;
                RotateTowards(targetDirection);
            }
        }
        else if (!hasTarget && targetDirection == Vector3.zero)
        {
            Debug.LogWarning("Projectile has no target and no direction. Destroying.");
            Destroy(gameObject);
            return;
        }
        transform.Translate(targetDirection * speed * Time.deltaTime, Space.World);
    }

    void RotateTowards(Vector2 direction)
    {
        if (direction != Vector2.zero && spriteRenderer != null)
        {
            float angle = Vector2.SignedAngle(spriteForwardDirection, direction);
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitSomething) // <<< 추가: 이미 무언가에 맞았다면 더 이상 처리하지 않음
        {
            return;
        }

        if (collision.gameObject.CompareTag("Monster") || collision.gameObject.CompareTag("Boss"))
        {
            hasHitSomething = true; // <<< 추가: 충돌 발생 시 플래그 설정

            MonsterScript monster = collision.gameObject.GetComponent<MonsterScript>();
            BossMonsterScript boss = collision.gameObject.GetComponent<BossMonsterScript>();

            // 원래 로그를 유지합니다.
            Debug.Log($"{gameObject.name} hit {collision.gameObject.name} for {damage} damage.");

            if (monster != null)
            {
                monster.TakeDamage(damage);
            }
            else if (boss != null)
            {
                boss.TakeDamage(damage);
            }

            Destroy(gameObject); // 충돌 후 발사체 파괴
        }
    }
}