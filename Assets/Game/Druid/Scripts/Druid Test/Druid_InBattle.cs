using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Druid_InBattle : DruidAction
{
    private enum MONSTER_ATTACK_TYPE
    {
        Stomp,
        Swipe,
        JumpAttack,
        Roar,
        Throw,
        Run
    }
    [Header("전투 관련"), Space(10)]
    [SerializeField] private GameObject throwingObject;
    //[SerializeField] private MonsterHitablePart[] hitablePart;        // 피격부위 정보
    [SerializeField] private DruidAttackablePart[] attackablePart;      // 공격부위 정보
    [SerializeField] private Transform rightHand;
    [SerializeField] private GameObject attackParticle;

    float moveSpeed = 3;
    bool attackCoolTime = false;
    bool startRoar = true;
    MONSTER_ATTACK_TYPE attackType;
    private new void Awake()
    {
        base.Awake();
    }

    void Update()
    {
        if (druidStatus.behaviorState != MONSTER_BEHAVIOR_STATE.InBattle) return;     // InBattle 상태가 아닐경우 update 실행X
        
        //if (CheckTargetIsInArea())
        //    ChangeState(MONSTER_BEHAVIOR_STATE.SerchingTarget);

        if(Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log( "드루이드 상태 : "+druidStatus.state);
        }
    }

    float targetOutTime = 0;
    bool CheckTargetIsInArea()
    {
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(druidStatus.target.transform.position, out hit, 2.0f, NavMesh.AllAreas))    // 타겟이 보스영역(네브메쉬) 밖으로 나갔을때 
        {
            targetOutTime += Time.deltaTime;

            if (targetOutTime > 3f)     // 3초이상 밖에 있을지 타겟해제
            {
                Debug.Log("타겟이 영역밖으로 나간 시간 : " + targetOutTime);
                druidStatus.target = null;
                return true;
            }
        }
        else    // 캐릭터가 보스 영역 안에 있을 경우
        {
            targetOutTime = 0;
        }
        return false;
    }
    public override void Init()
    {
        Debug.Log("전투중 Init");
        druidStatus.state = MONSTER_STATE.Idle;
        animator.SetInteger("Rotation", 0);
        animator.SetBool("Walk", druidStatus.state.HasFlag(MONSTER_STATE.Walk));

        attackCoolTime = false;
        animator.SetFloat("Breath Speed", 3f);
        StartCoroutine(CoolTime(Random.Range(2f, 4f)));   // 공격 쿨타임 돌기
        
        StartCoroutine(druidStatus.behaviorState.ToString());
        //if (rotationCoroutine != null)        // 이전에는 SerchingTartget에서 회전중일때의 코루틴을 멈추기 위해 썻었음
        //    StopCoroutine(rotationCoroutine);


    }
    //IEnumerator rotationCoroutine;
    IEnumerator InBattle()
    {
        Debug.Log("InBattle 실행");
        float targetDistance;
        float randomValue;

        if (startRoar)
        {
            Debug.Log("startRoar");
            SetDestinationDirection(druidStatus.target.transform);
            yield return StartCoroutine(rotationCoroutine);
            yield return StartCoroutine(WaitForAnimation("Roar", 1f));  // 표효하기
        }

        startRoar = true;

        while (true)
        {
            targetDistance = Vector3.Distance(transform.position, druidStatus.target.transform.position);

            if (attackCoolTime && (druidStatus.state == MONSTER_STATE.Idle || druidStatus.state == MONSTER_STATE.Walk))
            {
                if (targetDistance < 25)
                {
                    druidStatus.state &= ~MONSTER_STATE.Walk;
                    animator.SetBool("Walk", false);

                    druidStatus.state |= MONSTER_STATE.Rotation | MONSTER_STATE.Attack;
                    SetDestinationDirection(druidStatus.target.transform, 18);                  // 타겟방향찾기(각도, 몬스터기준 좌/우)
                    if (rotationCoroutine != null)
                        yield return StartCoroutine(rotationCoroutine);             // 타겟방향으로 회전

                    randomValue = Random.value;
                    attackType = DecideAttackType(targetDistance, randomValue);     // 거리에 따라 랜덤으로 공격패턴
                }
                else                            // 타겟과의 거리가 25f 이상일때 너무 멀어서 추격
                {
                    druidStatus.state |= MONSTER_STATE.Walk;
                }
            }
            else if (!attackCoolTime)            // 스킬 쿨타임일때 추격
            {
                druidStatus.state |= MONSTER_STATE.Walk;
                // yield return StartCoroutine(RunToTartget());
            }



            if (druidStatus.state == MONSTER_STATE.Attack)  // 공격실행
            {
                switch (attackType)
                {
                    case MONSTER_ATTACK_TYPE.Stomp:
                        yield return StartCoroutine(WaitForAnimation("Stomp", 1f));
                        break;

                    case MONSTER_ATTACK_TYPE.Swipe:
                        yield return StartCoroutine(WaitForAnimation("Swipe", 1f));
                        break;

                    case MONSTER_ATTACK_TYPE.Roar:
                        yield return StartCoroutine(WaitForAnimation("Roar", 1f));
                        break;

                    case MONSTER_ATTACK_TYPE.JumpAttack:
                        yield return StartCoroutine(WaitForAnimation("JumpAttack", 1f));
                        break;

                    case MONSTER_ATTACK_TYPE.Throw:
                        yield return StartCoroutine(WaitForAnimation("Throw", 1f));
                        break;

                    case MONSTER_ATTACK_TYPE.Run:
                        momentTargetPosition = druidStatus.target.transform.position;   // 달려갈 위치
                        yield return StartCoroutine(RunToTartget());
                        break;
                }
                attackCoolTime = false;
                druidStatus.state &= ~MONSTER_STATE.Attack;

                Debug.Log(attackType + " 후 상태 : " + druidStatus.state);

                randomValue = Random.value;
                if (attackType == MONSTER_ATTACK_TYPE.Run)
                {
                    StartCoroutine(CoolTime(Random.Range(0.5f, 1.5f)));
                    yield return new WaitForSecondsRealtime(Random.Range(0.2f, 0.6f));
                }
                else
                {
                    StartCoroutine(CoolTime(Random.Range(5f, 9f)));
                    yield return new WaitForSecondsRealtime(Random.Range(0.2f, 0.6f));

                    if (randomValue < 0.3f)
                        yield return StartCoroutine(WaitForAnimation("Flexing Muscle", 1f));
                    else if (randomValue < 0.4f)
                        yield return StartCoroutine(WaitForAnimation("Stretch", 1f));
                    else if(randomValue < 0.7f)
                    {
                        Debug.Log("공격 후 타겟방향보기");
                        SetDestinationDirection(druidStatus.target.transform);
                        if (rotationCoroutine != null)
                            yield return StartCoroutine(rotationCoroutine);
                    }
                    else
                        yield return new WaitForSecondsRealtime(Random.Range(0.5f, 1.1f));
                }
            }
   

            if (druidStatus.state.HasFlag(MONSTER_STATE.Walk))   // 걷기 상태 ON이면
            {
                if (targetDistance > 7f)    // 타겟과 거리가 7f 초과일때 추적하기
                {
                    ChaseTarget();
                }
                else                        // 타겟과 거리가 7f 이하일때 걷기 X
                {
                    druidStatus.state &= ~MONSTER_STATE.Walk;
                    animator.SetBool("Walk", false);
                    attackCoolTime = true;  // 가까워 졌으니 공격 ㄱㄱ
                                            // 코루틴 멈추기?
                }
            }

            yield return null;
        }
    }
    MONSTER_ATTACK_TYPE DecideAttackType(float targetDistance, float randomValue)
    {
        if (attackType == MONSTER_ATTACK_TYPE.Run)
            randomValue += 0.2f;

        if (targetDistance < 6.5f)              // 거리가 6.5미만 일때 
        {
            if (randomValue <= 0.7f)            // 70% 확률로 밟기공격
                return MONSTER_ATTACK_TYPE.Stomp;
            else                                // 30% 확률로 손휘두르기공격
                return MONSTER_ATTACK_TYPE.Swipe;

        }
        else if (targetDistance < 8.5f)         // 거리가 8.5미만 일때 
        {
            if (randomValue <= 0.65f)
                return MONSTER_ATTACK_TYPE.Swipe;
            else
                return MONSTER_ATTACK_TYPE.JumpAttack;
        }
        else if (targetDistance < 16)           // 거리가 16미만 일때 
        {
            if (randomValue <= 0.4f)
                return MONSTER_ATTACK_TYPE.JumpAttack;
            else if (randomValue <= 0.7f)
                return MONSTER_ATTACK_TYPE.Roar;
            else
                return MONSTER_ATTACK_TYPE.Run;
        }
        else                                    // 거리가 16~25 일때 
        {
            if (randomValue <= 0.3f)
                return MONSTER_ATTACK_TYPE.Roar;
            else if (randomValue <= 0.6f)
                return MONSTER_ATTACK_TYPE.Throw;
            else
                return MONSTER_ATTACK_TYPE.Run;
        }
    }
    void ChaseTarget()
    {
        animator.SetBool("Walk", true);
        //transform.LookAt(target.transform); // 없으면 어색할까 ? 
        Vector3 targetDir = (druidStatus.target.transform.position - transform.position).normalized;
        targetDir = new Vector3(targetDir.x, 0, targetDir.z);                           // 목표지점은 네브메쉬(땅)이니깐 Y축을 0으로 함으로써 바닥을 보지 않도록 해줌.
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir.normalized), 2f * Time.deltaTime);

        transform.Translate(targetDir * moveSpeed * Time.deltaTime, Space.World);       // 타겟쪽으로 이동
    }
    IEnumerator RunToTartget()
    {
        animator.SetBool("Run", true);
        //momentTargetPosition = new Vector3(momentTargetPosition.x, 0, momentTargetPosition.z);
        //transform.LookAt(momentTargetPosition); // 없으면 어색할까?

        Vector3 targetDir = (momentTargetPosition - transform.position).normalized;
        targetDir = new Vector3(targetDir.x, 0, targetDir.z);                         // 목표지점은 네브메쉬(땅)이니깐 Y축을 0으로 함으로써 바닥을 보지 않도록 해줌.

        while (Vector3.Distance(momentTargetPosition, transform.position) >= 7f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir.normalized), 2f * Time.deltaTime);
            transform.Translate(targetDir * 9 * Time.deltaTime, Space.World); // 타겟쪽으로 이동
            yield return null;
        }

        animator.SetBool("Run", false);
        Debug.Log("달리기 끝");
        yield return null;
    }

    IEnumerator WaitForAnimation(string name, float exitRatio, int layer = -1)
    {
        float playTime = 0;
        //animator.Play(name, layer, 0);  // layer에 name이름을 가진 애니메이션을 0초부터 시작해라
        animator.SetTrigger(name);

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(name))   // 애니메이션이 전환될때까지 대기
            yield return null;

        float exitTime = animator.GetCurrentAnimatorStateInfo(0).length * exitRatio;
        while (playTime < exitTime)
        {
            playTime += Time.deltaTime;
            yield return null;
        }

        
        if (name == "Stagger")  // 경직상태 
        {
            yield return StartCoroutine(WaitForAnimation("Stand Up(Front Up)", 1));
            Debug.Log("경직상태 해제");
            druidStatus.state &= ~MONSTER_STATE.Stagger;
            startRoar = false;

            //ChangeState(MONSTER_BEHAVIOR_STATE.InBattle);
            Init();
        }

        yield return null;

    }
    IEnumerator CoolTime(float cool)
    {
        while (cool > 0)
        {
            cool -= Time.deltaTime;
            if (attackCoolTime)         // 타겟과의 거리가 가까워져서 attackCoolTime이 true가 됐으면 코루틴 멈추기
                yield break;

            yield return null;
        }

        attackCoolTime = true;
    }
    #region 애니메이션 이벤트
    Projectile projectile;
    Vector3 momentTargetPosition;       // 그 행동을 할때 타겟의 위치
    void CreateRock()           // 돌 생성
    {
        projectile = Instantiate(throwingObject, rightHand.position + rightHand.right, transform.rotation, rightHand).GetComponent<Rock>();
    }
    void ThrowRock()            // 돌 던지기
    {
        momentTargetPosition = druidStatus.target.transform.position;       // 돌을 주을때 타겟의 위치로 돌을 던지기 위함
        projectile.Init(momentTargetPosition, 15, 35);                      // 매개변수 (타겟의 위치, 공격력, 속도)
    }
    void Roar()
    {
        Debug.Log("Roar실행 " + druidStatus.state);
        if (druidStatus.state.HasFlag(MONSTER_STATE.Attack))
        {
            Debug.Log("Roar 불뿜기");
            attackParticle.SetActive(true);
        }
    }
    void AttackStart(MONSTER_ATTACK_TYPE attackType)
    {
        switch (attackType)
        {
            case MONSTER_ATTACK_TYPE.Stomp:
                attackablePart[(int)attackType].Attack(true);
                break;
            case MONSTER_ATTACK_TYPE.Swipe:
                attackablePart[(int)attackType].Attack(true);
                break;
            case MONSTER_ATTACK_TYPE.JumpAttack:
                attackablePart[(int)attackType].Attack(true);
                break;
        }

    }
    void AttackEnd(MONSTER_ATTACK_TYPE attackType)
    {
        switch (attackType)
        {
            case MONSTER_ATTACK_TYPE.Stomp:
                attackablePart[(int)attackType].Attack(false);  // 왼발
                break;
            case MONSTER_ATTACK_TYPE.Swipe:
                attackablePart[(int)attackType].Attack(false);  // 왼손
                break;
            case MONSTER_ATTACK_TYPE.JumpAttack:
                attackablePart[(int)attackType].Attack(false);  // 오른발
                break;
        }

    }
    #endregion 애니메이션 이벤트
    public void StartStaggerState()
    {
        druidStatus.state = MONSTER_STATE.Stagger;
        StopAllCoroutines();
        Debug.Log("StartStaggerState");
        StartCoroutine(WaitForAnimation("Stagger", 1f));
    }
}

