using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panels")]
    public GameObject chatUI;
    public GameObject leaderboardUI;
    public GameObject pauseMenuUI;

    private void Awake()
    {
        Instance = this;
        // Na starcie wy³¹czamy wszystko
        chatUI.SetActive(false);
        leaderboardUI.SetActive(false);
        pauseMenuUI.SetActive(false);
    }

    void Update()
    {
        // 1. ESC - Najwy¿szy priorytet
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleEscape();
        }

        // 2. ENTER - Chat (blokowany przez ESC)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!pauseMenuUI.activeSelf)
            {
                ToggleChat();
            }
        }

        // 3. TAB - Leaderboard (blokowany przez ESC i Chat)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!pauseMenuUI.activeSelf && !chatUI.activeSelf)
            {
                ToggleLeaderboard();
            }
        }
    }

    private void ToggleEscape()
    {
        // Jeœli cokolwiek innego jest w³¹czone, po prostu to wy³¹cz i zakoñcz
        if (chatUI.activeSelf || leaderboardUI.activeSelf)
        {
            chatUI.SetActive(false);
            leaderboardUI.SetActive(false);
            SetCursorState(false); // Chowamy kursor po wyjœciu z UI
            return;
        }

        // Jeœli nic innego nie by³o w³¹czone, prze³¹czamy Pause Menu
        bool isPaused = !pauseMenuUI.activeSelf;
        pauseMenuUI.SetActive(isPaused);
        SetCursorState(isPaused);
    }

    private void ToggleChat()
    {
        bool isChatting = !chatUI.activeSelf;
        chatUI.SetActive(isChatting);

        // Zawsze upewniamy siê, ¿e leaderboard jest wy³¹czony przy chacie
        leaderboardUI.SetActive(false);

        SetCursorState(isChatting);
    }

    private void ToggleLeaderboard()
    {
        bool isShowingLeaderboard = !leaderboardUI.activeSelf;
        leaderboardUI.SetActive(isShowingLeaderboard);

        SetCursorState(isShowingLeaderboard);
    }

    private void SetCursorState(bool visible)
    {
        // Pomocnicza funkcja do zarz¹dzania kursorem myszy
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}