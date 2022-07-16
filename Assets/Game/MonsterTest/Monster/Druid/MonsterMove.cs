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
    private Vector3 point = Vector3.zero;


    //List<Collider> hitTargetList = new List<Collider>();
    RaycastHit hit;

    [SerializeField] bool DebugMode = true;
    [Range(0f, 360f)][SerializeField] float ViewAngle = 0f;
    [SerializeField] float ViewRadius = 1f;
    [SerializeField] LayerMask TargetMask;
    [SerializeField] LayerMask ObstacleMask;

    WaitForSecondsRealtime waitTime;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        monsterRigidbody = GetComponent<Rigidbody>();

        behaviorState = MonsterBehaviorState.SerchingTarget;
        StartCoroutine(behaviorState.ToString());
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
                        if (CreateRandomDestination(moveDestination.position, 20f, out point))
                        {
                            moveDestination.position = point;

                            state &= ~State.Walk;
                            animator.SetBool("Walk", state.HasFlag(State.Walk));

                            //yield return StartCoroutine(WaitForAnimation("Stretch", 1f));
                            //Debug.LogFormat("목표도착완료2");

                            yield return new WaitForSecondsRealtime(Random.Range(0.3f, 1.3f));
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
                            transform.Translate(transform.forward * moveSpeed * Time.deltaTime, Space.World);
                    }
                    break;

                default:
                    break;
            }

            if (FindTarget())       // target을 찾으면 행동상태변경
            {
                yield return new WaitForSecondsRealtime(0.1f);
                ChangeState(MonsterBehaviorState.ChasingTarget);
            }

            yield return null;
        }
    }
    Quaternion SetDestinationDirection()                            // 목적지 방향을 보게 하는 함수
    {        
        Vector3 dir = moveDestination.position - transform.position;
        dir = new Vector3(dir.x, 0, dir.z);                         // 목표지점은 네브메쉬(땅)이니깐 Y축을 0으로 함으로써 바닥을 보지 않도록 해줌.
        targetRotation = Quaternion.LookRotation(dir.normalized);   // 내가 바라볼 방향
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation); // 내 방향과 목표 방향의 각도차이

        Vector3 targetDir = moveDestination.position - transform.position;      // 타겟 방향으로 향하는 벡터를 구하기
        Vector3 crossVec = Vector3.Cross(targetDir, this.transform.forward);    // foward와 외적
        float dot = Vector3.Dot(crossVec, Vector3.up);                          // 위쪽과 내적
        if (dot > 0) // 왼쪽
        {
            if (angleDifference > 60)
            {
                animator.SetInteger("Rotation", -2);
                StartCoroutine(Rotation(-angleDifference));
            }
            else
            {
                animator.SetInteger("Rotation", -1);
                StartCoroutine(Rotation(-angleDifference));
            }
        }
        else if (dot < 0) // 오른쪽
        {
            if (angleDifference > 60)
            {
                animator.SetInteger("Rotation", 2);
                StartCoroutine(Rotation(angleDifference));
            }
            else
            {
                animator.SetInteger("Rotation", 1);
                StartCoroutine(Rotation(angleDifference));
            }
        }
        else // 가운데 (0일때)
        {
        }

        return targetRotation;
    }
    IEnumerator Rotation(float targetAngle)
    {
        yield return new WaitForSecondsRealtime(0.1f);  // 애니메이션 전환시간 때문에 0.1f의 딜레이를 줌
        float time = animator.GetCurrentAnimatorStateInfo(0).length;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            time = 0.93f - 0.30f;
        else
            time = time * 0.60f;                        // 현재 실행된 회전 애니메이션의 60%시간동안 코루틴 실행(회전하는 모습이 어색하지 않도록)

        for (int i = 0; i < 50; i++)                    // 50회에 걸쳐 회전하기
        {
            transform.Rotate(new Vector3(0, (targetAngle / 50), 0), Space.Self);
            yield return new WaitForSecondsRealtime(time / 50);
        }
    }
    bool CreateRandomDestination(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 40; i++)
        {
            Vector3 CreateRandomDestination = center + Random.insideUnitSphere * range;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(CreateRandomDestination, out hit, 1.5f, NavMesh.AllAreas) && Vector3.Distance(transform.position, CreateRandomDestination) > 6f)
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }
    Vector3 AngleToDir(float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radian), 0f, Mathf.Cos(radian));
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

    #endregion SerchingTargetState

    #region ChasingTartget
    IEnumerator ChasingTarget()
    {
        NavMeshHit hit;

        while (true)
        {
            if (!NavMesh.SamplePosition(target.transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                target = null;
                yield return new WaitForSecondsRealtime(0.1f);
                ChangeState(MonsterBehaviorState.SerchingTarget);
            }
            Debug.Log("적인지 :" + behaviorState.ToString());
            yield return null;
        }
    }

    #endregion ChasingTartget
    public void ChangeState(MonsterBehaviorState newState)
    {
        StopCoroutine(behaviorState.ToString());              // 기존 실행중인 코루틴 정지
        behaviorState = newState;
        Debug.Log("ChangeState! 바뀔 상태 : " + newState);
        StartCoroutine(behaviorState.ToString());             // 변경된 상태로 코루틴 시작
    }
    IEnumerator WaitForAnimation(string name, float ratio, int layer = -1)
    {
        //animator.Play(name, layer, 0);  // layer에 name이름을 가진 애니메이션을 0초부터 시작해라
        animator.SetTrigger(name);
        //Debug.Log("스트렛치 실행성공!, " + animator.GetCurrentAnimatorStateInfo(0).normalizedTime);

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < ratio && animator.GetCurrentAnimatorStateInfo(0).IsName(name))
        {
            //Debug.Log("스트렛치 실행중 : " + animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
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


#region 테스트 코드
/*
    if (rotate)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
 */
/*
    Quaternion SetDestinationDirection()
    {        
        Vector3 dir = moveDestination.position - transform.position;
        dir = new Vector3(dir.x, 0, dir.z);                         // 목표지점은 네브메쉬(땅)이니깐 Y축을 0으로 함으로써 바닥을 보지 않도록 해줌.
        targetRotation = Quaternion.LookRotation(dir.normalized);   // 내가 바라볼 방향
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation); // 내 방향과 목표 방향의 각도차이

        Vector3 targetDir = moveDestination.position - transform.position;      // 타겟 방향으로 향하는 벡터를 구하기
        Vector3 crossVec = Vector3.Cross(targetDir, this.transform.forward);    // foward와 외적
        float dot = Vector3.Dot(crossVec, Vector3.up);                          // 위쪽과 내적
        if (dot > 0) // 왼쪽
        {
            if (angleDifference > 60)
            {
                animator.SetInteger("Rotation", -2);
                StartCoroutine(Rotation(-angleDifference));
            }
            else
            {
                animator.SetInteger("Rotation", -1);
                StartCoroutine(Rotation(-angleDifference));
                rotationSpeed = 1.2f;
                //rotate = true;
            }
        }
        else if (dot < 0) // 오른쪽
        {
            if (angleDifference > 60)
            {
                animator.SetInteger("Rotation", 2);
                StartCoroutine(Rotation(angleDifference));
            }
            else
            {
                animator.SetInteger("Rotation", 1);
                StartCoroutine(Rotation(angleDifference));
                rotationSpeed = 1.2f;
                //rotate = true;
            }
        }
        else // 가운데 (0일때)
        {
            Debug.Log("가운데");
        }

        return targetRotation;
    }
 */
/* 애니메이션 테스트
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("걷기 상태 : " + state.HasFlag(State.Walk));
            animator.SetTrigger("Stretch");
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            //animator.SetTrigger("Test");
            
        }
        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            animator.SetInteger("Rotation", -2);
            StartCoroutine(Rotation(-90));
        }
        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            animator.SetInteger("Rotation", 2);
            StartCoroutine(Rotation(90));
        }
        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            animator.SetInteger("Rotation", -1);
        }
        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            animator.SetInteger("Rotation", 1);
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            animator.SetInteger("Rotation", 3);
            StartCoroutine(Rotation(90));
        }
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            animator.SetInteger("Rotation", -3);
            StartCoroutine(Rotation(90));
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            animator.SetInteger("Rotation", 4);
            StartCoroutine(Rotation(90));
            //Debug.Log("애니메이션 재생 시간 : " + animator.GetCurrentAnimatorStateInfo(0).length);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            animator.SetInteger("Rotation", -4);
            StartCoroutine(Rotation(-60));
            //Debug.Log("애니메이션 재생 시간 : " + animator.GetCurrentAnimatorStateInfo(0).length);
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            //animator.SetInteger("Rotation", 5);
            Debug.Log("애니메이션 재생 시간 : " + animator.GetCurrentAnimatorStateInfo(0).length);
        }
 */
#endregion