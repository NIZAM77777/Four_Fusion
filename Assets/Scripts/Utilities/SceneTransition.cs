using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransition : MonoBehaviour
{
    public static bool IsLoading { get; private set; }
    public static event System.Action Loading;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { IsLoading = false; Loading = null; }

    public static void Load(string scene)
    {
        if (IsLoading || (AdManager.Instance != null && AdManager.Instance.IsShowingAd)) return;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Scene is missing from the build: " + scene);
            FusionUI.Instance?.Notify("ERROR_SCENE");
            return;
        }
        IsLoading = true;
        Loading?.Invoke();
        var host = new GameObject("SceneTransition").AddComponent<SceneTransition>();
        DontDestroyOnLoad(host.gameObject);
        host.StartCoroutine(host.LoadRoutine(scene));
    }

    private IEnumerator LoadRoutine(string scene)
    {
        AIManager.Instance?.CancelTurn();
        AdManager.Instance?.HideBanner();
        yield return new WaitForSecondsRealtime(.15f);
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone) yield return null;
        IsLoading = false;
        FusionUI.Instance?.RefreshGameplay();
        Destroy(gameObject);
    }
}
