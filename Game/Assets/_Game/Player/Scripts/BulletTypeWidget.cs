using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI Widget that displays current bullet type and cycles automatically.
/// All bullet types share a single ammo pool.
/// Place on a Canvas UI element.
/// </summary>
public class BulletTypeWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image bulletIcon;
    [SerializeField] private TextMeshProUGUI ammoText;
    
    [Header("Shared Ammo Pool")]
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int currentAmmo;
    
    [Header("Auto-Cycle Settings")]
    [SerializeField] private float cycleInterval = 1f;
    [SerializeField] private bool autoCycle = true;
    
    [Header("Position Settings")]
    [SerializeField] private WidgetPosition widgetPosition = WidgetPosition.BottomRight;
    [SerializeField] private Vector2 offset = new Vector2(-20f, 20f);
    
    public enum WidgetPosition { TopRight, BottomRight }
    
    // Bullet types
    private List<IBulletType> bulletTypes = new List<IBulletType>();
    private int currentTypeIndex = 0;
    private float cycleTimer;
    
    // Singleton for easy access
    public static BulletTypeWidget Instance { get; private set; }
    
    // Current type accessor
    public IBulletType CurrentBulletType => bulletTypes.Count > 0 ? bulletTypes[currentTypeIndex] : null;
    public Color CurrentColor => CurrentBulletType?.BulletColor ?? Color.white;
    
    // Shared ammo accessors
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    void Awake()
    {
        Instance = this;
        
        // Initialize shared ammo
        currentAmmo = maxAmmo;
        
        // Initialize default bullet types (placeholders)
        bulletTypes.Add(new RedBulletType());
        bulletTypes.Add(new BlueBulletType());
        bulletTypes.Add(new GreenBulletType());
    }

    void Start()
    {
        // Auto-create UI if references not set
        if (bulletIcon == null || ammoText == null)
        {
            CreateUI();
        }
        
        PositionWidget();
        UpdateDisplay();
    }

    void Update()
    {
        if (autoCycle && bulletTypes.Count > 1)
        {
            cycleTimer += Time.deltaTime;
            if (cycleTimer >= cycleInterval)
            {
                cycleTimer = 0f;
                CycleNext();
            }
        }
    }

    void CreateUI()
    {
        // Get or create Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            // Find existing canvas or create new
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("BulletWidgetCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            transform.SetParent(canvas.transform, false);
        }
        
        // Create container panel
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 100);
        
        // Add background
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);
        
        // Create bullet icon
        GameObject iconObj = new GameObject("BulletIcon");
        iconObj.transform.SetParent(transform, false);
        bulletIcon = iconObj.AddComponent<Image>();
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.6f);
        iconRect.anchorMax = new Vector2(0.5f, 0.6f);
        iconRect.sizeDelta = new Vector2(50, 50);
        iconRect.anchoredPosition = Vector2.zero;
        
        // Create ammo text
        GameObject textObj = new GameObject("AmmoText");
        textObj.transform.SetParent(transform, false);
        ammoText = textObj.AddComponent<TextMeshProUGUI>();
        ammoText.alignment = TextAlignmentOptions.Center;
        ammoText.fontSize = 18;
        ammoText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.35f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    void PositionWidget()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) return;
        
        switch (widgetPosition)
        {
            case WidgetPosition.TopRight:
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(offset.x, -Mathf.Abs(offset.y));
                break;
                
            case WidgetPosition.BottomRight:
                rect.anchorMin = new Vector2(1, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = new Vector2(offset.x, Mathf.Abs(offset.y));
                break;
        }
    }

    void UpdateDisplay()
    {
        if (CurrentBulletType == null) return;
        
        // Update icon color
        if (bulletIcon != null)
        {
            bulletIcon.color = CurrentBulletType.BulletColor;
        }
        
        // Update ammo text (shared pool)
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
        }
    }

    /// <summary>
    /// Cycle to next bullet type.
    /// </summary>
    public void CycleNext()
    {
        if (bulletTypes.Count == 0) return;
        
        currentTypeIndex = (currentTypeIndex + 1) % bulletTypes.Count;
        UpdateDisplay();
    }

    /// <summary>
    /// Cycle to previous bullet type.
    /// </summary>
    public void CyclePrevious()
    {
        if (bulletTypes.Count == 0) return;
        
        currentTypeIndex--;
        if (currentTypeIndex < 0) currentTypeIndex = bulletTypes.Count - 1;
        UpdateDisplay();
    }

    /// <summary>
    /// Use ammo from shared pool. Returns false if out of ammo.
    /// </summary>
    public bool UseCurrentAmmo()
    {
        if (currentAmmo <= 0) return false;
        
        currentAmmo--;
        UpdateDisplay();
        return true;
    }

    /// <summary>
    /// Add ammo to shared pool.
    /// </summary>
    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        UpdateDisplay();
    }

    /// <summary>
    /// Refill ammo to max.
    /// </summary>
    public void RefillAmmo()
    {
        currentAmmo = maxAmmo;
        UpdateDisplay();
    }

    /// <summary>
    /// Force refresh the display.
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Set auto-cycle on/off.
    /// </summary>
    public void SetAutoCycle(bool enabled)
    {
        autoCycle = enabled;
    }
}
