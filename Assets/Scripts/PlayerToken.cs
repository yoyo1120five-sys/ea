using System.Collections;
using UnityEngine;
using UnityEngine.Splines;   // Splines package (Window > Package Manager)

// ============================================================
// PlayerToken  (GIVEN SCRIPT - change values in the Inspector only)
// ------------------------------------------------------------
// Put this on each player's character (a sprite).
// It knows which tile it is on, walks tile by tile, and climbs
// ladders / slides down snakes by itself (following their Spline
// path if they have one). Ladder / snake speeds are set on BoardPath.
//
// Tips:
// - Give the character's Sprite Renderer a higher "Order in Layer"
//   than the board, so it is drawn on top.
// - Set the sprite's Pivot to "Bottom" in its import settings,
//   so the character stands on the square.
// ============================================================
public class PlayerToken : MonoBehaviour
{
    // DESIGN: the name shown in messages, e.g. "Red wins!".
    public string playerName = "Red";

    // Drag the BoardPath object here.
    public BoardPath board;

    // DESIGN: how fast the character moves between tiles (units per second).
    public float moveSpeed = 6f;

    // DESIGN: a short pause on each tile, so players can count the steps.
    public float pauseOnEachTile = 0.15f;

    // The tile the character is standing on.
    // 0 = waiting at the Start Point, 1 = first tile.
    // (Testing tip: set this before pressing Play to start somewhere else.)
    public int currentTile = 0;

    // Where this player stands: its spot at the start, and its small
    // shift on tiles so players don't cover each other. Set by
    // LineUpPlayers() from the standing spots on BoardPath's StartPoint.
    private Vector3 startPosition;
    private Vector3 tileOffset;

    // OPTIONAL: set by special squares (Lose Next Turn / Shield).
    [HideInInspector]
    public bool skipNextTurn = false;
    [HideInInspector]
    public bool hasShield = false;

    void Start()
    {
        // Line up all players who are playing (safe to run many times).
        LineUpPlayers();
    }

    // ------------------------------------------------------------
    // Puts every ACTIVE player on its standing spot (see BoardPath),
    // in name order: Player1 -> Spot 1, Player2 -> Spot 2...
    // (or in the order given - the first gets Spot 1, and so on).
    // On tiles, players keep the same pattern, made smaller.
    // Called at the start, and again by MenuManager after choosing
    // how many players are playing.
    // ------------------------------------------------------------
    public static void LineUpPlayers(PlayerToken[] order = null)
    {
        // Use the given order (e.g. from character select), or else
        // all active players sorted by name.
        PlayerToken[] players = order;
        if (players == null)
        {
            players = FindObjectsOfType<PlayerToken>();   // active players only
            System.Array.Sort(players, (a, b) => string.CompareOrdinal(a.name, b.name));
        }

        int count = players.Length;
        for (int i = 0; i < count; i++)
        {
            PlayerToken player = players[i];
            BoardPath board = player.board;
            if (board == null)
            {
                continue;   // Board field not filled in yet
            }

            Vector3 centre = board.GetStartCentre();
            Vector3 spot = board.GetStartSpot(i);

            float z = player.transform.position.z;
            player.startPosition = spot;
            player.startPosition.z = z;

            Vector3 offset = spot - centre;
            player.tileOffset = new Vector3(offset.x * board.tileLayoutScale, offset.y * board.tileLayoutScale, 0f);

            // Put the player in its place (on its tile if Current Tile is set).
            player.transform.position = player.GetPositionOnTile(player.currentTile);
        }
    }

    // ------------------------------------------------------------
    // One whole move - called by the GameManager every turn.
    // Walks the steps (positive = forward, negative = backward),
    // then handles everything that can happen after landing:
    //   snakes & ladders and special squares (optional).
    //
    // What happens if the roll is too big to land on the last tile?
    //   bounceBack = false -> stop on the last tile
    //   bounceBack = true  -> reach the last tile, then walk BACK the
    //                         extra steps (e.g. on 24 of 26, roll 5:
    //                         25, 26, back to 25, 24, 23)
    //
    // This is a "coroutine": it can WAIT in the middle (yield return),
    // which is how we get the step... step... step... movement.
    // ------------------------------------------------------------
    public IEnumerator MoveSteps(int steps, bool bounceBack = false)
    {
        // OPTIONAL: this player lost this turn (Lose Next Turn tile).
        if (skipNextTurn)
        {
            skipNextTurn = false;
            board.ShowMessage(playerName + " misses this turn!");
            yield return new WaitForSeconds(board.messageTime);
            yield break;
        }

        if (steps == 0)
        {
            yield break;
        }

        yield return StartCoroutine(Walk(steps, bounceBack));
        yield return StartCoroutine(AfterLanding(bounceBack, true));
    }

