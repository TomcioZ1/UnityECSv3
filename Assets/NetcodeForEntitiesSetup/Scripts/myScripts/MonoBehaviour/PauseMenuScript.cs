using Unity.Entities;
using Unity.NetCode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.EventSystems.EventTrigger;

public class PauseMenuScript : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuPanel;
    bool escPressed = false;

    void Start()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        TogglePause();
    }


    public void TogglePause()
    {
#if ENABLE_INPUT_SYSTEM
        // wasPressedThisFrame dzia³a najlepiej w PresentationSystemGroup
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            escPressed = !escPressed;
        else return;
#else
        if (Input.GetKeyDown(KeyCode.Escape))
            escPressed = !ecsPressed;
        else return;
#endif

        World clientWorld = GetClientWorld();
        if (clientWorld == null) return;

        var em = clientWorld.EntityManager;

        // 1. Musimy znaleŸæ encjê, która posiada ten komponent
        var query = em.CreateEntityQuery(typeof(PressedKeyesComponent));

        if (!query.IsEmpty)
        {
            Entity entity = query.GetSingletonEntity();

            // 1. Pobierasz KOPIE danych
            var data = em.GetComponentData<PressedKeyesComponent>(entity);

            // 2. Zmieniasz wartoœæ w KOPII
            data.EscPressed = escPressed;

            // 3. KLUCZOWY KROK: Wysy³asz zmodyfikowan¹ kopiê z powrotem do ECS
            em.SetComponentData(entity, data);

            Debug.Log("Wys³ano do ECS: escPressed = " + data.EscPressed);
        }

        if (escPressed) pauseMenuPanel.SetActive(true);
        else 
            pauseMenuPanel.SetActive(false);



    }



   











    private World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                return world;
            }
        }
        return null;
    }
}

