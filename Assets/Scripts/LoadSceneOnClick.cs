using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneOnClick : MonoBehaviour
{
    // References for all the inputs
    public InputField terrainOffsetInput;
    public InputField gaussianVarianceInput;
    public Toggle useHeightline;
    public InputField heightlineColorR;
    public InputField heightlineColorG;
    public InputField heightlineColorB;

    // Constant for the name of the scene which the button should load
    public const string TERRAINSCENENAME = "TerrainScene";

    // The onClick function
    public void clicked()
    {
        // This script sets all the attributes of the StaticClass with the parameters which should transferred to the TerrainScene
        // While setting the attributes this function checks if the setter function says that the value was valid.
        // If the value wasn't valid it sets the color of the input to red. If it was valid it sets it to white.
        // As last action this functions calls the LoadScene from the SceneManager if all inputs are valid.

        bool everythingAlright = true;

        if (StaticClass.setTerrainOffset(float.Parse(terrainOffsetInput.text)))
        {
            terrainOffsetInput.image.color = Color.white;
        }
        else
        {
            terrainOffsetInput.image.color = Color.red;
            everythingAlright = false;
        }

        if (StaticClass.setGaussianVariance(double.Parse(gaussianVarianceInput.text)))
        {
            gaussianVarianceInput.image.color = Color.white;
        }
        else
        {
            gaussianVarianceInput.image.color = Color.red;
            everythingAlright = false;
        }

        if (StaticClass.setUseHeightline(useHeightline.isOn))
        {
            useHeightline.image.color = Color.white;
        }
        else
        {
            useHeightline.image.color = Color.red;
            everythingAlright = false;
        }

        if (StaticClass.setHeightlineColorR(int.Parse(heightlineColorR.text)))
        {
            heightlineColorG.image.color = Color.white;
        }
        else
        {
            heightlineColorR.image.color = Color.red;
            everythingAlright = false;
        }


        if (StaticClass.setHeightlineColorG(int.Parse(heightlineColorG.text)))
        {
            heightlineColorG.image.color = Color.white;
        }
        else
        {
            heightlineColorG.image.color = Color.red;
            everythingAlright = false;
        }


        if (StaticClass.setHeightlineColorB(int.Parse(heightlineColorB.text)))
        {
            heightlineColorB.image.color = Color.white;
        }
        else
        {
            heightlineColorB.image.color = Color.red;
            everythingAlright = false;
        }

        if (everythingAlright)
        {
            SceneManager.LoadScene(TERRAINSCENENAME);
        }
    }
}
