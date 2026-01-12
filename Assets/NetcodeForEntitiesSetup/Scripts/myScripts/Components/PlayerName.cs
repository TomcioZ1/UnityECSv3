using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using TMPro;

public struct PlayerName : IComponentData
{
    [GhostField]
    public FixedString64Bytes Value;
}

// Komponent zarz¹dzany do trzymania referencji do UI
public class PlayerNameReference : IComponentData
{
    public TextMeshPro TextElement;
}