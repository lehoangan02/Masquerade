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
        Action[] actions = new Action[4];
        actions[0] = null;
        actions[1] = null;
        actions[2] = SwitchFromWitchToWand;
        actions[3] = () => sceneLoader.LoadSceneByName("Level1_WalledGarden");
        dialog.lineActions = actions;
    }
    void SwitchFromWitchToWand()
    {
        Debug.Log("Switching from witch to wand");
        witch.SetActive(false);
        wand.SetActive(true);
    }
    void SwitchToNextScene()
    {
        wand.SetActive(false);
        sceneLoader.LoadSceneByName("Level1_WalledGarden");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
