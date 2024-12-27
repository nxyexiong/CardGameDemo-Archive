using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartClosedGameMenuController : MonoBehaviour
{
    private const string SceneName = "StartClosedGameMenu";

    public Button CreateLobbyButton;
    public Button JoinLobbyButton;
    public InputField JoinLobbyIdInput;
    public Button ReturnButton;

    void Start()
    {
        // init listeners
        CreateLobbyButton.onClick.AddListener(() =>
        {
            // TODO
            SceneManager.LoadScene("Gameplay");
        });
        JoinLobbyButton.onClick.AddListener(() =>
        {
            var lobbyId = JoinLobbyIdInput.text;
            // TODO
        });
        ReturnButton.onClick.AddListener(() =>
        {
            _ = SceneManager.UnloadSceneAsync(SceneName);
        });
    }

    void Update()
    {
    }
}
