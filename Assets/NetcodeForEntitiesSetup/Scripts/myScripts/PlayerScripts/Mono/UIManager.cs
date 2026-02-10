using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panels")]
    public GameObject chatUI;
    public GameObject leaderboardUI;
    public GameObject pauseMenuUI;

    private ChatPanel _chatPanel;

    private void Awake()
    {
        Instance = this;
        _chatPanel = chatUI.GetComponent<ChatPanel>();

        // Startowy stan
        chatUI.SetActive(false);
        leaderboardUI.SetActive(false);
        pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. ESC - Najwy¿szy priorytet
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            ToggleEscape();
        }

        // 2. ENTER - Chat (blokowany przez Pauzê)
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            if (!pauseMenuUI.activeSelf)
            {
                ToggleChat();
            }
        }

        // 3. TAB - Leaderboard (blokowany przez Pauzê i Chat)
        if (keyboard.tabKey.wasPressedThisFrame)
        {
            if (!pauseMenuUI.activeSelf && !chatUI.activeSelf)
            {
                ToggleLeaderboard();
            }
        }
    }

    private void ToggleEscape()
    {
        if (chatUI.activeSelf || leaderboardUI.activeSelf)
        {
            chatUI.SetActive(false);
            leaderboardUI.SetActive(false);
            _chatPanel.CloseChat();
            SetCursorState(true);

            return;
        }
        else
        {
            bool isPaused = !pauseMenuUI.activeSelf;
            pauseMenuUI.SetActive(isPaused);
        }
    }

    private void ToggleChat()
    {
        if (!chatUI.activeSelf)
        {
            // Otwieramy chat
            chatUI.SetActive(true);
            leaderboardUI.SetActive(false);
            _chatPanel.OpenChat();
            SetCursorState(false);
            
        }
        else
        {
            // Chat jest ju¿ otwarty
            if (string.IsNullOrWhiteSpace(_chatPanel.inputField.text))
            {
                // Puste pole -> Zamykamy
                chatUI.SetActive(false);
                _chatPanel.CloseChat();
                SetCursorState(true);
            }
            else
            {
                // Jest tekst -> Wysy³amy i zostajemy w chacie
                _chatPanel.SendMessage();
                _chatPanel.inputField.ActivateInputField(); // Re-focus
            }
        }
    }

    private void ToggleLeaderboard()
    {
        bool isShowing = !leaderboardUI.activeSelf;
        leaderboardUI.SetActive(isShowing);
        if (isShowing)
        {
            // Pobieramy skrypt i odœwie¿amy dane z ECS
            leaderboardUI.GetComponent<LeaderboardUIPanel>().RefreshLeaderboard();
        }
    }
    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

}