using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

class LenseDistortion : CustomPass
{
    private ShaderTagId[] shaderTags;
    private Material OverrideMaterial;
    public Transform Cube;
    public Transform Sphere;
    public float F;
    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return OverrideMaterial; }
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
        shaderTags = new ShaderTagId[4]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("HDUnlitShader")
        };
        OverrideMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/LenseDistortion"));
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
    {
        hdCamera.camera.TryGetCullingParameters(out var cullingParams);
        cullingParams.cullingOptions = CullingOptions.ShadowCasters;
        cullingResult = renderContext.Cull(ref cullingParams);
        
        var result = new RendererListDesc(shaderTags, cullingResult, hdCamera.camera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonOpaque,
            overrideMaterial = OverrideMaterial,
            overrideMaterialPassIndex = 0,
            excludeObjectMotionVectors = false,
            stateBlock = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(false, CompareFunction.LessEqual) }
        };
        //float sc = 1f/CalcScale(Cube.position.z, Sphere.localScale.x);
        //Debug.Log(sc);
        //Debug.Log(CalcNewDistance(Cube.position.z));

        //cmd.SetGlobalFloat("Scale",sc);
        //cmd.SetGlobalFloat("DistVec", CalcNewDistance(Cube.position.z));
        GetCameraBuffers(out var c, out var d);
        CoreUtils.SetRenderTarget(cmd, c, d);
        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
    }

    protected override void Cleanup()
    {
        CoreUtils.Destroy(OverrideMaterial);
        // Cleanup code
    }
    /*
    private float CalcScale(float s, float d)
    {
        return (s + d) / (s * d / Mathf.Abs(F(1f, 1.33f, 1.45f) + (s + d)));
    }*/
    private float CalcNewDistance(float s)
    {
        float t = 1f / F - 1f / s;
        return 1f / t;
    }
}