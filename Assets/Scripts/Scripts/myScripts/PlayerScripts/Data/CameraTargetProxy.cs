using UnityEngine;

// Plik musi nazywaæ siê: CameraTargetProxy.cs
public class CameraTargetProxy : MonoBehaviour
{
    // Upewnij siê, ¿e te pola s¹ PUBLICZNE
    public Vector3 Offset = new Vector3(0, 20f, -10f);
    public float Smoothness = 20f;
    public float PitchAngle = 60f;
}