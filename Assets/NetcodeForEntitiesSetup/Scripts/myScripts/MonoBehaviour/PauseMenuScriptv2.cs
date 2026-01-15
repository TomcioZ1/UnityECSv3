using Unity.Entities;
using Unity.NetCode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.EventSystems.EventTrigger;

public class UIMenuScriptv2 : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject chatMenuPanel;



    void Start()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        EscPresses();
        EnterPressed();
    }


    void EscPresses()
    {
#if ENABLE_INPUT_SYSTEM
        // wasPressedThisFrame dzia³a najlepiej w PresentationSystemGroup
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if(PlayerInfoClass.isMenuOpen == false && PlayerInfoClass.isChatOpen == false)
            {
                pauseMenuPanel.SetActive(true);
                PlayerInfoClass.isMenuOpen = true;
            }
            else if (PlayerInfoClass.isMenuOpen == false && PlayerInfoClass.isChatOpen == true)
            {
                chatMenuPanel.SetActive(false);
                PlayerInfoClass.isChatOpen = false;
            }
            else if (PlayerInfoClass.isMenuOpen == true && PlayerInfoClass.isChatOpen == false)
            {
                chatMenuPanel.SetActive(false);
                PlayerInfoClass.isChatOpen = false;
            }
        }
        else return;
#else
        if (Input.GetKeyDown(KeyCode.Escape))
            escPressed = !ecsPressed;
        else return;
#endif
        
        


    }
    void EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        // wasPressedThisFrame dzia³a najlepiej w PresentationSystemGroup
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (PlayerInfoClass.isMenuOpen == false && PlayerInfoClass.isChatOpen == false)
            {
                chatMenuPanel.SetActive(true);
                PlayerInfoClass.isChatOpen = true;
            }
            else if (PlayerInfoClass.isMenuOpen == false && PlayerInfoClass.isChatOpen == true)
            {
                chatMenuPanel.SetActive(false);
                PlayerInfoClass.isChatOpen = false;
            }
            else if (PlayerInfoClass.isMenuOpen == true && PlayerInfoClass.isChatOpen == false)
            {
                chatMenuPanel.SetActive(false);
                PlayerInfoClass.isChatOpen = false;
            }
        }
        else return;
#else
        if (Input.GetKeyDown(KeyCode.enter))
            enterPressed = !enterPressed;
        else return;
#endif

    }











}

