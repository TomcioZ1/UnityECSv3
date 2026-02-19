using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI; // DLA TOGGLE (Tego brakowa³o!)

public class ResolutionManager : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;

    void Start()
    {
        // Pobieramy dostêpne rozdzielczoœci monitora
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        fullscreenToggle.isOn = Screen.fullScreen;
    }

    public void ApplySettings()
    {
        // Pobranie wybranej rozdzielczoœci z listy
        Resolution resolution = resolutions[resolutionDropdown.value];

        // G³ówne polecenie zmieniaj¹ce okno
        Screen.SetResolution(resolution.width, resolution.height, fullscreenToggle.isOn);

        Debug.Log($"Ustawiono: {resolution.width}x{resolution.height} | Fullscreen: {fullscreenToggle.isOn}");
    }
}