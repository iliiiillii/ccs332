using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float lifeTime = 0.2f; // 이펙트가 지속될 시간 (초 단위)

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}