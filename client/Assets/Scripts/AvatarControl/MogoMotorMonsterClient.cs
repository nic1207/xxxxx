// ģ����   :  MogoMotorMonsterClient
// ������   :  Ī׿��
// �������� :  2012-3-20
// ��    �� :  �������(���ﵥ����)

using UnityEngine;
using System.Collections;
using Mogo.Util;

[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
[RequireComponent(typeof(CharacterController))]
public class MogoMotorMonsterClient : MogoMotor
{
    private CharacterController characterController;
    private UnityEngine.AI.NavMeshPath path;
    private uint cornersIdx = 0;
    private float currentPathPointDistance = 0;

    public UnityEngine.AI.NavMeshAgent navAgent;

    // Use this for initialization
    void Start()
    {
        navAgent = transform.GetComponent<UnityEngine.AI.NavMeshAgent>();
        characterController = transform.GetComponent<CharacterController>();
        characterController.center = new Vector3(0, 1, 0);
        path = new UnityEngine.AI.NavMeshPath();
        GetComponent<Animator>().applyRootMotion = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMovingToTarget)
        {
            float distance = Vector3.Distance(path.corners[cornersIdx], transform.position);
            if (distance > currentPathPointDistance)
            {
                //���ƫ���̫Զ�����¼���
                MoveTo(navAgent.destination);
                return;
            }
            //����0.1����ʵ��Ӧ�ô������������ڲ�֪���ٶȣ�������λ�ƣ�
            if (cornersIdx < path.corners.Length - 1)
            {
                if (distance < 0.1)
                {
                    transform.position = path.corners[cornersIdx];
                    cornersIdx++;
                    //������һ��ת�ǵ�
                    moveDirection = (path.corners[cornersIdx] - transform.position);
                    currentPathPointDistance = Vector3.Distance(transform.position, path.corners[cornersIdx]);
                }
            }

            else if (distance < 0.1 + navAgent.stoppingDistance)
            {
                transform.position = path.corners[cornersIdx] - transform.forward * navAgent.stoppingDistance;
                StopNav();
                EventDispatcher.TriggerEvent(ON_MOVE_TO, transform.gameObject, targetToMoveTo);
            }


            ApplyRotation();
        }
        //����ƶ�������moveDirectionΪ׼������transform.forward
        Move();
    }

    public override void StopNav()
    {
        currentPathPointDistance = 0;
        cornersIdx = 0;
        speed = 0;
        isMovingToTarget = false;
        navAgent.Stop();
        navAgent.ResetPath();
    }

    public override void MoveTo(Vector3 v, bool needToAdjustPosY = true)
    {
        speed = 3f;
        isMovingToTarget = true;
        navAgent.speed = 0.0f;
        navAgent.SetDestination(v);
        path.ClearCorners();
        navAgent.CalculatePath(v, path);
        cornersIdx = 1;
        moveDirection = (path.corners[cornersIdx] - transform.position).normalized;
        currentPathPointDistance = Vector3.Distance(transform.position, path.corners[cornersIdx]);
    }

    public override void SetStopDistance(float distance)
    {
        navAgent.stoppingDistance = distance;
    }

    private void ApplyRotation()
    {
        float targetAngleY;
        if (moveDirection.x > 0)
        {
            targetAngleY = Vector3.Angle(moveDirection, Vector3.forward);
        }
        else
        {
            targetAngleY = Vector3.Angle(moveDirection, Vector3.back) + 180;
        }

        base.ApplyRotation(targetAngleY);
    }

    private void Move()
    {
        characterController.Move(speed * moveDirection * Time.deltaTime);
    }
}