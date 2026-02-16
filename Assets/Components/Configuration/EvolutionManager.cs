using System.Collections.Generic;
using UnityEngine;
using Antymology.Components.Agents;
using Antymology.Terrain;
using System.Linq;

namespace Antymology.Components.Configuration
{
    public class EvolutionManager : Singleton<EvolutionManager>
    {
        public int GenerationCount { get; private set; } = 1;
        public float TimeRemaining { get; private set; }

        private List<Genome> _population = new List<Genome>();
        private bool _isSimulating = false;

        // Removed Start() as WorldManager calls StartGeneration now.

        public void StartGeneration()
        {
            TimeRemaining = ConfigurationManager.Instance.Generation_Duration;
            CollisionTracker.Clear(); // Clear any cached counters

            // Ensure population initialized
            if (_population.Count == 0)
            {
                for (int i = 0; i < ConfigurationManager.Instance.Population_Size; i++)
                {
                    // 6 Inputs * 6 Outputs = 36 Weights
                    _population.Add(new Genome(36));
                }
            }

            // Spawn Ants
            if (WorldManager.Instance.antPrefab == null)
            {
                Debug.LogError("Ant prefab missing");
                return;
            }

            // 1. Spawn Queen (from WorldManager so it is assignable in Inspector)
            GameObject qp = WorldManager.Instance.queenPrefab;

            if (qp != null)
            {
                SpawnAnt(qp, _population[0], true);
            }
            else
            {
                Debug.LogWarning("Queen Prefab not assigned on WorldManager! Spawning worker as queen placeholder.");
                SpawnAnt(WorldManager.Instance.antPrefab, _population[0], true);
            }

            // 2. Spawn Workers
            for (int i = 1; i < _population.Count; i++)
            {
                SpawnAnt(WorldManager.Instance.antPrefab, _population[i], false);
            }

            _isSimulating = true;
        }

        private void SpawnAnt(GameObject prefab, Genome genome, bool isQueen)
        {
            // Random valid position
            int dimX = WorldManager.Instance.GetBlockLayerDimension(0);
            int dimZ = WorldManager.Instance.GetBlockLayerDimension(2);
            int dimY = WorldManager.Instance.GetBlockLayerDimension(1);

            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = UnityEngine.Random.Range(1, dimX - 1);
                int z = UnityEngine.Random.Range(1, dimZ - 1);

                // Find surface
                int y = -1;
                for (int h = dimY - 2; h >= 0; h--)
                {
                    AbstractBlock b = WorldManager.Instance.GetBlock(x, h, z);
                    if (b is not AirBlock && b is not ContainerBlock)
                    {
                        y = h;
                        break;
                    }
                }

                if (y != -1)
                {
                    // Ant occupies the AIR cell above the ground block at y
                    int antY = y + 1;

                    Vector3 pos = new Vector3(x, y + 1.5f, z);
                    GameObject go = Instantiate(prefab, pos, Quaternion.identity);

                    Ant antScript;
                    if (isQueen)
                    {
                        // If prefab already has Queen script, GetComponent
                        antScript = go.GetComponent<Queen>();
                        if (antScript == null) antScript = go.AddComponent<Queen>();

                        // Visual distinction if using same prefab
                        if (prefab == WorldManager.Instance.antPrefab)
                        {
                            go.transform.localScale *= 1.5f;
                            go.GetComponent<Renderer>().material.color = Color.red;
                        }
                    }
                    else
                    {
                        antScript = go.GetComponent<Ant>();
                        if (antScript == null) antScript = go.AddComponent<Ant>();
                    }

                    // FIXED: Init should receive the grid cell the ant occupies (air cell), not the ground cell
                    antScript.Init(x, antY, z, genome);
                    return;
                }
            }
        }

        private void Update()
        {
            if (!_isSimulating) return;

            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining <= 0)
            {
                EndGeneration();
            }
        }

        // Tracking best genome for simple hill climbing
        private Genome _bestGenome;
        private int _bestFitness = -1;

        private void EndGeneration()
        {
            _isSimulating = false;

            int currentFitness = CountNestBlocks();
            Debug.Log($"Generation {GenerationCount} ended. Fitness: {currentFitness}");

            // Simple Evolution Strategy:
            // If this generation performed better (or it's the first), set as Best.
            // Then populate next generation with mutations of Best.

            if (_bestGenome == null || currentFitness > _bestFitness)
            {
                _bestFitness = currentFitness;

                if (_population.Count > 0)
                    _bestGenome = new Genome(_population[0].Weights); // Copy
            }

            // Re-populate for next gen
            _population.Clear();

            // Elitism: Keep best
            if (_bestGenome != null)
                _population.Add(new Genome(_bestGenome.Weights));
            else
                _population.Add(new Genome(36)); // Random start if failed

            // Fill rest with mutations of Best
            while (_population.Count < ConfigurationManager.Instance.Population_Size)
            {
                Genome mutant = new Genome(_bestGenome.Weights);
                mutant.Mutate(ConfigurationManager.Instance.Mutation_Rate, ConfigurationManager.Instance.Mutation_Strength);
                _population.Add(mutant);
            }

            // Clean up
            var ants = FindObjectsOfType<Ant>();
            foreach (var a in ants) Destroy(a.gameObject);

            WorldManager.Instance.ResetWorld();

            GenerationCount++;
            StartGeneration();
        }

        private int CountNestBlocks()
        {
            int count = 0;
            // Scan world for NestBlocks
            int dimX = WorldManager.Instance.GetBlockLayerDimension(0);
            int dimY = WorldManager.Instance.GetBlockLayerDimension(1);
            int dimZ = WorldManager.Instance.GetBlockLayerDimension(2);

            for (int x = 0; x < dimX; x++)
            for (int y = 0; y < dimY; y++)
            for (int z = 0; z < dimZ; z++)
            {
                if (WorldManager.Instance.GetBlock(x, y, z) is NestBlock) count++;
            }
            return count;
        }
    }

    public static class CollisionTracker
    {
        public static int NestBlockCount = 0; // Usage deprecated in favor of EndGeneration scan
        public static void Clear() { NestBlockCount = 0; }
    }
}
