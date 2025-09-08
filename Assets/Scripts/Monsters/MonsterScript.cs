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

    // MonsterScript.cs ������ Ŭ���� ���� �ٷ� �Ʒ�, �ٸ� ������ �ִ� ���� �߰�
    [Header("Directional Sprites")] // Inspector���� ���� ���� �׷� �̸� ����
    public Sprite frontSprite;  // ���� ���� �̹��� �Ҵ��
    public Sprite backSprite;   // ���� �޸� �̹��� �Ҵ��
    public Sprite sideSprite;   // ���� ���� �̹��� �Ҵ�� (�⺻������ �������� ���� �ִ� ��������Ʈ�� �غ��Ͻø� flipX�� ���ʵ� ǥ�� ����)

    [Header("Effects")]
    public GameObject hitEffectPrefab;

    [Header("Slow Effect Prefabs")]
    [SerializeField] private GameObject slowAuraPrefab;
    [SerializeField] private GameObject frostDustPrefab;
    // <<< �߰�: ��������Ʈ ������ ���� >>>
    private SpriteRenderer spriteRenderer; // ������ ��������Ʈ�� �����ϱ� ����

    // Start �Ǵ� Awake���� SpriteRenderer ������Ʈ�� �����ɴϴ�.
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponent<MonsterMovement>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] SpriteRenderer ������Ʈ�� ã�� �� �����ϴ�!");
        }
    }

    public void InitializeFromDB(MonsterDataRecord baseStats, float currentWaveHpMultiplier, float currentWaveGoldMultiplier)
    {
        dbData = baseStats;

        if (dbData == null)
        {
            Debug.LogError($"{gameObject.name}: DB�κ��� ���� �����͸� ���� ���߽��ϴ�! �⺻�� ���.");
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

        Debug.Log($"���� �ʱ�ȭ (DB): [{dbData.monsterName}] HP: {currentHp}, Gold: {rewardGold}");
    }

    public void SetHp(float newMaxHp)
    {
        initialHp = newMaxHp;
        currentHp = newMaxHp;
    }

    public void ApplySlowEffect(float slowAmount, float duration)
    {
        Debug.Log($"{name} ���ο� ȿ�� ����! {slowAmount * 100}% ����, {duration}�� ����");

        // <<< ����: ���� ���ο� ���� ȣ�� >>>
        if (movement != null)
        {
            movement.ApplySlow(slowAmount, duration);
        }
        else
        {
            Debug.LogWarning($"[{name}] MonsterMovement ��ũ��Ʈ�� ��� ���ο� ȿ���� ������ �� �����ϴ�.");
        }
    }

    public void ApplyPoisonEffect(float damagePerTick, float duration)
    {
        Debug.Log($"{name} �� ȿ�� ����! {duration}�� ���� �ʴ� {damagePerTick} ����");
        // ���� �� ������ ���� ���� �ʿ� (�ڷ�ƾ ��� ��)
    }

    public void ApplyFreezeEffect(float duration)
    {
        Debug.Log($"{name} �� ȿ�� ����! {duration}�� ���� ����");
        // ���� �̵� ���� ���� ���� �ʿ� (MonsterMovement�� �̵� �ߴ� ��)
    }

    private IEnumerator SlowEffectCoroutine(float duration)
    {
        // 1) ���� ����Ʈ ���̱�
        var aura = Instantiate(slowAuraPrefab, transform.position, Quaternion.identity, transform);
        var psAura = aura.GetComponent<ParticleSystem>();
        if (psAura != null) psAura.Play();

        // 2) �ܼ� ����Ʈ ���̱�
        var dust = Instantiate(frostDustPrefab, transform.position, Quaternion.identity, transform);
        var psDust = dust.GetComponent<ParticleSystem>();
        if (psDust != null) psDust.Play();

        // 3) duration ��ŭ ���
        yield return new WaitForSeconds(duration);

        // 4) ����Ʈ ����
        Destroy(aura);
        Destroy(dust);

        // TODO: MonsterMovement �ʿ��� �ӵ� ���� ���� ȣ��
    }

    public void TakeDamage(float dmg, TowerType attackType = TowerType.Normal) // �⺻���� Normal�� ����
    {
        if (currentHp <= 0) return;

        currentHp -= dmg;
        Debug.Log($"{name}��(��) {attackType} Ÿ�� �������� {dmg} ���� ����! ���� ü��: {currentHp}");

        // <<< ���⿡ ���� Ÿ�Կ� ���� �ٸ� ����Ʈ�� �����ϴ� ���� �߰� >>>
        if (attackType == TowerType.Slow)
        {
            // ���ο� ������ �޾��� ���� �ǰ� ����Ʈ ���� ����
            // ��: ���� ������ Ƣ�� ����Ʈ ��
            // ���� ���ο� ȿ�� ��ü�� ���� ���� �پ��ִ� ���� ���¶��,
            // ����Ʈ ������ TakeDamage�� �ƴ� ApplySlowEffect���� ó���ϴ� ���� �� ������ �� �ֽ��ϴ�.
            // ����� '�ǰ�' ������ ����Ʈ�� ó���Ѵٰ� �����մϴ�.
            Debug.Log("���ο� Ÿ�� �ǰ� ����Ʈ ����!");
            if (frostDustPrefab != null) Instantiate(frostDustPrefab);
        }
        else // ���ο찡 �ƴ� �ٸ� ��� ����
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] �Ϲ� �ǰ� ����Ʈ(hitEffectPrefab)�� �Ҵ���� �ʾҽ��ϴ�!");
            }
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} ���!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(rewardGold);
            GameManager.Instance.MonsterKilled(this.dbData);
        }

        OnMonsterDeath?.Invoke();
        Destroy(gameObject);
    }

    // <<< ���⿡ UpdateSpriteDirection �Լ� �߰�! >>>
    /// <summary>
    /// ������ �̵� ���⿡ ���� ��������Ʈ ������ ������Ʈ�մϴ�.
    /// </summary>
    /// <param name="direction">���Ͱ� �̵��ϴ� ���� (��: Vector2)</param>
    public void UpdateSpriteDirection(Vector2 direction)
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[{gameObject.name}] SpriteRenderer�� �����ϴ�! Awake���� �ʱ�ȭ�Ǿ����� Ȯ�����ּ���.");
            return;
        }

        // ���� ��������Ʈ���� Inspector���� ��� �Ҵ�Ǿ����� Ȯ�� (������ ����)
        if (frontSprite == null || backSprite == null || sideSprite == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Inspector���� frontSprite, backSprite, sideSprite �� �ϳ� �̻��� �Ҵ���� �ʾҽ��ϴ�! ��������Ʈ ������ ����� �۵����� ���� �� �ֽ��ϴ�.");
            // �� ���, �⺻���� flipX ������ �����ϰų�, �ƿ� �ƹ��͵� �� �� ���� �ֽ��ϴ�.
            // ���⼭�� �ϴ� �⺻���� flipX�� ���ܵΰ� �Լ��� �����ϰų�, �Ǵ� �⺻ ��������Ʈ�� ǥ���� �� �ֽ��ϴ�.
            // �Ʒ��� ���÷�, ���� flipX ������ �����ϰ� �Ѿ�� �ڵ��Դϴ�. (�� �κ��� ���� �����ο� �°� �����ϼ���)
            if (direction.x > 0.01f) { spriteRenderer.flipX = false; }
            else if (direction.x < -0.01f) { spriteRenderer.flipX = true; }
            return; // �߿��� ��������Ʈ�� ������ �� �̻� �������� ����
        }

        // Y��(����) �������� ���밪�� X��(�¿�) �������� ���밪���� Ŭ �� (���� �̵��� �켱���� ħ)
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            if (direction.y > 0.01f) // ���� �̵� (ȭ�� ����, ���� ĳ������ �޸��)
            {
                spriteRenderer.sprite = backSprite;
                spriteRenderer.flipX = false; // �޸���� ���� �¿������ �ʿ� �����ϴ�.
            }
            else if (direction.y < -0.01f) // �Ʒ��� �̵� (ȭ�� �Ʒ���, ���� ĳ������ �ո��)
            {
                spriteRenderer.sprite = frontSprite;
                spriteRenderer.flipX = false; // �ո���� ���� �¿������ �ʿ� �����ϴ�.
            }
            // ���� ���� �̵� �߿��� �¿츦 �ణ Ʋ�� �ʹٸ� ���⼭ flipX�� �߰��� ����� �� ������, ������ ����.
        }
        // X��(�¿�) �������� ���밪�� Y�� �������� ���밪���� ũ�ų� ���� �� (�¿� �̵��� �켱���� ħ, �Ǵ� �밢�� �̵� �� ��������� ó��)
        else if (Mathf.Abs(direction.x) > 0.01f) // �¿�� �̵� ���� ��
        {
            spriteRenderer.sprite = sideSprite; // ����� ��������Ʈ�� ����
            if (direction.x > 0.01f) // ���������� �̵�
            {
                spriteRenderer.flipX = false; // sideSprite�� �⺻������ �������� ���� �ִٰ� ����
            }
            else // �������� �̵� (direction.x < -0.01f)
            {
                spriteRenderer.flipX = true;  // sideSprite�� �¿�����Ͽ� ������ ���� ��
            }
        }
        // else: �������� ���� ���� �� (���ڸ�). �� ���� � ����� �������� �����մϴ�.
        // ���� ���, ������ �������� ������ �����ϰų�, �⺻������ ����(frontSprite)�� ������ �� �� �ֽ��ϴ�.
        // �Ʒ��� ���÷�, �������� ������ ������ ������ �ϴ� �ڵ��Դϴ�. (���� ����)
        /*
        else 
        {
            spriteRenderer.sprite = frontSprite;
            spriteRenderer.flipX = false;
        }
        */
        // ����� �������� ������ ������ ���¸� �����ϵ��� �ƹ��͵� ���� �ʽ��ϴ�. �ʿ信 ���� �� �ּ�ó�� �߰��ϼ���.
    }
}