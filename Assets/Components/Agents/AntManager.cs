using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Components.Agents
{
    public class AntManager : Singleton<AntManager>
    {
        private List<Ant> _ants = new List<Ant>();
        private Dictionary<Vector3Int, List<Ant>> _antOccupancy = new Dictionary<Vector3Int, List<Ant>>();

        public int AntCount => _ants.Count;

        /// <summary>How many mulch blocks have been consumed this generation.</summary>
        public int MulchConsumed { get; set; }

        /// <summary>How many ants are currently standing on acidic blocks.</summary>
        public int AntsOnAcid
        {
            get
            {
                int count = 0;
                foreach (var ant in _ants)
                {
                    if (ant == null) continue;
                    int groundY = ant.CurrentPosition.y - 1;
                    if (groundY < 0) continue;
                    var block = WorldManager.Instance.GetBlock(ant.CurrentPosition.x, groundY, ant.CurrentPosition.z);
                    if (block is AcidicBlock) count++;
                }
                return count;
            }
        }

        public void RegisterAnt(Ant ant)
        {
            if (!_ants.Contains(ant))
            {
                _ants.Add(ant);
                // Initial registration: add to occupancy at current position
                Vector3Int pos = ant.CurrentPosition;
                if (!_antOccupancy.ContainsKey(pos))
                {
                    _antOccupancy[pos] = new List<Ant>();
                }
                _antOccupancy[pos].Add(ant);
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

        public void UpdateOccupancy(Ant ant, Vector3Int oldPos, Vector3Int newPos)
        {
            RemoveFromOccupancy(ant, oldPos);
            
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

        public void ClearAll()
        {
            _ants.Clear();
            _antOccupancy.Clear();
            MulchConsumed = 0;
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
