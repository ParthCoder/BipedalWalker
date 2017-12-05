using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// for drawing the color specie distribution chart

public class TextureDraw : MonoBehaviour
{

    private Texture2D texture;
    private float screenWidth, screenHeight;

    private Color backgroundColor;

    private float yOffset = 0f;
    private float highest = 0f;

    private int maxLoop = 10;
    private int index = 0;

    List<SpeciesColorData> speciesData = new List<SpeciesColorData>();


    NEATGeneticControllerV2 manager = null;
    public void AddManager(NEATGeneticControllerV2 manager)
    {
        this.manager = manager;
    }


    // Use this for initialization
    void Start()
    {
        texture = new Texture2D(1, 1);
        this.screenWidth = (float)Screen.width * 0.4f;
        this.screenHeight = Screen.height;

        backgroundColor = Color.grey; backgroundColor.a = 1f;
        
    }


    void OnGUI()
    {


        float height = (screenHeight * 0.02f);
        this.screenWidth = (float)Screen.width * 0.4f;
        this.screenHeight = Screen.height;

        float offsetVertical = this.screenHeight * 0.5f;
        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(0, 0f+ offsetVertical, screenWidth + 10f, height* ((float)maxLoop-1)), texture);


        if (speciesData != null && speciesData.Count>0)
        {
            float xOffset = 0;
            float width = ((screenWidth + 10f) / (float)manager.populationSize);
            
            for (int i = 0; i < speciesData.Count; i++)
            {
                xOffset = 0;
                for (int j = 0; j < speciesData[i].distributions.Length; j++)
                {
                    float totalWidth = width * speciesData[i].distributions[j];
                    GUI.color = speciesData[i].colors[j];
                    GUI.DrawTexture(new Rect(xOffset, (int)height * i + offsetVertical, totalWidth, (int)height), texture);
                    xOffset += totalWidth;
                }
            }


        }

    }

    public void AddColorData(SpeciesColorData colordata)
    {
        if (this.speciesData.Count == 0)
        {
            for (var i = 0; i < maxLoop; i++)
            {
                this.speciesData.Add(colordata);
            }
        }
        else
        {
            this.speciesData.Insert(0, colordata);
            this.speciesData.RemoveAt(this.speciesData.Count - 1);
        }
    }

}



