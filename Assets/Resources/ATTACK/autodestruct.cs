using UnityEngine;

public class autodestruct : MonoBehaviour
{
    public float lifeTime = 0.5f; // ����Ʈ�� ���ӵ� �ð� (�� ����)

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}