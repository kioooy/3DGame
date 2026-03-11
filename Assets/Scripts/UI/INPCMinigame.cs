using UnityEngine;

public interface INPCMinigame
{
    string npcName { get; set; }
    bool isMinigameActive { get; set; }
    void EndMinigame(bool isWin, bool isDraw = false);
}
