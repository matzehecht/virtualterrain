using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneOnClick : MonoBehaviour
{
    public InputField TerrainOffsetInput;
    public InputField GaussianVarianceInput;

    public static string TERRAIN_SCENE_NAME = "TerrainScene";

    public void clicked()
    {
        bool anythingAlright = true;
        float tmpTerrainOffset;
        double tmpGaussianVariance;

        if (!float.TryParse(TerrainOffsetInput.text, out tmpTerrainOffset))
        {
            TerrainOffsetInput.image.color = Color.red;
            anythingAlright = false;
        }
        if (!double.TryParse(GaussianVarianceInput.text, out tmpGaussianVariance))
        {
            GaussianVarianceInput.image.color = Color.red;
            anythingAlright = false;
        }

        if (anythingAlright)
        {
            StaticClass.TerrainOffset = tmpTerrainOffset;
            StaticClass.GaussianVariance = tmpGaussianVariance;
            SceneManager.LoadScene(TERRAIN_SCENE_NAME);
        }
    }
}
