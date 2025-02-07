using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Each environment has:
 * 1. A Background that is rendered behind the main game layer
 * 2. A Foreground that is rendered infront of the main game layer
 * 
 * Each environment can:
 * 1. Have a random set of obstacles affecting the level grid
 * 2. Have a set of events that are triggered as the level time passes.
 *    The current time comes from messages LevelTimeChanged messages.
 * 
 * Each environment can be extended by:
 * 1. Attaching an environment specific script that handles StartEvent messages
 * 
 * Local/Sent Messages:
 * 1. StartEvent(string eventName)
 */
public class GridLevelEnvironment : MonoBehaviour
{
    /**
     * We need a reference to the level this environment is in
     * so we can:
     * 1. Get the duration
     */
    [Header("Basic Environment Settings")]
    [Tooltip("Provide referance to level")]
    public GridLevel Level;
    /**
     * The environment is drawn as a Bachground and Foreground so
     * we need to get references to these layers.
     */
    public GameObject Background;
    public GameObject Foreground;

    /**
     * Most levels will want to have some basic random variations
     * like a few random obstacles.
     */
    [Header("Basic Environment Generation")]
    public int NumRandomObstacles;
    public List<GameObject> RandomObstaclePrefabs;

    /**
     * Each Environment event has a name at a time to start
     * as a percentage of the level duration.  If the level
     * duration is 20min, 25% means start 5 minutes in to the
     * level play.
     * 
     * At the start of an event, the StartEvent message will
     * be sent to the components in the environment along
     * with the event name.  The enviroment specific script
     * will have a method like this:
     * 
     * public void StartEvent(string eventName)
     * {
     *     if (eventName == "Some Event") {
     *         ... Code to start Some Event ...
     *     } else if (...) {
     *     }
     * }
     * 
     * that will start the environment specific event.
     */
    [Serializable]
    public class EnvironmentEvent
    {
        public string Name;
        [Tooltip("Start event at 0 - 100% of level duration")]
        [Range(0, 100)]
        public int StartAtTimePercentage;
        protected internal bool Started;
    }

    [Header("Environment Events")]
    public List<EnvironmentEvent> Events;

    // Awake is called before Start
    void Awake()
    {
    }

    // Start is called before the first frame Update
    void Start()
    {
        // Ensure we have what we need
        if (Level == null)
        {
            throw new Exception("Level is missing");
        }
        if (Background == null)
        {
            throw new Exception("Background is missing");
        }
        if (Foreground == null)
        {
            throw new Exception("Foreground is missing");
        }

        AddRandomObstacles();
    }

    // Update is called once per frame
    void Update()
    {
    }

    // Add random obstacles to the background and block
    // those cells on the grid.
    private void AddRandomObstacles()
    {
        Debug.Log($"RandomObstacles: {NumRandomObstacles} {RandomObstaclePrefabs.Count}");
        if (NumRandomObstacles > 0 && RandomObstaclePrefabs.Count > 0)
        {
            // Get a list of random, usable cells to block
            List<Vector2Int> cells = Level.GridRandomCellsWithState(
                new List<GridLevel.GridCellState>() { GridLevel.GridCellState.Usable },
                NumRandomObstacles
            );
            foreach (var cell in cells) {
                // Update the cell state as blocked
                Level.SetGridCellState(cell.x, cell.y, GridLevel.GridCellState.Blocked);
                // Add a random obstacle prefab to the background at the cell's center
                int prefabNum = Level.Rand.Next(0, RandomObstaclePrefabs.Count);
                GameObject obstacle = Instantiate(RandomObstaclePrefabs[prefabNum], Background.transform);
                Vector2 cellCenter = Level.GridCellCenter(cell.x, cell.y);
                Debug.Log($"Cell[{cell.y}][{cell.x}].Center = {cellCenter}");
                obstacle.transform.position = (Vector3)cellCenter;
            }
        }
    }

    // Level.LevelTimeChanged message handler
    public void LevelTimeChanged(float levelTime)
    {
        // Debug.Log($"Time changed to {levelTime}");

        // Now that we have the time updated, update the events
        foreach (var e in Events)
        {
            if (!e.Started && (levelTime / Level.Duration) >= ((float)e.StartAtTimePercentage / 100.0f))
            {
                // Mark the event as started so we don't do it twice
                e.Started = true;
                /**
                 * To start the event, we just message all of the components
                 * that are part of the environment.
                 */
                SendMessage("StartEvent", e.Name);
            }
        }
    }
}
