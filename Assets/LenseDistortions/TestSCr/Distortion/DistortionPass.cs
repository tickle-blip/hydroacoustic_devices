using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class DistortionPass : CustomPass
{
    public Camera toplef_camera;
    public Camera botlef_camera;
    public Camera topright_camera;
    public Camera botright_camera;

    
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
    {
        // Executed every frame for all the camera inside the pass volume
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}