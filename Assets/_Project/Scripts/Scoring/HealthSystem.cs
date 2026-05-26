using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance { get; private set; }

    public float MaxHealth     = 100f;
    public float CurrentHealth { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        CurrentHealth = MaxHealth;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void TakeDamage(float dmg)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - dmg);
        HUD.Instance?.UpdateHealth(CurrentHealth / MaxHealth);
        if (CurrentHealth <= 0)
            GameManager.Instance?.GameOver();
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        HUD.Instance?.UpdateHealth(CurrentHealth / MaxHealth);
    }
}
