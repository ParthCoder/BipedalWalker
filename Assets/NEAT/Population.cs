using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of each individual species and it's population
/// </summary>
public class Population
{

    private List<NEATNet> population = null; //List of net's in a species
    private Color color; //color of this species

    /// <summary>
    /// Set the color of the species and create list of net's
    /// </summary>
    /// <param name="color">Color of the species</param>
    public Population(Color color)
    {
        this.color = color;
        population = new List<NEATNet>();
    }


    /// <summary>
    /// Add a member
    /// </summary>
    /// <param name="brain">new member</param>
    public void Add(NEATNet brain)
    {
        population.Add(brain);
    }

    /// <summary>
    /// Sort this poplation based on fitness
    /// </summary>
    public void Sort()
    {
        population.Sort();
    }

    /// <summary>
    /// Get a random member
    /// </summary>
    /// <returns>Random net from the population</returns>
    public NEATNet GetRandom()
    {
        if (population.Count == 0)
            return null;

        return population[UnityEngine.Random.Range(0, population.Count)];
    }

    /// <summary>
    /// Get entire population
    /// </summary>
    /// <returns></returns>
    public List<NEATNet> GetPopulation()
    {
        return population;
    }

    /// <summary>
    /// Get Color
    /// </summary>
    /// <returns>the color</returns>
    public Color GetColor()
    {
        return color;
    }

    /// <summary>
    /// Get shared cumulative fitness of this population.
    /// </summary>
    /// <returns>Fitness</returns>
    public float GetDistribution(float beta)
    {
        float distribution = 0;
        for (int j = 0; j < population.Count; j++)
        {
            float sh = 0;
            for (int k = j; k < population.Count; k++)
            {
                if (k != j)
                {
                    sh += NEATNet.SameSpeciesV2(population[j], population[k]) == true ? 1 : 0;
                }
            }
            if (sh == 0)
                sh = 1;
            float f = population[j].GetNetFitness();
            if (f < 0)
                f = 0;
            distribution += Mathf.Pow(f, beta) / sh;
        }
        if (distribution < 0)
            distribution = 0;
        return distribution;
    }

    /// <summary>
    /// Remote worst percet of the population.
    /// Note: it's not really percent, but rather percent/100
    /// </summary>
    /// <param name="percent">Percet of the population to remove</param>
    public void RemoveWorst(float percent)
    {
        population.Sort();

        if (population.Count > 1)
        {
            if (population.Count == 2 && percent > 0f)
            {
                population.RemoveAt(0);
            }
            else
            {
                int index = (int)(population.Count * percent);
                int amount = population.Count - (int)(population.Count * percent);
                for (int i = 0; i < amount; i++)
                    population.RemoveAt(0);
            }

        }
    }

    /// <summary>
    /// Get last net
    /// </summary>
    /// <returns>last net</returns>
    public NEATNet GetLast()
    {
        if (population.Count == 0)
            return null;
        return population[population.Count - 1];
    }

    /// <summary>
    /// Add brain to this population if it matches
    /// </summary>
    /// <param name="brain">The brain to match</param>
    /// <returns>True if added, false if not</returns>
    public bool AddIfMatch(NEATNet brain)
    {
        if (population.Count == 0)
        {
            population.Add(brain);
            return true;
        }
        else
        {
            if (NEATNet.SameSpeciesV2(GetRandom(), brain) == true)
            {
                population.Add(brain);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Return brain with highest fitness
    /// </summary>
    /// <returns></returns>
    public NEATNet GetBestBrain()
    {
        NEATNet best = null;
        float highestFitness = float.MinValue;
        for (int i = 0; i < population.Count; i++)
        {
            NEATNet foundBest = population[i];

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
