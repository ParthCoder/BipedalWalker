
using System.Collections.Generic;
using UnityEngine;


public class SpeciesColorData
{

    public Color[] colors;
    public int[] distributions;

    public SpeciesColorData(Species speciesManager)
    {
        List<Color> colorList = new List<Color>();
        List<int> distributionList = new List<int>();

        List<Population> species = speciesManager.GetSpecies();
        for (int i = 0; i < species.Count; i++)
        {
            List<NEATNet> population = species[i].GetPopulation();
            Color color = species[i].GetColor();
            int distribution = population.Count;

            colorList.Add(color);
            distributionList.Add(distribution);
        }

        colors = colorList.ToArray();
        distributions = distributionList.ToArray();
    }
}


