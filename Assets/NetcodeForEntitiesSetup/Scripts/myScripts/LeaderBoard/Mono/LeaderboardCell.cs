using TMPro;
using UnityEngine;

public class LeaderboardCell : MonoBehaviour
{
    public TMP_Text placeText;
    public TMP_Text nameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;
    public TMP_Text pingText;

    public void SetData(int place, string playerName, int kills, int deaths)
    {
        placeText.text = place.ToString();
        nameText.text = playerName;
        killsText.text = kills.ToString();
        deathsText.text = deaths.ToString();
        pingText.text = "0 ms"; 
    }
}