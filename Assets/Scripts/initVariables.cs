using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class initVariables : MonoBehaviour
{

    private Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        this.rend = GetComponent<Renderer>();
        this.rend.material.SetColor("_HeightLineColor", StaticClass.getHeightlineColor());

        float tmpUseHeightline = StaticClass.getUseHeightline() ? 1 : 0;
        this.rend.material.SetFloat("_UseHeightline", tmpUseHeightline);

        Debug.Log("Set Shader variables");
    }
}
