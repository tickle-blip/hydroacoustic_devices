using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class CustomRenderer : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        Debug.Log("REnder");
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }



}
