using UnityEngine;

public class Menu : MonoBehaviour
{
    public AudioManager audioManager;
    void Start()
    {
        audioManager.PlayMusic(audioManager.backgroundMusic);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
