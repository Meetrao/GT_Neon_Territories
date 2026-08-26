using System;
using System.Collections.Generic;

namespace NeonTerritories.Territories
{
    public class TerritoryGraph
    {
        public Dictionary<string, Territory> Territories { get; private set; }

        public TerritoryGraph()
        {
            Territories = new Dictionary<string, Territory>();
            GenerateDefaultNeonCityMap();
        }

        /// <summary>
        /// Generates the default map layout for Neon City with 14 interconnected territories.
        /// </summary>
        private void GenerateDefaultNeonCityMap()
        {
            // 1. Create Territories (ID, Name, Type, Base Defense, Strategic Value)
            AddTerritory(new Territory("T01", "Energy Grid North", TerritoryType.Energy, 20, 60));
            AddTerritory(new Territory("T02", "Silicon Valley East", TerritoryType.Technology, 25, 70));
            AddTerritory(new Territory("T03", "Financial Sector North", TerritoryType.Credit, 15, 65));
            AddTerritory(new Territory("T04", "Defense Citadel West", TerritoryType.Defense, 60, 80));
            AddTerritory(new Territory("T05", "Industrial Belt", TerritoryType.Energy, 30, 75));
            AddTerritory(new Territory("T06", "Commercial Hub East", TerritoryType.Credit, 20, 70));
            AddTerritory(new Territory("T07", "Research Lab South", TerritoryType.Technology, 35, 85));
            AddTerritory(new Territory("T08", "Central Core", TerritoryType.Core, 80, 150));
            AddTerritory(new Territory("T09", "Harbor District", TerritoryType.Credit, 20, 65));
            AddTerritory(new Territory("T10", "Tech Plaza West", TerritoryType.Technology, 25, 70));
            AddTerritory(new Territory("T11", "Power Grid West", TerritoryType.Energy, 20, 60));
            AddTerritory(new Territory("T12", "Defense Gate East", TerritoryType.Defense, 55, 75));
            AddTerritory(new Territory("T13", "Neon Boulevard", TerritoryType.Credit, 18, 68));
            AddTerritory(new Territory("T14", "Sub-Core Sub-Zero", TerritoryType.Core, 70, 130));

            // 2. Set Up Bidirectional Connections (Edges)
            // North Region
            AddConnection("T01", "T02");
            AddConnection("T01", "T03");
            AddConnection("T01", "T04");

            // Tech & Industry Sector
            AddConnection("T02", "T05");
            AddConnection("T02", "T08");
            AddConnection("T03", "T06");
            AddConnection("T03", "T08");

            // West Defense & Energy
            AddConnection("T04", "T05");
            AddConnection("T04", "T11");
            
            // Central Belt
            AddConnection("T05", "T07");
            AddConnection("T05", "T08");
            AddConnection("T06", "T07");
            AddConnection("T06", "T08");
            AddConnection("T06", "T09");

            // South Tech & Harbors
            AddConnection("T07", "T10");
            AddConnection("T09", "T12");
            AddConnection("T09", "T13");

            // West-South Nodes
            AddConnection("T10", "T11");
            AddConnection("T10", "T14");
            AddConnection("T11", "T14");

            // Central Core Connections
            AddConnection("T08", "T13");
            AddConnection("T08", "T14");

            // East-South Nodes
            AddConnection("T12", "T13");
            AddConnection("T12", "T14");
            AddConnection("T13", "T14");
        }

        private void AddTerritory(Territory territory)
        {
            if (!Territories.ContainsKey(territory.Id))
            {
                Territories.Add(territory.Id, territory);
            }
        }

        private void AddConnection(string idA, string idB)
        {
            if (Territories.ContainsKey(idA) && Territories.ContainsKey(idB))
            {
                Territories[idA].AddNeighbor(idB);
                Territories[idB].AddNeighbor(idA);
            }
        }

        /// <summary>
        /// Retrieves a territory by ID.
        /// </summary>
        public Territory GetTerritory(string id)
        {
            if (Territories.TryGetValue(id, out Territory territory))
            {
                return territory;
            }
            return null;
        }

        /// <summary>
        /// Check if two territories are neighbors.
        /// </summary>
        public bool AreNeighbors(string idA, string idB)
        {
            if (Territories.TryGetValue(idA, out Territory t))
            {
                return t.IsNeighborOf(idB);
            }
            return false;
        }

        /// <summary>
        /// Finds the shortest path in terms of movement/attack distance.
        /// Useful for AI navigation or path planning.
        /// </summary>
        public List<string> FindShortestPath(string startId, string endId)
        {
            if (!Territories.ContainsKey(startId) || !Territories.ContainsKey(endId))
                return null;

            Queue<string> queue = new Queue<string>();
            Dictionary<string, string> parentMap = new Dictionary<string, string>();
            HashSet<string> visited = new HashSet<string>();

            queue.Enqueue(startId);
            visited.Add(startId);

            bool found = false;
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (current == endId)
                {
                    found = true;
                    break;
                }

                foreach (string neighbor in Territories[current].NeighborIds)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parentMap[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (!found) return null;

            // Reconstruct path
            List<string> path = new List<string>();
            string curr = endId;
            while (curr != startId)
            {
                path.Add(curr);
                curr = parentMap[curr];
            }
            path.Add(startId);
            path.Reverse();
            return path;
        }
    }
}
