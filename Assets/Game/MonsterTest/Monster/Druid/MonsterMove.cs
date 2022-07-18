using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum MonsterBehaviorState
{
    SerchingTarget,
    ChasingTarget,
    InBattle
}

public class MonsterMove : MonoBehaviour
{
    private enum State
    {
        Idle = 1 << 0,
        Walk = 1 << 1,
        Rotation = 1 << 2,

        All = Walk | Rotation,
    }
    private MonsterBehaviorState behaviorState;
    private State state = 0;

    private Collider target;
    private Animator animator;
    private Rigidbody monsterRigidbody;
    
    [SerializeField] private Transform moveDestination;
    [SerializeField] private float moveSpeed = 2;
    
    private bool randomAnimationStart = false;

    [SerializeField] private bool DebugMode = true;
    [Range(0f, 360f)]
    [SerializeField] private float ViewAngle = 0f;
    [SerializeField] private float ViewRadius = 1f;
    [SerializeField] private LayerMask TargetMask;
    [SerializeField] private LayerMask ObstacleMask;

    void Start()
    {
        animator = GetComponent<Animator>();
        monsterRigidbody = GetComponent<Rigidbody>();
        
        StartCoroutine(RandomAnimation(Random.Range(10f, 20f)));
        behaviorState = MonsterBehaviorState.SerchingTarget;
        StartCoroutine(behaviorState.ToString());
    }
    private void Update()
    {
        if (behaviorState == MonsterBehaviorState.SerchingTarget && target == null)
        {
            if (FindTarget())
                ChangeState(MonsterBehaviorState.ChasingTarget);
        }
    }

    #region SerchingTargetState
    private enum SerchingTargetState { Rotation, Walk };
    SerchingTargetState serchingTargetState = SerchingTargetState.Rotation;
    Quaternion targetRotation;
    IEnumerator SerchingTarget()
    {
        while (true)
        {
            switch (serchingTargetState)
            {
                case SerchingTargetState.Rotation:
                    if (!state.HasFlag(State.Rotation))         // Rotation상태로 만들어줌 (상태가 바뀌었을때 한번만 실행되도록) 
                    {
                        state |= State.Rotation;
                        targetRotation = SetDestinationDirection();
                    }

                    if (Quaternion.Angle(transform.rotation, targetRotation) < 4)   // 두 각의 차이가 일정(4도) 이하일때 목표를 보고 있는것으로 판단
                    {
                        serchingTargetState = SerchingTargetState.Walk;
                        state &= ~State.Rotation;
                        animator.SetInteger("Rotation", 0);
                    }
                    break;

                case SerchingTargetState.Walk:
                    if (Vector3.Distance(transform.position, moveDestination.position) < 4f)    // 목표와 거리가 4f 이내
                    {
                        monsterRigidbody.velocity = Vector3.zero;
                        if (CreateRandomDestination(moveDestination.position, 20f))
                        {
                            state &= ~State.Walk;
                            animator.SetBool("Walk", state.HasFlag(State.Walk));

                            yield return new WaitForSecondsRealtime(Random.Range(0.3f, 1.3f));  // 도착했을때 잠깐 대기시간 부여
                            serchingTargetState = SerchingTargetState.Rotation;
                        }
                    }
                    else
                    {
                        if (!state.HasFlag(State.Walk))
                        {
                            state |= State.Walk;
                            animator.SetBool("Walk", state.HasFlag(State.Walk));
                        }

                        if (state.HasFlag(State.Walk))
                        {
                            transform.Translate(transform.forward * moveSpeed * Time.deltaTime, Space.World);
                        }

                        if (randomAnimationStart)
                        {
                            int random = Random.Range(0, 2);
                            if (random == 0)
                                yield return StartCoroutine(WaitForAnimation("Stretch", 1f));
                            else if (random == 1)
                                yield return StartCoroutine(WaitForAnimation("Look Around", 1f));
                            else
                                Debug.Log("행동안함");

                            randomAnimationStart = false;                            // 실행되고 나서 false로 바꿔주기
                            StartCoroutine(RandomAnimation(Random.Range(10f, 20f)));
                        }

                    }
                    break;

                default:
                    break;
            }

            yield return null;
        }
    }
    Quaternion SetDestinationDirection()                            // 목적지 방향을 보게 하는 함수
    {        
        // 몬스터가 얼마나 회전할지 각도 구하기
        Vector3 dir = moveDestination.position - transform.position;
        dir = new Vector3(dir.x, 0, dir.z);                         // 목표지점은 네브메쉬(땅)이니깐 Y축을 0으로 함으로써 바닥을 보지 않도록 해줌.
        targetRotation = Quaternion.LookRotation(dir.normalized);   // 내가 바라볼 방향
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);   // 내 방향과 목표 방향의 각도차이

