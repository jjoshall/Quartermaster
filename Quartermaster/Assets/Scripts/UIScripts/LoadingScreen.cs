using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuLoader : MonoBehaviour
{
    public GameObject loadingPanel; // Reference to your LoadingPanel

    // This method will be called when the Play button is clicked.
    public void OnPlayButtonClicked()
    {
        // Activate the loading panel first.
        loadingPanel.SetActive(true);

        // Start loading the game scene asynchronously.
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        // Replace "GameScene" with the actual name of your game scene.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");

        // Prevent the scene from activating immediately when load is complete.
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is almost loaded.
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Optionally, you can wait a little longer so the player sees the loading screen.
        yield return new WaitForSeconds(1f); // Adjust as needed.

        // Now allow the scene to activate.
        asyncLoad.allowSceneActivation = true;
    }
}


