using System;
using System.Collections.Generic;

namespace NeonTerritories.Players
{
    public enum FactionType
    {
        NOVA,     // Economic Advantage
        VORTEX,   // Military Advantage
        PULSE,    // Technology Advantage
        SYNTH     // Information/Diplomatic Advantage
    }

    [Serializable]
    public class Player
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public FactionType Faction { get; private set; }
        public PlayerResources Resources { get; private set; }
        public List<string> ControlledTerritoryIds { get; private set; }
        
        public int StrategicScore { get; set; }
        public int Reputation { get; set; } // 0 to 100
        public int ActionPoints { get; set; } // AP available in current turn
        public bool IsAI { get; set; }

        // Tracks faction-specific stats or modifiers
        public float CreditGainModifier { get; set; } = 1.0f;
        public float AttackPowerModifier { get; set; } = 1.0f;
        public float TechGainModifier { get; set; } = 1.0f;
        public float SpyCostModifier { get; set; } = 1.0f;

        public Player(string id, string name, FactionType faction, bool isAI = false)
        {
            Id = id;
            Name = name;
            Faction = faction;
            IsAI = isAI;
            Resources = new PlayerResources();
            ControlledTerritoryIds = new List<string>();
            StrategicScore = 0;
            Reputation = 50; // Neutral starting reputation
            ActionPoints = 3; // Standard 3 AP per round

            ApplyFactionModifiers();
            ApplyFactionStartingResources();
        }

        private void ApplyFactionModifiers()
        {
            switch (Faction)
            {
                case FactionType.NOVA:
                    CreditGainModifier = 1.25f; // 25% credit bonus
                    break;
                case FactionType.VORTEX:
                    AttackPowerModifier = 1.2f; // 20% combat strength bonus
                    break;
                case FactionType.PULSE:
                    TechGainModifier = 1.25f; // 25% technology generation bonus
                    break;
                case FactionType.SYNTH:
                    SpyCostModifier = 0.75f; // 25% discount on spy/intel actions
                    Reputation = 70; // Starts with better diplomatic trust
                    break;
            }
        }

        private void ApplyFactionStartingResources()
        {
            // Set default starting values
            Resources.Credits = 100;
            Resources.Energy = 50;
            Resources.Technology = 20;
            Resources.Influence = 20;

            // Apply faction-specific starting bonuses
            switch (Faction)
            {
                case FactionType.NOVA:
                    Resources.Credits += 50;
                    break;
                case FactionType.VORTEX:
                    Resources.Energy += 30;
                    break;
                case FactionType.PULSE:
                    Resources.Technology += 20;
                    break;
                case FactionType.SYNTH:
                    Resources.Influence += 30;
                    break;
            }
        }

        public void AddTerritory(string territoryId)
        {
            if (!ControlledTerritoryIds.Contains(territoryId))
            {
                ControlledTerritoryIds.Add(territoryId);
            }
        }

        public bool RemoveTerritory(string territoryId)
        {
            return ControlledTerritoryIds.Remove(territoryId);
        }

        public void ResetRoundActionPoints(int baseAP = 3)
        {
            ActionPoints = baseAP;
        }

        public override string ToString()
        {
            return $"{Name} ({Faction}) | Score: {StrategicScore} | Rep: {Reputation} | {Resources}";
        }
    }
}
