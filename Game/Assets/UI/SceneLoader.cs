using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public static SceneLoader GetInstance()
    {
        return instance;
    }
    [SerializeField] private Animator transitionAnimator;
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LoadScene("SampleScene");
        }
    }
    public void LoadSceneByName(string sceneName)
    {
        StartCoroutine(LoadScene(sceneName));
    }
    public void LoadSceneByIndex(int sceneIndex)
    {
        StartCoroutine(LoadScene(sceneIndex));
    }
    private IEnumerator LoadScene(string sceneName)
    {
        transitionAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
    private IEnumerator LoadScene(int sceneIndex)
    {
        transitionAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
    }

}
