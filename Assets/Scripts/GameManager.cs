using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
// GameManager  (★ WE ADD TO THIS SCRIPT IN LESSONS 1 AND 2 ★)
// ------------------------------------------------------------
// Put this on an empty GameObject called "GameManager".
// It is the "referee": it receives the dice result, moves the
// player whose turn it is, and decides when the dice can be
// tapped again.
//
// Already written for you: turns for 1-4 players, names in the
// messages, the bounce back rule (a checkbox).
// Lesson 1: lock / unlock the dice button
// Lesson 2: win screen and Play Again
// ============================================================
public class GameManager : MonoBehaviour
{
    [Header("Board & Players")]
    public BoardPath board;             // drag in the BoardPath object
    public PlayerToken[] players;       // drag in Player 1 (Player 2... in Lesson 2)
    private int currentPlayer = 0;      // whose turn: 0 = first in the list

    [Header("UI")]
    public Button rollButton;           // drag in the DiceButton
    public TMP_Text statusText;

    // Messages - typed in the Inspector, not in the script.
    // The player's name and a space are added in front, e.g.
    //   Roll Message:   - tap the dice!       ->  "Red - tap the dice!"
    //   Moving Message: is moving...          ->  "Red is moving..."
    //   Finish Message: reached the finish!   ->  "Red reached the finish!"
    public string rollMessage;
    public string movingMessage;
    public string finishMessage;

    // Ending rule (a checkbox in the Inspector):
    // ticked   = roll too high -> walk to the last tile, then back the extra steps
    // unticked = roll too high -> stop on the last tile
    public bool bounceBack = true;

    void Start()
    {
        // Show the first message.
        statusText.text = players[currentPlayer].playerName + " " + rollMessage;
    }

    // The Dice calls this after rolling.
    public void OnDiceRolled(int result)
    {
        StartCoroutine(TakeTurn(result));
    }

    // One turn, step by step.
    IEnumerator TakeTurn(int steps)
    {
        // The player whose turn it is.
        PlayerToken player = players[currentPlayer];

        // ★ TYPE HERE: lock the dice button

        statusText.text = player.playerName + " " + movingMessage;

        // Move the player, and WAIT until it has finished walking.
        // (Snakes, ladders and special squares happen inside MoveSteps.)
        yield return StartCoroutine(player.MoveSteps(steps, bounceBack));

        // Did this player reach the last tile?
        if (player.currentTile == board.TileCount)
        {
            statusText.text = player.playerName + " " + finishMessage;
            yield break; // end the turn here - the button stays locked
        }

        // Next player (after the last one, back to the first).
        currentPlayer = currentPlayer + 1;
        if (currentPlayer >= players.Length)
        {
            currentPlayer = 0;
        }
        statusText.text = players[currentPlayer].playerName + " " + rollMessage;

        // ★ TYPE HERE: unlock the dice button

    }
}
