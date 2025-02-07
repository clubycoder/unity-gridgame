using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * The grid level is meant to be the container of both
 * the playable game layer, the environment, and other
 * components of the game.
 * 
 * The grid level has:
 * 1. A name and description to display
 * 2. A screenshot to preview the level
 * 3. A difficulty rating from easy to nightmare
 * 4. A duration in seconds
 * 5. A the time played so far
 * 6. A grid layout with width, height, and cell size
 * 7. A state for each grid cell to inform the game player layer how that cell can be used
 * 
 * The grid level can:
 * 1. Be paused and notify child components of pause/unpause
 * 2. Notify child components of of time passing and if the duration is reached
 * 2. Provide the current state of a grid cell
 * 3. Allow a component to update a grid cell state
 * 4. Send a message to child components when a grid cell state changes
 * 
 * Child/Broadcasted Messages:
 * 1. LevelTimeChanged(float levelTime)
 * 2. LevelDurationReached(float duration)
 * 3. GridCellStateChanged({x, y, oldState, newState})
 */
public class GridLevel : MonoBehaviour
{
    [Header("Basic Level Info")]
    public string Name;
    [TextArea]
    public string Description;
    public Texture2D Screenshot;
    public enum DifficultyRating
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }
    public DifficultyRating Difficulty;
    public float Duration;

    [Header("Generation Settings")]
    public string Seed;
    [HideInInspector]
    public System.Random Rand;

    [Header("Grid Level Layout")]
    public int Width;
    public int Height;
    [Tooltip("The cell size is in units which is pixels / 100")]
    public float CellSize;

    // Level state
    private float LevelTime;
    private float LastUpdateLevelTime;
    private bool IsPaused;

    private Grid LevelGrid;

    /**
     * Each grid cell has a state that can change during play.
     * For example the environment could block a cell, and then
     * later the game play can fix/unblock that cell.
     * 
     * The states range from basic to event/game-play specific
     * states arranged between the game play and environment layers.
     */
    public enum GridCellState
    {
        Blocked, // Permanently blocked 
        Usable, // Ready to use
        Used, // Used by game-play
        Destroyed, // Was used or usable, but has been permanently destroyed
        Damaged, // Was used or usable, but has been temporarily damaged.  Game-play can repair
        Anomaly // Was used or usable, but is now affected by an anomaly.  The game-play needs to deal with or or wait
    }
    protected List<List<GridCellState>> GridCellStates;

    // Awake is called before start
    private void Awake()
    {
        // Initialize random-number-generator
        if (Seed != null && Seed.Length > 0)
        {
            Rand = new System.Random(Seed.GetHashCode());
        }
        else
        {
            Rand = new System.Random();
        }

        // Grab the Grid component
        LevelGrid = GetComponent<Grid>();
        if (LevelGrid == null)
        {
            throw new Exception("Grid not found");
        }
        LevelGrid.cellSize = new Vector3(CellSize, CellSize, 1);

        /**
         * We need to initialise the grid states here vs in
         * Start so we're ready for any updates the child
         * components make in their Starts.
         */
        GridCellStates = new List<List<GridCellState>>();
        for (int y = 0; y < Height; y++)
        {
            List<GridCellState> Row = new List<GridCellState>();
            for (int x = 0; x < Width; x++)
            {
                Row.Add(GridCellState.Usable);
                GridCellCenter(x, y);
            }
            GridCellStates.Add(Row);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // DEBUG
        Debug.Log($"Starting level: {name}");

        // Ensure we have a complete level
        if (GetComponentInChildren<GridLevelEnvironment>() == null)
        {
            throw new Exception("Environment not found");
        }
        if (GetComponentInChildren<GridLevelFactories>() == null)
        {
            throw new Exception("Game-play not found");
        }

        // Start the time at zero
        LevelTime = 0.0f;
        LastUpdateLevelTime = 0.0f;
        // Start out unpaused
        IsPaused = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTime();
    }

    // Pause is here to be called by the game UI or controls
    public void Pause()
    {
        IsPaused = true;
    }

    // Resume is here to be called by the game UI or controls
    public void Resume()
    {
        IsPaused = false;
    }

    /**
     * For performance we only want to update the child components
     * once per second.  To do this we keep track of the time that
     * we last updated the child components and only send an update
     * if a second or more hass passed or we have reached the
     * duration of the level.
     */
    private void UpdateTime()
    {
        // If we're not paused and the duration hasn't been reached
        if (!IsPaused && LevelTime < Duration)
        {
            // Add the delta time with is the fractional secinds since the last update
            // This is likely 1/60th of a second, but not guaranteed
            LevelTime += Time.deltaTime;
            // Cap the level time to the duration
            if (LevelTime > Duration)
            {
                LevelTime = Duration;
            }
            // If a second or more has passed or we're at the level duration,
            // message the child components
            if (LevelTime - LastUpdateLevelTime >= 1.0f || LevelTime >= Duration)
            {
                LastUpdateLevelTime = LevelTime;
                BroadcastMessage("LevelTimeChanged", LevelTime);
            }
            // If we have reached the duration of the level, notify the child components
            if (LevelTime >= Duration)
            {
                BroadcastMessage("LevelDurationReached", Duration);
            }
        }
    }

    /**
     * To get the world coordinate of the center of a cell
     * we are using a Grid component.  The Grid cells radiate
     * from the center with left and down being negative.
     * This means we need to:
     * 1. Substract half of the grid width from the x (column)
     * 2. Flip the y (row) and subtrack half of the grid height
     * Then we can use the Gris to get the world center.
     */
    public Vector2 GridCellCenter(int x, int y)
    {
        Debug.Log($"Grid Size: {LevelGrid.cellSize}");
        int gridX = x - (Width / 2);
        int gridY = (Height - 1 - y) - (Height / 2);
        Debug.Log($"[{y}][{x}] -> [{gridY}][{gridX}]");
        Vector3 worldCenter = LevelGrid.GetCellCenterWorld(new Vector3Int(gridX, gridY));
        Debug.Log($"[{y}][{x}].center = {worldCenter}");
        return (Vector2)worldCenter;
    }

    // Get a random list of cells that currently have any of these states 
    public List<Vector2Int> GridRandomCellsWithState(List<GridCellState> states, int numCells)
    {
        // Build list of all cells that have any of the states
        List<Vector2Int> possibleCells = new List<Vector2Int>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (states.Contains(GetGridCellState(x, y)))
                {
                    possibleCells.Add(new Vector2Int(x, y));
                }
            }
        }
        // If we have more than the number requested, shuffle them and take just the number we want
        if (possibleCells.Count > numCells)
        {
            possibleCells = possibleCells.OrderByDescending(i => Rand.Next()).Take(numCells).ToList();
        }
        return possibleCells;
    }

    /**
     * Grid cell state updates include:
     * 1. The X (column) and Y (row) of the cell updates
     * 2. The old state
     * 3. The new state
     */
    public class GridCellStateUpdate
    {
        public GridCellStateUpdate(int x, int y, GridCellState oldState, GridCellState newState)
        {
            X = x;
            Y = y;
            OldState = oldState;
            NewState = newState;
        }

        public int X;
        public int Y;
        public GridCellState OldState;
        public GridCellState NewState;
    }

    /**
     * The game-play or any other child component can call
     * this method to get the current state of a cell.
     */
    public GridCellState GetGridCellState(int x, int y)
    {
        return GridCellStates[y][x];
    }

    /**
     * The environment or any other child component can call
     * this to change the state of a cell.
     * 
     * Changes will cause a message to be sent to all childeren
     * with the update details.
     */
    public void SetGridCellState(int x, int y, GridCellState newState)
    {
        if (GridCellStates[y][x] != newState)
        {
            GridCellState oldState = GridCellStates[y][x];
            GridCellStates[y][x] = newState;
            BroadcastMessage("GridCellStateChanged", new GridCellStateUpdate(x, y, oldState, newState));
        }
    }
}
