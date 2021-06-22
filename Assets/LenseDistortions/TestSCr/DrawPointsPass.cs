using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using System.Collections.Generic;

class DrawPointsPass : CustomPass
{
    public Camera enCam;
    public Camera dotCamera;
    public Camera bakingCamera;

    public RawImage resultImg;

    public RawImage distImg;
    public int Pseudo3D_Resolution;
    public Vector2Int ResultDimensions;

    public float ZeroGroundLevel=0f;
    public float UpperLevel = 10f;
    private ShaderTagId[] shaderTags;

    private Material pseudo3dMaterial;
    private Material dotMaterial;
    private ComputeShader Pseudo3dComputer;
    private int handleMapDots;
    private int handleBoostDots;
    private ComputeBuffer pseudo3D_buffer;

    public RenderTexture pseudo3D_Positions;
    public RenderTexture pseudo3D_Result;
    private GraphicsFormat vectorFormat;
    private GraphicsFormat vector4Format;

    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return pseudo3dMaterial; }
    private void InitSHaders_CashNameHandles()
    {
        shaderTags = new ShaderTagId[4]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("HDUnlitShader")
        };
        pseudo3dMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/DistanceFillerShader"));
        //overrideMaterial = (Material)Resources.Load("Materials/SV_Solid_Mat");
        dotMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Unlit/DotShader"));
        Pseudo3dComputer = (ComputeShader)Resources.Load("ComputeShaders/GSO_Pseudo3D");

        handleMapDots = Pseudo3dComputer.FindKernel("MapDots");

        //handleBoostDots = Pseudo3dComputer.FindKernel("BoostDots");

    }


    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if (bakingCamera == null)
        {
            Debug.Log("Set up baking Camera via inspector");
            return;
        }

        if (dotCamera == null)
        {
            Debug.Log("Set up UI cameras via inspector");
            return;
        }
        InitSHaders_CashNameHandles();
        vectorFormat = GraphicsFormat.R32_SFloat;
        vector4Format = GraphicsFormat.R16G16B16A16_SFloat;
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
    {
        if (hdCamera.camera.cameraType == CameraType.SceneView)
            return;

        if (hdCamera.camera != enCam)
            return;
        AllocateTexturesIfNeeded();
        
        RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, pseudo3D_Positions);

        //cmd.SetComputeMatrixParam(Pseudo3dComputer, "_InvViewMatrix")

        cmd.SetComputeFloatParam(Pseudo3dComputer, "ZeroLevel", ZeroGroundLevel);
        cmd.SetComputeFloatParam(Pseudo3dComputer, "UpperLevel", UpperLevel);
        cmd.SetComputeBufferParam(Pseudo3dComputer, handleMapDots, "PosBuffer", pseudo3D_buffer);
        cmd.SetComputeTextureParam(Pseudo3dComputer, handleMapDots, "Source", pseudo3D_Positions);
        cmd.DispatchCompute(Pseudo3dComputer, handleMapDots, (pseudo3D_Positions.width + 15) / 16,
               (pseudo3D_Positions.height + 15) / 16, 1);

        dotMaterial.SetBuffer("buffer", pseudo3D_buffer);

        var v = dotCamera.worldToCameraMatrix;
        var p = GL.GetGPUProjectionMatrix(dotCamera.projectionMatrix, true);
        var vp = p * v;
        dotMaterial.SetMatrix("_CameraViewMatrix", v);
        dotMaterial.SetMatrix("_InvViewMatrix", v.inverse);

        dotMaterial.SetMatrix("_CameraProjMatrix", p);
        dotMaterial.SetMatrix("_CameraInvProjMatrix", p.inverse);
        dotMaterial.SetMatrix("_ViewProjMatrix", vp);
        dotMaterial.SetMatrix("_CameraInvViewProjMatrix", vp.inverse);
        dotMaterial.SetMatrix("_CameraViewProjMatrix", vp);
        dotMaterial.SetVector("_PrParams", new Vector4(dotCamera.nearClipPlane, dotCamera.farClipPlane, 1 / dotCamera.nearClipPlane, 1 / dotCamera.farClipPlane));
        CoreUtils.SetRenderTarget(cmd, pseudo3D_Result, ClearFlag.All, clearColor: Color.black);
        cmd.DrawProcedural(Matrix4x4.identity, dotMaterial, 0, MeshTopology.Points, pseudo3D_buffer.count);

        //CoreUtils.SetRenderTarget(cmd, TempResult, ClearFlag.All, clearColor: Color.black);
        //cmd.SetComputeTextureParam(Pseudo3dComputer, handleBoostDots, "Source", Result);
        //cmd.SetComputeTextureParam(Pseudo3dComputer, handleBoostDots, "Destination", TempResult);
        //cmd.DispatchCompute(Pseudo3dComputer, handleBoostDots, (TempResult.width + 15) / 16,
        //       (TempResult.height + 15) / 16, 1);
        //cmd.CopyTexture(TempResult, Result);

        //Graphics.ExecuteCommandBuffer(cmd);
        if (pseudo3D_Result != null)
        {
            resultImg.texture = pseudo3D_Result;
        }
        if (distImg != null)
        {
            distImg.texture = pseudo3D_Positions;
        }
    }

    private void RenderFromCamera(ScriptableRenderContext renderContext, CommandBuffer cmd, CullingResults cullingResult, Camera view, RenderTexture target_RT)
    {
        SetCameraMatrices(cmd);
        view.targetTexture = target_RT;
        view.TryGetCullingParameters(out var cullingParams);
        cullingParams.cullingOptions = CullingOptions.ShadowCasters;
        cullingResult = renderContext.Cull(ref cullingParams);
        
        //Draw Solid Objects
        var result = new RendererListDesc(shaderTags, cullingResult, view)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonOpaque,
            overrideMaterial = pseudo3dMaterial,
            overrideMaterialPassIndex = pseudo3dMaterial.FindPass("ForwardOnly"),
            excludeObjectMotionVectors = false,
            stateBlock = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(true, CompareFunction.LessEqual) }
        };
        CoreUtils.SetRenderTarget(cmd, target_RT, ClearFlag.All, clearColor: Color.black);
        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));

    }
    private void SetCameraMatrices(CommandBuffer cmd)
    {
        //var p = SetScissorRect(GL.GetGPUProjectionMatrix(bakingCamera.projectionMatrix, true), rect);
        var p = GL.GetGPUProjectionMatrix(bakingCamera.projectionMatrix, true);
        Matrix4x4 scaleMatrix = Matrix4x4.identity;
        scaleMatrix.m22 = -1.0f;

        var v = bakingCamera.worldToCameraMatrix;
        var vp = p * v;

        cmd.SetGlobalMatrix("_ViewMatrix", v);
        cmd.SetGlobalMatrix("_InvViewMatrix", v.inverse);

        cmd.SetGlobalMatrix("_ProjMatrix", p);
        cmd.SetGlobalMatrix("_InvProjMatrix", p.inverse);
        cmd.SetGlobalMatrix("_InvProjMatrix", p.inverse);
        cmd.SetGlobalMatrix("_ViewProjMatrix", vp);
        cmd.SetGlobalMatrix("_InvViewProjMatrix", vp.inverse);

        cmd.SetComputeMatrixParam(Pseudo3dComputer, "_InvViewMatrix", v.inverse);

        cmd.SetComputeMatrixParam(Pseudo3dComputer, "_InvViewProjMatrix", vp.inverse);

        cmd.SetGlobalMatrix("_CameraViewProjMatrix", vp);
        cmd.SetGlobalVector("_WorldSpaceCameraPos", Vector3.zero);
        cmd.SetGlobalVector("_ProjectionParams", new Vector4(bakingCamera.nearClipPlane, bakingCamera.farClipPlane, 1 / bakingCamera.nearClipPlane, 1 / bakingCamera.farClipPlane));
    }
    private void AllocateTexturesIfNeeded()
    {
        float aspect = 1.777f;

        if (Pseudo3D_Resolution <= 0) Pseudo3D_Resolution = 1;
        int ResolutionWidth = Mathf.FloorToInt(Pseudo3D_Resolution * aspect);
        if (ResultDimensions.y != 0 && ResultDimensions.x != 0 && (pseudo3D_Result == null || ResultDimensions.x!=pseudo3D_Result.width|| ResultDimensions.y !=pseudo3D_Result.height) )
        {
            //Result = RTHandles.Alloc(
            //ResultDim.x, ResultDim.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
            //name: "Result", enableRandomWrite: true);

            if (pseudo3D_Result != null) pseudo3D_Result.Release();
            pseudo3D_Result = new RenderTexture(ResultDimensions.x, ResultDimensions.y, 16, vector4Format);
            pseudo3D_Result.enableRandomWrite = true;
            pseudo3D_Result.filterMode = FilterMode.Bilinear;
            pseudo3D_Result.name = "Result";
            
        }
        if (Pseudo3D_Resolution != 0 && (pseudo3D_Positions == null || Pseudo3D_Resolution != pseudo3D_Positions.height) )
        {
            if (pseudo3D_Positions != null) pseudo3D_Positions.Release();
            pseudo3D_Positions = new RenderTexture(ResolutionWidth, Pseudo3D_Resolution, 16, vector4Format);
            pseudo3D_Positions.filterMode = FilterMode.Point;
            pseudo3D_Positions.name = "DistanceTexture";
        }

        if (Pseudo3D_Resolution != 0 && (pseudo3D_buffer == null || ResolutionWidth * Pseudo3D_Resolution != pseudo3D_buffer.count))
        {
            if (pseudo3D_buffer != null) pseudo3D_buffer.Dispose();
            pseudo3D_buffer = new ComputeBuffer(ResolutionWidth * Pseudo3D_Resolution, sizeof(float) * 4, ComputeBufferType.Default);
        }

        
}

    protected override void Cleanup()
    {
        if (pseudo3D_Result!=null) pseudo3D_Result.Release();

        if (pseudo3D_buffer != null) pseudo3D_buffer.Dispose();
        if (pseudo3D_Positions!=null) pseudo3D_Positions.Release();
        CoreUtils.Destroy(pseudo3dMaterial);
        CoreUtils.Destroy(dotMaterial);
    }
}