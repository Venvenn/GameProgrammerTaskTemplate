using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// System for running commands and storing command variables
/// </summary>
public class UniversalCommandSystem 
{
    private Dictionary<string, object> _variables = new Dictionary<string, object>();

    public void RunCommand(CommandData commandData)
    {
        switch (commandData.commandType)
        {
            case "set":
            {
                SetValue(commandData.variableName, commandData.value);
                break;
            }
            default:
            {
                Debug.LogError($"[UniversalCommandSystem] The command {commandData.commandType} is invalid");
                return;
            }
        }
    }

    public void SetValue(string variableName, string stringValue)
    {
        _variables[variableName] = stringValue;
    }

    public void SetValue(string variableName, float floatValue)
    {
        _variables[variableName] = floatValue;
    }

    public void SetValue(string variableName, bool boolValue)
    {
        _variables[variableName] = boolValue;
    }

    public bool TryGetValue<T>(string variableName, out T result)
    {
        if (_variables.ContainsKey(variableName))
        {
            result = (T)_variables[variableName];
            return result != null;
        }
  
        Debug.LogWarning($"[UniversalCommandSystem] You are trying to get the value of variable {variableName}, however no variable with that name currently exists, make sure to add a variable before you access it");
        
        result = default;
        return false;
    }

    public void Clear()
    {
        _variables.Clear();
    }

    public bool Contains(string variableName)
    {
        return _variables.ContainsKey(variableName);
    }
}
