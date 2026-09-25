using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashManager : MonoBehaviour
{
    [SerializeField] private float splashTime = 3f;

    private IEnumerator Start()
    {
        // Import/load the menu while the existing splash animation is visible.
        var loading = SceneManager.LoadSceneAsync("MainMenu");
        loading.allowSceneActivation = false;
        yield return new WaitForSecondsRealtime(Mathf.Min(splashTime, 1.2f));
        loading.allowSceneActivation = true;
    }
}
