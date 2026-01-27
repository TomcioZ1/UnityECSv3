/*using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CrosshairSystem : SystemBase
{
    protected override void OnCreate()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    protected override void OnUpdate()
    {
        if (Mouse.current == null) return;

        // Pobieramy pozycjê myszy
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // SystemAPI.Query jest bardzo szybkie
        foreach (var crosshair in SystemAPI.Query<CrosshairTag>())
        {
            if (crosshair.Transform != null)
            {
                // Ustawiamy pozycjê bezpoœrednio na ekranie
                // Wymuszamy Z = 0 i upewniamy siê, ¿e nie ma przesuniêæ
                crosshair.Transform.position = new Vector3(mousePos.x, mousePos.y, 0f);
            }
        }

        // Zabezpieczenie dla builda - wymuszaj ukrycie kursora
        if (Application.isFocused && Cursor.visible)
        {
            Cursor.visible = false;
        }
    }
}*/