    // Walk tile by tile (positive = forward, negative = backward).
    // It never goes back past tile 1. (Also used by special squares.)
    public IEnumerator Walk(int steps, bool bounceBack)
    {
        int direction = 1;
        if (steps < 0)
        {
            direction = -1;
        }

        int stepCount = Mathf.Abs(steps);

        for (int i = 0; i < stepCount; i++)
        {
            int nextTile = currentTile + direction;

            // Rolled past the last tile?
            if (nextTile > board.TileCount)
            {
                if (bounceBack)
                {
                    direction = -1;            // turn around
                    nextTile = currentTile - 1;
                }
                else
                {
                    break;                     // just stop on the last tile
                }
            }

            // Never go back past tile 1.
            if (nextTile < 1)
            {
                break;
            }

            yield return StartCoroutine(SlideTo(GetPositionOnTile(nextTile), moveSpeed));
            currentTile = nextTile;

            yield return new WaitForSeconds(pauseOnEachTile);
        }
    }

    // Everything that can happen after landing on a tile.
    // allowSpecial = false stops special tiles chaining forever.
    public IEnumerator AfterLanding(bool bounceBack, bool allowSpecial)
    {
        // Landed on a snake or a ladder?
        BoardJump jump = board.GetJump(currentTile);
        if (jump != null)
        {
            yield return new WaitForSeconds(board.pauseBeforeJump);
            yield return StartCoroutine(FollowJump(jump));
        }

        // OPTIONAL: landed on a special square? (only if SpecialTile is used)
        if (allowSpecial)
        {
            TileAction special = board.GetSpecialTile(currentTile);
            if (special != null)
            {
                yield return StartCoroutine(special.Run(this, bounceBack));
            }
        }
    }

    // ------------------------------------------------------------
    // Climb a ladder / slide down a snake.
    // With a Spline path: follow its shape. Without: straight line.
    // ------------------------------------------------------------
    IEnumerator FollowJump(BoardJump jump)
    {
        bool isLadder = jump.toTile > jump.fromTile;

        // OPTIONAL: a Shield blocks one snake.
        if (!isLadder && hasShield)
        {
            hasShield = false;
            board.ShowMessage(playerName + "'s shield blocks the snake!");
            yield return new WaitForSeconds(board.messageTime);
            yield break;
        }

        // Optional sound (BoardPath > Sounds).
        board.PlaySound(isLadder ? board.ladderSound : board.snakeSound);

        float speed = isLadder ? board.ladderSpeed : board.snakeSpeed;

        if (jump.path != null && jump.path.Spline != null && jump.path.Spline.Count >= 2)
        {
            Spline spline = jump.path.Spline;
            Transform pathTransform = jump.path.transform;

            // The spline can be drawn in either direction:
            // start from whichever end is closer to the player.
            Vector3 splineStart = pathTransform.TransformPoint(spline.EvaluatePosition(0f));
            Vector3 splineEnd = pathTransform.TransformPoint(spline.EvaluatePosition(1f));
            bool reverse = Vector3.Distance(transform.position, splineEnd)
                         < Vector3.Distance(transform.position, splineStart);

            float length = spline.CalculateLength(pathTransform.localToWorldMatrix);
            float duration = Mathf.Max(0.1f, length / Mathf.Max(0.1f, speed));
            float z = transform.position.z;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);

                // Ladders: smooth start and stop. Snakes: speed up like falling.
                float eased = isLadder ? Mathf.SmoothStep(0f, 1f, progress) : progress * progress;
                float t = reverse ? 1f - eased : eased;

                Vector3 point = pathTransform.TransformPoint(spline.EvaluatePosition(t));
                point.x += tileOffset.x;
                point.y += tileOffset.y;
                point.z = z;
                transform.position = point;
                yield return null;
            }
        }

        // Finish exactly on the target tile (or the whole move if there is no path).
        yield return StartCoroutine(SlideTo(GetPositionOnTile(jump.toTile), speed));
        currentTile = jump.toTile;
    }

    // ------------------------------------------------------------
    // Slide straight to any tile in one go
    // (used by "Jump To Tile" special tiles in Lesson 2; 0 = START).
    // ------------------------------------------------------------
    public IEnumerator JumpToTile(int tileNumber)
    {
        yield return StartCoroutine(SlideTo(GetPositionOnTile(tileNumber), moveSpeed));
        currentTile = tileNumber;
    }

    // Smoothly moves the character to a position, a little bit every frame.
    IEnumerator SlideTo(Vector3 target, float speed)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, speed * Time.deltaTime);
            yield return null; // wait one frame, then continue the loop
        }
        transform.position = target; // snap exactly onto the tile
    }

    // The tile's position plus this player's offset,
    // keeping the character's own Z value.
    // Tile 0 = this player's place at the Start Point.
    Vector3 GetPositionOnTile(int tileNumber)
    {
        if (tileNumber <= 0)
        {
            return startPosition;
        }

        Vector3 pos = board.GetTilePosition(tileNumber);
        pos.x += tileOffset.x;
        pos.y += tileOffset.y;
        pos.z = transform.position.z;
        return pos;
    }
}
