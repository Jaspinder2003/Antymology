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

        // Tracking best genome for simple hill climbing
        private Genome _bestGenome;
        private int _bestFitness = -1;

        // Neural-net sizing: 6 inputs * 6 outputs = 36 weights
        private const int GENOME_SIZE = 36;

        public void StartGeneration()
        {
            TimeRemaining = ConfigurationManager.Instance.Generation_Duration;

            // Ensure population initialized
            if (_population.Count == 0)
            {
                for (int i = 0; i < ConfigurationManager.Instance.Population_Size; i++)
                {
                    _population.Add(new Genome(GENOME_SIZE));
                }
            }

            if (WorldManager.Instance.antPrefab == null)
            {
                Debug.LogError("Ant prefab missing on WorldManager!");
                return;
            }

            // Spawn 1 Queen + (N-1) Workers, all using the ant prefab
            // The Queen gets the Queen component; workers get the Ant component.
            for (int i = 0; i < _population.Count; i++)
            {
                bool isQueen = (i == 0); // first individual is the queen
                SpawnAnt(_population[i], isQueen);
            }

            _isSimulating = true;
            Debug.Log($"Generation {GenerationCount} started — {_population.Count} ants (1 queen).");
        }

        private void SpawnAnt(Genome genome, bool isQueen)
        {
            int dimX = WorldManager.Instance.GetBlockLayerDimension(0);
            int dimZ = WorldManager.Instance.GetBlockLayerDimension(2);
            int dimY = WorldManager.Instance.GetBlockLayerDimension(1);

            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = UnityEngine.Random.Range(2, dimX - 2);
                int z = UnityEngine.Random.Range(2, dimZ - 2);

                // Find surface
                int groundY = -1;
                for (int h = dimY - 2; h >= 0; h--)
                {
                    AbstractBlock b = WorldManager.Instance.GetBlock(x, h, z);
                    if (b != null && b is not AirBlock && b is not ContainerBlock)
                    {
                        groundY = h;
                        break;
                    }
                }

                if (groundY < 0) continue;

                int antY = groundY + 1; // air cell above ground
                Vector3 visualPos = new Vector3(x, antY + 0.5f, z);
                GameObject go = Instantiate(WorldManager.Instance.antPrefab, visualPos, Quaternion.identity);

                Ant script;
                if (isQueen)
                {
                    script = go.GetComponent<Queen>();
                    if (script == null) script = go.AddComponent<Queen>();

                    go.transform.localScale *= 1.5f;
                    var r = go.GetComponent<Renderer>();
                    if (r != null) r.material.color = Color.red;
                    go.name = "Queen";
                }
                else
                {
                    script = go.GetComponent<Ant>();
                    if (script == null) script = go.AddComponent<Ant>();
                }

                script.Init(x, antY, z, genome);
                return;
            }

            Debug.LogWarning("Failed to find spawn position after 100 attempts");
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

        private void EndGeneration()
        {
            _isSimulating = false;

            int currentFitness = CountNestBlocks();
            Debug.Log($"Generation {GenerationCount} ended. Fitness: {currentFitness}");

            // Hill-climbing: keep best genome
            if (_bestGenome == null || currentFitness > _bestFitness)
            {
                _bestFitness = currentFitness;
                if (_population.Count > 0)
                    _bestGenome = new Genome(_population[0].Weights);
            }

            // Build next generation
            _population.Clear();

            // Elitism: exact copy of best
            if (_bestGenome != null)
                _population.Add(new Genome(_bestGenome.Weights));
            else
                _population.Add(new Genome(GENOME_SIZE));

            // Fill rest with mutations
            while (_population.Count < ConfigurationManager.Instance.Population_Size)
            {
                Genome mutant = new Genome(_bestGenome != null ? _bestGenome.Weights : new float[GENOME_SIZE]);
                mutant.Mutate(ConfigurationManager.Instance.Mutation_Rate, ConfigurationManager.Instance.Mutation_Strength);
                _population.Add(mutant);
            }

            // Clean up
            AntManager.Instance.ClearAll();
            var ants = FindObjectsOfType<Ant>();
            foreach (var a in ants) Destroy(a.gameObject);

            WorldManager.Instance.ResetWorld();

            GenerationCount++;
            StartGeneration();
        }

        private int CountNestBlocks()
        {
            return WorldManager.Instance.NestBlockCount;
        }
    }

    public static class CollisionTracker
    {
        public static int NestBlockCount = 0;
        public static void Clear() { NestBlockCount = 0; }
    }
}
