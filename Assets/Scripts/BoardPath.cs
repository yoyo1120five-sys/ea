using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;   // Splines package (Window > Package Manager)
using TMPro;

// ============================================================
// BoardPath  (GIVEN SCRIPT - students do not need to edit this)
// ------------------------------------------------------------
// Put this on an empty GameObject called "BoardPath"
// (NOT on the board picture, which is called "Board").
//
// Every CHILD of BoardPath is one tile (waypoint), and the ORDER
// of the children in the Hierarchy is the order the players walk.
//
//   BoardPath
//    ├─ Tile 1   <- first square
//    ├─ Tile 2
//    ├─ ...
//    └─ Tile 26  <- last square = the finish
//
// Each tile is just an EMPTY GameObject placed on top of a numbered
// square in the board picture. Empties are invisible in the game,
// but this script draws Gizmos (circles, numbers and lines) in the
// Scene view so you can see and check them.
// ============================================================

// One snake or ladder: "if you land on fromTile, go to toTile".
// (toTile bigger = ladder, toTile smaller = snake)
// path (optional): a Spline drawn along the snake / ladder picture.
//   The player follows its shape. It can be drawn in either direction.
//   If empty, the player moves in a straight line.
[System.Serializable]
public class BoardJump
{
    public int fromTile;
    public int toTile;
    public SplineContainer path;
}

public class BoardPath : MonoBehaviour
{
    // ------------------------------------------------------------
    // WHERE PLAYERS STAND
    // Start Point: an empty object on the START sign. Must NOT be a
    // child of BoardPath, otherwise it would be counted as a tile.
    //
    // Standing spots (Lesson 2): inside StartPoint, add one empty
    // object per player and place it where that player should wait:
    //
    //   StartPoint
    //    ├─ Spot 1   <- Player1
    //    ├─ Spot 2   <- Player2
    //    ├─ Spot 3   <- Player3
    //    └─ Spot 4   <- Player4
    //
    // Spots never move: Player1 always uses Spot 1, and so on.
    // On the tiles, players stand in the same pattern, made smaller.
    // ------------------------------------------------------------
    [Header("Where Players Stand")]
    public Transform startPoint;

    // DESIGN: how big the standing pattern is on each tile,
    // compared with the start (0.4 = 40% of the size).
    [Range(0f, 1f)]
    public float tileLayoutScale = 0.4f;

    // ------------------------------------------------------------
    // END OF LESSON 1: list every snake and ladder on your board picture.
    // (You can leave this list empty in Lesson 1.)
    //   Ladder example: From 5  To 14
    //   Snake  example: From 23 To 12
    // In the Scene view, ladders show as GREEN lines and snakes as
    // RED lines, so you can check they match your picture.
    // ------------------------------------------------------------
    [Header("Snakes & Ladders")]
    public List<BoardJump> snakesAndLadders = new List<BoardJump>();

    // DESIGN: how fast players climb ladders and slide down snakes
    // (units per second). Slow climb + fast slide feels dramatic!
    public float ladderSpeed = 3f;
    public float snakeSpeed = 8f;

    // DESIGN: a short pause before the climb / slide starts.
    public float pauseBeforeJump = 0.3f;

    // ------------------------------------------------------------
    // LESSON 2: messages for special tiles and rules.
    // Drag in a UI Text - TextMeshPro (e.g. "TileMessage").
    // It is hidden until there is a message to show.
    // ------------------------------------------------------------
    [Header("Messages (Lesson 2)")]
    public TMP_Text messageText;
    public float messageTime = 1.2f;   // how long the game waits for each message

    private Coroutine messageRoutine;

    // ------------------------------------------------------------
    // LESSON 3 (optional): sounds for snakes, ladders and special tiles.
    // Drag in a sound file (AudioClip). Leave empty = no sound.
    // ------------------------------------------------------------
    [Header("Sounds (optional)")]
    public AudioClip ladderSound;
    public AudioClip snakeSound;
    public AudioClip specialTileSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    private AudioSource soundPlayer;   // created automatically

    [Header("Scene view only")]
    // DESIGN: size of the circles drawn in the Scene view (editor only).
    public float gizmoSize = 0.3f;

    // The list of tiles, filled in automatically from the children.
    private List<Transform> tiles = new List<Transform>();

    // The centre of the start area (Start Point, or tile 1 if empty).
    public Vector3 GetStartCentre()
    {
        if (startPoint != null)
        {
            return startPoint.position;
        }
        return GetTilePosition(1);
    }

    // Where player number "index" (0 = Player1) stands at the start:
    // its Spot inside StartPoint, or the Start Point itself if there
    // is no spot for it.
    public Vector3 GetStartSpot(int index)
    {
        if (startPoint != null && index < startPoint.childCount)
        {
            return startPoint.GetChild(index).position;
        }
        return GetStartCentre();
    }

    // How many tiles the board has (the last tile number = the finish).
    public int TileCount
    {
        get { return tiles.Count; }
    }

