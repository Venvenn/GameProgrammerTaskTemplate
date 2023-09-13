using UnityEngine;

[CreateAssetMenu(fileName = "BounceMechanicSettings", menuName = "Data/Settings/BounceMechanicSettings")]
public class BounceMechanicSettings : ScriptableObject
{
    [Tooltip("If true the LLM will be used, if false the designer runtime data will be used")]
    public bool useLLM;
    [Tooltip("Commands run at the start of the  ")]
    public CommandData[] initialisationCommands;
}
