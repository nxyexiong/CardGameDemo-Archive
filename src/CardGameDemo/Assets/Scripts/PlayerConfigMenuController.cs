using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerConfigMenuController : MonoBehaviour
{
    private const string SceneName = "PlayerConfigMenu";
    private const string PlayerNameKey = "PlayerName";
    private const string PlayerNameDefaultValue = "Player";
    private const string ServerIpKey = "ServerIp";
    private const string ServerIpDefaultValue = "127.0.0.1";

    public InputField PlayerNameInput;
    public InputField ServerIpInput;
    public Button ReturnButton;

    void Start()
    {
        // load default
        LoadDefaultPlayerConfig();

        // init ui
        PlayerNameInput.text = PlayerPrefs.GetString(PlayerNameKey) ?? string.Empty;
        ServerIpInput.text = PlayerPrefs.GetString(ServerIpKey) ?? string.Empty;

        // init listeners
        PlayerNameInput.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetString(PlayerNameKey, value);
        });
        ServerIpInput.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetString(ServerIpKey, value);
        });
        ReturnButton.onClick.AddListener(() =>
        {
            _ = SceneManager.UnloadSceneAsync(SceneName);
        });
    }

    void Update()
    {
    }

    private void LoadDefaultPlayerConfig()
    {
        if (string.IsNullOrEmpty(PlayerPrefs.GetString(PlayerNameKey)))
            PlayerPrefs.SetString(PlayerNameKey, PlayerNameDefaultValue);
        if (string.IsNullOrEmpty(PlayerPrefs.GetString(ServerIpKey)))
            PlayerPrefs.SetString(ServerIpKey, ServerIpDefaultValue);
    }
}

