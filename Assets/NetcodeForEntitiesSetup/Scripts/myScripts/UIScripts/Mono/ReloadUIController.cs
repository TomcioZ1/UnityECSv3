using UnityEngine;
using UnityEngine.UI;

public class ReloadUIController : MonoBehaviour
{
    public static ReloadUIController Instance;
    public GameObject reloadPanel;
    public Slider reloadSlider;

    private void Awake() => Instance = this;

    public void UpdateProgressFromData(float progress, bool isVisible)
    {
        if (reloadPanel.activeSelf != isVisible)
            reloadPanel.SetActive(isVisible);

        if (isVisible)
            reloadSlider.value = progress;
    }
}