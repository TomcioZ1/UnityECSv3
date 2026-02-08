using UnityEngine;

public class TargetFPS : MonoBehaviour
{
    void Start()
    {
        // Sprawdzamy, czy aplikacja NIE JEST serwerem dedykowanym
        // Systemy typu "Headless Server" nie powinny limitowaæ klatek w ten sposób
        if (!Application.isBatchMode)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Debug.Log("Client detected: Setting FPS limit to 60");
        }
        else
        {
            // Opcjonalnie dla serwera: ustawiamy nielimitowane klatki, 
            // bo serwer i tak nie renderuje grafiki
            Application.targetFrameRate = -1;
            Debug.Log("Server detected: Removing FPS limit");
        }
    }
}
