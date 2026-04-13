using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using System.Linq;
using UnityEngine.SceneManagement;

public class UITimerDisplay : MonoBehaviour
{
    [SerializeField] GameObject LeaderBoardPanel;
    [SerializeField] GameObject QuitButton;
    [SerializeField] TextMeshProUGUI timerText;

    void Update()
    {
        // Zamiast LINQ (FirstOrDefault), u¿ywamy zwyk³ej pêtli
        World clientWorld = null;

        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                clientWorld = world;
                break;
            }
        }

        if (clientWorld == null) return;

        var em = clientWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(GameTimer));

        if (query.HasSingleton<GameTimer>())
        {
            var timerData = query.GetSingleton<GameTimer>();
            float displayTime = Mathf.Max(0, timerData.TimeRemaining);

            // Obliczamy minuty i sekundy
            int minutes = Mathf.FloorToInt(displayTime / 60);
            int seconds = Mathf.FloorToInt(displayTime % 60);

            // Formatujemy string: 
            // D2 oznacza, ¿e zawsze bêd¹ 2 cyfry (np. 01 zamiast 1)
            timerText.text = string.Format("{0}:{1:D2}", minutes, seconds);

            if(timerData.TimeRemaining <= 0.5)
            {
                LeaderBoardPanel.SetActive(true);
                QuitButton.SetActive(true);
                if (timerData.TimeRemaining <= 0)
                {
                    timerText.text = "0:00";
                }
            }

        }

    }


    public void QuitButtonPressed()
    {
        SceneManager.LoadScene("ConnectionUI");
    }




}