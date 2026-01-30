using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerInputSystem : SystemBase
{
    private InputAction _moveAction;

    protected override void OnCreate()
    {
        // Inicjalizacja Inputu bez zmian
        var map = new InputActionMap("PlayerControls");
        _moveAction = map.AddAction("Move", binding: "<Gamepad>/leftStick");

        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        _moveAction.Enable();
    }

    protected override void OnUpdate()
    {
        // 1. Pobieramy wartoœci z klawiatury/pada
        Vector2 input = _moveAction.ReadValue<Vector2>();
        float2 moveVector = new float2(input.x, input.y);

        // 2. NOWY STANDARD: U¿ywamy SystemAPI.Query zamiast Entities.ForEach
        // W SystemBase musimy u¿yæ pêtli foreach, co jest teraz zalecan¹ metod¹
        foreach (var moveInput in SystemAPI.Query<RefRW<PlayerMoveInput>>())
        {
            Debug.Log($"Player Input Move Vector: {moveVector}");
            moveInput.ValueRW.Value = moveVector;
        }
    }

    protected override void OnDestroy()
    {
        _moveAction?.Disable();
        _moveAction?.Dispose();
    }
}