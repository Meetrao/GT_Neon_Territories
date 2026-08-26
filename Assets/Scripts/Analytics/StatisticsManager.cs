using System;
using System.Collections.Generic;
using NeonTerritories.Strategies;

namespace NeonTerritories.Analytics
{
    public class StatisticsManager
    {
        public int TotalAttacks { get; private set; }
        public int TotalDefenses { get; private set; }
        public int TotalExpansions { get; private set; }
        public int TotalInvestments { get; private set; }
        public int TotalSpies { get; private set; }
        public int TotalAlliancesFormed { get; private set; }

        private Dictionary<string, Dictionary<ActionType, int>> _factionActionCounts;
        private List<float> _nashAlignmentSamples;

        public StatisticsManager()
        {
            _factionActionCounts = new Dictionary<string, Dictionary<ActionType, int>>();
            _nashAlignmentSamples = new List<float>();
            Reset();
        }

        public void Reset()
        {
            TotalAttacks = 0;
            TotalDefenses = 0;
            TotalExpansions = 0;
            TotalInvestments = 0;
            TotalSpies = 0;
            TotalAlliancesFormed = 0;
            _factionActionCounts.Clear();
            _nashAlignmentSamples.Clear();
        }

        public void RecordAction(string factionName, ActionType type)
        {
            if (!_factionActionCounts.ContainsKey(factionName))
            {
                _factionActionCounts[factionName] = new Dictionary<ActionType, int>();
                foreach (ActionType act in Enum.GetValues(typeof(ActionType)))
                {
                    _factionActionCounts[factionName][act] = 0;
                }
            }

            _factionActionCounts[factionName][type]++;

            switch (type)
            {
                case ActionType.Attack: TotalAttacks++; break;
                case ActionType.Defend: TotalDefenses++; break;
                case ActionType.Expand: TotalExpansions++; break;
                case ActionType.Invest: TotalInvestments++; break;
                case ActionType.Spy: TotalSpies++; break;
            }
        }

        public void RecordAllianceFormed()
        {
            TotalAlliancesFormed++;
        }

        public void RecordNashAlignment(float alignmentPercentage)
        {
            _nashAlignmentSamples.Add(alignmentPercentage);
        }

        public float GetAverageNashAlignment()
        {
            if (_nashAlignmentSamples.Count == 0) return 0f;
            float sum = 0f;
            foreach (float val in _nashAlignmentSamples)
            {
                sum += val;
            }
            return sum / _nashAlignmentSamples.Count;
        }

        public Dictionary<ActionType, float> GetFactionActionPercentages(string factionName)
        {
            var percentages = new Dictionary<ActionType, float>();
            if (!_factionActionCounts.ContainsKey(factionName)) return percentages;

            var counts = _factionActionCounts[factionName];
            int total = 0;
            foreach (var count in counts.Values)
            {
                total += count;
            }

            if (total == 0) return percentages;

            foreach (var kvp in counts)
            {
                percentages[kvp.Key] = (float)kvp.Value / total;
            }

            return percentages;
        }
    }
}
