using UnityEngine;
using System.Collections;
using TMPro;

public class WinManager : MonoBehaviour
{
    public GameObject wand;
    public GameObject wand_broken;
    public SpriteRenderer wandSprite;
    public SpriteRenderer wandBrokenSprite;
    public TextMeshProUGUI youWonText;

    bool triggered;

    void Start()
    {
        if (youWonText != null)
        {
            Color c = youWonText.color;
            c.a = 0f;
            youWonText.color = c;
            youWonText.gameObject.SetActive(false);
        }

        if (wandBrokenSprite != null)
        {
            Color c = wandBrokenSprite.color;
            c.a = 0f;
            wandBrokenSprite.color = c;
        }

        if (wand_broken != null)
            wand_broken.SetActive(false);
    }

    void Update()
    {
        if (!triggered && Time.timeSinceLevelLoad >= 2f && wand.activeSelf)
        {
            triggered = true;
            StartCoroutine(FadeOutInWand());
        }
    }

    IEnumerator FadeOutInWand()
    {
        float duration = 1f;
        float elapsed = 0f;

        Color wandColor = wandSprite.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            wandColor.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            wandSprite.color = wandColor;
            yield return null;
        }

        wandColor.a = 0f;
        wandSprite.color = wandColor;
        wand.SetActive(false);

        wand_broken.SetActive(true);

        elapsed = 0f;
        Color brokenColor = wandBrokenSprite.color;
        brokenColor.a = 0f;
        wandBrokenSprite.color = brokenColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            brokenColor.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            wandBrokenSprite.color = brokenColor;
            yield return null;
        }

        brokenColor.a = 1f;
        wandBrokenSprite.color = brokenColor;

        if (youWonText != null)
        {
            youWonText.gameObject.SetActive(true);

            elapsed = 0f;
            Color winColor = youWonText.color;
            winColor.a = 0f;
            youWonText.color = winColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                winColor.a = Mathf.Lerp(0f, 1f, elapsed / duration);
                youWonText.color = winColor;
                yield return null;
            }

            winColor.a = 1f;
            youWonText.color = winColor;
        }
    }
}
