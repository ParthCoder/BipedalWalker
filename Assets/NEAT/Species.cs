using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of a list of species and judges species based on performance.
/// </summary>
public class Species
{
    private List<Population> species;

    NEATGeneticControllerV2 manager; //manager that controls all
    
    /// <summary>
    /// Create a species
    /// </summary>
    /// <param name="manager">Main Manager</param>
    public Species(NEATGeneticControllerV2 manager)
    {
        species = new List<Population>();
        this.manager = manager;
    }

    /// <summary>
    /// Create an empty specie with only color
    /// </summary>
    /// <param name="color">Color</param>
    /// <returns>New empty population</returns>
    public Population CreateNewSpecie(Color color)
    {
        Population newPop = new Population(color);
        species.Add(newPop);
        return newPop;
    }

    /// <summary>
    /// Creating a new specie
    /// </summary>
    /// <param name="species">List of population in a new specie</param>
    /// <param name="color">Color of this new specie</param>
    /// <returns>New populaiton that contains this new specie</returns>
    public static Population CreateNewSpecie(List<Population> species, Color color)
    {
        Population newPop = new Population(color);
        species.Add(newPop);
        return newPop;
    }

    /// <summary>
    /// Find closest species that match this brain
    /// </summary>
    /// <param name="brain">Brain to match</param>
    /// <returns>Species that matches else null</returns>
    public Population ClosestSpecies(NEATNet brain)
    {
        for (int i = 0; i < species.Count; i++)
        {
            if (NEATNet.SameSpeciesV2(species[i].GetRandom(), brain) == true)
            {
                return species[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Get the closest species that match a given brain
    /// </summary>
    /// <param name="species">Species to match</param>
    /// <param name="brain">Brain to match to species</param>
    /// <returns>A population if match otherwise null</returns>
    public static Population ClosestSpecies(List<Population> species, NEATNet brain)
    {
        for (int i = 0; i < species.Count; i++)
        {
            NEATNet random = species[i].GetRandom();
            if (random == null || NEATNet.SameSpeciesV2(random, brain) == true)
            {
                return species[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Get all species
    /// </summary>
    /// <returns>All species</returns>
    public List<Population> GetSpecies()
    {
        return species;
    }

    /// <summary>
    /// Get top n brains
    /// </summary>
    /// <param name="n">The number of brains to get</param>
    /// <returns>Top N brains</returns>
    public List<NEATNet> GetTopBrains(int n)
    {

        List<NEATNet> population = new List<NEATNet>();
        for (var i = 0; i < species.Count; i++)
        {
            List<NEATNet> pop = species[i].GetPopulation();

            for (var j = 0; j < pop.Count; j++)
            {
                population.Add(pop[j]);
            }
        }

        population.Sort();
        if (population.Count < n)
        {
            Debug.Log("ISSUE " + population.Count);
            return null;
        }
        return population.GetRange(population.Count - n, n);
    }

    /// <summary>
    /// Create new generation of net's
    /// </summary>
    /// <param name="maxCap"></param>
    public void GenerateNewGeneration(int maxCap)
    {
        float totalSharedFitness = 0f; //total shared fitness of the whole population 
        float totalNetworks = 0f; //total number of organisums (used for distribution)
        List<float> distribution = new List<float>();


        for (int i = 0; i < species.Count; i++)
        {
            float dist = species[i].GetDistribution(manager.beta);
            distribution.Add(dist);
            totalSharedFitness += dist;
            species[i].RemoveWorst(manager.removeWorst);
        }

        for (int i = 0; i < distribution.Count; i++)
        {
            if (totalSharedFitness <= 0f)
                distribution[i] = 0;
            else
                distribution[i] = (int)((distribution[i] / totalSharedFitness) * maxCap);
            totalNetworks += distribution[i];
        }



        if (maxCap > totalNetworks)
        {
            Debug.Log("More added: " + totalNetworks + " " + maxCap + " " + (maxCap - totalNetworks));
            for (int i = 0; i < (maxCap - totalNetworks); i++)
            {
                int highIndex = species.Count / 2;
                int randomInsertIndex = UnityEngine.Random.Range(highIndex, species.Count);
                distribution[randomInsertIndex] = distribution[randomInsertIndex] + 1;
            }
        }
        else if (maxCap < totalNetworks)
        {
            Debug.Log("Some removed: " + totalNetworks + " " + maxCap + " " + (maxCap - totalNetworks));
            for (int i = 0; i < (totalNetworks - maxCap); i++)
            {
                bool removed = false;
                while (removed == false)
                {
                    int randomInsertIndex = UnityEngine.Random.Range(0, species.Count);
                    if (distribution[randomInsertIndex] > 0)
                    {
                        distribution[randomInsertIndex] = distribution[randomInsertIndex] - 1;
                        removed = true;
                    }

                }
            }
        }

        for (int i = species.Count - 1; i >= 0; i--)
        {
            if (distribution[i] <= 0 || species[i].GetPopulation().Count == 0)
            {
                distribution.RemoveAt(i);
                species.RemoveAt(i);
            }
        }

        totalNetworks = maxCap;


        List<Population> newSpecies = new List<Population>();
        for (int i = 0; i < distribution.Count; i++)
        {
            int newDist = (int)distribution[i];
            Population popOldGen = species[i];
            newSpecies.Add(new Population(species[i].GetColor()));

            for (int j = 0; j < newDist; j++)
            {
                NEATNet net = null;

                if (j > newDist * manager.elite)
                {
                    NEATNet organism1 = popOldGen.GetRandom();
                    NEATNet organism2 = popOldGen.GetRandom();

                    net = NEATNet.Corssover(organism1, organism2);
                    net.Mutate();
                }
                else
                {
                    NEATNet netL = popOldGen.GetLast();
                    if (netL == null)
                        Debug.Log(newDist + " " + popOldGen.GetPopulation().Count);
                    net = new NEATNet(popOldGen.GetLast());
                }


                //reset copied stats
                net.SetNetFitness(0f);
                net.SetTimeLived(0f);
                net.SetTestTime(manager.testTime);
                net.ClearNodeValues();

                net.GenerateNeuralNetworkFromGenome();  //< NEW LSES ADDITION

                bool added = newSpecies[i].AddIfMatch(net);

                if (added == false)
                {
                    Population foundPopulation = Species.ClosestSpecies(newSpecies, net);
                    if (foundPopulation == null)
                    {
                        Population newSpecie = Species.CreateNewSpecie(newSpecies, new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)));
                        newSpecie.Add(net);
                    }
                    else
                    {
                        foundPopulation.Add(net);
                    }
                }
            }



        }
        this.species = newSpecies;
    }

    /// <summary>
    /// Append to species color data
    /// </summary>
    /// <returns></returns>
    public SpeciesColorData GetSpeciesColorData()
    {
        return new SpeciesColorData(this);
    }

    /// <summary>
    /// The best brain that currently exists
    /// </summary>
    /// <returns>Brain</returns>
    public NEATNet GetBestBrain()
    {
        NEATNet best = null;
        float highestFitness = float.MinValue;
        for (int i = 0; i < species.Count; i++)
        {
            NEATNet foundBest = species[i].GetBestBrain();

            if (foundBest != null)
            {
                if (foundBest.GetNetFitness() > highestFitness)
                {
                    highestFitness = foundBest.GetNetFitness();
                    best = foundBest;
                }
            }

        }

        return best;
    }
}


