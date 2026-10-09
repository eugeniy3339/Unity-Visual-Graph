using System;
using UnityEngine;

/// <summary>Writes a message to the console, then continues.</summary>
[Serializable]
[BlockMenu("Debug/Log")]
public class DebugLogBlock : GraphBlock
{
    [SerializeField, TextArea] private string _message = "Hello from the graph";
    [SerializeField] private LogType _logType = LogType.Log;

    public override string DisplayName => "Debug Log";

    public override void StartBlock()
    {
        switch (_logType)
        {
            case LogType.Warning: Debug.LogWarning(_message); break;
            case LogType.Error:
            case LogType.Exception: Debug.LogError(_message); break;
            default: Debug.Log(_message); break;
        }
        Fire();
    }
}
