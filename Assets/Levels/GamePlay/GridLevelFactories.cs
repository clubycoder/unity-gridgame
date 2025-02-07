using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridLevelFactories : MonoBehaviour
{
    /**
     * We need a reference to the level this environment is in
     * so we can:
     * 1. Get the duration
     */
    [Header("Basic Game Play Settings")]
    [Tooltip("Provide referance to level")]
    public GridLevel Level;

    // Debugging prefabs
    public GameObject UsablePrefab;
    public GameObject BlockedPrefab;
    public GameObject DamagedPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Starting game-play");
        // Ensure we have what we need
        if (Level == null)
        {
            throw new Exception("Level is missing");
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    // Level.GridCellStateChanged message handler
    public void GridCellStateChanged(GridLevel.GridCellStateUpdate update)
    {
        Debug.Log($"Grid[{update.Y}][{update.X}] changed from {update.OldState} to {update.NewState}");
        string debugName = $"debug_{update.Y}_{update.X}";
        Transform oldDebugStateObjectTransform = transform.Find(debugName);
        if (oldDebugStateObjectTransform != null)
        {
            Destroy(oldDebugStateObjectTransform.gameObject);
        }
        GameObject debugStateObject;
        if (update.NewState == GridLevel.GridCellState.Usable)
        {
            debugStateObject = Instantiate(UsablePrefab, transform);
        }
        else if (update.NewState == GridLevel.GridCellState.Blocked)
        {
            debugStateObject = Instantiate(BlockedPrefab, transform);
        }
        else
        {
            debugStateObject = Instantiate(DamagedPrefab, transform);
        }
        debugStateObject.name = debugName;
        debugStateObject.transform.position = Level.GridCellCenter(update.X, update.Y);
    }

    // Level.LevelDurationReached message handler
    public void LevelDurationReached(float duration)
    {
        Debug.Log($"[Factories] Level duration {duration} reached.  Game over man!");
    }

}
