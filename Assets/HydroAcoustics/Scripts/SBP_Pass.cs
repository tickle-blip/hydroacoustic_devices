using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

using System.Collections.Generic;
using System;


[Serializable]
public class ProfilerDisplay
{
    public int Scroll=1;
    public bool MsScale = false;
    public bool ShowGrid = false;
    public int GridCount = 4;
    public bool EventOn = false;
    public ProfilerEvent Event;
    [HideInInspector] public float eventTimer = 0f;
    [HideInInspector] public float frameTimer = 0f;
    [HideInInspector] public float globalTimer = 0f;

    [HideInInspector] public Transform DisplayImage;
    [HideInInspector] public Transform TextPlaceholder;
    [HideInInspector] public Transform RulerTextPlaceholder;
    [HideInInspector] public List<Text> RulerText;
}
[Serializable]
public class ProfilerEvent
{
    public bool Line=true;
    public bool Text=true;
    public float EventInterval=30f;
    public string EventText="";
    public Stack<EventText> TextPool;
    public List<EventText> TextList;
}

public class EventText
{
    public Text text1;
    public float uvOffset;
    public string textStrip = "";
    public EventText(Text text1)
    {
        this.text1 = text1;
        uvOffset = -0.1f;
    }
}
class SBP_Pass : CustomPass
{
    #region Fields
    //public RawImage Test_img;
    //public RawImage Test_img1;
    //public Texture2D test;
    //public RawImage distance_img;
    public Vector2Int ResultDimensions = new Vector2Int(1920, 1080);
    public Camera UI_Camera_LF;
    public Camera UI_Camera_HF;
    public RawImage ResultLFProfilerImage;
    public RawImage ResultHFProfilerImage;

    /////-------------"UI Fields"------------------/////

    [Range(6f, 300f)]
    public float MaxScanRange = 10f;

    [Range(0f, 1f)]
    public float LFGain = 0.25f;
    [Range(0f, 1f)]
    public float LFSensitivity = 0; 
    [Range(0f, 2f)]
    public float LFContrast = 1f;

    [Range(0f, 1f)]
    public float HFGain = 0.25f;
    [Range(0f, 1f)]
    public float HFSensitivity = 0;
    [Range(0f, 2f)]
    public float HFContrast = 1f;

    [Range(1f, 720)]
    [SerializeField] private int _Resolution = 360; 

