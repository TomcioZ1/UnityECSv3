using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;

public class HealthBarLink : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;

    [HideInInspector] public Entity TargetEntity;
    [HideInInspector] public EntityManager Manager;

    /// <summary>
    /// Aktualizuje wartoœæ slidera i kolor paska.
    /// </summary>
    /// <param name="currentHealth">Wartoœæ HP z ECS.</param>
    /// <param name="isLocal">Czy to jest postaæ sterowana przez tego klienta?</param>
    public void UpdateHealth(int currentHealth, bool isLocal)
    {
        if (slider != null)
        {
            slider.value = currentHealth;
        }

        if (fillImage != null)
        {
            // ZIELONY dla Ciebie, CZERWONY dla innych graczy
            fillImage.color = isLocal ? Color.green : Color.red;
        }
    }
}