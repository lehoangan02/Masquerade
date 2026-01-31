using System;
using System.Collections.Generic;
using UnityEngine;

public class IntroManager : MonoBehaviour
{
    public GameObject witch;
    public GameObject wand;
    public SceneLoader sceneLoader;
    public Dialog dialog;
    void Start()
    {
        string dialog1 = "There was a witch in a land called Sunvale.";
        string dialog2 = "She experience some spells and accidentally turn most people into wandering souls.";
        string dialog3 = "You need to find the wand she left behind and break it.";
        dialog.dialogLines = new string[] { dialog1, dialog2, dialog3 };
        dialog.ResetDialog();
        dialog.StartDialog();
        Action[] actions = new Action[3];
        actions[0] = SwitchFromWitchToWand;
        actions[1] = null;
        actions[2] = () => sceneLoader.LoadSceneByName("Level1_WalledGarden");
        dialog.lineActions = actions;
    }
    void SwitchFromWitchToWand()
    {
        Debug.Log("Switching from witch to wand");
        witch.SetActive(false);
        wand.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
