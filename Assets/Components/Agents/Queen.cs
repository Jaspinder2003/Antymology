using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Components.Agents
{
    public class Queen : Ant
    {
        /// <summary>
        /// Queen's special action: build a nest block on the ground she is standing on.
        /// Costs 1/3 of MaxHealth.
        /// </summary>
        public void BuildNest()
        {
            float cost = MaxHealth / 3f;
            if (CurrentHealth < cost) return;

            int x = CurrentPosition.x;
            int z = CurrentPosition.z;
            int groundY = CurrentPosition.y - 1; // ground beneath queen
            if (groundY < 0) return;

            AbstractBlock ground = WorldManager.Instance.GetBlock(x, groundY, z);

            // Can only convert solid, non-special blocks into nest
            if (ground is AirBlock || ground is ContainerBlock || ground is NestBlock) return;

            CurrentHealth -= cost;
            WorldManager.Instance.SetBlock(x, groundY, z, new NestBlock());
        }

        protected override void PerformSpecial()
        {
            BuildNest();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Queen aggressively tries to build nests when she has enough health
            // This supplements the neural-net decisions to ensure nest production
            if (CurrentHealth >= MaxHealth / 3f && Time.frameCount % 30 == 0)
            {
                BuildNest();
            }
        }
    }
}
