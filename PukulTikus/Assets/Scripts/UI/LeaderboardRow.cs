using TMPro;
using UnityEngine;

public class LeaderboardRow : MonoBehaviour
{
    public TMP_Text txtRank;
    public TMP_Text txtName;
    public TMP_Text txtScore;
    public TMP_Text txtKills;
    public TMP_Text txtCombo;

    public void Bind(LeaderboardEntryDto e)
    {
        if (txtRank) txtRank.text = e.rank.ToString();
        if (txtName) txtName.text = e.playerName;
        if (txtScore) txtScore.text = e.score.ToString();
        if (txtKills) txtKills.text = e.kills.ToString();
        if (txtCombo) txtCombo.text = e.maxCombo.ToString();
    }
}
