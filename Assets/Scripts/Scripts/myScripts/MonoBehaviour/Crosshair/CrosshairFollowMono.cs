using UnityEngine;

namespace Christina.CustomCursor
{
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private Texture2D cursorTextureDefault;

        void Start()
        {
            if (cursorTextureDefault != null)
            {
                // Obliczamy œrodek tekstury
                // Jeœli tekstura ma 64x64, hotspot bêdzie (32, 32)
                Vector2 centerHotspot = new Vector2(cursorTextureDefault.width / 2f, cursorTextureDefault.height / 2f);

                Cursor.SetCursor(cursorTextureDefault, centerHotspot, CursorMode.Auto);
            }
            else
            {
                Debug.LogError("Brak tekstury kursora w CursorController!");
            }
        }
    }
}