/*
    void SetDestinationDirection(Transform targetPos, float angleLimit = 0f)    // 목적지 방향을 보게 하는 함수
    {
        // 몬스터가 얼마나 회전할지 각도 구하기
        Vector3 dir = targetPos.position - transform.position;
        dir = new Vector3(dir.x, 0, dir.z);                         // 목표지점은 네브메쉬(땅)이니깐 Y축을 0으로 함으로써 바닥을 보지 않도록 해줌.
        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);   // 내가 바라볼 방향
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);   // 내 방향과 목표 방향의 각도차이

        if (angleDifference <= angleLimit)                          // angleLimit 이하의 각도는 돌지 않는다.
        {
            druidStatus.state &= ~MONSTER_STATE.Rotation;
            rotationCoroutine = null;
            return;
        }

        // 몬스터가 어떤 방향으로 회전할지 구하기 
        Vector3 targetDir = targetPos.position - transform.position;                    // 타겟 방향으로 향하는 벡터를 구하기
        Vector3 crossVec = Vector3.Cross(targetDir, this.transform.forward);            // foward와 외적
        float dot = Vector3.Dot(crossVec, Vector3.up);                                  // 위방향과 내적
        if (dot > 0) // 왼쪽
        {
            if (angleDifference > 60)
            {
                animator.SetInteger("Rotation", -2);
                rotationCoroutine = Rotation("Turn Left", -angleDifference);
            }
            else
            {
                animator.SetInteger("Rotation", -1);
                rotationCoroutine = Rotation("Turn Left Slow", -angleDifference);
            }
        }
        else if (dot < 0) // 오른쪽
        {
            if (angleDifference >= 60)
            {
                animator.SetInteger("Rotation", 2);
                rotationCoroutine = Rotation("Turn Right", angleDifference);
            }
            else
            {
                animator.SetInteger("Rotation", 1);
                rotationCoroutine = Rotation("Turn Right Slow", angleDifference);
            }
        }
        else // 가운데 (0일때)
        {
        }
    }
    IEnumerator Rotation(string name, float targetAngle)
    {

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(name))
            yield return null;

        float playTime = 0f;
        float exitTime = animator.GetCurrentAnimatorStateInfo(0).length;

        if (targetAngle < 60)
            exitTime = exitTime * 0.90f;    // 현재 실행된 회전 애니메이션의 90%시간동안 코루틴 실행(회전하는 모습이 어색하지 않도록)


        while (playTime < exitTime)
        {
            playTime += 0.02f;
            transform.Rotate(new Vector3(0, (targetAngle / (exitTime / 0.02f)), 0), Space.Self);

            yield return new WaitForFixedUpdate();
        }
        
        druidStatus.state &= ~MONSTER_STATE.Rotation;
        animator.SetInteger("Rotation", 0);
    }
 */