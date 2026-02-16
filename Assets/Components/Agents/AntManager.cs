using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Components.Agents
{
    public class AntManager : Singleton<AntManager>
    {
        private List<Ant> _ants = new List<Ant>();
        private Dictionary<Vector3Int, List<Ant>> _antOccupancy = new Dictionary<Vector3Int, List<Ant>>();

        public void RegisterAnt(Ant ant)
        {
            if (!_ants.Contains(ant))
            {
                _ants.Add(ant);
                UpdateOccupancy(ant, ant.CurrentPosition);
            }
        }

        public void UnregisterAnt(Ant ant)
        {
            if (_ants.Contains(ant))
            {
                _ants.Remove(ant);
                RemoveFromOccupancy(ant, ant.CurrentPosition);
            }
        }

        public void UpdateOccupancy(Ant ant, Vector3Int newPos)
        {
            RemoveFromOccupancy(ant, ant.CurrentPosition);
            
            if (!_antOccupancy.ContainsKey(newPos))
            {
                _antOccupancy[newPos] = new List<Ant>();
            }
            _antOccupancy[newPos].Add(ant);
        }

        private void RemoveFromOccupancy(Ant ant, Vector3Int pos)
        {
            if (_antOccupancy.ContainsKey(pos))
            {
                _antOccupancy[pos].Remove(ant);
                if (_antOccupancy[pos].Count == 0)
                {
                    _antOccupancy.Remove(pos);
                }
            }
        }

        public List<Ant> GetAntsAt(Vector3Int pos)
        {
            if (_antOccupancy.ContainsKey(pos))
            {
                return _antOccupancy[pos];
            }
            return new List<Ant>();
        }

        private void FixedUpdate()
        {
            // Update all ants
            // iterate reversed to allow removal
            for (int i = _ants.Count - 1; i >= 0; i--)
            {
                if (_ants[i] != null)
                {
                    _ants[i].OnUpdate();
                }
            }
        }
    }
}
