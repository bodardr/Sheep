using System;
using UnityEngine;
using UnityEngine.AI;

public class Wolf : DamageableBase
{
    [SerializeField] private int scentTicks;
    [SerializeField] private float scentRadius;
    [SerializeField] private float maxFollowRadius;

    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Attackable attackable;

    private int currentScentTick;
    private Collider[] allSensedObjects = new Collider[10];

    public override Alignment Alignment => Alignment.Enemy;

    private void FixedUpdate()
    {
        if (Vector3.Distance(attackable.Target.transform.position, transform.position) > maxFollowRadius)
            attackable.Target = null;

        if (++currentScentTick < scentTicks)
            return;
        
        scentTicks = 0;
        attackable.Target = Targeting.FindClosestTarget(transform.position, scentRadius, allSensedObjects, Alignment);

        if (attackable.Target != null)
            navMeshAgent.SetDestination(attackable.Target.transform.position);
    }

    private void OnDrawGizmos()
    {
        var scentCol = Color.red;
        scentCol.a = 0.3f;
        Gizmos.color = scentCol;
        
        Gizmos.DrawSphere(transform.position, scentRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxFollowRadius);
    }
}