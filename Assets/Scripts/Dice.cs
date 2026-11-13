using UnityEngine;
using UnityEngine.UI;

// ============================================================
// Dice  (ANSWER - final version, no more changes after Lesson 1)
// ------------------------------------------------------------
// Put this on the RollButton.
// Then in the Button's On Click () list, drag in RollButton
// and choose  Dice > Roll().
//
// Setup in the Inspector:
//   Dice Image   -> the DiceImage (UI Image) that shows the face
//   Dice Faces   -> drag in the 6 dice pictures IN ORDER (1 to 6)
//   Game Manager -> the GameManager object
// ============================================================
public class Dice : MonoBehaviour
{
    // DESIGN: how many sides does your dice have? (normal dice = 6)
    // If you change this, you need the same number of Dice Faces.
    public int sides = 6;

    // The UI Image that shows the face we rolled.
    public Image diceImage;

    // The pictures of each face, in order: element 0 = face 1 ... element 5 = face 6.
    public Sprite[] diceFaces;

    // The GameManager that moves the player after we roll.
    public GameManager gameManager;

    // Called by the Roll button.
    public void Roll()
    {
        // 1. Random.Range with whole numbers: the MAX is NOT included,
        //    so (1, 7) gives 1, 2, 3, 4, 5 or 6.
        int result = Random.Range(1, sides + 1);

        // 2. Show the face for that number.
        ShowFace(result);

        // 3. Tell the GameManager - it moves the player, not the dice.
        gameManager.OnDiceRolled(result);
    }

    // Shows the picture for a number.
    public void ShowFace(int number)
    {
        // Lists start counting at 0, so face 1 is diceFaces[0].
        diceImage.sprite = diceFaces[number - 1];
    }
}
