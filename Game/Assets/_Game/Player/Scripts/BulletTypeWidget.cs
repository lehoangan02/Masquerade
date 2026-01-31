using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI Widget that displays current mask type and cycles automatically.
/// Shows a roulette-style display with 3 masks visible.
/// All mask types share a single ammo pool.
/// Place on a Canvas UI element.
/// </summary>
public class BulletTypeWidget : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image bulletIcon;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI typeNameText;
    
    [Header("Roulette Display")]
    [SerializeField] private Image leftMaskIcon;
    [SerializeField] private Image centerMaskIcon;
    [SerializeField] private Image rightMaskIcon;
    [SerializeField] private Image selectionFrame;
    
    [Header("Roulette Settings")]
    [SerializeField] private float sideMaskAlpha = 0.5f;
    [SerializeField] private float centerMaskSize = 60f;
    [SerializeField] private float sideMaskSize = 40f;
    [SerializeField] private float maskSpacing = 50f;
    
    [Header("Mask Prefabs")]
    [SerializeField] private GameObject redMaskPrefab;
    [SerializeField] private GameObject blueMaskPrefab;
    [SerializeField] private GameObject yellowMaskPrefab;
    
    [Header("Mask Sprites (Assign from Assets/_Game/Arts/)")]
    [SerializeField] private Sprite redMaskSprite;
    [SerializeField] private Sprite blueMaskSprite;
    [SerializeField] private Sprite yellowMaskSprite;
    
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
    
    // Mask types
    private List<ThrowableType> maskTypes = new List<ThrowableType>();
    private List<Sprite> maskSprites = new List<Sprite>();
    private int currentTypeIndex = 0;
    private float cycleTimer;
    
    // Singleton for easy access
    public static BulletTypeWidget Instance { get; private set; }
    
    // Current type accessor
    public ThrowableType CurrentMaskType => maskTypes.Count > 0 ? maskTypes[currentTypeIndex] : null;
    public Color CurrentColor => CurrentMaskType?.Color ?? Color.white;
    public GameObject CurrentPrefab => CurrentMaskType?.Prefab;
    
    /// <summary>
    /// Get the current MaskType enum value for EnemyBase compatibility.
    /// </summary>
    public MaskType CurrentMaskTypeEnum
    {
        get
        {
            if (CurrentMaskType == null) return MaskType.None;
            
            // Match by type name or color
            string typeName = CurrentMaskType.TypeName?.ToLower() ?? "";
            if (typeName.Contains("red") || CurrentMaskType.Color == Color.red) return MaskType.Red;
            if (typeName.Contains("blue") || CurrentMaskType.Color == Color.blue || CurrentMaskType.Color == Color.cyan) return MaskType.Green;
            if (typeName.Contains("yellow") || CurrentMaskType.Color == Color.yellow) return MaskType.Yellow;
            
            return MaskType.None;
        }
    }
    
    // Legacy accessor for compatibility
    public IBulletType CurrentBulletType => null; // Deprecated, use CurrentMaskType
    
    // Shared ammo accessors
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    void Awake()
    {
        Instance = this;
        
        // Initialize shared ammo
        currentAmmo = maxAmmo;
        
        // Initialize mask types: Red, Blue, Yellow
        maskTypes.Add(new RedMaskType(redMaskPrefab));
        maskTypes.Add(new BlueMaskType(blueMaskPrefab));
        maskTypes.Add(new YellowMaskType(yellowMaskPrefab));
        
        // Use assigned sprites directly
        InitializeMaskSprites();
    }
    
    void InitializeMaskSprites()
    {
        maskSprites.Clear();
        maskSprites.Add(redMaskSprite);    // Index 0: Red
        maskSprites.Add(blueMaskSprite);   // Index 1: Blue
        maskSprites.Add(yellowMaskSprite); // Index 2: Yellow
        
        // Log warning if sprites are missing
        if (redMaskSprite == null) Debug.LogWarning("BulletTypeWidget: Red Mask Sprite not assigned!");
        if (blueMaskSprite == null) Debug.LogWarning("BulletTypeWidget: Blue Mask Sprite not assigned!");
        if (yellowMaskSprite == null) Debug.LogWarning("BulletTypeWidget: Yellow Mask Sprite not assigned!");
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
        if (autoCycle && maskTypes.Count > 1)
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
        rect.sizeDelta = new Vector2(180, 120);
        
        // Add background
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);
        
        // Create roulette container
        GameObject rouletteContainer = new GameObject("RouletteContainer");
        rouletteContainer.transform.SetParent(transform, false);
        RectTransform rouletteRect = rouletteContainer.AddComponent<RectTransform>();
        rouletteRect.anchorMin = new Vector2(0, 0.35f);
        rouletteRect.anchorMax = new Vector2(1, 1f);
        rouletteRect.offsetMin = Vector2.zero;
        rouletteRect.offsetMax = Vector2.zero;
        
        // Create left mask icon (previous)
        leftMaskIcon = CreateMaskIcon(rouletteContainer.transform, "LeftMask", -maskSpacing, sideMaskSize);
        Color leftColor = leftMaskIcon.color;
        leftColor.a = sideMaskAlpha;
        leftMaskIcon.color = leftColor;
        
        // Create selection frame behind center mask
        GameObject frameObj = new GameObject("SelectionFrame");
        frameObj.transform.SetParent(rouletteContainer.transform, false);
        selectionFrame = frameObj.AddComponent<Image>();
        selectionFrame.color = new Color(1f, 0.84f, 0f, 0.8f); // Gold color
        RectTransform frameRect = frameObj.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(centerMaskSize + 12, centerMaskSize + 12);
        frameRect.anchoredPosition = Vector2.zero;
        
        // Create center mask icon (current) - on top of frame
        centerMaskIcon = CreateMaskIcon(rouletteContainer.transform, "CenterMask", 0, centerMaskSize);
        bulletIcon = centerMaskIcon; // Keep reference for compatibility
        
        // Create right mask icon (next)
        rightMaskIcon = CreateMaskIcon(rouletteContainer.transform, "RightMask", maskSpacing, sideMaskSize);
        Color rightColor = rightMaskIcon.color;
        rightColor.a = sideMaskAlpha;
        rightMaskIcon.color = rightColor;
        
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
    
    Image CreateMaskIcon(Transform parent, string name, float xOffset, float size)
    {
        GameObject iconObj = new GameObject(name);
        iconObj.transform.SetParent(parent, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.preserveAspect = true;
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(size, size);
        iconRect.anchoredPosition = new Vector2(xOffset, 0);
        
        return icon;
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
        if (CurrentMaskType == null || maskTypes.Count == 0) return;
        
        // Calculate indices for left (previous) and right (next) masks
        int leftIndex = (currentTypeIndex - 1 + maskTypes.Count) % maskTypes.Count;
        int rightIndex = (currentTypeIndex + 1) % maskTypes.Count;
        
        // Update left mask (previous)
        if (leftMaskIcon != null)
        {
            UpdateMaskIcon(leftMaskIcon, leftIndex, sideMaskAlpha);
        }
        
        // Update center mask (current)
        if (centerMaskIcon != null)
        {
            UpdateMaskIcon(centerMaskIcon, currentTypeIndex, 1f);
        }
        
        // Update right mask (next)
        if (rightMaskIcon != null)
        {
            UpdateMaskIcon(rightMaskIcon, rightIndex, sideMaskAlpha);
        }
        
        // Legacy: Update bulletIcon if it's separate from centerMaskIcon
        if (bulletIcon != null && bulletIcon != centerMaskIcon)
        {
            UpdateMaskIcon(bulletIcon, currentTypeIndex, 1f);
        }
        
        // Update type name
        if (typeNameText != null)
        {
            typeNameText.text = CurrentMaskType.TypeName;
        }
        
        // Update ammo text (shared pool)
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
        }
    }
    
    void UpdateMaskIcon(Image icon, int typeIndex, float alpha)
    {
        if (icon == null || typeIndex < 0 || typeIndex >= maskTypes.Count) return;
        
        // Set sprite if available
        if (typeIndex < maskSprites.Count && maskSprites[typeIndex] != null)
        {
            icon.sprite = maskSprites[typeIndex];
            icon.color = new Color(1f, 1f, 1f, alpha); // Use sprite's own colors
        }
        else
        {
            // Fallback to colored box if no sprite
            icon.sprite = null;
            Color color = maskTypes[typeIndex].Color;
            color.a = alpha;
            icon.color = color;
        }
    }

    /// <summary>
    /// Cycle to next mask type.
    /// </summary>
    public void CycleNext()
    {
        if (maskTypes.Count == 0) return;
        
        currentTypeIndex = (currentTypeIndex + 1) % maskTypes.Count;
        UpdateDisplay();
    }

    /// <summary>
    /// Cycle to previous mask type.
    /// </summary>
    public void CyclePrevious()
    {
        if (maskTypes.Count == 0) return;
        
        currentTypeIndex--;
        if (currentTypeIndex < 0) currentTypeIndex = maskTypes.Count - 1;
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
