using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private static SceneLoader instance;

    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private float transitionTime = 1f;

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
            return;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LoadSceneByName("SampleScene");
        }
    }

    public static SceneLoader GetInstance()
    {
        return instance;
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
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadScene(int sceneIndex)
    {
        transitionAnimator.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        transitionAnimator = FindFirstObjectByType<Animator>();
        transitionAnimator.SetTrigger("End");
    }

}
