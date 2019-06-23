using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticClass
{
    // Using a static class and its attributes to transfer the values of the menu scene to the terrain scene.
    // Thís class have to use static attributes and functions because their value should be the same in the 
    // object instantiated by the MainMenue and the object instantiated by the TerrainScene.
    // All the getter functions are simply returning the value of the attributes.
    // The setters are returning true if the value to set is valid and false if it is invalid.
    // For the most setters all values of the specified types are valid.
    // The setters for the color values are setting the attributes of the color object.
    // Also the setters for the colot values are checking wether the value to set is between 0 and 255.

    private static float terrainOffset = 10;
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

    public static bool getUseHeightline()
    {
        return useHeightline;
    }

    public static bool setUseHeightline(bool use)
    {
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
