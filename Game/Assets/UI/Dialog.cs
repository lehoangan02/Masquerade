using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class Dialog : MonoBehaviour
{
    public Action[] lineActions;
    public TextMeshProUGUI dialogText;
    public string[] dialogLines;
    public int currentDialogIndex = 0;
    public float textSpeed = 0.05f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentDialogIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame
        || Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentDialogIndex >= 0 && currentDialogIndex < dialogLines.Length)
            {
                if (dialogText.text == dialogLines[currentDialogIndex])
                {
                    NextLine();
                }
                else
                {
                    StopAllCoroutines();
                    dialogText.text = dialogLines[currentDialogIndex];
                }
            }
        }
    }
    public void StartDialog()
    {
        dialogText.text = "";
        // Execute action for the first line if it exists
        if (lineActions != null && lineActions.Length > 0 && lineActions[0] != null)
        {
            lineActions[0]?.Invoke();
        }
        StartCoroutine(TypeLine());
    }
    void NextLine()
    {
        if (currentDialogIndex < dialogLines.Length - 1)
        {
            currentDialogIndex++;
            dialogText.text = "";
            // Execute action for this line if it exists
            if (lineActions != null && currentDialogIndex < lineActions.Length && lineActions[currentDialogIndex] != null)
            {
                Debug.Log("Executing action for line " + currentDialogIndex);
                lineActions[currentDialogIndex]?.Invoke();
            }
            StartCoroutine(TypeLine());
        }
        else
        {
            dialogText.text = "";
            gameObject.SetActive(false);
            // Execute action for after the dialog closes (last+1)
            int afterLast = currentDialogIndex + 1;
            if (lineActions != null && afterLast < lineActions.Length && lineActions[afterLast] != null)
            {
                Debug.Log("Executing action after dialog closes: " + afterLast);
                lineActions[afterLast]?.Invoke();
            }
        }
    }
    IEnumerator TypeLine()
    {
        foreach (char c in dialogLines[currentDialogIndex].ToCharArray())
        {
            dialogText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }
    public void ResetDialog()
    {
        currentDialogIndex = 0;
        dialogText.text = "";
    }
    public bool IsDialogFinished()
    {
        return currentDialogIndex >= dialogLines.Length;
    }
}