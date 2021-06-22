using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RulerSettings
{
    public int RulerScale = 7;
    public bool showSmallDivisions = false;
    public Color BackGroundColor = Color.gray;
    public Color AmplitudeScaleColor = Color.black;
    public Color DivisionsColor = Color.black;
    public Color DigitsColor = Color.black;
}

public enum MarkType
{
    Box,
    Triangle,
    Circle,
    Custom1,
    Custom2,
    Custom3,
    Custom4,
    Custom5,
}
[Serializable]
public class Mark
{
    public MarkType type;
    public Rect rect = new Rect(0.5f, 0.5f, 100f, 100f);
    public Color color = Color.red;
    public Mark()
    {
        type = MarkType.Box;
        rect = new Rect(0.5f, 0.5f, 100f, 100f);
        color = Color.red;
    }
}
[Serializable]
public struct TypeBox
{
    public MarkType type;
    public Sprite sprite;

    public TypeBox(MarkType type, Sprite sprite) : this()
    {
        this.type = type;
        this.sprite = sprite;
    }
}

[CreateAssetMenu(fileName = "TypeCreator", menuName = "ScriptableObjects/TypeCreator", order = 1)]
public class TypeCreator : ScriptableObject
{
    public List<TypeBox> TypesList;
}