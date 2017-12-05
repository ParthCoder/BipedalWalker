# BipedalWalker
Trained Bipedal Creature to walk using NEAT Algorithm

# NEAT
Neuro-evolution of Augmenting Topologies (NEAT) is a Neuroevolution algorithm that not only evolves the weights of the neural network but also its topology offering the possibility of evolving increasingly complex solutions over generations. NEAT starts with a minimal neural network structure with no hidden layer units which evolves over generations and get increasingly complex so as to optimize the output of NN. NEAT outperforms the best fixed-topology method on a challenging bechmark reinforcement learning task of Pole Balancing. Details can be found in research paper "Evolving Neural Networks through Augmenting Topologies" by Kenneth O. Stanley and Risto Miikkulainen

# Biped
We applied NEAT for walking of Biped creature. We present a very simplified 2d model of bipedal creature. Here we are considering only pelvis and below part, so that focus is on training the creature on how to walk rather than handling other complications like balancing.
We consider 2d model with a main body along with 2 limbs each having a connected thigh and leg. 4 joints are provided between legs and thighs and thighs and main body. Joint angles are constained to -45 to 45 degrees.

# Unity3D
To show the simulation of learning process, we created this 2d bipedal model in Unity and used NEAT implementation in C#. NN is given 10 inputs (body angle, 4 joint angles, 4 touch sensors, 1 bias) each frame, which outputs 4 joints motor speed. Within 25 generations, we can the creatures learning to walk.

# How to Use
Select Assets > Scenes > BipedalWalking.unity. Select CREATE NEW to initialize a new neural network, select speed with TIME SCALE, and then select numbers of GENERATIONS. After that, learning process begins. In scene view, you can see the creatures trying to reach the destination. In Game view, you can see the fitness, species distribution and best fitness neural network. You can check the biped prefab and its controller script in Assets > AgentProblems > BipedalWalking

Credits to @TheOne (youtube) for implementation of NEAT
