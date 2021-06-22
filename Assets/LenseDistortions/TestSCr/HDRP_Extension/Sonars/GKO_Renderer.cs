using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
[Serializable]
public class GKO_Renderer : SonarRenderer
{
    protected override void Cleanup()
    {
        base.Cleanup();
    }

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        base.Setup(renderContext, cmd);
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        throw new System.NotImplementedException();
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
