using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gpt4All;
using UnityEngine;

/// <summary>
/// System responsible for operating on the chess units and applying the effects of the bounce mechanic 
/// </summary>
public class BounceSystem
{
    private UniversalCommandSystem _commandSystem;
    private BounceMechanicSettings _bounceSettings;
    
    public BounceSystem(BounceMechanicSettings settings)
    {
        _bounceSettings = settings;
        _commandSystem = new UniversalCommandSystem();

        RunInitialisationCommands(_bounceSettings.initialisationCommands);
    }
    
    private void RunInitialisationCommands(CommandData[] commands)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            _commandSystem.RunCommand(commands[i]);
        }
    }

    public async Task<ChessmanBounceData> CreateBounceData(LlmManager llmManager)
    {
        bool successful= _commandSystem.TryGetValue("maxHealth", out float maxHealth) & 
                         _commandSystem.TryGetValue("maxStrength", out float maxStrength);

        Debug.Assert(successful, "[BounceSystem] The 'maxHealth' and/or 'maxStrength' variables have not been set up correctly, make sure they exist before trying to access them");
  
        float health = Random.Range(1, (int)maxHealth);
        float strength = Random.Range(1, (int)maxStrength);
        
        //Removed llm use here as it was far too slow.

        return new ChessmanBounceData(strength, health);
    }

    public async Task<(int x, int y)> BounceToTile(LlmManager llmManager, List<(int x, int y)> freeTiles)
    {
        if (_bounceSettings.useLLM)
        {
            string prompt = $"between 0 and {freeTiles.Count - 1}, what number do you like the most";
            string result = await llmManager.Prompt(prompt);
            string resultString = Regex.Match(result, @"\d+").Value;
            int pickedNumber = int.Parse(resultString);

            return freeTiles[pickedNumber];
        }
        else
        {
            return freeTiles[Random.Range(0, freeTiles.Count)];
        }
    }

    public FightResult ResolveFight(Chessman attacker, Chessman defender)
    {
        FightResult fightResult = CalculateFightResult(attacker, defender);

        //Get damage values
        bool successful = _commandSystem.TryGetValue("winnerDamage", out float winnerDamage) & 
                          _commandSystem.TryGetValue("loserDamage", out float loserDamage);

        Debug.Assert(successful, "[BounceSystem] The 'winnerDamage' and/or 'loserDamage' variables have not been set up correctly, make sure they exist before trying to access them");

        switch (fightResult)
        {
            case FightResult.Win:
            {
                DamageUnit(attacker, winnerDamage);
                DamageUnit(defender, loserDamage);
                break;
            }
            case FightResult.Draw:
            {
                DamageUnit(attacker, winnerDamage);
                DamageUnit(defender, winnerDamage);
                break;
            }
            case FightResult.Lose:
            {
                DamageUnit(attacker, loserDamage);
                DamageUnit(defender, winnerDamage);
                break;
            }
        }

        return fightResult;
    }

    private FightResult CalculateFightResult(Chessman attacker, Chessman defender)
    {
        if (attacker.bounceData.strength > defender.bounceData.strength)
        {
            return FightResult.Win;
        }
        else if (attacker.bounceData.strength < defender.bounceData.strength)
        {
            return FightResult.Lose;
        }
        else
        {
            return FightResult.Draw;
        }
    }

    private void DamageUnit(Chessman unit, float amount)
    {
        unit.bounceData.currentHealth -= amount;
    }
}
