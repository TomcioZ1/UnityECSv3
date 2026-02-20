using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class ResolutionManager : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;

    void Start()
    {
        // Pobieramy wszystkie dostêpne rozdzielczoœci
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResIndex = 0;
        int defaultTargetIndex = -1; // Indeks dla 800x600

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // 1. Szukamy 800x600, aby ustawiæ jako bazow¹
            if (resolutions[i].width == 800 && resolutions[i].height == 600)
            {
                defaultTargetIndex = i;
            }

            // 2. Szukamy aktualnej (jako backup, gdyby 800x600 nie by³o na liœcie)
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        // Ustawiamy wartoœæ: jeœli znaleziono 800x600, u¿yj jej. Jeœli nie, u¿yj aktualnej.
        resolutionDropdown.value = (defaultTargetIndex != -1) ? defaultTargetIndex : currentResIndex;

        resolutionDropdown.RefreshShownValue();
        fullscreenToggle.isOn = Screen.fullScreen;

        // Opcjonalne: Wymuœ 800x600 przy samym starcie aplikacji
        ApplySettings(); 
    }

    public void ApplySettings()
    {
        if (resolutions.Length > 0)
        {
            Resolution resolution = resolutions[resolutionDropdown.value];
            Screen.SetResolution(resolution.width, resolution.height, fullscreenToggle.isOn);
            Debug.Log($"Zastosowano: {resolution.width}x{resolution.height}");
        }
    }
}