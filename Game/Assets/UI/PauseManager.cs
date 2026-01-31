using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private Animator pauseAnimator;
    private bool isPaused = false;
    void Start()
    {
        isPaused = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void RestartScene()
    {
        Debug.Log("Restarting scene: " + SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void TogglePause()
    {
        Debug.Log("Toggling pause. Current state: " + isPaused);
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    public void PauseGame()
    {
        isPaused = true;
        pauseAnimator.SetTrigger("onPause");
        // Time.timeScale = 0f;
    }
    public void ResumeGame()
    {
        isPaused = false;
        pauseAnimator.SetTrigger("offPause");
        // Time.timeScale = 1f;
    }
}
