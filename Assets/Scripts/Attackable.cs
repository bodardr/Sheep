using System;
using UnityEngine;
using UnityEngine.AI;
public class Attackable : MonoBehaviour
{
    [Tooltip("Attack interval in seconds")]
    [SerializeField] private float attackSpeed;
    [SerializeField] private int attackDamage;

    [SerializeField] private NavMeshAgent agent;

    public IDamageable Target
    {
        get => target;
        set
        {
            target = value;

            if (value == null)
                currentAttackTime = 0;
        }
    }

    private float currentAttackTime = 0;
    private IDamageable target;

    private void FixedUpdate()
    {
        if (Target == null)
            return;

        currentAttackTime += Time.deltaTime;
        while (currentAttackTime > attackSpeed)
        {
            currentAttackTime -= attackSpeed;
            target.ReceiveDamage(attackDamage);
        }
    }

    private void Update()
    {
        agent.updateRotation = Target == null;

        if (Target != null)
            transform.rotation = Quaternion.LookRotation(Target.transform.position - transform.position);
    }
}
