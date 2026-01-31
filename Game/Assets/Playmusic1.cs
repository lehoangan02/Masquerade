using UnityEngine;

public class Playmusic1 : MonoBehaviour
{
    public AudioManager audioManager;
    void Start()
    {
        audioManager.PlayMusic(audioManager.levelMusicArray[1]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
