using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Sheep : DamageableBase
{
    public enum State
    {
        Idle,
        Attracted,
        Moving,
        Hold,
        Attacking
    }

    [SerializeField] private NavMeshAgent navMeshAgent;

    [SerializeField] private float wolfSenseRadius;
    [SerializeField] private float wolfFollowRadius;

    [SerializeField] private Attackable attackable;

    private State currentState = State.Idle;
    private Vector3 moveTarget;

    private PlayerSpeaker attractionTarget;
    private Collider[] allSensedObjects = new Collider[10];

    public bool IsAttracted => attractionTarget != null;

    public override Alignment Alignment => Alignment.Ally;
    public State CurrentState
    {
        get => currentState;
        set => currentState = value;
    }

    public void Attract(PlayerSpeaker speaker)
    {
        attractionTarget = speaker;

        if (CurrentState == State.Idle)
            CurrentState = State.Attracted;
    }

    public void MoveTo(Vector3 target)
    {
        StartCoroutine(MoveToCoroutine(target));
    }
    private IEnumerator MoveToCoroutine(Vector3 target)
    {
        CurrentState = State.Moving;
        navMeshAgent.SetDestination(target);

        yield return new WaitUntil(() => CurrentState != State.Moving || navMeshAgent.isStopped);

        if (CurrentState == State.Moving)
            CurrentState = State.Hold;
    }

    private void FixedUpdate()
    {
        if (CurrentState is not (State.Moving or State.Hold))
        {
            attackable.Target = null;
            return;
        }
        
        if(attackable.Target != null && Vector3.Distance(attackable.Target.transform.position, transform.position) > wolfFollowRadius)
            attackable.Target = null;

        attackable.Target = Targeting.FindClosestTarget(transform.position, wolfSenseRadius, allSensedObjects, Alignment);
        CurrentState = attackable.Target != null ? State.Attacking : State.Hold;
        
        if(attackable.Target != null)
            navMeshAgent.SetDestination(attackable.Target.transform.position);
    }
    
    private void OnDrawGizmos()
    {
        var scentCol = Color.green;
        scentCol.a = 0.3f;
        Gizmos.color = scentCol;
        
        Gizmos.DrawSphere(transform.position, wolfSenseRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wolfFollowRadius);
    }
}
