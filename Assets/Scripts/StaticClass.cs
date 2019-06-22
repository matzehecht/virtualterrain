using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticClass
{
    private static float terrainOffset = 10;
    private static double gaussianVariance = 20;
    private static bool useHeightline = true;
    private static Color heightlineColor = new Color(0, 0, 0);

    public static float getTerrainOffset()
    {
        return terrainOffset;
    }

    public static bool setTerrainOffset(float offset)
    {
        terrainOffset = offset;
        return true;
    }
    public static double getGaussianVariance()
    {
        return gaussianVariance;
    }

    public static bool setGaussianVariance(double variance)
    {
        gaussianVariance = variance;
        return true;
    }

    public static bool getUseHeightline()
    {
        return useHeightline;
    }

    public static bool setUseHeightline(bool use)
    {
        Debug.Log(use);
        useHeightline = use;
        return true;
    }

    public static Color getHeightlineColor()
    {
        return heightlineColor;
    }

    public static bool setHeightlineColorR(int rVal)
    {
        if (rVal >= 0 && rVal <= 255)
        {
            heightlineColor.r = rVal;
            return true;
        }
        return false;
    }

    public static bool setHeightlineColorG(int gVal)
    {
        if (gVal >= 0 && gVal <= 255)
        {
            heightlineColor.g = gVal;
            return true;
        }
        return false;
    }

    public static bool setHeightlineColorB(int bVal)
    {
        if (bVal >= 0 && bVal <= 255)
        {
            heightlineColor.b = bVal;
            return true;
        }
        return false;
    }
}