    void Awake()
    {
        CollectTiles();

        // A speaker for the optional sounds (2D: same volume everywhere).
        soundPlayer = gameObject.AddComponent<AudioSource>();
        soundPlayer.playOnAwake = false;
        soundPlayer.spatialBlend = 0f;

        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    // Play one of the optional sounds (used by PlayerToken).
    // Does nothing if the sound slot is empty.
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && soundPlayer != null)
        {
            soundPlayer.PlayOneShot(clip, soundVolume);
        }
    }

    // Show a message for a short time (used by PlayerToken).
    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }
        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageTime * 1.5f);
        messageText.gameObject.SetActive(false);
    }

    // Reads all children of this object, in Hierarchy order.
    public void CollectTiles()
    {
        tiles.Clear();
        foreach (Transform child in transform)
        {
            tiles.Add(child);
        }
    }

    // Returns the world position of a tile.
    // NOTE: tile numbers start at 1 (like the real board game),
    // but lists in C# start at 0, so we subtract 1 here.
    // (Tile 0 = the Start Point - see GetStartCentre.)
    public Vector3 GetTilePosition(int tileNumber)
    {
        int index = Mathf.Clamp(tileNumber - 1, 0, tiles.Count - 1);
        return tiles[index].position;
    }

    // ------------------------------------------------------------
    // Is there a snake or ladder on this tile?
    // Returns it, or null if it is a normal tile.
    // (Used by PlayerToken after every move.)
    // ------------------------------------------------------------
    public BoardJump GetJump(int tileNumber)
    {
        foreach (BoardJump jump in snakesAndLadders)
        {
            if (jump != null && jump.fromTile == tileNumber)
            {
                return jump;
            }
        }
        return null;
    }

    // ------------------------------------------------------------
    // OPTIONAL (special squares): returns the special square script on
    // a tile, or null if that tile is a normal tile. (Used by PlayerToken.)
    // Works with or without SpecialTile.cs in the project.
    // ------------------------------------------------------------
    public TileAction GetSpecialTile(int tileNumber)
    {
        if (tileNumber < 1 || tileNumber > tiles.Count)
        {
            return null;
        }
        return tiles[tileNumber - 1].GetComponent<TileAction>();
    }

    // ------------------------------------------------------------
    // HELPER: right-click the BoardPath component title in the
    // Inspector and choose "Rename And Number Tiles".
    // It renames every child to "Tile 1", "Tile 2"...
    // ------------------------------------------------------------
    [ContextMenu("Rename And Number Tiles")]
    void RenameAndNumberTiles()
    {
        int number = 1;
        foreach (Transform child in transform)
        {
            child.name = "Tile " + number;

            TMP_Text label = child.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = number.ToString();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(label);
#endif
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(child.gameObject);
#endif
            number++;
        }
    }

    // ------------------------------------------------------------
    // GIZMOS: helper drawings that appear only in the Scene view,
    // never in the game:
    //   yellow line + circles + numbers = the walking path
    //   green circle                    = start point
    //   small green circles P1, P2...   = standing spots
    //   green line = ladder,  red line  = snake
    //   (curved if the snake / ladder has a Spline path)
    // ------------------------------------------------------------
    void OnDrawGizmos()
    {
        // The walking path.
        Transform previous = startPoint;
        int number = 1;

        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, gizmoSize);

            // Standing spots: small circles labelled P1, P2...
            for (int i = 0; i < startPoint.childCount; i++)
            {
                Vector3 spot = startPoint.GetChild(i).position;
                Gizmos.DrawWireSphere(spot, gizmoSize * 0.5f);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(spot + Vector3.up * 0.25f, "P" + (i + 1));
#endif
            }
        }

        foreach (Transform child in transform)
        {
            Gizmos.color = Color.yellow;
            if (previous != null)
            {
                Gizmos.DrawLine(previous.position, child.position);
            }
            Gizmos.DrawWireSphere(child.position, gizmoSize);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(child.position + Vector3.up * 0.4f, number.ToString());
#endif
            previous = child;
            number++;
        }

        // Snakes and ladders.
        int childCount = transform.childCount;
        foreach (BoardJump jump in snakesAndLadders)
        {
            if (jump == null) continue;
            if (jump.fromTile < 1 || jump.fromTile > childCount) continue;
            if (jump.toTile < 1 || jump.toTile > childCount) continue;

            Vector3 from = transform.GetChild(jump.fromTile - 1).position;
            Vector3 to = transform.GetChild(jump.toTile - 1).position;

            Gizmos.color = jump.toTile > jump.fromTile ? Color.green : Color.red;

            if (jump.path == null || jump.path.Spline == null || jump.path.Spline.Count < 2)
            {
                Gizmos.DrawLine(from, to);
            }
            else
            {
                // Draw the spline's shape as many short lines.
                Transform pathTransform = jump.path.transform;
                Vector3 previousPoint = pathTransform.TransformPoint(jump.path.Spline.EvaluatePosition(0f));
                for (int i = 1; i <= 30; i++)
                {
                    Vector3 point = pathTransform.TransformPoint(jump.path.Spline.EvaluatePosition(i / 30f));
                    Gizmos.DrawLine(previousPoint, point);
                    previousPoint = point;
                }
            }
            Gizmos.DrawSphere(to, gizmoSize * 0.5f); // dot shows where you land
        }
    }
}

// ============================================================
// TileAction - the "plug" for special squares.
// Anything that happens when a player lands on a tile builds on
// this (SpecialTile.cs, given later, is one). Nothing to set up.
// ============================================================
public abstract class TileAction : MonoBehaviour
{
    // Runs after a player lands on this tile.
    public abstract IEnumerator Run(PlayerToken player, bool bounceBack);
}
