using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    private float originalMoveSpeed;
    private Coroutine slowCoroutine;
    private bool isOriginalSpeedSet = false; // <<< 추가: 초기 속도 저장 여부 확인 플래그

    private int currentTargetIndex = 0;
    private List<Vector3> pathPoints = new List<Vector3>();

    private MonsterScript monsterScript;
    private BossMonsterScript bossMonsterScript;

    void Awake()
    {
        monsterScript = GetComponent<MonsterScript>();
        bossMonsterScript = GetComponent<BossMonsterScript>();
        // <<< 여기서 originalMoveSpeed 저장 로직 제거! >>>
        // originalMoveSpeed = moveSpeed; 

        if (monsterScript == null && bossMonsterScript == null)
        {
            Debug.LogWarning($"[{gameObject.name}] MonsterScript 또는 BossMonsterScript 컴포넌트를 찾을 수 없습니다.");
        }
    }

    private void Start()
    {
        // ... (내용 그대로) ...
    }

    public void InitializePath(List<Transform> waypoints)
    {
        // <<< 여기에 초기 속도 저장 로직 추가 >>>
        if (!isOriginalSpeedSet)
        {
            originalMoveSpeed = moveSpeed; // MonsterSpawner에서 설정된 최종 속도를 저장
            isOriginalSpeedSet = true;
            Debug.Log($"[{gameObject.name}]의 실제 초기 속도 저장됨: {originalMoveSpeed}");
        }

        pathPoints.Clear(); // 기존 경로 초기화

        // <<< 제가 실수로 누락했던, 경로를 실제로 추가하는 부분! >>>
        if (waypoints != null && waypoints.Count > 0)
        {
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    pathPoints.Add(waypoint.position);
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] 경로 설정 중 null인 웨이포인트가 감지되었습니다.");
                }
            }
        }
        else // waypoints가 null이거나 비어있으면
        {
            Debug.LogError($"[{gameObject.name}] InitializePath: 유효한 웨이포인트 목록을 받지 못했습니다!");
            return; // 경로가 없으면 더 이상 진행하지 않음
        }
        // <<< 여기까지 누락된 부분 >>>


        // 경로가 제대로 설정되었는지 확인
        if (pathPoints.Count > 0)
        {
            currentTargetIndex = 0;
            transform.position = pathPoints[0]; // 몬스터를 경로의 첫 번째 지점으로 즉시 이동
            Debug.Log($"[{gameObject.name}] 경로 초기화 완료. {pathPoints.Count}개의 웨이포인트. 첫 위치: {transform.position}");

            // 경로 설정 후, 초기 이동 방향에 따른 스프라이트 설정
            if (pathPoints.Count > 1)
            {
                Vector2 initialDirection = ((Vector2)pathPoints[1] - (Vector2)pathPoints[0]).normalized;
                UpdateSpriteBasedOnDirection(initialDirection);
            }
            else
            {
                UpdateSpriteBasedOnDirection(Vector2.down);
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 유효한 웨이포인트가 없어 경로를 설정하지 못했습니다.");
        }
    }

    // ... (Update, UpdateSpriteBasedOnDirection, ApplySlow, SlowEffectCoroutine 함수는 그대로) ...
    private void Update()
    {
        if (pathPoints.Count == 0)
        {
            return;
        }

        if (currentTargetIndex >= pathPoints.Count)
        {
            currentTargetIndex = 0;
            if (pathPoints.Count == 0) return;
            if (currentTargetIndex < pathPoints.Count)
            {
                Vector2 nextDirection = ((Vector2)pathPoints[currentTargetIndex] - (Vector2)transform.position).normalized;
                if (nextDirection.sqrMagnitude > 0.01f) UpdateSpriteBasedOnDirection(nextDirection);
            }
        }

        Vector3 targetPosition = pathPoints[currentTargetIndex];
        Vector3 currentPosition = transform.position;

        Vector2 moveDirection = ((Vector2)targetPosition - (Vector2)currentPosition).normalized;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            UpdateSpriteBasedOnDirection(moveDirection);
        }

        transform.position = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(currentPosition, targetPosition) <= 0.05f)
        {
            currentTargetIndex++;
        }
    }

    private void UpdateSpriteBasedOnDirection(Vector2 direction)
    {
        if (monsterScript != null)
        {
            monsterScript.UpdateSpriteDirection(direction);
        }
        else if (bossMonsterScript != null)
        {
            bossMonsterScript.UpdateSpriteDirection(direction);
        }
    }

    public void ApplySlow(float slowPercentage, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        slowCoroutine = StartCoroutine(SlowEffectCoroutine(slowPercentage, duration));
    }
    private IEnumerator SlowEffectCoroutine(float slowPercentage, float duration)
    {
        moveSpeed = originalMoveSpeed * (1f - slowPercentage);
        Debug.Log($"[{gameObject.name}] 슬로우 적용! 속도: {originalMoveSpeed} -> {moveSpeed}");

        yield return new WaitForSeconds(duration);

        moveSpeed = originalMoveSpeed;
        Debug.Log($"[{gameObject.name}] 슬로우 효과 종료. 속도 복구: {moveSpeed}");
        slowCoroutine = null;
    }
}