using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashManager : MonoBehaviour
{
    [SerializeField] private float splashTime = 3f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(splashTime);

        SceneManager.LoadScene("MainMenu");
    }
}