# Antymology: Emergent Ant Colony Simulation

## Overview
Antymology is an emergent computing simulation developed for CPSC 565. This project simulates an evolving colony of ants that must learn to survive, gather resources, and construct a nest in a voxel-based procedural environment.

The simulation utilizes an **Evolutionary Algorithm** combined with a simple **Neural Network** (Perceptron) to evolve ant behaviors over successive generations. The goal of the colony is to maximize the production of Nest Blocks.

(SimulationScreenshot.png)

## Features

### 1. Intelligent Agents
The simulation features two types of agents:
- **Worker Ants**: Responsible for digging, gathering food (Mulch), and sharing health with the colony.
- **Queen Ant**: A specialized agent (larger, red) capable of constructing Nest Blocks at the cost of her own health.

### 2. Genetic Evolution
Each generation, the colony's behavior is driven by a shared **Genome** which encodes the weights of a neural network.
- **Inputs**: Health status, terrain analysis (obstacles, food, hazards).
- **Outputs**: Movement direction, digging, and building/sharing actions.
- **Selection**: At the end of each generation, the colony's fitness (total Nest Blocks) is evaluated. If the new generation outperforms the previous best, the genome is adopted and mutated for the next iteration (Hill Climbing strategy).

### 3. Dynamic Environment
The world acts as the selection pressure:
- **Mulch**: Provides health/energy.
- **Acidic Ground**: Accelerates health decay.
- **Terrain**: Requires navigation (max step height of 2).

## Implementation Details

### Architecture
- **WorldManager**: Handles voxel terrain generation and chunk management.
- **EvolutionManager**: Manages the simulation loop, spawning agents, and executing the evolutionary algorithm.
- **AntManager**: Optimizes agent updates and tracks spatial occupancy for interaction rules.
- **Neural Network**: A feed-forward network in `Genome.cs` maps environmental sensory data to action probabilities.

### Key Scripts
- `Ant.cs`: Base agent logic (health, movement, physics).
- `Queen.cs`: Extends Ant with nest-building capabilities.
- `Genome.cs`: Handles genetics and decision-making logic.
- `EvolutionManager.cs`: Controls the life-cycle of generations.

## How to Run
1. Open the project in **Unity 6000.3**.
2. Load `SampleScene`.
3. Press **Play**.
4. Observe the `NestUI` in the top-left corner for Generation and Nest Count stats.
5. Watch as ants explore; over time, they should become more efficient at surviving and building nests.

## Configuration
You can tweak simulation parameters in the `ConfigurationManager` via the Inspector:
- `Population_Size`: Number of ants per generation.
- `Generation_Duration`: Time in seconds per generation.
- `Mutation_Rate`: Probability of gene mutation.
