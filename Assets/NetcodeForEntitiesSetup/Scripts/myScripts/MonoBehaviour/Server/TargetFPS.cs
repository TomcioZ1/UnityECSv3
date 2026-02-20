using UnityEngine;

public class TargetFPS : MonoBehaviour
{
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 400;
        Application.targetFrameRate = -1;
    }
}
