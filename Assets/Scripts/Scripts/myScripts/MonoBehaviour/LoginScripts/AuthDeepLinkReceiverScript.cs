using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthListener : MonoBehaviour
{
    public TextMeshProUGUI debugText; // TMP na Canvasie
    public static string AuthToken;
    public static string PlayerName;

    private HttpListener listener;
    private ConcurrentQueue<string> messagesQueue = new ConcurrentQueue<string>();
    private bool shouldLoadScene = false;

    private void Start()
    {
        listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:3001/token/");
        listener.Start();
        Debug.Log("[DEBUG] Auth listener started");

        // Start w osobnym w¹tku
        System.Threading.Thread listenerThread = new System.Threading.Thread(Listen);
        listenerThread.Start();
    }

    private void Listen()
    {
        while (listener.IsListening)
        {
            try
            {
                var context = listener.GetContext();
                var request = context.Request;
                var response = context.Response;

                var query = request.QueryString;
                string token = query["token"];
                string username = query["username"];
                Debug.Log($"[DEBUG] Received token: {token}, username: {username}");
                PlayerInfoClass.PlayerName = username;
                PlayerInfoClass.AuthToken = token;

                if (!string.IsNullOrEmpty(token))
                {
                    // Przekazujemy do g³ównego w¹tku
                    messagesQueue.Enqueue($"{username}");
                    AuthToken = token;
                    PlayerName = username;

                    shouldLoadScene = true; // <-- sygna³

                    // OdpowiedŸ dla przegl¹darki
                    string responseString = "<html><body>Login successful. You can close this window.</body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else
                {
                    response.StatusCode = 400;
                    response.OutputStream.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[ERROR] Auth listener exception: " + e);
            }
        }
    }

    private void Update()
    {
        while (messagesQueue.TryDequeue(out string message))
        {
            debugText.text = message;
        }

        if (shouldLoadScene)
        {
            shouldLoadScene = false; // zabezpieczenie
            StartCoroutine(LoadSceneDelayed());
        }
    }

    private void OnApplicationQuit()
    {
        listener?.Stop();
        listener?.Close();
    }

    IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSeconds(1f); // opóŸnienie (mo¿esz zmieniæ)
        SceneManager.LoadScene("ConnectionUI"); // <-- NAZWA SCENY
    }
}
