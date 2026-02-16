using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Components.UI
{
    public class NestUI : MonoBehaviour
    {
        private float _nextUpdate = 0f;
        private int _nestCount = 0;

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 200, 20), $"Nest Blocks: {_nestCount}");
            
            // Also display generation info if EvolutionManager exists
            if (Configuration.EvolutionManager.Instance != null)
            {
                GUI.Label(new Rect(10, 30, 200, 20), $"Generation: {Configuration.EvolutionManager.Instance.GenerationCount}");
                GUI.Label(new Rect(10, 50, 200, 20), $"Time Remaining: {Configuration.EvolutionManager.Instance.TimeRemaining:F1}");
            }
        }

        private void Update()
        {
            if (Time.time > _nextUpdate)
            {
                _nextUpdate = Time.time + 0.5f; // Update every 0.5s to save perf
                CountNestBlocks();
            }
        }

        private void CountNestBlocks()
        {
            _nestCount = 0;
            // Iterate all blocks (expensive? maybe optimization needed later)
            // WorldManager doesn't have a quick list of nest blocks.
            // Using GetBlockLayerDimension
            
            // Actually, querying every block 64x64x128 is too slow for Update.
            // Better to have WorldManager (or us) track/cache it?
            // "You must create a basic UI which shows the current number of nest blocks in the world"
            
            // Optimization: WorldManager.SetBlock could trigger an event.
            // For now, let's do a sampling or just trust it's fast enough for small 16x4x16 chunks?
            // 16 chunks * 8 blocks = 128 blocks width.
            // 128 * 32 * 128 = ~500k blocks. Too many to iterate.
            
            // I will add a static counter to NestBlock? No, blocks are re-created.
            // WorldManager should track it.
        }
    }
}
