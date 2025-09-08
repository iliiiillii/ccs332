using UnityEngine;
using System.Collections; // �ʿ信 ����

public class BossMonsterScript : MonoBehaviour
{
    private MonsterDataRecord dbData;
    public MonsterDataRecord DbData { get { return dbData; } }

    public float currentHp;
    private float initialHp;
    private int rewardGold;

    [Header("Effects")]
    public GameObject hitEffectPrefab;

    // <<< �߰�: ���⺰ ��������Ʈ ���� >>>
    [Header("Directional Sprites")]
    public Sprite frontSprite;  // ������ ���� �̹���
    public Sprite backSprite;   // ������ �޸� �̹���
    public Sprite sideSprite;   // ������ ���� �̹��� (�⺻������ �������� ���ٰ� ����)

    // <<< �߰�: ��������Ʈ ������ ���� >>>
    private SpriteRenderer spriteRenderer;

    public static event System.Action OnBossDeath;

    // <<< �߰�: Awake �Լ� >>>
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] Boss���� SpriteRenderer ������Ʈ�� �����ϴ�!");
        }
    }

    public void InitializeFromDB(MonsterDataRecord baseStats, float currentWaveHpMultiplier, float currentWaveGoldMultiplier)
    {
        dbData = baseStats;

        if (dbData == null)
        {
            Debug.LogError($"{gameObject.name}: DB�κ��� ���� ���� �����͸� ���� ���߽��ϴ�! �⺻�� ���.");
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

        Debug.Log($"���� ���� �ʱ�ȭ (DB): [{dbData.monsterName}] HP: {currentHp}, Gold: {rewardGold}");
    }

    private void Update()
    {
        // TODO: ���� �̵� ���� (MonsterMovement ��� �Ǵ� ��ü ����)
        // TODO: ���� ���� ���� ����
    }

    public void TakeDamage(float dmg)
    {
        if (currentHp <= 0) return;

        currentHp -= dmg;
        Debug.Log($"{name} (����) ���� ����! ���� ü��: {currentHp}");

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
        Debug.Log($"{name} (����) ���!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(rewardGold);
            GameManager.Instance.MonsterKilled(this.dbData);
        }

        OnBossDeath?.Invoke();
        Destroy(gameObject);
    }

    // <<< �߰�: ���⿡ ���� ��������Ʈ ������Ʈ �Լ� >>>
    public void UpdateSpriteDirection(Vector2 direction)
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[{gameObject.name}] SpriteRenderer�� �����ϴ�! ���� ��ȯ �Ұ�.");
            return;
        }

        // ���� ��������Ʈ���� ��� �Ҵ�Ǿ����� Ȯ�� (������������ ������)
        if (frontSprite == null || backSprite == null || sideSprite == null)
        {
            // Debug.LogWarning($"[{gameObject.name}] Boss�� ���⺰ ��������Ʈ�� ��� �Ҵ���� �ʾҽ��ϴ�.");
            // ��������Ʈ�� ������ �⺻ flipX���̶� �۵��ϵ��� �� �� ����
            if (direction.x > 0.01f) spriteRenderer.flipX = false;
            else if (direction.x < -0.01f) spriteRenderer.flipX = true;
            return;
        }

        // ���� �̵��� �켱����, �¿� �̵��� �켱������ ���� ��������Ʈ ����
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            if (direction.y > 0) // ���� �̵� -> �޸��
            {
                spriteRenderer.sprite = backSprite;
                spriteRenderer.flipX = false;
            }
            else // �Ʒ��� �̵� -> �ո��
            {
                spriteRenderer.sprite = frontSprite;
                spriteRenderer.flipX = false;
            }
        }
        else // �¿� �̵��� �� ũ�ų� ���� ��
        {
            spriteRenderer.sprite = sideSprite;
            if (direction.x > 0) // ���������� �̵�
            {
                spriteRenderer.flipX = false; // sideSprite�� �������� ���� �ִٰ� ����
            }
            else // �������� �̵�
            {
                spriteRenderer.flipX = true; // sideSprite�� �¿� ����
            }
        }
    }

    // TODO: ���� ���͸��� Ư�� ��ų �Լ��� �߰� (��: ���� ����, ���� ��ȯ ��)
}