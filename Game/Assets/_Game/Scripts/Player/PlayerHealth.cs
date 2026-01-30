using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Handles player health, damage, death, and invincibility frames.
/// Implements IDamageable for universal damage system.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Invincibility Frames")]
    [SerializeField] private bool useIFrames = true;
    [SerializeField] private float iFrameDuration = 1f;
    private bool isInvincible = false;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashInterval = 0.1f;

    [Header("Events")]
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;
    public UnityEvent OnHealthChanged;

    // Property to access current health from other scripts
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;

    void Awake()
    {
        currentHealth = maxHealth;
        
        // Try to get SpriteRenderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Implementation of IDamageable interface.
    /// Called when the player takes damage from any source.
    /// </summary>
    public void TakeDamage(int amount)
    {
        // Ignore damage if invincible
        if (isInvincible) return;

        // Apply damage
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth); // Clamp to 0

        // Trigger events
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke();
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
        else if (useIFrames)
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    /// <summary>
    /// Heal the player by specified amount.
    /// </summary>
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Clamp to max
        OnHealthChanged?.Invoke();
        Debug.Log($"{gameObject.name} healed {amount}. HP: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Fully restore health to maximum.
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died!");
        OnDeath?.Invoke();
        
        // Disable player controls (communicate with PlayerController)
        var controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // You can add death animation, respawn logic, or game over screen here
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        // Visual feedback - flash the sprite
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            while (elapsed < iFrameDuration)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }
            spriteRenderer.enabled = true; // Ensure visible at end
        }
        else
        {
            yield return new WaitForSeconds(iFrameDuration);
        }

        isInvincible = false;
    }

    // Debug method to test damage in editor
    [ContextMenu("Test Take 10 Damage")]
    private void TestDamage()
    {
        TakeDamage(10);
    }
}
