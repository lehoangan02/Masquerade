using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI textDisplay;

    [Header("Settings")]
    public float typingSpeed = 0.03f;

    private List<string> linesToPlay = new List<string>();
    private int currentIndex;
    private bool isTyping;
    private bool dialogueActive;

    void Awake()
    {
        if (Instance == null) Instance = this;
        dialoguePanel.SetActive(false);
    }

    // This is the core function that finds your #Key inside the .txt file
    public void PlayDialogue(string fileName, string key)
    {
        TextAsset file = Resources.Load<TextAsset>("Dialogues/" + fileName);
        if (file == null)
        {
            Debug.LogError($"File 'Resources/Dialogues/{fileName}' not found!");
            return;
        }

        string[] allLines = file.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        linesToPlay.Clear();

        bool foundKey = false;
        foreach (string line in allLines)
        {
            string trimmedLine = line.Trim();
            if (trimmedLine == "#" + key) {
                foundKey = true;
                continue;
            }
            if (foundKey && trimmedLine.StartsWith("#")) break; // Hit the next section

            if (foundKey && !string.IsNullOrWhiteSpace(trimmedLine)) {
                linesToPlay.Add(trimmedLine);
            }
        }

        if (linesToPlay.Count > 0) {
            dialogueActive = true;
            currentIndex = 0;
            dialoguePanel.SetActive(true);
            StartCoroutine(TypeLine());
        }
    }

    void Update()
    {
        if (!dialogueActive) return;

        // Press Space, Enter, or Left Click to advance
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                textDisplay.text = linesToPlay[currentIndex];
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        textDisplay.text = "";
        foreach (char c in linesToPlay[currentIndex].ToCharArray())
        {
            textDisplay.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    void NextLine()
    {
        currentIndex++;
        if (currentIndex < linesToPlay.Count)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        dialogueActive = false;
        dialoguePanel.SetActive(false);
    }
}