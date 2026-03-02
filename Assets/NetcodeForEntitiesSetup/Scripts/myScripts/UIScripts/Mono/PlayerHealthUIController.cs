using UnityEngine;
using UnityEngine.UI;
using TMPro; // Opcjonalnie dla tekstu

public class PlayerHealthUIController : MonoBehaviour
{
    public static PlayerHealthUIController Instance;

    public Slider healthSlider;
    public TextMeshProUGUI healthText; // Opcjonalnie: "100 / 100"

    private void Awake() => Instance = this;

    public void UpdateHealth(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (healthText != null)
        {
            healthText.text = $"{current} / {max}";
        }
    }
}