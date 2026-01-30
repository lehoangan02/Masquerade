using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("File Settings")]
    public string fileName = "Level1_Library"; // Must match .txt name in Resources/Dialogues/
    public string key;                         // Must match the #Key in the text file

    [Header("Trigger Settings")]
    public bool triggerOnStart = false;        // Good for intro text
    public bool triggerOnCollision = true;     // Good for signs/doors
    public bool playOnce = false;              // Should it happen only once?

    private bool hasPlayed = false;

    void Start()
    {
        if (triggerOnStart) TriggerDialogue();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnCollision && other.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }

    public void TriggerDialogue()
    {
        if (playOnce && hasPlayed) return;

        DialogueManager.Instance.PlayDialogue(fileName, key);
        hasPlayed = true;
    }
}