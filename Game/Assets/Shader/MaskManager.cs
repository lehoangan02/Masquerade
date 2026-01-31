using UnityEngine;
using UnityEngine.InputSystem;

public class MaskManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer fireSpriteRenderer;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        transform.position = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
        // get average color of sprite renderer
        Color avgColor = GetAverageColor(spriteRenderer);
        // set fire sprite renderer color to average color
        fireSpriteRenderer.color = avgColor;
    }
    public static Color GetAverageColor(SpriteRenderer sr)
    {
        Texture2D tex = sr.sprite.texture;
        Rect r = sr.sprite.textureRect;

        Color[] pixels = tex.GetPixels(
            (int)r.x,
            (int)r.y,
            (int)r.width,
            (int)r.height
        );

        float rSum = 0, gSum = 0, bSum = 0, aSum = 0;

        foreach (var c in pixels)
        {
            rSum += c.r;
            gSum += c.g;
            bSum += c.b;
            aSum += c.a;
        }

        float count = pixels.Length;
        return new Color(rSum / count, gSum / count, bSum / count, aSum / count);
    }
}
