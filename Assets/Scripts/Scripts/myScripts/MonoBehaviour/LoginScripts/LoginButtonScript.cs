using UnityEngine;

public class LoginButtonScript : MonoBehaviour
{
    public string loginUrl = "http://127.0.0.1:3000/login";

    public void OnLoginClick()
    {
        Application.OpenURL(loginUrl);
    }
}
