using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PropertiesSetter : MonoBehaviour
{

    [Range(0f, 1f)]
    public float Reflectivity, Bias;
    [Range(0f, 20f)]
    public float Wideness;
    // Start is called before the first frame update
    void Start()
    {
        SetColor();
    }

    void OnValidate()
    {
        SetColor();
    }

    void SetColor()
    {
        var rndr = GetComponent<Renderer>();

        var propertyBlock = new MaterialPropertyBlock();
        rndr.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat("Reflectivity", Reflectivity);
        propertyBlock.SetFloat("Wideness", Wideness);
        propertyBlock.SetFloat("Bezier1", Bias);

        rndr.SetPropertyBlock(propertyBlock);
    }
}
