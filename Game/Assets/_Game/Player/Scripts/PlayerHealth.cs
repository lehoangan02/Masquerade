using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Player health system with i-frames.
/// Implements IDamageable interface.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Invincibility Frames")]
    [SerializeField] private bool useIFrames = true;
    [SerializeField] private float iFrameDuration = 1f;
    [SerializeField] private float flashInterval = 0.1f;
    private bool isInvincible = false;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Events")]
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;
    public UnityEvent OnHealthChanged;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;

    void Awake()
    {
        currentHealth = maxHealth;
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke();
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (useIFrames)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        OnDeath?.Invoke();
        
        var controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.LockMovement();
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            while (elapsed < iFrameDuration)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }
            spriteRenderer.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(iFrameDuration);
        }

        isInvincible = false;
    }

    [ContextMenu("Test: Take 10 Damage")]
    private void TestDamage() => TakeDamage(10);
}
