using System.ComponentModel;
using UnityEngine;
public abstract class DamageableBase : MonoBehaviour, IDamageable, INotifyPropertyChanged
{
    [SerializeField] private int maxHealth;
    private int health;

    public bool IsAlive => Health > 0;
    public float HealthRatio => Health / (float)maxHealth;
    
    public abstract Alignment Alignment { get; }
    public int Health
    {
        get => health;
        set
        {
            health = Mathf.Clamp(value, 0, maxHealth);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Health)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HealthRatio)));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    
    public void ReceiveDamage(int amount)
    {
        Health -= amount;
        
        if(!IsAlive)
            Destroy(gameObject);
    }
}