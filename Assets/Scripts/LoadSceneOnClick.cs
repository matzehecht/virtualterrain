using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneOnClick : MonoBehaviour
{
    public InputField terrainOffsetInput;
    public InputField gaussianVarianceInput;
    public Toggle useHeightline;
    public InputField heightlineColorR;
    public InputField heightlineColorG;
    public InputField heightlineColorB;

    public static string TERRAIN_SCENE_NAME = "TerrainScene";

    public void clicked()
    {
        if (!StaticClass.setTerrainOffset(float.Parse(terrainOffsetInput.text)))
        {
            terrainOffsetInput.image.color = Color.red;
            return;
        }

        if (!StaticClass.setGaussianVariance(double.Parse(gaussianVarianceInput.text)))
        {
            gaussianVarianceInput.image.color = Color.red;
            return;
        }

        if (!StaticClass.setUseHeightline(useHeightline.isOn))
        {
            useHeightline.image.color = Color.red;
            return;
        }

        if (!StaticClass.setHeightlineColorR(int.Parse(heightlineColorR.text)))
        {
            heightlineColorR.image.color = Color.red;
            return;
        }


        if (!StaticClass.setHeightlineColorG(int.Parse(heightlineColorG.text)))
        {
            heightlineColorG.image.color = Color.red;
            return;
        }


        if (!StaticClass.setHeightlineColorB(int.Parse(heightlineColorB.text)))
        {
            heightlineColorB.image.color = Color.red;
            return;
        }

        SceneManager.LoadScene(TERRAIN_SCENE_NAME);
    }
}
