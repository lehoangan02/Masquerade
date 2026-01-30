# Player Setup Guide - Unity Editor

Follow these steps to set up the Player in Unity.

---

## 📁 Folder Structure Created

```
Assets/
├── _Game/
│   ├── Prefabs/
│   │   └── Player/          ← Player prefab goes here
│   └── Scripts/
│       ├── Interfaces/
│       │   └── IDamageable.cs
│       └── Player/
│           ├── PlayerMovement.cs (PlayerController)
│           ├── PlayerHealth.cs
│           └── PlayerInput.cs
└── Scenes/
    └── _Sandbox/             ← Your test scenes go here
```

---

## 🎮 Step-by-Step: Create Player GameObject

### 1. Create a Sandbox Scene (Follow the Sandbox Rule!)
1. Go to `File > New Scene`
2. Save it as `Assets/Scenes/_Sandbox/Test_Player.unity`
3. Work here to avoid merge conflicts!

### 2. Create the Player GameObject
1. In Hierarchy: `Right-click > Create Empty`
2. Rename it to **"Player"**
3. Set Tag to **"Player"** (create if needed)
4. Set Layer to **"Player"** (create if needed: Edit > Project Settings > Tags and Layers)

### 3. Add Visual (Temporary Square)
1. With Player selected: `Right-click > 2D Object > Sprites > Square`
2. This creates a child called "Square"
3. Optionally rename to "Sprite"
4. Adjust scale if needed (e.g., 0.5, 0.5, 1)

### 4. Add Components to Player GameObject

**Required Components:**
| Component | Settings |
|-----------|----------|
| **Rigidbody2D** | Body Type: Dynamic, Gravity Scale: 0, Freeze Rotation Z: ✓ |
| **BoxCollider2D** | Size: Match your sprite (e.g., 0.5 x 0.5) |
| **PlayerController** | (from PlayerMovement.cs) |
| **PlayerInput** | - |
| **PlayerHealth** | Max Health: 100, Use IFrames: ✓ |

**In Inspector, configure:**
- Drag the child sprite's **SpriteRenderer** to PlayerHealth's Sprite Renderer field
- Leave Animator empty until you have animations

### 5. Rigidbody2D Settings (Important for Top-Down!)
```
Body Type: Dynamic
Material: None
Simulated: ✓
Use Auto Mass: ✗
Mass: 1
Linear Damping: 0
Angular Damping: 0.05
Gravity Scale: 0          ← CRITICAL for top-down!
Collision Detection: Continuous
Sleeping Mode: Start Awake
Interpolate: Interpolate
Constraints: Freeze Rotation Z ✓
```

### 6. Create the Prefab (Follow the Prefab Rule!)
1. Drag the **Player** GameObject from Hierarchy into `Assets/_Game/Prefabs/Player/`
2. This creates a Prefab - now all changes sync everywhere!
3. Delete the instance in the scene (you can drop the prefab back anytime)

---

## 🧪 Testing

1. Drop the Player prefab into your Test_Player scene
2. Add a simple floor:
   - `Right-click > 2D Object > Sprites > Square`
   - Scale it large (10, 10, 1)
   - Move it below the player
   - Add **BoxCollider2D** (so player can collide)
3. Press **Play** and use **WASD** to move!

---

## 🔧 Scripts Overview

### PlayerController (PlayerMovement.cs)
- Handles WASD movement
- State machine (Normal, Locked, Dashing)
- Physics-based movement with Rigidbody2D
- Animation parameters ready for sprites

### PlayerInput
- Separates input from logic
- Easy to swap input systems later
- Inputs: Move (WASD), Attack (LMB), Dash (Space), Interact (RMB)

### PlayerHealth (implements IDamageable)
- Health system with max/current
- Invincibility frames (i-frames) after damage
- Visual feedback (sprite flashing)
- UnityEvents for UI/sound hooks
- Test damage: Right-click component > "Test Take 10 Damage"

### IDamageable Interface
- The "universal adapter" for damage
- Any script can damage anything implementing this
- Example: `target.GetComponent<IDamageable>()?.TakeDamage(10);`

---

## 🎯 Quick Test: Damage System

Create a test damage zone:
1. Create Empty > name it "DamageZone"
2. Add BoxCollider2D, check "Is Trigger"
3. Create new script `TestDamageZone.cs`:

```csharp
using UnityEngine;

public class TestDamageZone : MonoBehaviour
{
    public int damage = 10;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Uses the Interface Rule!
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }
    }
}
```

---

## ✅ Checklist

- [ ] Created `_Sandbox` test scene
- [ ] Player has Rigidbody2D (Gravity Scale = 0)
- [ ] Player has BoxCollider2D
- [ ] Player has PlayerController, PlayerInput, PlayerHealth
- [ ] Saved as Prefab in `_Game/Prefabs/Player/`
- [ ] WASD movement works
- [ ] Damage system works (test with DamageZone)

---

Happy game jamming! 🎮
