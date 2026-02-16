using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Components.Agents
{
    public class Queen : Ant
    {
        public void BuildNest()
        {
            float cost = MaxHealth / 3f;
            if (CurrentHealth < cost) return;

            int x = CurrentPosition.x;
            int y = CurrentPosition.y;
            int z = CurrentPosition.z;

            int buildY = y - 1; // ground beneath queen
            if (buildY < 0) return;

            AbstractBlock ground = WorldManager.Instance.GetBlock(x, buildY, z);
            AbstractBlock cell = WorldManager.Instance.GetBlock(x, y, z); // queen's cell (should be air)

            // Queen must be in air, and ground must be solid (not air/container/nest)
            if (!(cell is AirBlock)) return;
            if (ground is AirBlock || ground is ContainerBlock || ground is NestBlock) return;

            CurrentHealth -= cost;
            WorldManager.Instance.SetBlock(x, buildY, z, new NestBlock());
        }
    }
}
