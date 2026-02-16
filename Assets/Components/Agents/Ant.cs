using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Components.Agents
{
    public class Ant : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float CurrentHealth;
        public float HealthDecayRate = 2f; // Increased for faster testing
        public Genome AntGenome;

        public Vector3Int CurrentPosition { get; protected set; }

        private MeshRenderer _renderer;

        public void Init(int x, int y, int z, Genome genome)
        {
            CurrentPosition = new Vector3Int(x, y, z);
            AntGenome = genome;
            CurrentHealth = MaxHealth;
            
            // Visuals: align to block center + 0.5 up
            transform.position = new Vector3(x, y + 1.5f, z);
            
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<MeshRenderer>();
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gameObject.AddComponent<MeshFilter>().mesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                Destroy(tempSphere);
            }
            
            // Assign material or color
            if (_renderer.sharedMaterial == null)
            {
                 // Create a default material if none exists (runtime only)
                 _renderer.material = new Material(Shader.Find("Standard")); 
                 _renderer.material.color = Color.black;
            }

            AntManager.Instance.RegisterAnt(this);
        }

        public virtual void OnUpdate()
        {
            if (CurrentHealth <= 0)
            {
                Die();
                return;
            }

            // Decay health
            AbstractBlock blockUnder = WorldManager.Instance.GetBlock(CurrentPosition.x, CurrentPosition.y, CurrentPosition.z);
            float decayMult = 1f;

            if (blockUnder is AcidicBlock)
            {
                decayMult = 2f;
            }

            CurrentHealth -= HealthDecayRate * decayMult * Time.deltaTime;

            // Decision making ---------------------------
            // Inputs: 
            // 0: Bias
            // 1: Health Ratio
            // 2: Is Block Ahead Air? (1 if yes)
            // 3: Is Block Below Mulch?
            // 4: Is Block Below Acid?
            // 5: Random
            
            float[] inputs = new float[6];
            inputs[0] = 1.0f;
            inputs[1] = CurrentHealth / MaxHealth;
            
            // Look ahead (roughly based on last move or random?)
            // We don't have "facing" yet. Let's assume we want to decide dx, dz.
            // For simplicity, let's just use outputs to decide direction directly.
            
            AbstractBlock blockAhead = new AirBlock(); // Placeholder
            // Since we don't have orientation, we can't easily check "ahead".
            // Let's check "South" (Z+1) as a proxy or just ignore?
            // Let's use "Is Block Below Solid" as input 2.
            inputs[2] = (blockUnder is not AirBlock) ? 1.0f : 0.0f;
            
            inputs[3] = (blockUnder is MulchBlock) ? 1.0f : 0.0f;
            inputs[4] = (blockUnder is AcidicBlock) ? 1.0f : 0.0f;
            inputs[5] = UnityEngine.Random.value;
            
            // Outputs:
            // 0: Move X+
            // 1: Move X-
            // 2: Move Z+
            // 3: Move Z-
            // 4: Dig
            // 5: Build (if Queen) / Share
            
            float[] outputs = AntGenome.FeedForward(inputs);
            
            // Softmax or Winner Takes All? Winner Takes All for discrete actions.
            int maxIdx = 0;
            float maxVal = outputs[0];
            for (int i = 1; i < outputs.Length; i++)
            {
                if (outputs[i] > maxVal)
                {
                    maxVal = outputs[i];
                    maxIdx = i;
                }
            }
            
            if (maxVal > 0) // Activation threshold
            {
                switch (maxIdx)
                {
                    case 0: Move(1, 0); break;
                    case 1: Move(-1, 0); break;
                    case 2: Move(0, 1); break;
                    case 3: Move(0, -1); break;
                    case 4: Dig(); break;
                    case 5: PerformSpecial(); break;
                }
            }
        }

        protected virtual void PerformSpecial()
        {
             // Share health default
             if (UnityEngine.Random.value < 0.5f) ShareHealth();
        }

        protected void Move(int dx, int dz)
        {
            if (dx == 0 && dz == 0) return;

            int newX = CurrentPosition.x + dx;
            int newZ = CurrentPosition.z + dz;

            // Bounds check
            if (newX < 0 || newX >= WorldManager.Instance.GetBlockLayerDimension(0) ||
                newZ < 0 || newZ >= WorldManager.Instance.GetBlockLayerDimension(2)) 
            {
                return;
            }

            // Find valid Y (Solid block)
            // Scan down from current Y + 2 (max climb height)
            // Ensure we don't scan outside world bounds
            int maxY = WorldManager.Instance.GetBlockLayerDimension(1) - 1;
            int startY = Mathf.Min(CurrentPosition.y + 2, maxY);
            
            int targetY = -1;
            for (int y = startY; y >= 0; y--)
            {
                 AbstractBlock b = WorldManager.Instance.GetBlock(newX, y, newZ);
                 if (b is not AirBlock)
                 {
                     targetY = y;
                     break;
                 }
            }

            if (targetY == -1) return; // Void

            // Height check
            if (targetY - CurrentPosition.y > 2) return;

            // Move
            Vector3Int oldPos = CurrentPosition;
            CurrentPosition = new Vector3Int(newX, targetY, newZ);
            
            // Notify Manager
            AntManager.Instance.UpdateOccupancy(this, CurrentPosition);

            // Visual Lerp (approximate)
            transform.position = new Vector3(newX, targetY + 1.5f, newZ);
        }

        protected void Dig()
{
    Vector3Int dir = FacingDirection switch
    {
        0 => Vector3Int.right,
        1 => Vector3Int.forward,
        2 => Vector3Int.left,
        _ => Vector3Int.back
    };

    int targetX = CurrentPosition.x + dir.x;
    int targetZ = CurrentPosition.z + dir.z;

    // Dig the ground block in front (not the air cell)
    int digY = CurrentPosition.y - 1;

    AbstractBlock block = WorldManager.Instance.GetBlock(targetX, digY, targetZ);
    if (block is AirBlock || block is ContainerBlock || block is NestBlock) return;

    WorldManager.Instance.SetBlock(targetX, digY, targetZ, new AirBlock());
}

        protected void ConsumeMulch()
        {
            // "Ants cannot consume mulch if another ant is also on the same mulch block"
            List<Ant> antsOnBlock = AntManager.Instance.GetAntsAt(CurrentPosition);
            if (antsOnBlock.Count > 1) return; // More than just me

            CurrentHealth = Mathf.Min(CurrentHealth + 20f, MaxHealth);
            WorldManager.Instance.SetBlock(CurrentPosition.x, CurrentPosition.y, CurrentPosition.z, new AirBlock());
            
            // Fall after eating
             int newY = CurrentPosition.y - 1;
             while (newY >= 0)
             {
                 AbstractBlock b = WorldManager.Instance.GetBlock(CurrentPosition.x, newY, CurrentPosition.z);
                 if (b is not AirBlock)
                 {
                     break;
                 }
                 newY--;
             }
             Vector3Int oldPos = CurrentPosition;
             CurrentPosition = new Vector3Int(CurrentPosition.x, newY, CurrentPosition.z);
             AntManager.Instance.UpdateOccupancy(this, CurrentPosition);
             
             transform.position = new Vector3(CurrentPosition.x, CurrentPosition.y + 1.5f, CurrentPosition.z);
        }

        protected void ShareHealth()
        {
            List<Ant> antsOnBlock = AntManager.Instance.GetAntsAt(CurrentPosition);
             foreach (Ant other in antsOnBlock)
             {
                 if (other != this && other.CurrentHealth < other.MaxHealth && CurrentHealth > 10)
                 {
                     float amount = Mathf.Min(10f, CurrentHealth - 1);
                     CurrentHealth -= amount;
                     other.ReceiveHealth(amount);
                     break; // Share with one per tick
                 }
             }
        }
        
        public void ReceiveHealth(float amount)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        }

        protected void Die()
        {
            AntManager.Instance.UnregisterAnt(this);
            Destroy(gameObject);
        }
    }
}
