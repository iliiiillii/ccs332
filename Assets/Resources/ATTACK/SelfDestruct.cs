using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float lifeTime = 0.2f; // ����Ʈ�� ���ӵ� �ð� (�� ����)

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}