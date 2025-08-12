using UnityEngine;
public interface IDamageable
{
    public Transform transform { get; }
    public Alignment Alignment { get; }

    public void ReceiveDamage(int amount);
}