        // 몬스터가 어떤 방향으로 회전할지 구하기 
        Vector3 targetDir = moveDestination.position - transform.position;              // 타겟 방향으로 향하는 벡터를 구하기
        Vector3 crossVec = Vector3.Cross(targetDir, this.transform.forward);            // foward와 외적
        float dot = Vector3.Dot(crossVec, Vector3.up);                                  // 위방향과 내적
        if (dot > 0) // 왼쪽
        {
            if (angleDifference > 60)
            {
                animator.SetInteger("Rotation", -2);
                StartCoroutine(Rotation("Turn Left", -angleDifference));
            }
            else
            {
                animator.SetInteger("Rotation", -1);
                StartCoroutine(Rotation("Turn Left Slow", -angleDifference));
            }
        }
        else if (dot < 0) // 오른쪽
        {
            if (angleDifference > 60)
            {
                animator.SetInteger("Rotation", 2);
                StartCoroutine(Rotation("Turn Right", angleDifference));
            }
            else
            {
                animator.SetInteger("Rotation", 1);
                StartCoroutine(Rotation("Turn Right Slow",angleDifference));
            }
        }
        else // 가운데 (0일때)
        {
        }

        return targetRotation;
    }
    WaitForSecondsRealtime rotationDelay = new WaitForSecondsRealtime(0.01f);
    IEnumerator Rotation(string name, float targetAngle)
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(name))
            yield return null;

        float playTime = 0f;
        float exitTime = animator.GetCurrentAnimatorStateInfo(0).length;
        
        exitTime = exitTime * 0.30f;                        // 현재 실행된 회전 애니메이션의 30%시간동안 코루틴 실행(회전하는 모습이 어색하지 않도록)
        
        while (playTime < exitTime)
        {
            playTime += 0.01f;
            transform.Rotate(new Vector3(0, (targetAngle / (exitTime / 0.01f)), 0), Space.Self);
            yield return rotationDelay;
        }
    }
    /*
        IEnumerator Rotation(float targetAngle)
    {
        yield return new WaitForSecondsRealtime(0.05f);  // 애니메이션 전환시간때문
        float playTime = 0f;
        float time = animator.GetCurrentAnimatorStateInfo(0).length;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            Debug.Log("Idle임 : " + time);
            time = 0.93f - 0.30f;
        }
        else
        {
            Debug.Log("잘됨 : " + time);
            time = time * 0.60f;                        // 현재 실행된 회전 애니메이션의 60%시간동안 코루틴 실행(회전하는 모습이 어색하지 않도록)
        }

        for (int i = 0; i < 50; i++)                    // 50회에 걸쳐 회전하기
        {
            playTime += Time.deltaTime;
            Debug.Log("걸린시간 : " + playTime);
            transform.Rotate(new Vector3(0, (targetAngle / 50f), 0), Space.Self);
            yield return new WaitForSecondsRealtime(time / 50f);
        }
    }
     */
    bool CreateRandomDestination(Vector3 center, float range)
    {
        for (int i = 0; i < 40; i++)        // 조건을 만족하는 목적지가 나올때까지 시행 (최대 40회)
        {
            Vector3 CreateRandomDestination = center + Random.insideUnitSphere * range;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(CreateRandomDestination, out hit, 1.5f, NavMesh.AllAreas) && Vector3.Distance(transform.position, CreateRandomDestination) > 7f)
            {
                moveDestination.position = hit.position;
                return true;
            }
        }
        return false;
    }
    private bool FindTarget()
    {
        NavMeshHit hit;
        Collider[] targets = Physics.OverlapSphere(transform.position, ViewRadius, TargetMask);

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 targetPos = targets[i].transform.position;
            Vector3 targetDir = (targetPos - transform.position).normalized;
            float targetAngle = Mathf.Acos(Vector3.Dot(AngleToDir(transform.eulerAngles.y), targetDir)) * Mathf.Rad2Deg;
            if (targetAngle <= ViewAngle * 0.5f && !Physics.Raycast(transform.position, targetDir, ViewRadius, ObstacleMask))
            {
                if (NavMesh.SamplePosition(targets[i].transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    target = targets[i];
                    return true;
                }

                if (DebugMode) Debug.DrawLine(transform.position, targetPos + Vector3.up, Color.red);
            }
        }
        return false;
    }
    Vector3 AngleToDir(float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radian), 0f, Mathf.Cos(radian));
    }
    IEnumerator RandomAnimation(float cool)
    {
        while (cool > 0)
        {
            cool -= Time.deltaTime;
            yield return null;
        }
        randomAnimationStart = true;
    }
    #endregion SerchingTargetState

    #region ChasingTartget
    IEnumerator ChasingTarget()
    {
        // 몬스터 상태 초기화
        InitState();

        // 적을 발견시 Roar 실행
        //Vector3 dir = moveDestination.position - transform.position;
        //dir = new Vector3(dir.x, 0, dir.z);                         // 목표지점은 네브메쉬(땅)이니깐 Y축을 0으로 함으로써 바닥을 보지 않도록 해줌.
        //targetRotation = Quaternion.LookRotation(dir.normalized);   // 내가 바라볼 방향
        transform.LookAt(target.transform.position);
        yield return StartCoroutine(WaitForAnimation("Roar",1f));

        NavMeshHit hit;
        while (true)
        {
            if (!NavMesh.SamplePosition(target.transform.position, out hit, 2.0f, NavMesh.AllAreas))        // 타겟이 보스영역 밖으로 나갔을때 
            {
                target = null;
                yield return new WaitForSecondsRealtime(0.1f);
                ChangeState(MonsterBehaviorState.SerchingTarget);
            }
            //Debug.Log("적인지 :" + behaviorState.ToString());
            yield return null;
        }
    }
    void InitState()
    {
        state = State.Idle;
        animator.SetInteger("Rotation", 0);
        animator.SetBool("Walk", state.HasFlag(State.Walk));
    }

    #endregion ChasingTartget
    public void ChangeState(MonsterBehaviorState newState)
    {
        StopCoroutine(behaviorState.ToString());              // 기존 실행중인 코루틴 정지
        behaviorState = newState;
        Debug.Log("ChangeState! 바뀔 상태 : " + newState);
        StartCoroutine(behaviorState.ToString());             // 변경된 상태로 코루틴 시작
    }
    IEnumerator WaitForAnimation(string name, float exitRatio, int layer = -1)
    {
        float playTime =0;
        animator.Play(name, layer, 0);  // layer에 name이름을 가진 애니메이션을 0초부터 시작해라

        //animator.SetTrigger(name);
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(name))   // 애니메이션이 전환될때까지 대기
            yield return null;

        float exitTime = animator.GetCurrentAnimatorStateInfo(0).length * exitRatio;
        while (playTime < exitTime)
        {
            playTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }
    private void OnDrawGizmos()
    {
        if (!DebugMode) return;
        Vector3 myPos = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawWireSphere(myPos, ViewRadius);

        float lookingAngle = transform.eulerAngles.y;  //캐릭터가 바라보는 방향의 각도
        Vector3 rightDir = AngleToDir(transform.eulerAngles.y + ViewAngle * 0.5f);
        Vector3 leftDir = AngleToDir(transform.eulerAngles.y - ViewAngle * 0.5f);
        Vector3 lookDir = AngleToDir(lookingAngle);

        Debug.DrawRay(myPos, rightDir * ViewRadius, Color.blue);
        Debug.DrawRay(myPos, leftDir * ViewRadius, Color.blue);
        Debug.DrawRay(myPos, lookDir * ViewRadius, Color.green);
    }
}

