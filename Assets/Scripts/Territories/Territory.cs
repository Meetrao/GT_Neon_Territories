using System;
using System.Collections.Generic;
using NeonTerritories.Players;

namespace NeonTerritories.Territories
{
    public enum TerritoryType
    {
        Energy,
        Technology,
        Credit,
        Defense,
        Core
    }

    [Serializable]
    public class Territory
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public TerritoryType Type { get; private set; }
        public string OwnerId { get; set; } // Empty or null if neutral
        
        public PlayerResources GenerationRate { get; private set; }
        public int BaseDefense { get; set; }
        public int CurrentDefense { get; set; }
        public int StrategicValue { get; set; }
        public int InvestmentLevel { get; set; }

        public List<string> NeighborIds { get; private set; }

        public Territory(string id, string name, TerritoryType type, int baseDefense, int strategicValue)
        {
            Id = id;
            Name = name;
            Type = type;
            OwnerId = string.Empty;
            BaseDefense = baseDefense;
            CurrentDefense = baseDefense;
            StrategicValue = strategicValue;
            InvestmentLevel = 0;
            NeighborIds = new List<string>();
            
            InitializeGenerationRate();
        }

        private void InitializeGenerationRate()
        {
            GenerationRate = new PlayerResources();
            switch (Type)
            {
                case TerritoryType.Energy:
                    GenerationRate.Energy = 30;
                    break;
                case TerritoryType.Technology:
                    GenerationRate.Technology = 20;
                    break;
                case TerritoryType.Credit:
                    GenerationRate.Credits = 40;
                    break;
                case TerritoryType.Defense:
                    GenerationRate.Influence = 10; // Defense generates some diplomatic influence
                    break;
                case TerritoryType.Core:
                    // Core provides a balanced mix of all resources
                    GenerationRate.Credits = 15;
                    GenerationRate.Energy = 15;
                    GenerationRate.Technology = 10;
                    GenerationRate.Influence = 10;
                    break;
            }
        }

        /// <summary>
        /// Applies an investment to upgrade the territory's stats.
        /// </summary>
        public void Invest(PlayerResources investmentCost)
        {
            InvestmentLevel++;
            
            // Upgrade defense and strategic value
            CurrentDefense += 10;
            StrategicValue += 15;

            // Increase generation rate based on type
            switch (Type)
            {
                case TerritoryType.Energy:
                    GenerationRate.Energy += 10;
                    break;
                case TerritoryType.Technology:
                    GenerationRate.Technology += 8;
                    break;
                case TerritoryType.Credit:
                    GenerationRate.Credits += 15;
                    break;
                case TerritoryType.Defense:
                    CurrentDefense += 15; // Extra defense boost
                    GenerationRate.Influence += 5;
                    break;
                case TerritoryType.Core:
                    GenerationRate.Credits += 5;
                    GenerationRate.Energy += 5;
                    GenerationRate.Technology += 3;
                    GenerationRate.Influence += 3;
                    break;
            }
        }

        public void AddNeighbor(string neighborId)
        {
            if (!NeighborIds.Contains(neighborId))
            {
                NeighborIds.Add(neighborId);
            }
        }

        public bool IsNeighborOf(string otherId)
        {
            return NeighborIds.Contains(otherId);
        }

        public void ResetDefense()
        {
            // Reset to base + investment bonuses
            CurrentDefense = BaseDefense + (InvestmentLevel * 10) + (Type == TerritoryType.Defense ? InvestmentLevel * 15 : 0);
        }

        public override string ToString()
        {
            string owner = string.IsNullOrEmpty(OwnerId) ? "Neutral" : OwnerId;
            return $"[{Id}] {Name} ({Type}) | Owner: {owner} | Def: {CurrentDefense} | Value: {StrategicValue}";
        }
    }
}