    public Vector3[] Colors = { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1) }; //Color Palette. colors in range (0.0 - 1.0)

    public Color BackgroundColor = Color.black;
    public Color GridColor = Color.grey;
    public bool HighFrequencyOn = false;
    public ProfilerDisplay LFDisplay = new ProfilerDisplay();
    public ProfilerDisplay HFDisplay = new ProfilerDisplay();

    /////--------------"Scripting Fields"------------------/////
    public bool OnlyTerrain;
    public float MainWindowToRulerRatio=0.92f;
    public RulerSettings RulerSettings = new RulerSettings();
    [Range(0f, 3f)]
    public float LinesThickness = 1f; //Rings and grid thickness

    public Font CustomFont;
    [Range(1f, 60f)]
    public int EventFontSize = 10;
    [Range(1f, 60f)]
    public int RulerFontSize = 10;
    public float UpdateTimeMs = 20f;
    [Range(3f, 30f)]
    public float FieldOfView = 4.5f;


    public int ScrollAmount = 1;
    [Range(0.01f, 50f)]
    public float SubBot_Pattern_Scale = 2f;
    [Range(0.01f, 50f)]
    public float Bot_Pattern_Scale = 1.14f;
    [Range(0.001f, 1f)]
    public float SubBot_Pattern_Bias = 0.5f;
    [Range(0.001f, 0.3f)]
    public float Bot_Pattern_Bias = 0.2f;

    [Range(0.01f, 100f)]
    public float SubBot_Pattern_Scroll = 5f;


    [Range(0f, 300f)]
    public float MaxAboveBotNoiseWideness = 20f;
    [Range(0f, 300f)]
    public float MaxBelowBotNoiseWideness = 20f;
    [Range(0.01f, 100f)]
    public float Noise_Pattern_Scroll = 5f;
    [Range(0.001f, 2f)]
    public float AboveBot_Noise_Scale_X = 0.1f;
    [Range(0.001f, 2f)]
    public float BelowBot_Noise_Scale_X = 0.1f;
    [Range(0f, 2f)]
    public float AboveBot_Noise_Gain_Mult = 0.5f;
    [Range(0f, 2f)]
    public float BelowBot_Noise_Gain_Mult = 0.5f;

    [Range(0f, 1f)]
    public float Noise_Reflectivity_Min = 0.5f;
    [Range(0f, 1f)]
    public float Noise_Reflectivity_Max = 0.5f;
    

    public Camera bakingCamera = null;

    public Transform shipTransform = null;
    
    /////-------------"Private Fields"------------------/////
    
    //Final Texture
    [HideInInspector] public RTHandle LFResult, HFResult;
    //Texture Handles

    private RTHandle DistanceTexture_Temp;
    private RenderTexture DistanceTexture;//public for debuging
    private RTHandle DistStrip_Temp, NoiseStrip_Temp, DistTex_Temp;
    private RTHandle LFDistStrip_Main, LFNoiseStrip_Main, LFDistTex_Main, LF_Ruler;
    private RTHandle HFDistStrip_Main, HFNoiseStrip_Main, HFDistTex_Main, HF_Grid;
    private ComputeBuffer distanceBuffer;
    private GraphicsFormat VectorFormat;
    private GraphicsFormat Vector4Format;

    private ComputeShader DistributionComputer;
    private ComputeShader InterpolationComputer;

    private Material overrideMaterial = null;
    private Material terrainOverrideMaterial = null;

    private int handleClearTextureStrip;
    private int handleClearBuffer;

    private int handleNoiseDistributionMain;
    private int handleDistributionMain;
    private int handleShiftTex;
    private int handleRenderToResultTex;
    private int handleRemap;
    private int handleRenderDepthRuler;
    private ShaderTagId[] shaderTags;
    
    #endregion

    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return overrideMaterial; }
    
    private void InitSHaders_CashNameHandles()
    {
        overrideMaterial = (Material)Resources.Load("Materials/SBP/SBP_Solid_Mat");
        terrainOverrideMaterial = (Material)Resources.Load("Materials/Shared/Shared_Terrain_Mat");

        DistributionComputer = (ComputeShader)Resources.Load("ComputeShaders/SBP/SBP_DistributionComputer");
        InterpolationComputer = (ComputeShader)Resources.Load("ComputeShaders/SBP/SBP_InterpolationComputer");

        handleClearTextureStrip = DistributionComputer.FindKernel("ClearTextureStrip");
        handleClearBuffer = DistributionComputer.FindKernel("ClearBuffer");

        handleDistributionMain = DistributionComputer.FindKernel("MapDepthToQuadProfiler");
        handleNoiseDistributionMain = DistributionComputer.FindKernel("MapNoiseToQuadSonar");
        handleRemap = InterpolationComputer.FindKernel("RemapQuad");
        handleShiftTex = InterpolationComputer.FindKernel("ShiftQuad");
        handleRenderToResultTex = InterpolationComputer.FindKernel("RenderInPolar");
        handleRenderDepthRuler = InterpolationComputer.FindKernel("RenderDepthRuler");
        //handle_Zoom_RenderInPolar = InterpolationComputer.FindKernel("Zoom_RenderInPolar");

        shaderTags = new ShaderTagId[4]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("HDUnlitShader")
        };
    }

    private void SetUpCameraData(Camera cam)
    {
        if (cam == null) return;
        var camData = cam.GetComponent<HDAdditionalCameraData>();
        camData.customRenderingSettings = true;
        var frameSettings = camData.renderingPathCustomFrameSettings;

        var frameSettingsMask = camData.renderingPathCustomFrameSettingsOverrideMask;
        frameSettingsMask.mask[(uint)FrameSettingsField.Postprocess] = true;

        camData.renderingPathCustomFrameSettingsOverrideMask = frameSettingsMask;
        camData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
    }
    
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {

        if (bakingCamera == null)
        {
            Debug.Log("Set up Profiler Camera via Inspector");
            return;
        }

        //Ship transform is needed to animate noise 
        if (shipTransform == null)
        {
            Debug.Log("Set up ShipTransform via Inspector");
            return;
        }
        if (UI_Camera_LF == null)
        {
            Debug.Log("Set up UI cameras via Inspector");
            return;
        }

        VectorFormat = GraphicsFormat.R32_SFloat;
        Vector4Format = GraphicsFormat.R16G16B16A16_SFloat;

        bakingCamera.enabled = false;
        //SerializeMarkSpriteDictionaryIfPossible();
        SetUpCameraData(UI_Camera_LF);
        SetUpCameraData(UI_Camera_HF);

        InitSHaders_CashNameHandles();

        AllocateTexturesIfNeeded();


        LFDisplay.eventTimer = 0;
        HFDisplay.eventTimer = 0;
        LFDisplay.globalTimer = 0;
        HFDisplay.globalTimer = 0;
        LFDisplay.frameTimer = 0;
        HFDisplay.frameTimer = 0;
        LFDisplay.DisplayImage = ResultLFProfilerImage.transform;
        HFDisplay.DisplayImage = ResultHFProfilerImage.transform;
        CreateTextPoolAndList();
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
    {

        if (!bakingCamera || !shipTransform || !terrainOverrideMaterial || !overrideMaterial || !DistanceTexture || !UI_Camera_LF)
            return;

        if (camera.camera.cameraType == CameraType.SceneView)
            return;
        
        if (camera.camera != UI_Camera_LF)
            return;

        AllocateTexturesIfNeeded();

        ///////////----Render Objects With Override Materials
        CoreUtils.SetRenderTarget(cmd, DistanceTexture, ClearFlag.All, clearColor: Color.black);
        RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, DistanceTexture,LFDisplay);


        SetVar_DistributionComputer(cmd);
        SetVar_SonarGridAndColor(cmd);
        
        RenderImage(cmd, LFDisplay);
        if (HighFrequencyOn)
        {
            ///////////----Render Objects With Override Materials
            CoreUtils.SetRenderTarget(cmd, DistanceTexture, ClearFlag.All, clearColor: Color.black);
            RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, DistanceTexture, HFDisplay);
            RenderImage(cmd, HFDisplay);
        }

        if (ResultLFProfilerImage != null)
        {
            ResultLFProfilerImage.texture = LFResult;
            ResultLFProfilerImage.SetNativeSize();
        }
        if (ResultHFProfilerImage != null && HighFrequencyOn)
        {
            ResultHFProfilerImage.texture = HFResult;
            ResultHFProfilerImage.SetNativeSize();
        }

         /*if (distance_img != null)
            distance_img.texture = DistanceTexture;
         if (Test_img != null)
             Test_img.texture = DistanceTexture_Temp;
         if (Test_img1 != null)
             Test_img1.texture = LF_Ruler;*/
    }

    private void RenderImage(CommandBuffer cmd, ProfilerDisplay Display)
    {
        EmitEventLine(Display);
        Display.frameTimer += Time.deltaTime;
        if (Display.frameTimer < (float)Display.Scroll * UpdateTimeMs / 1000f)
            return;
        Display.globalTimer += (float)Display.Scroll * UpdateTimeMs / 1000f;
        Display.frameTimer =0f;

        MoveEventLine(Display);
        RTHandle distStrip, noiseStrip, distTex,result;
        
        if (Display == LFDisplay)
        {

            cmd.SetComputeFloatParam(DistributionComputer, "Gain", LFGain);
            cmd.SetComputeFloatParam(InterpolationComputer, "LowerFactor", LFSensitivity);
            cmd.SetComputeFloatParam(InterpolationComputer, "MiddleFactor", LFContrast);
            cmd.SetComputeVectorParam(DistributionComputer, "Noise12_Scale_Bias", new Vector4(SubBot_Pattern_Scale, Bot_Pattern_Scale, SubBot_Pattern_Bias, Bot_Pattern_Bias));
            distStrip = LFDistStrip_Main;
            noiseStrip = LFNoiseStrip_Main;
            distTex = LFDistTex_Main;
            result = LFResult;
        }
        else
        {
            cmd.SetComputeFloatParam(DistributionComputer, "Gain", HFGain);
            cmd.SetComputeFloatParam(InterpolationComputer, "LowerFactor", HFSensitivity);
            cmd.SetComputeFloatParam(InterpolationComputer, "MiddleFactor",HFContrast);
            cmd.SetComputeVectorParam(DistributionComputer, "Noise12_Scale_Bias", new Vector4(SubBot_Pattern_Scale*20f, SubBot_Pattern_Scale*20f, Bot_Pattern_Bias/3f, Bot_Pattern_Bias/3f));
            distStrip = HFDistStrip_Main;
            noiseStrip = HFNoiseStrip_Main;
            distTex = HFDistTex_Main;
            result = HFResult;
        }

        DrawRuler(Display);

        cmd.SetComputeFloatParam(InterpolationComputer, "RulerScale", RulerSettings.RulerScale);
        cmd.SetComputeFloatParam(InterpolationComputer, "ShowDivisions", RulerSettings.showSmallDivisions ? 1 : 0);
        cmd.SetComputeVectorParam(InterpolationComputer, "RulerBackground", RulerSettings.BackGroundColor);
        cmd.SetComputeVectorParam(InterpolationComputer, "RulerScaleColor", RulerSettings.AmplitudeScaleColor);
        cmd.SetComputeVectorParam(InterpolationComputer, "RulerDivisionsColor", RulerSettings.DivisionsColor);

        cmd.SetComputeFloatParam(InterpolationComputer, "WindowsRatio", MainWindowToRulerRatio); 
        cmd.SetComputeIntParam(InterpolationComputer, "GridOn", Display.ShowGrid == true ? 1 : 0);
        cmd.SetComputeIntParam(InterpolationComputer, "GridCount", Mathf.Clamp(Display.GridCount,1,10));
        cmd.SetComputeFloatParam(DistributionComputer, "Time", (Time.realtimeSinceStartup * 10 % 500));
        cmd.SetComputeIntParam(InterpolationComputer, "EventLine", ((Display.Event.Line && Display.EventOn) == true) ? 1 : 0);


        cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", distanceBuffer);
        float[] uvOffsetArray =new float[Display.Event.TextList.Count];
        for (int i = 0; i < uvOffsetArray.Length; i++)
        {
            EventText m = (EventText)Display.Event.TextList[i];
            uvOffsetArray[i] = m.uvOffset;
        }

        cmd.SetComputeFloatParams(InterpolationComputer, "uvOffsetArray", uvOffsetArray);
        cmd.SetComputeIntParam(InterpolationComputer, "uvArrayCount", uvOffsetArray.Length);
        
        //Shift Previous strip
        cmd.SetComputeFloatParam(InterpolationComputer, "Scroll", 1f);
        cmd.SetComputeTextureParam(InterpolationComputer, handleShiftTex, "Source", distStrip);
        cmd.SetComputeTextureParam(InterpolationComputer, handleShiftTex, "Destination", DistStrip_Temp);
        cmd.DispatchCompute(InterpolationComputer, handleShiftTex, (DistStrip_Temp.referenceSize.x + 1) / 2, (DistStrip_Temp.referenceSize.y + 179) / 180, 1);
        cmd.CopyTexture(DistStrip_Temp, distStrip);

        //Shift Previous Noise strip
        cmd.SetComputeFloatParam(InterpolationComputer, "Scroll", 1f);
        cmd.SetComputeTextureParam(InterpolationComputer, handleShiftTex, "Source", noiseStrip);
        cmd.SetComputeTextureParam(InterpolationComputer, handleShiftTex, "Destination", NoiseStrip_Temp);
        cmd.DispatchCompute(InterpolationComputer, handleShiftTex, (NoiseStrip_Temp.referenceSize.x + 1) / 2, (NoiseStrip_Temp.referenceSize.y + 179) / 180, 1);
        cmd.CopyTexture(NoiseStrip_Temp, noiseStrip);

        //Clear ComputeBuffer
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count + 1023) / 1024, 1, 1);

        //Render Noise Strip at uv.=1; 
        cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Source", DistanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Destination", noiseStrip);
        cmd.DispatchCompute(DistributionComputer, handleNoiseDistributionMain, (DistanceTexture.width + 15) / 16,
               (DistanceTexture.height + 15) / 16, 1);

        //Clear ComputeBuffer
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count + 1023) / 1024, 1, 1);

       //Render Strip at uv.=1; 
        cmd.SetComputeFloatParam(DistributionComputer, "Time", (Time.realtimeSinceStartup * 10 % 500));
        cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Source", DistanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Destination", distStrip);
        cmd.DispatchCompute(DistributionComputer, handleDistributionMain, (DistanceTexture.width + 15) / 16,
               (DistanceTexture.height + 15) / 16, 1);

        //Combine Noise And Distance Strips
        //Shift Texture left by "Scroll Amount" pixels + Render Strip "Scroll Amount" wide with interpolation
        cmd.CopyTexture(distTex, DistTex_Temp);
        cmd.SetComputeFloatParam(InterpolationComputer, "Scroll", Mathf.Clamp(ScrollAmount, 1f, 30f));

        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "NoiseStrip", noiseStrip);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "DistStrip", distStrip);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Source",  DistTex_Temp);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Destination", distTex);
        cmd.DispatchCompute(InterpolationComputer, handleRemap, (distTex.referenceSize.x + 15) / 16,
               (distTex.referenceSize.y + 15) / 16, 1);


        //Render Ruler 
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderDepthRuler, "DistStrip", distStrip);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderDepthRuler, "NoiseStrip", noiseStrip);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderDepthRuler, "Destination", LF_Ruler);
        cmd.DispatchCompute(InterpolationComputer, handleRenderDepthRuler, (LF_Ruler.referenceSize.x + 15) / 16, (LF_Ruler.referenceSize.y + 15) / 16, 1);

        ////////////----Render Final Color Image
        CoreUtils.SetRenderTarget(cmd, result, ClearFlag.Color, clearColor: Color.clear);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderToResultTex, "Ruler", LF_Ruler);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderToResultTex, "Source", distTex);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderToResultTex, "Destination", result);

        cmd.DispatchCompute(InterpolationComputer, handleRenderToResultTex, (ResultDimensions.x + 31) / 32,
               (ResultDimensions.y + 31) / 32, 1);

    }

    private void SetVar_DistributionComputer(CommandBuffer cmd)
    {
        cmd.SetComputeVectorParam(DistributionComputer, "WorldPosRot", new Vector4(shipTransform.position.x,
                                                                                              shipTransform.position.y,
                                                                                             shipTransform.position.z,
                                                                                              shipTransform.localEulerAngles.y));
        cmd.SetComputeFloatParam(DistributionComputer, "MaxDistance", MaxScanRange);
        cmd.SetComputeFloatParam(DistributionComputer, "NoiseUpperWideness", MaxAboveBotNoiseWideness/ MaxScanRange);
        cmd.SetComputeFloatParam(DistributionComputer, "NoiseLowerWideness", MaxBelowBotNoiseWideness / MaxScanRange);
        cmd.SetComputeFloatParam(DistributionComputer, "SB_Pattern_Scroll_Speed", SubBot_Pattern_Scroll);
            cmd.SetComputeFloatParam(DistributionComputer, "Noise_Pattern_Scroll", Noise_Pattern_Scroll);
        cmd.SetComputeVectorParam(DistributionComputer, "Noise_Refl_Min_Max", new Vector2(Noise_Reflectivity_Min, Noise_Reflectivity_Max));

        cmd.SetComputeFloatParam(DistributionComputer, "AboveBot_Noise_Scale_X", AboveBot_Noise_Scale_X);
        cmd.SetComputeFloatParam(DistributionComputer, "BelowBot_Noise_Scale_X", BelowBot_Noise_Scale_X);
        cmd.SetComputeFloatParam(DistributionComputer, "AboveBot_Noise_Gain_Mult", AboveBot_Noise_Gain_Mult);
        cmd.SetComputeFloatParam(DistributionComputer, "BelowBot_Noise_Gain_Mult", BelowBot_Noise_Gain_Mult);
        cmd.SetComputeBufferParam(DistributionComputer, handleDistributionMain, "DistanceBuffer", distanceBuffer);
        cmd.SetComputeBufferParam(DistributionComputer, handleNoiseDistributionMain, "DistanceBuffer", distanceBuffer);

    }


    private void SetVar_SonarGridAndColor(CommandBuffer cmd)
    {

        cmd.SetComputeFloatParam(InterpolationComputer, "Time", (Time.timeSinceLevelLoad));
        //Debug.Log(Time.timeSinceLevelLoad);
        cmd.SetComputeFloatParam(InterpolationComputer, "Thickness", LinesThickness / 1000f);
        cmd.SetComputeFloatParam(InterpolationComputer, "MaxDistanceMark", bakingCamera.farClipPlane);


        Vector4[] _cols = new Vector4[Colors.Length + 1];
        _cols[0] = BackgroundColor;
        for (int i = 0; i < Colors.Length; i++)
        {
            _cols[i + 1] = Colors[i];
            
        }
        cmd.SetComputeVectorArrayParam(InterpolationComputer, "_Colors", _cols);
        cmd.SetComputeIntParam(InterpolationComputer, "_ColorsCount", _cols.Length);
        cmd.SetComputeVectorParam(InterpolationComputer, "_BackgroundColor", BackgroundColor);
        cmd.SetComputeVectorParam(InterpolationComputer, "_GridColor", GridColor);

    }

    private void RenderFromCamera(ScriptableRenderContext renderContext, CommandBuffer cmd, CullingResults cullingResult, Camera view, RenderTexture target_RT, ProfilerDisplay display)
    {
        view.targetTexture = target_RT;
        view.TryGetCullingParameters(out var cullingParams);
        cullingParams.cullingOptions = CullingOptions.ShadowCasters;
        cullingResult = renderContext.Cull(ref cullingParams);

        bakingCamera.farClipPlane = MaxScanRange;
        bakingCamera.focalLength = 57f;


        bakingCamera.fieldOfView = FieldOfView;

        SetCameraMatrices(cmd);
        //Draw Terrain
        var result = new RendererListDesc(shaderTags, cullingResult, bakingCamera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonOpaque,
            overrideMaterial = terrainOverrideMaterial,
            overrideMaterialPassIndex = terrainOverrideMaterial.FindPass("ForwardOnly"),
            excludeObjectMotionVectors = false,
            layerMask = LayerMask.GetMask("Terrain"),
            stateBlock = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(false, CompareFunction.LessEqual) }
        };

        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));

        if (OnlyTerrain == true) return;

        cmd.CopyTexture(DistanceTexture, DistanceTexture_Temp);
        cmd.SetGlobalFloat("MaxScanRange", MaxScanRange);
        if (display == HFDisplay) cmd.SetGlobalFloat("ShowSBSolid", 1);
        else cmd.SetGlobalFloat("ShowSBSolid", 0);
        cmd.SetGlobalTexture("_DistanceTex", DistanceTexture_Temp);
        renderContext.ExecuteCommandBuffer(cmd);
        //Draw Solid Objects
        result = new RendererListDesc(shaderTags, cullingResult, bakingCamera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonOpaque,
            overrideMaterial = overrideMaterial,
            overrideMaterialPassIndex = 0,
            excludeObjectMotionVectors = false,
            layerMask = LayerMask.GetMask("Solid"),
            stateBlock = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(true, CompareFunction.LessEqual) }
        };
        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));


    }

    private void SetCameraMatrices(CommandBuffer cmd)
    {
        var p = GL.GetGPUProjectionMatrix(bakingCamera.projectionMatrix, true);
        Matrix4x4 scaleMatrix = Matrix4x4.identity;
        scaleMatrix.m22 = -1.0f;

        var v = bakingCamera.worldToCameraMatrix;
        var vp = p * v;

        cmd.SetGlobalMatrix("_ViewMatrix", v);
        cmd.SetGlobalMatrix("_InvViewMatrix", v.inverse);
        cmd.SetGlobalMatrix("_ProjMatrix", p);
        cmd.SetGlobalMatrix("_InvProjMatrix", p.inverse);
        cmd.SetGlobalMatrix("_ViewProjMatrix", vp);
        cmd.SetGlobalMatrix("_InvViewProjMatrix", vp.inverse);
        cmd.SetGlobalMatrix("_CameraViewProjMatrix", vp);
        cmd.SetGlobalVector("_WorldSpaceCameraPos", Vector3.zero);
        cmd.SetGlobalVector("_ProjectionParams", new Vector4(bakingCamera.nearClipPlane, bakingCamera.farClipPlane, 1 / bakingCamera.nearClipPlane, 1 / bakingCamera.farClipPlane));
    }

    private void AllocateTexturesIfNeeded()
    {

        if (LFResult == null || ResultDimensions != LFResult.referenceSize && ResultDimensions != Vector2Int.zero)
        {
            LFResult?.Release();
            LFResult = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: Vector4Format,
                name: "Result", enableRandomWrite: true
            );
            HFResult?.Release();
            HFResult = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: Vector4Format,
                name: "Result", enableRandomWrite: true
            );
        }

        if (LFDistStrip_Main == null || _Resolution != LFDistStrip_Main.referenceSize.y && _Resolution != 0)
        {

            DistanceTexture_Temp?.Release();
            DistanceTexture_Temp = RTHandles.Alloc(_Resolution, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: Vector4Format, name: "Tempdist", enableRandomWrite: true, autoGenerateMips: false);

            DistanceTexture?.Release();
            DistanceTexture = new RenderTexture(_Resolution, _Resolution, 16, Vector4Format);
            DistanceTexture.filterMode = FilterMode.Point;
            DistanceTexture.name = "DistanceTexture";

            distanceBuffer?.Dispose();
            distanceBuffer = new ComputeBuffer(_Resolution * 2, sizeof(float), ComputeBufferType.Default);

            DistStrip_Temp?.Release();
            DistStrip_Temp = RTHandles.Alloc(
               2, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
               name: "DistStrip_Temp", enableRandomWrite: true, autoGenerateMips: false);

            NoiseStrip_Temp?.Release();
            NoiseStrip_Temp = RTHandles.Alloc(
               2, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
               name: "NoiseStrip_Temp", enableRandomWrite: true, autoGenerateMips: false);

            DistTex_Temp?.Release();
            DistTex_Temp = RTHandles.Alloc(
                960, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
                name: "DistTex_Temp", enableRandomWrite: true);

            LF_Ruler?.Release();
            LF_Ruler = RTHandles.Alloc(
            192, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: Vector4Format,
            name: "Grid and Frequency", enableRandomWrite: true
            );

            LFDistStrip_Main?.Release();
            LFDistStrip_Main = RTHandles.Alloc(
            2,_Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: VectorFormat,
            name: "DistStrip_Main", enableRandomWrite: true
            );
            LFNoiseStrip_Main?.Release();
            LFNoiseStrip_Main = RTHandles.Alloc(
            2, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: VectorFormat,
            name: "NoiseStrip_Main", enableRandomWrite: true
            );
            LFDistTex_Main?.Release();
            LFDistTex_Main = RTHandles.Alloc(
                960, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
                name: "DistTex_Main", enableRandomWrite: true);

            HFDistStrip_Main?.Release();
            HFDistStrip_Main = RTHandles.Alloc(
            2, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: VectorFormat,
            name: "DistStrip_Main", enableRandomWrite: true
            );
            HFNoiseStrip_Main?.Release();
            HFNoiseStrip_Main = RTHandles.Alloc(
            2, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: VectorFormat,
            name: "NoiseStrip_Main", enableRandomWrite: true
            );
            HFDistTex_Main?.Release();
            HFDistTex_Main = RTHandles.Alloc(
                960, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
                name: "DistTex_Main", enableRandomWrite: true);

        }

    }

    private void DrawRuler(ProfilerDisplay Display)
    {
        var resRect = new Vector2(ResultDimensions.x, ResultDimensions.y);
        
        if (RulerSettings.RulerScale == 0) RulerSettings.RulerScale = 1;
        if (RulerSettings.RulerScale!=0 && Display.RulerText.Count != (RulerSettings.RulerScale) || Display.RulerText == null )
        {
            CreateRulerTextList(Display, RulerSettings.RulerScale);
        }
        Draw(resRect, resRect / 2f, 1f);

        void Draw(Vector2 resWH, Vector2 Cent, float z)
        {
            Vector2 dir, offset_val, dCoord, offset_sign;
            string text;

            dir = Vector2.left;
            offset_val = new Vector2(3f, 0f);
            offset_sign = new Vector2(1f, -1f);

            int C = Display.RulerText.Count;
            for (int i = 0; i < C; i++)
            {
                offset_sign = new Vector2(1f, -1f);
                float yCoord = 1.0f;
                if (i == C - 1) offset_sign.y *= -1f;
                dCoord = new Vector2(resWH.x / 2, (resWH.y) *(yCoord*(i)/(C-1)-0.5f) + offset_val.x * (1- (yCoord * (i) / (C - 1) - 0.5f))) + Vector2.Perpendicular(dir) * offset_val.y;

                string dim = "m";
                float rangeVal = MaxScanRange * (1 - ((float)i / (C - 1)));
                if (Display.MsScale == true) { rangeVal *= 1000f / 1435f; dim = "ms"; }
                text = Mathf.Round(rangeVal).ToString()+ dim;
                sm(Display.RulerText[i], text, dCoord, dir, offset_sign);
            }
        }

        void sm(Text mark, string text, Vector2 coord, Vector2 offsetDir, Vector2 offset_sign)
        {
            var bounds = new Vector2(mark.preferredWidth + 0f, mark.preferredHeight) / 2f;
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = RulerFontSize;

            var dCoord = coord + offsetDir * bounds.x * offset_sign.x + Vector2.Perpendicular(offsetDir) * bounds.y * offset_sign.y;

            mark.rectTransform.localPosition = dCoord;
            mark.color =RulerSettings.DigitsColor;
            mark.gameObject.SetActive(true);
        }
    }
    private void MoveEventLine(ProfilerDisplay Display)
    {
        var resRect = new Vector2(ResultDimensions.x, ResultDimensions.y);
        Move(Display, resRect, resRect / 2f, 1f);

        void sm(Text mark, string text, Vector2 coord, Vector2 offsetDir, Vector2 offset_sign)
        {
            var bounds = new Vector2(mark.preferredHeight + 0f, mark.preferredWidth + 4f) / 2f;
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = EventFontSize;

            var dCoord = coord + offsetDir * bounds.x * offset_sign.x + Vector2.Perpendicular(offsetDir) * bounds.y * offset_sign.y;

            mark.rectTransform.localPosition = dCoord;
            mark.color = GridColor;
            mark.gameObject.SetActive(true);
        }
        void Move(ProfilerDisplay display, Vector2 resWH, Vector2 Cent, float z)
        {
            if (display.EventOn == false)
            {
                for (int k = 0; k < display.Event.TextList.Count; k++)
                {
                    display.Event.TextList[k].text1.gameObject.SetActive(false);
                    //display.Event.TextList[k].uvOffset = 1.2f;
                }
                display.eventTimer = 0f;
                return;
            }
            if (display.Event.Text == false)
            {
                for (int k = 0; k < display.Event.TextList.Count; k++)
                {
                    display.Event.TextList[k].text1.gameObject.SetActive(false);
                }
            }

            display.Event.EventInterval = Mathf.Clamp(display.Event.EventInterval, 1f, 30f);

            float ScrollTime = (float)display.Scroll * UpdateTimeMs * (960f / MainWindowToRulerRatio / (float)ScrollAmount) / 1000f;
            ScrollTime = Mathf.Clamp(ScrollTime, 0f, 60f);
            float MaxLines = Mathf.Clamp(ScrollTime / display.Event.EventInterval, 1f, 60f);

            int currentSize = display.Event.TextList.Count;
            int neededSize = Mathf.FloorToInt(MaxLines) + 1;

            CheckIfListSizeIsSufficient(out bool less);

            for (int i = 0; i < display.Event.TextList.Count; i++)
            {
                Vector2 dir, offset_val, dCoord, offset_sign;
                string text;

                dir = Vector2.up;
                offset_val = new Vector2(0f, 0f);
                offset_sign = new Vector2(1f, 1f);

                if (display.Event.TextList[i].uvOffset < 0)
                {
                    //                    Debug.Log(display.Event.TextList[i].uvOffset);
                    if (less == true)
                    {
                        display.Event.TextList[i].text1.gameObject.SetActive(false);
                        display.Event.TextPool.Push(display.Event.TextList[i]);
                        display.Event.TextList.RemoveAt(i);
                        i--;
                        if (neededSize == display.Event.TextList.Count) less = false;
                    }
                    continue;
                }
                display.Event.TextList[i].uvOffset -= ScrollAmount / (960f/MainWindowToRulerRatio);

                float uv_offset = display.Event.TextList[i].uvOffset - 0.5f;
                text = display.Event.TextList[i].textStrip;
                dCoord = new Vector2(uv_offset * resWH.x, resWH.y / 2) + Vector2.Perpendicular(dir) * offset_val.y;
                dir = Vector2.Perpendicular(dir);
                sm(display.Event.TextList[i].text1, text, dCoord, dir, offset_sign);

            }

            void CheckIfListSizeIsSufficient(out bool sizeLess)
            {
                sizeLess = false;

                if (neededSize > currentSize)
                {
                    //timer = 0;
                    int delta = Mathf.Abs(neededSize - currentSize);
                    for (int i = 0; i < delta; i++)
                    {
                        display.Event.TextList.Insert(0, display.Event.TextPool.Pop());
                    }
                }
                else if (neededSize < currentSize)
                {
                    sizeLess = true;
                }
            }
        }

    }

    private void EmitEventLine(ProfilerDisplay display)
    {
        display.eventTimer += Time.deltaTime;
        if (display.eventTimer > display.Event.EventInterval)
        {
            if (display.Event.TextList[0].uvOffset > 0) display.Event.TextList.Insert(0, display.Event.TextPool.Pop());
            display.eventTimer -= display.Event.EventInterval;
            display.Event.TextList[0].text1.gameObject.SetActive(true);
            display.Event.TextList[0].uvOffset = MainWindowToRulerRatio;
            //display.Event.TextList[0].textStrip = (Mathf.Round(Time.timeSinceLevelLoad * 100f) / 100f).ToString();
            display.Event.TextList[0].textStrip = display.Event.EventText;
            PushFirstToEnd(display.Event.TextList);
        }
    }

    private void PushFirstToEnd(List<EventText> list)
    {
        var x = list[0];
        list.RemoveAt(0);
        list.Add(x);
    }

    private void CreateTextPoolAndList()
    {

        ClearTextPool(LFDisplay);
        ClearTextPool(HFDisplay);

        CreatePoolOf(LFDisplay);
        CreatePoolOf(HFDisplay);

        void CreatePoolOf(ProfilerDisplay display)
        {
            display.Event.TextList = new List<EventText>();
            display.Event.TextPool = new Stack<EventText>();
            for (int i = 0; i < 100; i++)
            {
                var go = new GameObject("GridMark " + i);
                var tmp = go.AddComponent<Text>() as Text;
                display.Event.TextPool.Push(new EventText(tmp));
                go.transform.SetParent(display.TextPlaceholder);
                go.transform.localScale = Vector3.one;
                go.transform.eulerAngles = new Vector3(0, 0, 90);
                tmp.alignment = TextAnchor.MiddleCenter;
                go.SetActive(false);
            }
        }
    }

    private void ClearTextPool(ProfilerDisplay display)
    {
        string name;
        if (display == LFDisplay) name = "LFTextMarksPlaceholder";
        else name = "HFTextMarksPlaceholder";
        display.TextPlaceholder = display.DisplayImage.Find(name);

        if (display.TextPlaceholder == null)
        {
            display.TextPlaceholder = new GameObject(name).transform;
            display.TextPlaceholder.SetParent(display.DisplayImage, false);
            display.Event.TextPool?.Clear();
        }
        else
        {
            for (int i = display.TextPlaceholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(display.TextPlaceholder.GetChild(i).gameObject);
        }
    }
    private void CreateRulerTextList(ProfilerDisplay display, int size)
    {
        ClearRulerTextList(display);

        display.RulerText = new List<Text>();
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("GridMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            display.RulerText.Add(tmp);
            go.transform.SetParent(display.RulerTextPlaceholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
        
    }
    private void ClearRulerTextList(ProfilerDisplay display)
    {

        string name;
        if (display == LFDisplay) name = "LFRulerTextMarksPlaceholder";
        else name = "HFRulerTextMarksPlaceholder";
        display.RulerTextPlaceholder = display.DisplayImage.Find(name);

        if (display.RulerTextPlaceholder == null)
        {
            display.RulerTextPlaceholder = new GameObject(name).transform;
            display.RulerTextPlaceholder.SetParent(display.DisplayImage, false);
            display.RulerText?.Clear();
        }
        else
        {
            for (int i = display.RulerTextPlaceholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(display.RulerTextPlaceholder.GetChild(i).gameObject);
        }
    }
    protected override void Cleanup()
    {
        // Cleanup code

        distanceBuffer?.Dispose();
        DistanceTexture?.Release();
        LFDistStrip_Main?.Release();
        LFDistTex_Main?.Release();
        DistTex_Temp?.Release();
        LFResult?.Release();

        ClearTextPool(LFDisplay);
        ClearTextPool(HFDisplay);
    }


}
