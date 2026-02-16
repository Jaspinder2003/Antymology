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
        public float HealthDecayRate = 1f;
        public Genome AntGenome;

        public Vector3Int CurrentPosition { get; protected set; }

        // Facing: 0=X+, 1=Z+, 2=X-, 3=Z-
        protected int FacingDirection = 0;

        // Action cooldown — prevents acting every single FixedUpdate frame
        private float _actionCooldown = 0f;
        private const float ACTION_INTERVAL = 0.15f; // act ~6-7 times per second

        // Exploration: probability of choosing a random action instead of the NN output
        private const float EXPLORATION_RATE = 0.20f;

        private MeshRenderer _renderer;

        public void Init(int x, int y, int z, Genome genome)
        {
            CurrentPosition = new Vector3Int(x, y, z);
            AntGenome = genome;
            CurrentHealth = MaxHealth;
            FacingDirection = UnityEngine.Random.Range(0, 4);

            transform.position = new Vector3(x, y + 0.5f, z);

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<MeshRenderer>();
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gameObject.AddComponent<MeshFilter>().mesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                Destroy(tempSphere);
            }

            if (_renderer.sharedMaterial == null)
            {
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

            // --- Health decay ---
            AbstractBlock blockBelow = GetBlockBelow();
            float decayMult = 1f;
            if (blockBelow is AcidicBlock) decayMult = 2f;

            CurrentHealth -= HealthDecayRate * decayMult * Time.deltaTime;

            // --- Auto-eat mulch if standing on it ---
            if (blockBelow is MulchBlock)
            {
                // "Ants cannot consume mulch if another ant is on the same block"
                List<Ant> others = AntManager.Instance.GetAntsAt(CurrentPosition);
                if (others.Count <= 1)
                {
                    ConsumeMulch();
                }
            }

            // --- Action cooldown ---
            _actionCooldown -= Time.deltaTime;
            if (_actionCooldown > 0f) return;
            _actionCooldown = ACTION_INTERVAL;

            // --- Neural-net decision ---
            //
            // Inputs (6):
            //  0: Bias (always 1)
            //  1: Health ratio  (0..1)
            //  2: Is block below solid? (1 = solid, 0 = air)
            //  3: Is block below mulch? (1 = yes)
            //  4: Is block below acidic? (1 = yes)
            //  5: Random noise  (0..1)
            //
            // Outputs (6) — winner-takes-all:
            //  0: Move forward
            //  1: Turn left
            //  2: Turn right
            //  3: Dig forward
            //  4: Special (Queen builds nest / Worker shares health)
            //  5: Idle (do nothing)

            float[] inputs = new float[6];
            inputs[0] = 1.0f;
            inputs[1] = CurrentHealth / MaxHealth;
            inputs[2] = (blockBelow is not AirBlock) ? 1f : 0f;
            inputs[3] = (blockBelow is MulchBlock) ? 1f : 0f;
            inputs[4] = (blockBelow is AcidicBlock) ? 1f : 0f;
            inputs[5] = UnityEngine.Random.value;

            int bestAction;

            // Exploration: sometimes pick a random action to avoid stationarity
            if (UnityEngine.Random.value < EXPLORATION_RATE)
            {
                // Bias exploration toward movement/turning (actions 0-2)
                float r = UnityEngine.Random.value;
                if (r < 0.45f)      bestAction = 0; // move forward
                else if (r < 0.65f) bestAction = 1; // turn left
                else if (r < 0.85f) bestAction = 2; // turn right
                else if (r < 0.92f) bestAction = 3; // dig
                else if (r < 0.97f) bestAction = 4; // special
                else                bestAction = 5; // idle
            }
            else
            {
                float[] outputs = AntGenome.FeedForward(inputs);

                // Winner-takes-all
                bestAction = 0;
                float bestVal = outputs[0];
                for (int i = 1; i < outputs.Length; i++)
                {
                    if (outputs[i] > bestVal)
                    {
                        bestVal = outputs[i];
                        bestAction = i;
                    }
                }
            }

            switch (bestAction)
            {
                case 0: MoveForward(); break;
                case 1: TurnLeft(); break;
                case 2: TurnRight(); break;
                case 3: Dig(); break;
                case 4: PerformSpecial(); break;
                case 5: /* idle */ break;
            }
        }

        // ------------------------------------------------------------------
        //  Actions
        // ------------------------------------------------------------------

        protected virtual void PerformSpecial()
        {
            ShareHealth();
        }

        protected void TurnLeft()
        {
            FacingDirection = (FacingDirection + 3) % 4; // +3 == -1 mod 4
        }

        protected void TurnRight()
        {
            FacingDirection = (FacingDirection + 1) % 4;
        }

        private Vector3Int FacingDelta()
        {
            return FacingDirection switch
            {
                0 => new Vector3Int(1, 0, 0),
                1 => new Vector3Int(0, 0, 1),
                2 => new Vector3Int(-1, 0, 0),
                _ => new Vector3Int(0, 0, -1),
            };
        }

        protected void MoveForward()
        {
            Vector3Int delta = FacingDelta();
            int newX = CurrentPosition.x + delta.x;
            int newZ = CurrentPosition.z + delta.z;

            int dimX = WorldManager.Instance.GetBlockLayerDimension(0);
            int dimZ = WorldManager.Instance.GetBlockLayerDimension(2);
            int dimY = WorldManager.Instance.GetBlockLayerDimension(1);

            if (newX < 1 || newX >= dimX - 1 || newZ < 1 || newZ >= dimZ - 1) return;

            // Find ground at (newX, newZ) — scan downward from a generous height
            int startY = Mathf.Min(CurrentPosition.y + 3, dimY - 1);
            int groundY = -1;
            for (int y = startY; y >= 0; y--)
            {
                AbstractBlock b = WorldManager.Instance.GetBlock(newX, y, newZ);
                if (b != null && b is not AirBlock)
                {
                    groundY = y;
                    break;
                }
            }
            if (groundY < 0) return;

            // Height difference constraint (max climb = 2 blocks up, unlimited fall)
            int heightDiff = groundY - (CurrentPosition.y - 1); // compare ground levels
            if (heightDiff > 2) return;

            // The ant occupies the air cell above ground
            int antY = groundY + 1;

            Vector3Int oldPos = CurrentPosition;
            Vector3Int newPos = new Vector3Int(newX, antY, newZ);
            CurrentPosition = newPos;

            AntManager.Instance.UpdateOccupancy(this, oldPos, newPos);
            transform.position = new Vector3(newX, antY + 0.5f, newZ);
        }

        protected void Dig()
        {
            int x = CurrentPosition.x;
            int z = CurrentPosition.z;
            int digY = CurrentPosition.y - 1; // the block the ant is standing on

            if (digY < 0) return;

            AbstractBlock block = WorldManager.Instance.GetBlock(x, digY, z);
            if (block is AirBlock || block is ContainerBlock || block is NestBlock) return;

            WorldManager.Instance.SetBlock(x, digY, z, new AirBlock());

            // After removing the block below, the ant falls
            FallDown();
        }

        protected void ConsumeMulch()
        {
            int groundY = CurrentPosition.y - 1;
            if (groundY < 0) return;

            AbstractBlock block = WorldManager.Instance.GetBlock(CurrentPosition.x, groundY, CurrentPosition.z);
            if (block is not MulchBlock) return;

            CurrentHealth = Mathf.Min(CurrentHealth + 30f, MaxHealth);
            WorldManager.Instance.SetBlock(CurrentPosition.x, groundY, CurrentPosition.z, new AirBlock());
            AntManager.Instance.MulchConsumed++;

            // Fall after the mulch below is gone
            FallDown();
        }

        private void FallDown()
        {
            int x = CurrentPosition.x;
            int z = CurrentPosition.z;
            int y = CurrentPosition.y - 1;

            while (y >= 0)
            {
                AbstractBlock b = WorldManager.Instance.GetBlock(x, y, z);
                if (b is not AirBlock)
                {
                    break;
                }
                y--;
            }

            // y is now the ground, ant sits at y+1
            int newAntY = y + 1;
            Vector3Int oldPos = CurrentPosition;
            CurrentPosition = new Vector3Int(x, newAntY, z);
            AntManager.Instance.UpdateOccupancy(this, oldPos, CurrentPosition);
            transform.position = new Vector3(x, newAntY + 0.5f, z);
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
                    break;
                }
            }
        }

        public void ReceiveHealth(float amount)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the block directly below the ant (i.e. the ground the ant stands on).
        /// The ant occupies an air cell; the ground is at y-1.
        /// </summary>
        protected AbstractBlock GetBlockBelow()
        {
            int groundY = CurrentPosition.y - 1;
            if (groundY < 0) return new AirBlock();
            return WorldManager.Instance.GetBlock(CurrentPosition.x, groundY, CurrentPosition.z);
        }

        protected void Die()
        {
            AntManager.Instance.UnregisterAnt(this);
            Destroy(gameObject);
        }
    }
}
