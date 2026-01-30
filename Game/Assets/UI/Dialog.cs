using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using System;

public class Dialog : MonoBehaviour
{
    public TextMeshProUGUI dialogText;
    public string[] dialogLines;
    public int currentDialogIndex = 0;
    public float textSpeed = 0.05f;
    public Action onDialogComplete;
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
    public void StartDialog()
    {
        dialogText.text = "";
        StartCoroutine(TypeLine());
        Debug.Log(dialogText.text);
    }
    void NextLine()
    {
        if (currentDialogIndex < dialogLines.Length - 1)
        {
            currentDialogIndex++;
            dialogText.text = "";
            StartCoroutine(TypeLine());
        }
        else
        {
            dialogText.text = "";
            gameObject.SetActive(false);
            if (onDialogComplete != null)
            {
                onDialogComplete.Invoke();
                onDialogComplete = null;
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