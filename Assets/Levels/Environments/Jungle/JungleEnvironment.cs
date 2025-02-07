using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JungleEnvironment : MonoBehaviour
{
    private GridLevelEnvironment Environment;

    private void Start()
    {
        Environment = GetComponent<GridLevelEnvironment>();
        if (Environment == null)
        {
            throw new Exception("Environment not found");
        }
    }

    // Environment.StartEvent message handler
    public void StartEvent(string eventName)
    {
        Debug.Log($"Event {eventName} started");
        if (eventName == "Damage Cells")
        {
            List<Vector2Int> cells = Environment.Level.GridRandomCellsWithState(
                new List<GridLevel.GridCellState> { GridLevel.GridCellState.Usable },
                3
            );
            foreach (var cell in cells)
            {
                Environment.Level.SetGridCellState(cell.x, cell.y, GridLevel.GridCellState.Damaged);
            }
        }
    }

    // Level.LevelDurationReached message handler
    public void LevelDurationReached(float duration)
    {
        Debug.Log($"[Environment] Level duration {duration} reached.  Blowing up planet!");
    }
}
