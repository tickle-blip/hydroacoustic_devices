using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

using System.Collections.Generic;
using System;

using TMPro;

enum ScanMode
{
    SideScan,
    ForwardLooking
}

enum WedgeDisplayMode
{
    FullImage,
    OnlyNoise,
    None
}
class F50_Pass : CustomPass
{
    #region Fields
    //public RawImage Test_img3;
    //public RawImage Test_img1;
    //public RawImage distance_img;
    public Vector2Int ResultDimensions = new Vector2Int(1920, 1080);
    public RawImage ResultSonarImage;
    public RawImage ZoomResultImage;
    public RawImage SnippetsResult;
    public RawImage WaterColumnResult;
    public RawImage SideScanResult;
    public RawImage Pseudo3DResult;

    public Camera Camera_UI;
    public Camera Zoom_Camera_UI;

    public TypeCreator Type_Creator;
    /////-------------"UI Fields"------------------/////

    [HideInInspector] public float FrameRate;
    [Range(0f, 1f)]
    public float Gain = 0.25f;
    [Range(0f, 10f)]
    public float TVG = 1f;

    [Range(90, 1440)]
    [SerializeField] private int _Resolution = 720; // Change via SetResolution

    public int BeamCount = 512;
    [Range(2f, 300f)]
    [SerializeField] private float MaxScanRange = 60f;
    [Range(0f, 300f)]
    [SerializeField] private float MinScanRange = 0;

    [Range(2f, 500f)]
    public float MaxDisplayRange = 60f;
    [Range(0f, 300f)]
    public float MinDisplayRange = 0f;

    [Range(0f, 1f)]
    public float Sensitivity = 0;

    [Range(0f, 2f)]
    public float Contrast = 1f;

    [Range(0f, 2f)]
    public float Gamma = 0.5f;

    public int SmoothRadius = 1;
    public bool Smoothing = false;

    public ScanMode ScanMode = ScanMode.SideScan;

    public WedgeDisplayMode WedgeDisplayMode = WedgeDisplayMode.FullImage;

    //FlexMode
    public bool EnableFlexMode = false;
    public float LeftFlexAngle = 0f;
    public float RightFlexAngle = 130f;
    public float BeamDensity = 0f;

    //Scroll settings
    [Range(1f, 60f)]
    public int RulerFontSize = 10;
    public int ScrollAmount = 2;
    public float MainWindowToRulerRatio = 0.92f;
        
    //SideScanSettings
    public bool EnableSideScan = false;
    public RulerSettings SideScanRulerSettings = new RulerSettings();

    //Scan Modes toogles
    public bool EnableWaterColumn = false;
    [Range(1f, 60f)]
    public int EventFontSize = 10;

    public ProfilerDisplay WaterColumnDisplay = new ProfilerDisplay();
    public int WC_BeamNumber = 1;

    public bool EnableSnippets = false;
    public RulerSettings SnippetsRulerSettings = new RulerSettings();

    public bool EnableDetect = false;
    public Color DetectColor = Color.green;
    public float MinDetectRange = 0f;
    public float MaxDetectRange = 100f;
    public bool EnableHistory = false;
    public Vector3[] History_Colors = { new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(1,1,0),new Vector3(1, 0, 0) }; //Color Palette. colors in range (0.0 - 1.0)

    public float ZeroGroundLevel = 0f;
    public float UpperLevel = 3f;
    public float HistoryPingDelay = 1f;
    //public float Wedge_ZeroGroundLevel = 5f;
    //public float Wedge_UpperLevel = 10f;
    public Vector2 ShiftSwath = new Vector2(0f, -1f);
    public int SwathSpacing = 1;
    public int Wedge_MemoryAmount = 256;
    //Pseudo3d Settings
    public bool EnablePseudo3D = false;

    public Vector3[] Pseudo3d_Colors = { new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) }; //Color Palette. colors in range (0.0 - 1.0)

//    public bool ExplorationMode3d = false;
    //public int MemoryAmount = 100;

    //Image Display settings
    public bool MirrorImage = true;
    public float ImageRotation = 0;
    public Vector2Int CentrePercent = Vector2Int.zero; //Change via SetPositionX, SetPositionY
    public int RadiusPercent = 50; //Change via SetRadius

    public Vector3[] Colors = { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1) }; //Color Palette. colors in range (0.0 - 1.0)

    public Color BackgroundColor = Color.black;
    public Color GridColor = Color.grey;
    public Color BoxColor = Color.red;

    [Range(0f, 10f)]
    public int Rings = 4;
    [Range(0f, 10f)]
    public int Lines = 5;

    public bool ShowDigits = true;
    public bool ShowGrid = false;
    public bool ShowMark = false;
    public bool PolarView = false;


    //Zoom Settings
    [HideInInspector]public bool AcousticZoom = false;
    [HideInInspector] public Vector2 MouseCoords = new Vector2(0.5f, 0.5f); // Mouse Coords image UV space  . (0,0) - left bottom corner
    [HideInInspector] public Vector2Int ZoomWindowRect = new Vector2Int(300, 500); //Zoom window Width, Height
    [HideInInspector] public Vector2Int ZoomParams = new Vector2Int(50, 1); // Zoom %, Zoom Factor

    /////--------------"Scripting Fields"------------------/////

    public Font CustomFont;

    public Dictionary<MarkType, Sprite> MarkSprites;

    [Range(0f, 3f)]
    public float LinesThickness = 1f; //Rings and grid thickness
    [Range(1, 10)]
    public int BoxSize = 5;
    [Range(1f, 60f)]
    public int FontSize = 20;

    [Range(3f, 25f)]
    public float SideScan_VerticalFieldOfView = 3f;
    [Range(3f, 25f)]
    public float Forward_VericalFieldOfView = 20f;

    private float VerticalFieldOfView = 20f;
    [Range(90f, 130f)]
    public float HorizontalFieldOfView = 115f;

    //[Range(2f, 300f)]
    //public float NoiseTresholdForMaxFOV = 60f;
    //[Range(0f, 300f)]
    //public float NoiseTresholdForMinFOV = 0f;

    [Range(0.5f, 20f)]
    public float NoiseWideness = 10;

    [Range(0f, 150f)]
    public float Noise1_Scale = 20f;
    [Range(0f, 150f)]
    public float Noise2_Scale = 8f;
    [Range(0f, 1f)]
    public float Noise1_Bias = 0.54f;
    [Range(0f, 1f)]
    public float Noise2_Bias = 0.379f;

    [Range(0.5f, 200f)]
    public float NoisePatternScroll = 30f;

    public float CameraLookForwardAngle = 20f;
    public float CameraLookDownAngle = 90f;
    public Camera bakingCamera = null;
    public Camera pseudo3D_camera = null;
    public Transform shipTransform = null;

    /////-------------"Private Fields"------------------/////

    private int currentResolution = 0, currentBeamCount = 0;
    [SerializeField] private List<Mark> MarksList;
    private List<Text> S_ScanDistanceMarks;
    private List<Text> RingsDistanceMarks, Zoom_RingsDistanceMarks;
    private List<Text> GridDistanceMarks, Zoom_GridDistanceMarks;
    private List<Image> MarksGoList;
    private List<Image> Zoom_MarksGoList;
    private List<Text> SideScan_RulerText;
    private List<Text> Snippets_RulerText;
    private List<Text> WaterColumn_RulerText;
    private Transform text_marks_sideScan_placeholder;
    private Transform marks_placeholder;
    private Transform zoom_marks_placeholder;

    private Transform text_marks_placeholder;
    private Transform zoom_text_marks_placeholder;
    private Transform sidescan_ruler_text_marks_placeholder;
    private Transform snippets_ruler_text_marks_placeholder;
    private Transform watercolumn_text_marks_placeholder, watercolumn_ruler_text_marks_placeholder;

    //Final Texture
    [HideInInspector] public RTHandle Result, Zoom_Result;

    private GraphicsFormat uintFormat;
    private GraphicsFormat vectorFormat;
    private GraphicsFormat vector4Format;
    //Texture Handles
    private RenderTexture distanceTexture;
    private RenderTexture zoom_DistanceTexture;

    private RenderTexture pseudo3D_Positions;
    private RenderTexture pseudo3D_Result;
    private RTHandle distribution_Transit1, distribution_Transit2, distribution_Together;
    private RTHandle detect_Texture;
    private RTHandle zoom_distribution_Transit1, zoom_distribution_Transit2, zoom_distribution_Together;

    private RTHandle waterColumn_scan_strip,scan_strip, ruler;
    private RTHandle waterColumn_scroll, waterColumn_scroll_temp, waterColumn_result;
    private RTHandle sidescan_scroll, sidescan_scroll_temp, sidescan_result;
    private RTHandle snippets_scroll, snippets_scroll_temp, snippets_result;
    //private RTHandle noise_strip;
    private ComputeBuffer distanceBuffer, zoom_distanceBuffer, strip_distanceBuffer;
    private ComputeBuffer pseudo3D_buffer;

    private int wedgePingNumber = 0;
    private ComputeBuffer BeamBuffer,BeamHistoryBuffer;
    private int pingNumber = 0;
    //Shader Fields

    private ShaderTagId[] shaderTags;

    private ComputeShader DistributionComputer;
    private ComputeShader InterpolationComputer;
    private ComputeShader Pseudo3dComputer;
    private ComputeShader BlurComputer;

    private Material overrideMaterial = null;
    private Material terrainOverrideMaterial = null;
    private Material pseudo3dMaterial = null;
    private Material dotMaterial = null;
    //Shader Hash Handles

    private int handleMapDots;
    private int handleProcessImage;

    private int handleClearBuffer;
    private int handleClearBeamBuffer;


    private int handleDistribution_Main;
    private int handleDistribution_NoiseMain;
    private int handleDistribution_FillWaterColumnStrip;
    private int handleDistribution_FillSideScanStrip;
    private int handleDistribution_FillSnippetsStrip;
    private int handleDistribution_ComputeDetect;

    private int handleInterpolation_RenderInPolar;
    private int handleInterpolation_RenderInCartesian;
    private int handleInterpolation_RenderDetect;
    private int handleInterpolation_DrawWedgeHistory;
    private int handleInterpolation_ShiftSideScanTexture;
    private int handleInterpolation_ShiftWaterColumnTexture;
    private int handleInterpolation_RenderWaterColumnFinal;
    private int handleInterpolation_RenderSideScanFinal;
    private int handleInterpolation_RenderDepthRuler;

    private int handleBlurHor;
    private int handleBlurVer;

    private int handleInterpolation_Remap;
    private int handle_Zoom_RenderInPolar;
    private int handle_Zoom_RenderInCartesian;

    private int handleDrawWedge;
    //Fields for Fps
    private float time, pingTime,wedge_pingTime;
    private Vector4 zoomRect;
    private Vector2Int zoomDim;

    private ScanMode _scMode_flag;
    private int zoom_Resolution;
    #endregion

    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return overrideMaterial; yield return terrainOverrideMaterial; }
    
    public void ChangeView(ScanMode mode)
    {
        if (_scMode_flag == mode)
            return;
        _scMode_flag = mode;
        var r = bakingCamera.transform.localEulerAngles;

        switch (mode)
        {
            case ScanMode.SideScan:
                {
                    MirrorImage = true;
                    bakingCamera.transform.localEulerAngles = new Vector3(CameraLookDownAngle, r.y , r.z);
                    VerticalFieldOfView = SideScan_VerticalFieldOfView;
                    ImageRotation = 180f;
                    CentrePercent.y = 50;
                    break;
                }
            case ScanMode.ForwardLooking:
                {
                    MirrorImage = false;
                    ImageRotation = 0f;
                    CentrePercent.y = 0;
                    bakingCamera.transform.localEulerAngles = new Vector3(CameraLookForwardAngle, r.y, r.z);
                    VerticalFieldOfView = Forward_VericalFieldOfView;
                    break;
                }
        }
    }

    public void CreateMark()
    {
        MarksList.Add(new Mark());
    }

    public void ClearMark(int index)
    {
        MarksList.RemoveAt(index);
    }
    public void ClearMark(Mark mark)
    {
        MarksList.Remove(mark);
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

    private void InitSHaders_CashNameHandles()
    {

        shaderTags = new ShaderTagId[4]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("HDUnlitShader")
        };
        overrideMaterial = (Material)Resources.Load("Materials/Shared/Shared_Solid_Mat");
        terrainOverrideMaterial = (Material)Resources.Load("Materials/Shared/Shared_Terrain_Mat");
        pseudo3dMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/DistanceFillerShader"));
        dotMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Unlit/DotShader"));

        DistributionComputer = (ComputeShader)Resources.Load("ComputeShaders/F50/GSO_DistributionComputer");
        InterpolationComputer = (ComputeShader)Resources.Load("ComputeShaders/F50/GSO_InterpolationComputer");
        Pseudo3dComputer = (ComputeShader)Resources.Load("ComputeShaders/F50/GSO_Pseudo3D");
        BlurComputer = (ComputeShader)Resources.Load("ComputeShaders/Shared/BlurComputer");


        handleClearBuffer = DistributionComputer.FindKernel("ClearBuffer");
        handleClearBeamBuffer = DistributionComputer.FindKernel("ClearBeamBuffer");
        handleMapDots = Pseudo3dComputer.FindKernel("MapDots");

        handleDistribution_NoiseMain = DistributionComputer.FindKernel("MapBeamNoiseDistanceData");
        handleDistribution_Main = DistributionComputer.FindKernel("MapBeamDistanceData");

        handleDistribution_FillWaterColumnStrip = DistributionComputer.FindKernel("FillWaterColumnStrip");
        handleDistribution_ComputeDetect = DistributionComputer.FindKernel("ComputeDetect");
         handleDistribution_FillSideScanStrip = DistributionComputer.FindKernel("FillSideScanStrip");
         handleDistribution_FillSnippetsStrip = DistributionComputer.FindKernel("FillSnippetsStrip"); 

         
        
        handleInterpolation_Remap = InterpolationComputer.FindKernel("RemapQuad");
        handleInterpolation_RenderInPolar = InterpolationComputer.FindKernel("RenderInPolar");
        handleInterpolation_RenderInCartesian = InterpolationComputer.FindKernel("RenderInCartesian");
        handleInterpolation_DrawWedgeHistory = InterpolationComputer.FindKernel("DrawWedgeHistory");
        handleInterpolation_ShiftSideScanTexture = InterpolationComputer.FindKernel("FillSideScanScroll");
        handleInterpolation_ShiftWaterColumnTexture = InterpolationComputer.FindKernel("ShiftWaterColumnScroll");

        handleInterpolation_RenderSideScanFinal = InterpolationComputer.FindKernel("RenderSideScanFinal");
        handleInterpolation_RenderWaterColumnFinal = InterpolationComputer.FindKernel("RenderWaterColumnFinal");

        handleInterpolation_RenderDepthRuler = InterpolationComputer.FindKernel("RenderDepthRuler");

        handleInterpolation_RenderDetect = InterpolationComputer.FindKernel("RenderDetect");


        handleBlurHor = BlurComputer.FindKernel("HorzBlurCs");
        handleBlurVer = BlurComputer.FindKernel("VertBlurCs");

        handle_Zoom_RenderInPolar = InterpolationComputer.FindKernel("Zoom_RenderInPolar");

        handle_Zoom_RenderInCartesian = InterpolationComputer.FindKernel("Zoom_RenderInCartesian");

    }

    private bool IsNextUpdateAvaliable()
    {
        time += Time.deltaTime;
        pingTime += Time.deltaTime;
        //Frame rate varies from resolution and range
        var fr = 25f - 10f * (_Resolution / 1440f) - 10f * (MaxScanRange / 300);
        FrameRate = fr;
        if (time < 1f / fr)

            return false;
        else
        {
            time = 0;
            return true;
        }
    }

    //INIT
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if (bakingCamera == null)
        {
            Debug.Log("Set up baking Camera via inspector");
            return;
        }
        if (pseudo3D_camera == null)
        {
            Debug.Log("Set up pseudo3D_camera via inspector");
            return;
        }

        if (shipTransform == null)
        {
            Debug.Log("Set up shipTransform via inspector");
            return;
        }
        if (Camera_UI == null)
        {
            Debug.Log("Set up UI cameras via inspector");
            return;
        }
        if (Type_Creator == null)
            return;
        uintFormat = GraphicsFormat.R32_UInt;
        vectorFormat = GraphicsFormat.R32_SFloat;
        vector4Format = GraphicsFormat.R16G16B16A16_SFloat;

        SerializeMarkSpriteDictionaryIfPossible();

        bakingCamera.usePhysicalProperties = true;

        bakingCamera.enabled = false;
        pseudo3D_camera.enabled = false;
        SetUpCameraData(Camera_UI);
        SetUpCameraData(Zoom_Camera_UI);
        InitSHaders_CashNameHandles();

        zoomDim = Vector2Int.zero;
        currentResolution = 0;
        currentBeamCount = 0;
        AllocateTexturesIfNeeded();
        time = 0f;

        marks_placeholder = ResultSonarImage.transform.Find("MarksPlaceholder");
        zoom_marks_placeholder = ZoomResultImage.transform.Find("ZoomMarksPlaceholder");
        text_marks_placeholder = ResultSonarImage.transform.Find("TextMarksPlaceholder");
        text_marks_sideScan_placeholder = ResultSonarImage.transform.Find("SideScan_TextMarksPlaceholder");
        zoom_text_marks_placeholder = ZoomResultImage.transform.Find("ZoomTextMarksPlaceholder");
        sidescan_ruler_text_marks_placeholder = SideScanResult.transform.Find("SideScanMarksPlaceholder");
        snippets_ruler_text_marks_placeholder = SnippetsResult.transform.Find("SnippetsMarksPlaceholder");
        watercolumn_text_marks_placeholder = WaterColumnResult.transform.Find("WaterColumnMarksPlaceholder");
        watercolumn_ruler_text_marks_placeholder = WaterColumnResult.transform.Find("WaterColumnRulerMarksPlaceholder");
        MarksList = new List<Mark>();
        MarksGoList = new List<Image>();
        Zoom_MarksGoList = new List<Image>();
        SideScan_RulerText = new List<Text>();
        Snippets_RulerText = new List<Text>();
        WaterColumn_RulerText = new List<Text>();
        RingsDistanceMarks = new List<Text>();
        S_ScanDistanceMarks = new List<Text>();
        GridDistanceMarks = new List<Text>();
        Zoom_RingsDistanceMarks = new List<Text>();
        Zoom_GridDistanceMarks = new List<Text>();
        ClearTextMarks();
        ClearRulerTextList();

        cmd.SetComputeBufferParam(DistributionComputer, handleClearBeamBuffer, "BeamBuffer", BeamHistoryBuffer);
        cmd.DispatchCompute(DistributionComputer, handleClearBeamBuffer,(BeamHistoryBuffer.count + 511) / 512, 1, 1);
        WaterColumnDisplay.eventTimer = 0;
        WaterColumnDisplay.globalTimer = 0;
        WaterColumnDisplay.frameTimer = 0;
        WaterColumnDisplay.DisplayImage = WaterColumnResult.transform;
        CreateTextPoolAndList();

        AcousticZoom = false;
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
    {
        if (!bakingCamera || !pseudo3D_camera || !shipTransform || !terrainOverrideMaterial || !overrideMaterial || !distanceTexture || !Camera_UI)
        {
            Debug.Log("SMTH WENT WRONG");
            return;
        }
        if (camera.camera.cameraType == CameraType.SceneView)
            return;

        if (camera.camera != Camera_UI)
            return;

        ChangeView(ScanMode);
        
            time += Time.deltaTime;
        pingTime += Time.deltaTime;
        wedge_pingTime += Time.deltaTime;
        //Frame rate varies from resolution and range
        var fr = 25f - 10f * (_Resolution / 1440f) - 10f * (MaxScanRange / 300);
        FrameRate = fr;

        AllocateTexturesIfNeeded();
        ///////////----Render Objects With Override Materials
        CoreUtils.SetRenderTarget(cmd, distanceTexture, ClearFlag.All, clearColor: Color.black);
        RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, distanceTexture, new Vector4(0f, 0f, 1f, 1f));

        ///////////----Remap Camera Texture To Distance Distribution
        SetVar_DistributionComputer(cmd);


        float bc = (float)BeamCount;
        cmd.SetComputeFloatParam(InterpolationComputer, "ShowSSGrid", ScanMode == ScanMode.ForwardLooking ? -1f :1f);
        cmd.SetComputeFloatParam(InterpolationComputer, "MirrorImage", MirrorImage == true ? -1 : 1);
        cmd.SetComputeFloatParam(DistributionComputer, "Resolution", (float)_Resolution);
        cmd.SetComputeFloatParam(DistributionComputer, "FlexModeEnabled", EnableFlexMode ? 1f : 0f);
        cmd.SetComputeFloatParam(InterpolationComputer, "BeamCount", bc);
        cmd.SetComputeFloatParam(DistributionComputer, "BeamCount", bc);

        cmd.SetComputeFloatParam(InterpolationComputer, "Resolution", (float)_Resolution);
        cmd.SetComputeFloatParam(InterpolationComputer, "DetectModeEnabled", EnableDetect ? 1f : 0f);
        cmd.SetComputeFloatParam(InterpolationComputer, "FlexModeEnabled", EnableFlexMode ? 1f : 0f);


        //////Fill FlexMode parameters////////////
        CoreUtils.SetRenderTarget(cmd, distribution_Transit1, ClearFlag.Color, clearColor: Color.clear);
        CoreUtils.SetRenderTarget(cmd, distribution_Transit2, ClearFlag.Color, clearColor: Color.clear);

        LeftFlexAngle = Mathf.Clamp(LeftFlexAngle, 10f, Mathf.Max(RightFlexAngle-1f,0f));
        RightFlexAngle = Mathf.Clamp(RightFlexAngle, Mathf.Max(LeftFlexAngle + 1f, 0f), HorizontalFieldOfView-10f);
        int OutBeamRange = (int)((1f - BeamDensity) * BeamCount * (1f - (RightFlexAngle - LeftFlexAngle) / HorizontalFieldOfView));
        int InBeamRange = BeamCount - OutBeamRange;

        float leftUV = LeftFlexAngle / HorizontalFieldOfView;
        float rightUV = RightFlexAngle / HorizontalFieldOfView;

        float outBeamDensity = (1f - (rightUV - leftUV)) * distanceTexture.width / (float)OutBeamRange;
        float inBeamDensity = (rightUV - leftUV) * distanceTexture.width / (float)InBeamRange;
        float defBeamDensity = (float)(distanceTexture.width / (float)BeamCount);

        float leftcount = (OutBeamRange * (leftUV) / (1 + leftUV - rightUV));
        //Debug.Log(BeamCount - (leftcount + InBeamRange));

        cmd.SetComputeFloatParam(DistributionComputer, "BeamCount_left", leftcount);
        cmd.SetComputeFloatParam(DistributionComputer, "BeamCount_mid", InBeamRange);

        cmd.SetComputeFloatParam(DistributionComputer, "LeftUVBorder", leftUV);
        cmd.SetComputeFloatParam(DistributionComputer, "RightUVBorder", rightUV);
        cmd.SetComputeFloatParam(DistributionComputer, "OutBeamDensity", outBeamDensity);
        cmd.SetComputeFloatParam(DistributionComputer, "InBeamDensity", inBeamDensity);
        cmd.SetComputeFloatParam(DistributionComputer, "DefaultBeamDensity", defBeamDensity);

        cmd.SetComputeFloatParam(DistributionComputer, "LeftNumBorder", outBeamDensity);
        cmd.SetComputeFloatParam(DistributionComputer, "RightNumBorder", inBeamDensity);
        //////END FILL////////////
        
        //ClearDistanceBuffer
        cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", distanceBuffer);
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count + 1023) / 1024, 1, 1);

        //Fill Beam Data Main
        cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_Main, "DistanceBuffer", distanceBuffer);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_Main, "Source", distanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_Main, "DestinationUINT", distribution_Transit1);
        cmd.DispatchCompute(DistributionComputer, handleDistribution_Main, BeamCount,
                (distanceTexture.height+31)/32, 1);


        //ClearDistanceBuffer
        cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", distanceBuffer);
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count + 1023) / 1024, 1, 1);

        //Fill Beam Data Noise
        cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_NoiseMain, "DistanceBuffer", distanceBuffer);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_NoiseMain, "Source", distanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_NoiseMain, "DestinationUINT", distribution_Transit2);
        cmd.DispatchCompute(DistributionComputer, handleDistribution_NoiseMain, BeamCount,
                (distanceTexture.height + 31) / 32, 1);




        ////Interpolate Beam data

        outBeamDensity = (1f - (rightUV - leftUV)) * distribution_Together.referenceSize.x / (float)OutBeamRange;
        inBeamDensity = (rightUV - leftUV) * distribution_Together.referenceSize.x / (float)InBeamRange;

        defBeamDensity = (float)(distribution_Together.referenceSize.x / (float)BeamCount);
        
        cmd.SetComputeFloatParam(InterpolationComputer, "BeamCount", bc);
        cmd.SetComputeFloatParam(InterpolationComputer, "BeamCount_left", leftcount);
        cmd.SetComputeFloatParam(InterpolationComputer, "BeamCount_mid", InBeamRange);

        cmd.SetComputeFloatParam(InterpolationComputer, "LeftUVBorder", leftUV);
        cmd.SetComputeFloatParam(InterpolationComputer, "RightUVBorder", rightUV);
        cmd.SetComputeFloatParam(InterpolationComputer, "OutBeamDensity", outBeamDensity);
        cmd.SetComputeFloatParam(InterpolationComputer, "InBeamDensity", inBeamDensity);
        cmd.SetComputeFloatParam(InterpolationComputer, "DefaultBeamDensity", defBeamDensity);

        if (EnableWaterColumn == true)
        {


            EmitEventLine(WaterColumnDisplay);
            WaterColumnDisplay.frameTimer += Time.deltaTime;
            
            if (WaterColumnDisplay.frameTimer > (float)WaterColumnDisplay.Scroll * (1f/FrameRate) / 1000f)
            {
                WaterColumnDisplay.globalTimer += (float)WaterColumnDisplay.Scroll * (1 / FrameRate) / 1000f;
                WaterColumnDisplay.frameTimer = 0f;

                MoveEventLine(WaterColumnDisplay);
                DrawWaterColumnRuler();
                cmd.SetComputeIntParam(DistributionComputer, "WC_BeamNumber", WC_BeamNumber);
                //Fill WaterColumn Strip
                cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_FillWaterColumnStrip, "Read_TexUint_1", distribution_Transit2);
                cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_FillWaterColumnStrip, "Read_TexUint_2", distribution_Transit1);
                cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_FillWaterColumnStrip, "Destination", waterColumn_scan_strip);
                cmd.DispatchCompute(DistributionComputer, handleDistribution_FillWaterColumnStrip, (distribution_Transit1.referenceSize.y + 63) / 64,
                       1, 1);
                //Shift Texture left by "Scroll Amount" pixels + Render Strip "Scroll Amount" wide with interpolation
                cmd.CopyTexture(waterColumn_scroll, waterColumn_scroll_temp);
                cmd.SetComputeFloatParam(InterpolationComputer, "Scroll", Mathf.Clamp(ScrollAmount, 1f, 30f));

                cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftWaterColumnTexture, "DistStrip", waterColumn_scan_strip);
                cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftWaterColumnTexture, "Source", waterColumn_scroll_temp);
                cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftWaterColumnTexture, "Destination", waterColumn_scroll);
                cmd.DispatchCompute(InterpolationComputer, handleInterpolation_ShiftWaterColumnTexture, (waterColumn_scroll.referenceSize.x + 15) / 16,
                       (waterColumn_scroll.referenceSize.y + 15) / 16, 1);


                cmd.SetComputeIntParam(InterpolationComputer, "WC_GridOn", WaterColumnDisplay.ShowGrid == true ? 1 : 0);
                cmd.SetComputeIntParam(InterpolationComputer, "WC_GridCount", Mathf.Clamp(WaterColumnDisplay.GridCount, 1, 10));
                cmd.SetComputeFloatParam(DistributionComputer, "Time", (Time.realtimeSinceStartup * 10 % 500));
                cmd.SetComputeIntParam(InterpolationComputer, "WC_EventLine", ((WaterColumnDisplay.Event.Line && WaterColumnDisplay.EventOn) == true) ? 1 : 0);
                cmd.SetComputeVectorParam(InterpolationComputer, "WC_GridColor", GridColor);

                //Render Grid
                float[] uvOffsetArray = new float[WaterColumnDisplay.Event.TextList.Count];
                for (int i = 0; i < uvOffsetArray.Length; i++)
                {
                    EventText m = (EventText)WaterColumnDisplay.Event.TextList[i];
                    uvOffsetArray[i] = m.uvOffset;
                }

                cmd.SetComputeFloatParams(InterpolationComputer, "uvOffsetArray", uvOffsetArray);
                cmd.SetComputeIntParam(InterpolationComputer, "uvArrayCount", uvOffsetArray.Length);
                //Render Final ?
                cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderWaterColumnFinal, "Source", waterColumn_scroll);
                cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderWaterColumnFinal, "Destination", waterColumn_result);
                cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderWaterColumnFinal, (waterColumn_result.referenceSize.x + 31) / 32,
                       (waterColumn_result.referenceSize.y + 31) / 32, 1);
            }
        }
        

        if (EnableDetect == true && wedge_pingTime > HistoryPingDelay)
        {

            
            var v = bakingCamera.cameraToWorldMatrix;
            var p = GL.GetGPUProjectionMatrix(bakingCamera.projectionMatrix, true);
            var vp = p * v;
            wedge_pingTime = 0;


            cmd.SetComputeFloatParam(DistributionComputer, "MinDetectRange", MinDetectRange);
            cmd.SetComputeFloatParam(DistributionComputer, "MaxDetectRange", MaxDetectRange);

            cmd.SetComputeFloatParam(DistributionComputer, "OutBeamDensity", outBeamDensity);
            cmd.SetComputeFloatParam(DistributionComputer, "InBeamDensity", inBeamDensity);
            cmd.SetComputeFloatParam(DistributionComputer, "DefaultBeamDensity", defBeamDensity);

            cmd.SetComputeIntParam(DistributionComputer, "PingNumber", wedgePingNumber);
            cmd.SetComputeMatrixParam(DistributionComputer, "_InvViewMatrix", v);
            cmd.SetComputeIntParam(DistributionComputer, "Wedge_MemoryAmount", Wedge_MemoryAmount);
            cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_ComputeDetect, "PosBuffer", pseudo3D_buffer);
            cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_ComputeDetect, "BeamHistoryBuffer", BeamHistoryBuffer);
            cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_ComputeDetect, "BeamBuffer", BeamBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_ComputeDetect, "Read_TexUint_1", distribution_Transit2);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_ComputeDetect, "Read_TexUint_2", distribution_Transit1);
            cmd.DispatchCompute(DistributionComputer, handleDistribution_ComputeDetect, (distribution_Transit1.referenceSize.x + 63) / 64,
                   1, 1);


            wedgePingNumber = wedgePingNumber < (Wedge_MemoryAmount - 1) ? wedgePingNumber + 1 : 0;
            
        }



        ///////////----Connect Noise and True distr. to one texture

        switch (WedgeDisplayMode)
        {
            case WedgeDisplayMode.FullImage:
                break;
            case WedgeDisplayMode.OnlyNoise:{
                    CoreUtils.SetRenderTarget(cmd, distribution_Transit1, ClearFlag.Color, clearColor: Color.clear);
                    break;
                }
            case WedgeDisplayMode.None:{
                    CoreUtils.SetRenderTarget(cmd, distribution_Transit1, ClearFlag.Color, clearColor: Color.clear);
                    CoreUtils.SetRenderTarget(cmd, distribution_Transit2, ClearFlag.Color, clearColor: Color.clear);
                    break;
                }

        }
        cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_Remap, "Read_UintTex_1", distribution_Transit1);
        cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_Remap, "Read_UintTex_2", distribution_Transit2);
        cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_Remap, "Destination", distribution_Together);
        cmd.DispatchCompute(InterpolationComputer, handleInterpolation_Remap, (distribution_Together.referenceSize.x + 31) / 32,
               (distribution_Together.referenceSize.y + 31) / 32, 1);



        SetVar_DisplayAndZoomParams(cmd);
        SetVar_SonarParams(cmd);
        SetVar_SonarGridAndColor(cmd);


        if (Smoothing == true)
            BlurDistribution(cmd);


        ////////////----Render to Polar or Cartesian
        CoreUtils.SetRenderTarget(cmd, Result, ClearFlag.Color, clearColor: Color.clear);
        if (PolarView == true)
        {
            //cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderInPolar, "Noise", distribution_Transit2);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderInPolar, "Source", distribution_Together);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderInPolar, "Destination", Result);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderInPolar, (ResultDimensions.x + 31) / 32,
                   (ResultDimensions.y + 31) / 32, 1);
        }
        else
        {

            //cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderInCartesian, "Noise", distribution_Transit2);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderInCartesian, "Source", distribution_Together);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderInCartesian, "Destination", Result);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderInCartesian, (ResultDimensions.x + 31) / 32,
                  (ResultDimensions.y + 31) / 32, 1);
        }
       
        if (EnableDetect == true)
        {

            int tempRes = Shader.PropertyToID("tempResult");
            cmd.GetTemporaryRT(tempRes, Result.rt.descriptor);
            cmd.CopyTexture(Result, tempRes);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_DrawWedgeHistory, "Source", tempRes);
            //CoreUtils.SetRenderTarget(cmd, Result, ClearFlag.Color, clearColor: Color.clear);
            if (EnableHistory == true)
            {

                Vector4[] _cols = new Vector4[History_Colors.Length + 1];
                for (int i = 0; i < History_Colors.Length; i++)
                {
                    _cols[i] = History_Colors[i];
                }
                cmd.SetComputeVectorArrayParam(InterpolationComputer, "History_Colors", _cols);
                cmd.SetComputeIntParam(InterpolationComputer, "History_ColorsCount", _cols.Length);

                cmd.SetComputeFloatParam(InterpolationComputer, "ZeroLevel", ZeroGroundLevel);
                cmd.SetComputeFloatParam(InterpolationComputer, "UpperLevel", UpperLevel);
                cmd.SetComputeFloatParam(InterpolationComputer, "Range", MaxScanRange);
                cmd.SetComputeFloatParam(InterpolationComputer, "SWS_x", ShiftSwath.x);
                cmd.SetComputeFloatParam(InterpolationComputer, "SWS_y", ShiftSwath.y);
                cmd.SetComputeIntParam(InterpolationComputer, "SwathSpacing", SwathSpacing);


                cmd.SetComputeIntParam(InterpolationComputer, "Wedge_MemoryAmount", Wedge_MemoryAmount);
                cmd.SetComputeIntParam(InterpolationComputer, "PingNumber", wedgePingNumber);

                cmd.SetComputeBufferParam(InterpolationComputer, handleInterpolation_DrawWedgeHistory, "BeamBuffer", BeamHistoryBuffer);

                cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_DrawWedgeHistory, "Destination", Result);
                cmd.DispatchCompute(InterpolationComputer, handleInterpolation_DrawWedgeHistory, (BeamHistoryBuffer.count + 63) / 64,
                       1, 1);
            }
            cmd.SetComputeVectorParam(InterpolationComputer, "DetectColor", DetectColor);
            cmd.SetComputeBufferParam(InterpolationComputer, handleInterpolation_RenderDetect, "BeamBuffer", BeamBuffer);

            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderDetect, "Destination", Result);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderDetect, (BeamBuffer.count + 63) / 64,
                    1, 1);
          
            cmd.ReleaseTemporaryRT(tempRes);
        }
        
        if (AcousticZoom == true)
        {
            CoreUtils.SetRenderTarget(cmd, zoom_DistanceTexture, ClearFlag.All, clearColor: Color.black);
            RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, zoom_DistanceTexture, zoomRect);

            ///////////----Render Depth To Planar Texture
            cmd.SetComputeVectorParam(InterpolationComputer, "OriginTexWH", (Vector2)Result.referenceSize);

            cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", zoom_distanceBuffer);
            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (zoom_distanceBuffer.count + 1023) / 1024, 1, 1);

            CoreUtils.SetRenderTarget(cmd, zoom_distribution_Transit1, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_NoiseMain, "DistanceBuffer", zoom_distanceBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_NoiseMain, "Source", zoom_DistanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_NoiseMain, "Destination", zoom_distribution_Transit1);

            cmd.DispatchCompute(DistributionComputer, handleDistribution_NoiseMain, (zoom_DistanceTexture.width + 15) / 16,
                    (zoom_DistanceTexture.height + 15) / 16, 1);

            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (zoom_distanceBuffer.count + 1023) / 1024, 1, 1);

            CoreUtils.SetRenderTarget(cmd, zoom_distribution_Transit2, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_Main, "DistanceBuffer", zoom_distanceBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_Main, "Source", zoom_DistanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_Main, "Destination", zoom_distribution_Transit2);

            cmd.DispatchCompute(DistributionComputer, handleDistribution_Main, (zoom_DistanceTexture.width + 15) / 16,
                    (zoom_DistanceTexture.height + 15) / 16, 1);


            CoreUtils.SetRenderTarget(cmd, zoom_distribution_Together, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_Remap, "Noise", zoom_distribution_Transit1);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_Remap, "Source", zoom_distribution_Transit2);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_Remap, "Destination", zoom_distribution_Together);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_Remap, (zoom_distribution_Together.referenceSize.x + 15) / 16,
                   (zoom_distribution_Together.referenceSize.y + 15) / 16, 1);

            if (Smoothing == true) BlurZoomDistribution(cmd);

            ////////////----Render Polar and Cartesian
            CoreUtils.SetRenderTarget(cmd, Zoom_Result, ClearFlag.Color, clearColor: Color.clear);
            if (PolarView == true)
            {
                cmd.SetComputeTextureParam(InterpolationComputer, handle_Zoom_RenderInPolar, "Source", zoom_distribution_Together);
                cmd.SetComputeTextureParam(InterpolationComputer, handle_Zoom_RenderInPolar, "Destination", Zoom_Result);
                cmd.DispatchCompute(InterpolationComputer, handle_Zoom_RenderInPolar, (Zoom_Result.referenceSize.x + 31) / 32,
                       (Zoom_Result.referenceSize.x + 31) / 32, 1);
            }
            else
            {
                cmd.SetComputeTextureParam(InterpolationComputer, handle_Zoom_RenderInCartesian, "Source", zoom_distribution_Together);
                cmd.SetComputeTextureParam(InterpolationComputer, handle_Zoom_RenderInCartesian, "Destination", Zoom_Result);
                cmd.DispatchCompute(InterpolationComputer, handle_Zoom_RenderInCartesian, (Zoom_Result.referenceSize.x + 31) / 32,
                       (Zoom_Result.referenceSize.y + 31) / 32, 1);
            }
        }

        //Render SideScan
        //
        if (EnableSideScan == true)
        {
            //CoreUtils.SetRenderTarget(cmd, snippets_strip, ClearFlag.Color, clearColor: Color.clear);

            CoreUtils.SetRenderTarget(cmd, scan_strip, ClearFlag.Color, clearColor: Color.clear);

            //Clear buffer
            cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", strip_distanceBuffer);
            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (strip_distanceBuffer.count + 1023) / 1024, 1, 1);

            cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_FillSideScanStrip, "DistanceBuffer", strip_distanceBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_FillSideScanStrip, "Source", distanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_FillSideScanStrip, "Destination", scan_strip);

            cmd.DispatchCompute(DistributionComputer, handleDistribution_FillSideScanStrip, (distanceTexture.width + 15) / 16,
                   (distanceTexture.height + 15) / 16, 1);

            //SideScanLookAngle = Mathf.Clamp(SideScanLookAngle, -HorizontalFieldOfView / 2f, HorizontalFieldOfView / 2f);
            //cmd.SetComputeFloatParam(InterpolationComputer, "LookAngle", HorizontalFieldOfView / 2f + SideScanLookAngle);


            //Shift Texture left by "Scroll Amount" pixels + Render Strip "Scroll Amount" wide with interpolation
            cmd.CopyTexture(sidescan_scroll, sidescan_scroll_temp);
            cmd.SetComputeFloatParam(InterpolationComputer, "Scroll", Mathf.Clamp(ScrollAmount, 1f, 30f));

            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, "DistStrip", scan_strip);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, "Source", sidescan_scroll_temp);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, "Destination", sidescan_scroll);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, (sidescan_scroll.referenceSize.x + 15) / 16,
                   (sidescan_scroll.referenceSize.y + 15) / 16, 1);

            //Render Ruler 

            cmd.SetComputeFloatParam(InterpolationComputer, "WindowsRatio", MainWindowToRulerRatio);
            cmd.SetComputeFloatParam(InterpolationComputer, "RulerScale", SideScanRulerSettings.RulerScale);
            cmd.SetComputeFloatParam(InterpolationComputer, "ShowDivisions", SideScanRulerSettings.showSmallDivisions ? 1 : 0);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerBackground", SideScanRulerSettings.BackGroundColor);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerScaleColor", SideScanRulerSettings.AmplitudeScaleColor);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerDivisionsColor", SideScanRulerSettings.DivisionsColor);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerDigitsColor", SideScanRulerSettings.DigitsColor);

            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderDepthRuler, "DistStrip", scan_strip);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderDepthRuler, "Destination", ruler);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderDepthRuler, (ruler.referenceSize.x + 15) / 16, (ruler.referenceSize.y + 15) / 16, 1);

            ////////////----Render Final SideScan Image
            CoreUtils.SetRenderTarget(cmd, sidescan_result, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderSideScanFinal, "Ruler", ruler);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderSideScanFinal, "Source", sidescan_scroll);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderSideScanFinal, "Destination", sidescan_result);

            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderSideScanFinal, (ResultDimensions.x + 31) / 32,
                   (ResultDimensions.y + 31) / 32, 1);

            DrawSideScanRuler();
        }
        if (EnableSnippets == true)
        {

            //CoreUtils.SetRenderTarget(cmd, snippets_strip, ClearFlag.Color, clearColor: Color.clear);

            CoreUtils.SetRenderTarget(cmd, scan_strip, ClearFlag.Color, clearColor: Color.clear);

            //Clear buffer
            cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", strip_distanceBuffer);
            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (strip_distanceBuffer.count + 1023) / 1024, 1, 1);

            cmd.SetComputeBufferParam(DistributionComputer, handleDistribution_FillSnippetsStrip, "DistanceBuffer", strip_distanceBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_FillSnippetsStrip, "Source", distanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistribution_FillSnippetsStrip, "Destination", scan_strip);

            cmd.DispatchCompute(DistributionComputer, handleDistribution_FillSnippetsStrip, (distanceTexture.width + 15) / 16,
                   (distanceTexture.height + 15) / 16, 1);

            //SideScanLookAngle = Mathf.Clamp(SideScanLookAngle, -HorizontalFieldOfView / 2f, HorizontalFieldOfView / 2f);
            //cmd.SetComputeFloatParam(InterpolationComputer, "LookAngle", HorizontalFieldOfView / 2f + SideScanLookAngle);


            //Shift Texture left by "Scroll Amount" pixels + Render Strip "Scroll Amount" wide with interpolation
            cmd.CopyTexture(snippets_scroll, snippets_scroll_temp);
            cmd.SetComputeFloatParam(InterpolationComputer, "Scroll", Mathf.Clamp(ScrollAmount, 1f, 30f));

            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, "DistStrip", scan_strip);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, "Source", snippets_scroll_temp);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, "Destination", snippets_scroll);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_ShiftSideScanTexture, (snippets_scroll.referenceSize.x + 15) / 16,
                   (snippets_scroll.referenceSize.y + 15) / 16, 1);

            //Render Ruler 

            cmd.SetComputeFloatParam(InterpolationComputer, "WindowsRatio", MainWindowToRulerRatio);
            cmd.SetComputeFloatParam(InterpolationComputer, "RulerScale", SideScanRulerSettings.RulerScale);
            cmd.SetComputeFloatParam(InterpolationComputer, "ShowDivisions", SideScanRulerSettings.showSmallDivisions ? 1 : 0);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerBackground", SideScanRulerSettings.BackGroundColor);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerScaleColor", SideScanRulerSettings.AmplitudeScaleColor);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerDivisionsColor", SideScanRulerSettings.DivisionsColor);
            cmd.SetComputeVectorParam(InterpolationComputer, "RulerDigitsColor", SideScanRulerSettings.DigitsColor);

            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderDepthRuler, "DistStrip", scan_strip);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderDepthRuler, "Destination", ruler);
            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderDepthRuler, (ruler.referenceSize.x + 15) / 16, (ruler.referenceSize.y + 15) / 16, 1);

            ////////////----Render Final SideScan Image
            CoreUtils.SetRenderTarget(cmd, snippets_result, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderSideScanFinal, "Ruler", ruler);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderSideScanFinal, "Source", snippets_scroll);
            cmd.SetComputeTextureParam(InterpolationComputer, handleInterpolation_RenderSideScanFinal, "Destination", snippets_result);

            cmd.DispatchCompute(InterpolationComputer, handleInterpolation_RenderSideScanFinal, (ResultDimensions.x + 31) / 32,
                   (ResultDimensions.y + 31) / 32, 1);

            DrawSnippetsRuler();
        }
        ////////////////Render Pseudo 3d

        if (EnablePseudo3D == true)
        {
            var v = pseudo3D_camera.worldToCameraMatrix;
            var p = GL.GetGPUProjectionMatrix(pseudo3D_camera.projectionMatrix, true);
            var vp = p * v;
            Vector4 surfLevels = new Vector4(ZeroGroundLevel, UpperLevel, 1, 1);
            //cmd.SetGlobalFloatArray("SurfaceLevels", surfLevels);

            


            cmd.SetGlobalVector("SurfaceLevels", surfLevels);
            dotMaterial.SetBuffer("buffer", pseudo3D_buffer);

            dotMaterial.SetMatrix("_CameraViewMatrix", v);
            dotMaterial.SetMatrix("_InvViewMatrix", v.inverse);

            dotMaterial.SetMatrix("_CameraProjMatrix", p);
            dotMaterial.SetMatrix("_CameraInvProjMatrix", p.inverse);
            dotMaterial.SetMatrix("_ViewProjMatrix", vp);
            dotMaterial.SetMatrix("_CameraInvViewProjMatrix", vp.inverse);
            dotMaterial.SetMatrix("_CameraViewProjMatrix", vp);
            dotMaterial.SetVector("_PrParams", new Vector4(pseudo3D_camera.nearClipPlane, pseudo3D_camera.farClipPlane, 1 / pseudo3D_camera.nearClipPlane, 1 / pseudo3D_camera.farClipPlane));
            Vector4[] _cols = new Vector4[Pseudo3d_Colors.Length + 1];
            for (int i = 0; i < Pseudo3d_Colors.Length; i++)
            {
                _cols[i] = Pseudo3d_Colors[i];
            }
            cmd.SetGlobalVectorArray("History_Colors", _cols);
            cmd.SetGlobalInt("History_ColorsCount", _cols.Length);

            CoreUtils.SetRenderTarget(cmd, pseudo3D_Result, ClearFlag.All, clearColor: Color.black);
            //
            cmd.DrawProcedural(Matrix4x4.identity, dotMaterial, 0, MeshTopology.Points, pseudo3D_buffer.count);


        }
        



        if (ResultSonarImage != null)
        {
            ResultSonarImage.texture = Result;
            ResultSonarImage.SetNativeSize();
        }
        if (ZoomResultImage != null && Zoom_Result != null)
        {
            ZoomResultImage.gameObject.SetActive(AcousticZoom);
            ZoomResultImage.texture = Zoom_Result;
            ZoomResultImage.SetNativeSize();
        }
        if (SideScanResult != null)
        {
            SideScanResult.gameObject.SetActive(EnableSideScan);
            SideScanResult.texture = sidescan_result;
            //SideScanResult.SetNativeSize();
        }

        if (SnippetsResult != null)
        {
            SnippetsResult.gameObject.SetActive(EnableSnippets);
            SnippetsResult.texture = snippets_result;
            //SideScanResult.SetNativeSize();
        }

        if (WaterColumnResult != null)
        {
            WaterColumnResult.gameObject.SetActive(EnableWaterColumn);
            WaterColumnResult.texture = waterColumn_result;
            //SideScanResult.SetNativeSize();
        }
        DrawMarksOnUI();
        if (PolarView == false)
        {
                DrawCartesianViewDigits();
                DrawDownLook_ViewDigits();
        }
        else
            DrawPolarViewDigits();

        if (Pseudo3DResult != null)
        {
            Pseudo3DResult.gameObject.SetActive(EnablePseudo3D);
            Pseudo3DResult.texture = pseudo3D_Result;
        }
      /*  if (distance_img != null)
            distance_img.texture = sidescan_result;
        if (Test_img3 != null)
            Test_img3.texture = distribution_Transit1;
        if (Test_img1 != null && zoom_distribution_Together != null)
            Test_img1.texture = snippets_result;*/
    }

    private void BlurDistribution(CommandBuffer cmd)
    {
        //SetParams
        var weights = BlurCore.GetWeights(SmoothRadius);
        cmd.SetComputeBufferParam(BlurComputer, handleBlurHor, "gWeights", weights);
        cmd.SetComputeBufferParam(BlurComputer, handleBlurVer, "gWeights", weights);
        cmd.SetComputeIntParam(BlurComputer, "blurRadius", (int)SmoothRadius);

        int tempID = Shader.PropertyToID("tempTogether");
        cmd.GetTemporaryRT(tempID,distribution_Together.rt.descriptor);
        
        //SetTextures
        cmd.SetComputeTextureParam(BlurComputer, handleBlurHor, "source", distribution_Together);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurHor, "horBlurOutput", tempID);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurVer, "horBlurOutput", tempID);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurVer, "verBlurOutput", distribution_Together);
        //DispatchShaders
        cmd.DispatchCompute(BlurComputer, handleBlurHor, (distribution_Together.referenceSize.x + 1023) / 1024, distribution_Together.referenceSize.y, 1);
        cmd.DispatchCompute(BlurComputer, handleBlurVer, distribution_Together.referenceSize.x, (distribution_Together.referenceSize.y + 1023) / 1024, 1);

        cmd.ReleaseTemporaryRT(tempID);
    }

    private void BlurZoomDistribution(CommandBuffer cmd)
    {
        //SetTextures
        cmd.SetComputeTextureParam(BlurComputer, handleBlurHor, "source", zoom_distribution_Together);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurHor, "horBlurOutput", zoom_distribution_Transit1);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurVer, "horBlurOutput", zoom_distribution_Transit1);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurVer, "verBlurOutput", zoom_distribution_Together);
        //DispatchShaders
        cmd.DispatchCompute(BlurComputer, handleBlurHor, (zoom_distribution_Together.referenceSize.x + 1023) / 1024, zoom_distribution_Together.referenceSize.y, 1);
        cmd.DispatchCompute(BlurComputer, handleBlurVer, zoom_distribution_Together.referenceSize.x, (zoom_distribution_Together.referenceSize.y + 1023) / 1024, 1);
    }

    private void SetVar_DistributionComputer(CommandBuffer cmd)
    {
        cmd.SetComputeVectorParam(DistributionComputer, "WorldPosRot", new Vector4(shipTransform.position.x,
                                                                                              shipTransform.position.y,
                                                                                             shipTransform.position.z,
                                                                                              shipTransform.localEulerAngles.y));
        cmd.SetComputeFloatParam(DistributionComputer, "NoiseWideness", NoiseWideness);
        cmd.SetComputeFloatParam(DistributionComputer, "NoisePatternScroll", NoisePatternScroll);
        cmd.SetComputeFloatParam(DistributionComputer, "Range", MaxScanRange);
        cmd.SetComputeVectorParam(DistributionComputer, "Noise12_Scale_Bias", new Vector4(Noise1_Scale, Noise2_Scale, Noise1_Bias, Noise2_Bias));

        cmd.SetComputeFloatParam(DistributionComputer, "Gain", Gain);
        cmd.SetComputeFloatParam(DistributionComputer, "TVG", TVG);

        cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle1", 0);
        cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle2", 1);

        //cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle1", zoomRect.x);
        //cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle2", zoomRect.width + zoomRect.x);
        cmd.SetComputeFloatParam(DistributionComputer, "HorisontalFOV", HorizontalFieldOfView);
        cmd.SetComputeFloatParam(DistributionComputer, "VerticalFOV", VerticalFieldOfView);
       // cmd.SetComputeFloatParam(DistributionComputer, "TopNoiseTreshold", NoiseTresholdForMaxFOV);
       // cmd.SetComputeFloatParam(DistributionComputer, "BottomNoiseTreshold", NoiseTresholdForMinFOV);
    }

    private void SetVar_DisplayAndZoomParams(CommandBuffer cmd)
    {
        var offset = (Vector2)CentrePercent * 0.01f;
        var scale = 2f * RadiusPercent / 100f;

        cmd.SetComputeVectorParam(InterpolationComputer, "Offset", offset);
        cmd.SetComputeFloatParam(InterpolationComputer, "Scale_factor", scale);

        cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle1", zoomRect.x);
        cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle2", zoomRect.z + zoomRect.x);

        var left = PaniniProjectionScreenPosition(2 * zoomRect.x - 1) + 0.5f;
        left = zoomRect.y;

        var right = PaniniProjectionScreenPosition(2 * (zoomRect.z + zoomRect.x) - 1) + 0.5f;
        right = zoomRect.w;
        cmd.SetComputeFloatParam(InterpolationComputer, "Zoom_angle1", left);
        cmd.SetComputeFloatParam(InterpolationComputer, "Zoom_angle2", right);

        cmd.SetComputeVectorParam(InterpolationComputer, "MouseCoords", MouseCoords);
        cmd.SetComputeVectorParam(InterpolationComputer, "ZoomBox", (Vector2)ZoomWindowRect);
        cmd.SetComputeVectorParam(InterpolationComputer, "ZoomParams", (Vector2)ZoomParams);


        cmd.SetComputeFloatParam(InterpolationComputer, "MinRadius", (MinDisplayRange / MaxScanRange));
        cmd.SetComputeFloatParam(InterpolationComputer, "MaxRadius", MaxDisplayRange / MaxScanRange);
    }

    private void SetVar_SonarParams(CommandBuffer cmd)
    {

        cmd.SetComputeFloatParam(InterpolationComputer, "HorisontalFOV", HorizontalFieldOfView);
        cmd.SetComputeFloatParam(InterpolationComputer, "VerticalFOV", VerticalFieldOfView);
        cmd.SetComputeFloatParam(InterpolationComputer, "MinimumRange", Mathf.Clamp01(MinScanRange / MaxScanRange));
    }

    private void SetVar_SonarGridAndColor(CommandBuffer cmd)
    {

        cmd.SetComputeIntParam(InterpolationComputer, "GridCount", ShowGrid == true ? 2 : 0);
        cmd.SetComputeIntParam(InterpolationComputer, "LinesCount", ScanMode == ScanMode.ForwardLooking ? Lines:0);
        cmd.SetComputeIntParam(InterpolationComputer, "Rings", ScanMode == ScanMode.ForwardLooking ? Rings : 1);
        cmd.SetComputeFloatParam(InterpolationComputer, "Rotation", ImageRotation);

        cmd.SetComputeFloatParam(InterpolationComputer, "ApplyZoom", AcousticZoom == true ? 1f : 0);
        cmd.SetComputeFloatParam(InterpolationComputer, "ShowSector", ShowGrid == true ? 1f : 0);

        cmd.SetComputeFloatParam(InterpolationComputer, "Thickness", LinesThickness / 1000f);
        cmd.SetComputeIntParam(InterpolationComputer, "FontSize", FontSize);

        cmd.SetComputeFloatParam(InterpolationComputer, "LowerFactor", Sensitivity);
        cmd.SetComputeFloatParam(InterpolationComputer, "MiddleFactor", Contrast);
        cmd.SetComputeVectorParam(InterpolationComputer, "BoxColor", BoxColor);
        cmd.SetComputeFloatParam(InterpolationComputer, "BoxSize", BoxSize);

        Vector4[] _cols = new Vector4[Colors.Length + 1];
        _cols[0] = BackgroundColor;
        for (int i = 0; i < Colors.Length; i++)
        {
            _cols[i + 1] = Colors[i];
        }
        cmd.SetComputeVectorArrayParam(InterpolationComputer, "_Colors", _cols);
        cmd.SetComputeIntParam(InterpolationComputer, "_ColorsCount", _cols.Length);

        cmd.SetComputeFloatParam(InterpolationComputer, "Gamma", Gamma);
        cmd.SetComputeVectorParam(InterpolationComputer, "_BackgroundColor", BackgroundColor);
        cmd.SetComputeVectorParam(InterpolationComputer, "_GridColor", GridColor);

    }

    private void CheckIfMouseIsInImageBounds()
    {
        //Add Rotation
        if (PolarView == false)
        {
            var boxC = MouseCoords * ResultDimensions;
            var sonarC = (Vector2)ResultDimensions / 2f + (Vector2)CentrePercent / 100f * (Vector2)ResultDimensions;
            var boxCentRelative = boxC - sonarC;
            Vector2 d1 = boxCentRelative - (Vector2)ZoomWindowRect / 2f * ZoomParams.x / 100f;
            Vector2 d2 = boxCentRelative + (Vector2)ZoomWindowRect / 2f * ZoomParams.x / 100f;
            var minDim = Mathf.Min(ResultDimensions.x, ResultDimensions.y);
            Vector2 SonarMinRect = sonarC - new Vector2(minDim * RadiusPercent / 100f, -ZoomWindowRect.y / 2f * ZoomParams.x / 100f);
            Vector2 SonarMaxRect = sonarC + new Vector2(minDim * RadiusPercent / 100f, minDim * RadiusPercent / 100f);
            boxC.x = Mathf.Clamp(boxC.x, SonarMinRect.x, SonarMaxRect.x);
            boxC.y = Mathf.Clamp(boxC.y, SonarMinRect.y, SonarMaxRect.y);
            MouseCoords = boxC / ResultDimensions;
        }
        else
        {
            var boxC = MouseCoords * ResultDimensions;
            Vector2 SonarMinRect = new Vector2(0, 0);
            Vector2 SonarMaxRect = ResultDimensions;
            boxC.x = Mathf.Clamp(boxC.x, SonarMinRect.x, SonarMaxRect.x);
            boxC.y = Mathf.Clamp(boxC.y, SonarMinRect.y, SonarMaxRect.y);
            MouseCoords = boxC / ResultDimensions;
        }
    }
    private Vector4 CalculateZoomCameraRect()
    {
        var boxC = MouseCoords * ResultDimensions;
        CheckIfMouseIsInImageBounds();
        if (PolarView == false)
        {
            var sonarC = (Vector2)ResultDimensions / 2f + (Vector2)CentrePercent / 100f * (Vector2)ResultDimensions;
            var boxCentRelative = boxC - sonarC;
            Vector2 s = new Vector2(Mathf.Sign(boxCentRelative.x), Mathf.Sign(boxCentRelative.y));

            boxCentRelative = new Vector2(Mathf.Abs(boxCentRelative.x), Mathf.Abs(boxCentRelative.y));
            Vector2 d1 = boxCentRelative - (Vector2)ZoomWindowRect / 2f * ZoomParams.x / 100f;
            Vector2 d2 = boxCentRelative + (Vector2)ZoomWindowRect / 2f * ZoomParams.x / 100f;
            d1 *= 2f * RadiusPercent / 100f;
            d2 *= 2f * RadiusPercent / 100f;
            float signx = Mathf.Sign(d1.x);
            float signy = Mathf.Sign(d1.y);
            Vector2 leftV = new Vector2(Mathf.Min(d1.x, d2.x), Mathf.Max(signx * d1.y, signx * d2.y) * signx);
            Vector2 rightV = new Vector2(Mathf.Max(signy * d1.x, signy * d2.x) * signy, Mathf.Min(d1.y, d2.y));
            leftV = leftV * s;
            rightV = rightV * s;
            leftV.Normalize();
            rightV.Normalize();

            //Debug.Log(boxCentRelative);
            //Debug.Log(d1);
            var leftAngle = (90f - Mathf.Atan2(leftV.y, leftV.x) * Mathf.Rad2Deg) % 360 - ImageRotation;
            //leftAngle = Mathf.Acos(leftV.y)*Mathf.Rad2Deg;
            var rightAngle = (90f - Mathf.Atan2(rightV.y, rightV.x) * Mathf.Rad2Deg) % 360 - ImageRotation;

            leftAngle = Mathf.Clamp(leftAngle, -HorizontalFieldOfView / 2f, HorizontalFieldOfView / 2f) + HorizontalFieldOfView / 2f;
            rightAngle = Mathf.Clamp(rightAngle, -HorizontalFieldOfView / 2f, HorizontalFieldOfView / 2f) + HorizontalFieldOfView / 2f;

            leftAngle /= HorizontalFieldOfView;
            rightAngle /= HorizontalFieldOfView;
            var nleftAngle = Mathf.Min(leftAngle, rightAngle);
            var nrightAngle = Mathf.Max(leftAngle, rightAngle);

            var r = new Vector4(0, nleftAngle, nrightAngle - nleftAngle, nrightAngle);
            nleftAngle = FunctionXFromY(nleftAngle);
            nrightAngle = FunctionXFromY(nrightAngle);

            r.x = nleftAngle;
            r.z = nrightAngle - nleftAngle;
            return r;
            //float S1_angle = ImageRotation + (90 - HorizontalFieldOfView / 2);
            //float S2_angle = ImageRotation + (90 + HorizontalFieldOfView / 2);
            //float a = S1_angle + HorizontalFieldOfView * (k / (Lines + 1f));
            //Vector2 vec = new Vector2(-Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            //vec.Normalize();
        }
        else
        {
            Vector2 d1 = boxC - (Vector2)ZoomWindowRect / 2f * ZoomParams.x / 100f;
            Vector2 d2 = boxC + (Vector2)ZoomWindowRect / 2f * ZoomParams.x / 100f;
            d1 /= ResultDimensions;
            d2 /= ResultDimensions;

            var r = new Vector4(0, d1.x, 0, d2.x);

            d1.x = FunctionXFromY(d1.x);
            d2.x = FunctionXFromY(d2.x);
            r.x = d1.x;
            r.z = d2.x - d1.x;
            return r;
        }
    }


    float PaniniProjection(float V, float d, float s)
    {
        float XZLength = Mathf.Sqrt(V * V + 1.0f);
        float sinPhi = V / XZLength;
        //const float tanTheta = V.y / XZLength;
        float cosPhi = Mathf.Sqrt(1.0f - sinPhi * sinPhi);
        float S = (d + 1.0f) / (d + cosPhi);
        return S * sinPhi;
    }

    float FunctionXFromY(float yTarget)
    {
        float xTolerance = 0.0005f; //adjust as you please

        float lower = 0;
        float upper = 1;
        float percent = (upper + lower) / 2;

        //get initial x
        float x = percent;
        float y = PaniniProjectionScreenPosition(2 * x - 1) + 0.5f;
        //loop until completion
        int iter = 0;
        while (Mathf.Abs(yTarget - y) > xTolerance && iter < 100)
        {
            if (yTarget > y)
                lower = percent;
            else
                upper = percent;

            percent = (upper + lower) / 2;
            y = PaniniProjectionScreenPosition(2 * percent - 1f) + 0.5f;
            iter++;
        }
        //we're within tolerance of the desired x value.
        //return the y value.
        return percent;
    }

    float PaniniProjectionScreenPosition(float screenPosition)
    {
        float fovH = HorizontalFieldOfView * (3.14159265359f / 180f);
        const float D = 3;
        const float S = 1;
        float upscale = HorizontalFieldOfView <= 115 ? Mathf.Lerp(0.65f, 0.82f, (HorizontalFieldOfView - 90) / 25) : Mathf.Lerp(0.82f, 1.01f, (HorizontalFieldOfView - 115) / 15);

        float unproject = Mathf.Tan(0.5f * fovH);
        float project = 1.0f / unproject;

        // unproject the screenspace position, get the viewSpace xy and use it as direction
        float viewDirection = screenPosition * unproject;
        float paniniPosition = PaniniProjection(viewDirection, D, S);

        // project & upscale the panini position 
        return paniniPosition * project * upscale;
    }


    private float FieldOfViewToSensorSize(float fieldOfView, float focalL)
    {
        return Mathf.Tan(fieldOfView * Mathf.PI / 360.0f) * 2.0f * focalL;
    }

    private void RenderFromCamera(ScriptableRenderContext renderContext, CommandBuffer cmd, CullingResults cullingResult, Camera view, RenderTexture target_RT, Vector4 rect)
    {
        view.targetTexture = target_RT;
        view.TryGetCullingParameters(out var cullingParams);
        cullingParams.cullingOptions = CullingOptions.ShadowCasters;
        cullingResult = renderContext.Cull(ref cullingParams);
        bakingCamera.farClipPlane = MaxScanRange;
        bakingCamera.focalLength = 57f;
        float _horisontalFieldOfView = FieldOfViewToSensorSize(HorizontalFieldOfView, bakingCamera.focalLength);
        float _verticalFieldOfView = FieldOfViewToSensorSize(VerticalFieldOfView, bakingCamera.focalLength);
        bakingCamera.sensorSize = new Vector2(_horisontalFieldOfView, _verticalFieldOfView);
        bakingCamera.gateFit = Camera.GateFitMode.None;
        //bakingCamera.rect = new Rect(rect.x,rect.y,rect.z,rect.w);

        SetCameraMatrices(cmd, rect);

        //Draw Solid Objects
        var result = new RendererListDesc(shaderTags, cullingResult, bakingCamera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonOpaque,
            overrideMaterial = overrideMaterial,
            overrideMaterialPassIndex = overrideMaterial.FindPass("ForwardOnly"),
            excludeObjectMotionVectors = false,
            layerMask = LayerMask.GetMask("Solid"),
            stateBlock = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(true, CompareFunction.LessEqual) }
        };

        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));

        //Draw Terrain
        result = new RendererListDesc(shaderTags, cullingResult, bakingCamera)
        {
            rendererConfiguration = PerObjectData.None,
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = SortingCriteria.CommonOpaque,
            overrideMaterial = terrainOverrideMaterial,
            overrideMaterialPassIndex = terrainOverrideMaterial.FindPass("ForwardOnly"),
            excludeObjectMotionVectors = false,
            layerMask = LayerMask.GetMask("Terrain"),
            stateBlock = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(true, CompareFunction.LessEqual) }
        };

        HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));

    }

    private Matrix4x4 SetScissorRect(Matrix4x4 cam, Vector4 rect)
    {

        if (rect.x < 0)
        {
            //r.width += r.x;
            rect.x = 0;
        }

        if (rect.y < 0)
        {
            //r.height += r.y;
            rect.y = 0;
        }

        var width = Mathf.Min(1 - rect.x, rect.z);
        var height = Mathf.Min(1 - rect.y, rect.w);
        var r = new Rect(rect.x, rect.y, width, height);
        Matrix4x4 m = cam;
        Matrix4x4 m1 = Matrix4x4.TRS(new Vector3(r.x, r.y, 0), Quaternion.identity, new Vector3(r.width, r.height, 1));
        Matrix4x4 m2 = Matrix4x4.TRS(new Vector3((1 / r.width - 1), (1 / r.height - 1), 0), Quaternion.identity, new Vector3(1 / r.width, 1 / r.height, 1));
        Matrix4x4 m3 = Matrix4x4.TRS(new Vector3(-r.x * 2 / r.width, -r.y * 2 / r.height, 0), Quaternion.identity, Vector3.one);
        return m3 * m2 * m;
    }
    private void SetCameraMatrices(CommandBuffer cmd, Vector4 rect)
    {
        var p = SetScissorRect(GL.GetGPUProjectionMatrix(bakingCamera.projectionMatrix, true), rect);
        //var p = GL.GetGPUProjectionMatrix(bakingCamera.projectionMatrix, true);
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
        float texAmplifier = Mathf.Clamp( VerticalFieldOfView/12.5f,0f,1f);
        int DistTexHeight = (int)(512 * texAmplifier);
        if (ResultDimensions == Vector2Int.zero)
        {
            Debug.Log("ResultDimensions is ZERO");
            ResultDimensions = Vector2Int.one;
        }
        if (_Resolution == 0)
        {
            Debug.Log("_Resolution iS ZERO");
            _Resolution = 1;
        }


        //Result Textures
        if (Result == null || ResultDimensions != Result.referenceSize)
        {
            Debug.Log("Result Inited");
            Result?.Release();
            Result = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
                name: "Result", enableRandomWrite: true
            );
            Debug.Log(Result.referenceSize);
            pseudo3D_Result?.Release();
            pseudo3D_Result = new RenderTexture(ResultDimensions.x, ResultDimensions.y, 16, vector4Format);
            pseudo3D_Result.enableRandomWrite = true;
            pseudo3D_Result.filterMode = FilterMode.Bilinear;
            pseudo3D_Result.name = "pseudo3D_Result";

            waterColumn_result?.Release();
            waterColumn_result = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
                name: "waterColumn_result", enableRandomWrite: true
            );
            sidescan_result?.Release();
            sidescan_result = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
                name: "sidescan_result", enableRandomWrite: true
            );
            snippets_result?.Release();
            snippets_result = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
                name: "snippets_result", enableRandomWrite: true
            );
        }

        if (BeamBuffer == null || BeamCount != BeamBuffer.count || BeamCount*Wedge_MemoryAmount != BeamHistoryBuffer.count)
        {
            BeamBuffer?.Dispose();
            BeamBuffer = new ComputeBuffer(BeamCount, sizeof(uint) * 2, ComputeBufferType.Default);
            BeamHistoryBuffer?.Dispose();
            BeamHistoryBuffer = new ComputeBuffer(BeamCount*Wedge_MemoryAmount, sizeof(uint) * 3, ComputeBufferType.Default);

        }

        if (currentResolution != _Resolution || currentBeamCount != BeamCount)
        {

            //Main Distribution Textures and buffers

            distanceBuffer?.Dispose();
            distanceBuffer = new ComputeBuffer(BeamCount * 512, sizeof(float), ComputeBufferType.Default);

            if (distanceTexture != null) distanceTexture.Release();
            distanceTexture = new RenderTexture(_Resolution * 4, DistTexHeight, 16, vector4Format);
            distanceTexture.filterMode = FilterMode.Point;
            distanceTexture.name = "DistanceTexture";

            distribution_Transit1?.Release();
            distribution_Transit1 = RTHandles.Alloc(
                BeamCount, 512, dimension: TextureDimension.Tex2D,
                colorFormat: uintFormat,
                name: "distribution_Transit1", enableRandomWrite: true
                );
            distribution_Transit2?.Release();
            distribution_Transit2 = RTHandles.Alloc(
                BeamCount, 512, dimension: TextureDimension.Tex2D,
                colorFormat: uintFormat,
                name: "distribution_Transit2", enableRandomWrite: true
                );
            distribution_Together?.Release();
            distribution_Together = RTHandles.Alloc(
                _Resolution, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "distribution_Together", enableRandomWrite: true, autoGenerateMips: false);

        }

        //Zoom Textures
        if ((Zoom_Result == null || ZoomWindowRect != Zoom_Result.referenceSize))
        {
            if (ZoomWindowRect.x < 0 || ZoomWindowRect.y < 0) ZoomWindowRect = Vector2Int.one;

            Zoom_Result?.Release();
            Zoom_Result = RTHandles.Alloc(
                ZoomWindowRect.x, ZoomWindowRect.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
                name: "zoom_Result", enableRandomWrite: true
            );
        }

        zoomRect = CalculateZoomCameraRect();
        int z_res = (int)(Mathf.Clamp(_Resolution * ZoomParams.y, 360, 1440));
        Vector2Int _z_Dim = new Vector2Int(Mathf.FloorToInt(z_res * (zoomRect.z)), z_res);
        if (zoomDim != _z_Dim && AcousticZoom )
        {
            //Check Values
            ZoomParams.x = ZoomParams.x == 0 ? 1 : ZoomParams.x;
            ZoomParams.y = ZoomParams.y == 0 ? 1 : ZoomParams.y;

            zoomDim = _z_Dim;


            zoom_DistanceTexture?.Release();
            zoom_DistanceTexture = new RenderTexture(zoomDim.x * 4, zoomDim.y, 16, vector4Format);
            zoom_DistanceTexture.filterMode = FilterMode.Point;
            zoom_DistanceTexture.name = "Zoom_DistanceTexture";

            zoom_distanceBuffer?.Release();
            zoom_distanceBuffer = new ComputeBuffer(Mathf.RoundToInt(zoomDim.x * zoomDim.y), sizeof(float), ComputeBufferType.Default);

            zoom_distribution_Transit1?.Release();
            zoom_distribution_Transit1 = RTHandles.Alloc(
                zoomDim.x, zoomDim.y, dimension: TextureDimension.Tex2D,
                colorFormat: vectorFormat,
                name: "Zoom_distribution_Transit1", enableRandomWrite: true);
            zoom_distribution_Transit2?.Release();
            zoom_distribution_Transit2 = RTHandles.Alloc(
                zoomDim.x, zoomDim.y, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "Zoom_distribution_Transit2", enableRandomWrite: true);
            zoom_distribution_Together?.Release();
            zoom_distribution_Together = RTHandles.Alloc(
                zoomDim.x, zoomDim.y, dimension: TextureDimension.Tex2D,
                colorFormat: vectorFormat,
                name: "Zoom_distribution_Together", enableRandomWrite: true);
        }



        //SideScan





        if ((pseudo3D_buffer == null || BeamCount * Wedge_MemoryAmount != pseudo3D_buffer.count))
        {
            if (pseudo3D_buffer != null) pseudo3D_buffer.Dispose();
            pseudo3D_buffer = new ComputeBuffer(BeamCount * Wedge_MemoryAmount, sizeof(float) * 3, ComputeBufferType.Default);
        }


        //SideScan textures
        if (currentResolution != _Resolution)
        {
            strip_distanceBuffer?.Dispose();
            strip_distanceBuffer = new ComputeBuffer(_Resolution, sizeof(float), ComputeBufferType.Default);

            /*if (noise_strip != null) noise_strip.Release();
            noise_strip = RTHandles.Alloc(
                _Resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "noise_strip", enableRandomWrite: true);*/
            if (waterColumn_scan_strip != null) waterColumn_scan_strip.Release();
            waterColumn_scan_strip = RTHandles.Alloc(
                1, 512, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "waterColumn_scan_strip", enableRandomWrite: true);
            if (scan_strip != null) scan_strip.Release();
            scan_strip = RTHandles.Alloc(
                _Resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "sidescan_strip", enableRandomWrite: true);
            if (ruler != null) ruler.Release();
            ruler = RTHandles.Alloc(
                960, 192, dimension: TextureDimension.Tex2D,
                colorFormat: vector4Format,
                name: "Grid and Frequency", enableRandomWrite: true);

            if (waterColumn_scroll != null) waterColumn_scroll.Release();
            waterColumn_scroll = RTHandles.Alloc(
                960,_Resolution, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "waterColumn_scroll", enableRandomWrite: true);

            if (waterColumn_scroll_temp != null) waterColumn_scroll_temp.Release();
            waterColumn_scroll_temp = RTHandles.Alloc(
                960,_Resolution, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "waterColumn_scroll_temp", enableRandomWrite: true);



            if (sidescan_scroll != null) sidescan_scroll.Release();
            sidescan_scroll = RTHandles.Alloc(
                _Resolution, 960, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "sidescan_scroll", enableRandomWrite: true);

            if (sidescan_scroll_temp != null) sidescan_scroll_temp.Release();
            sidescan_scroll_temp = RTHandles.Alloc(
                _Resolution, 960, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "sidescan_scroll_temp", enableRandomWrite: true);



            if (snippets_scroll != null) snippets_scroll.Release();
            snippets_scroll = RTHandles.Alloc(
                _Resolution, 960, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "snippets_scroll", enableRandomWrite: true);

            if (snippets_scroll_temp != null) snippets_scroll_temp.Release();
            snippets_scroll_temp = RTHandles.Alloc(
                _Resolution, 960, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                name: "snippets_scroll_temp", enableRandomWrite: true);
            /* if (snippets_ruler != null) snippets_ruler.Release();
             snippets_ruler = RTHandles.Alloc(
                 960, 192, dimension: TextureDimension.Tex2D,
                 colorFormat: vector4Format,
                 name: "Grid and Frequency", enableRandomWrite: true
                 );*/
        }

        currentResolution = _Resolution;
        currentBeamCount = BeamCount;
    }

    private void CreateTextS_ScanMarks(int size)
    {
        S_ScanDistanceMarks = new List<Text> { };
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("SS_Mark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            S_ScanDistanceMarks.Add(tmp);
            go.transform.SetParent(text_marks_sideScan_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
    }

    private void ClearS_ScanTextMarks()
    {
        if (text_marks_sideScan_placeholder == null)
        {
            text_marks_sideScan_placeholder = new GameObject("SideScan_TextMarksPlaceholder").transform;
            text_marks_sideScan_placeholder.transform.SetParent(ResultSonarImage.rectTransform, false);
        }
        else
        {

            for (int i = text_marks_sideScan_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(text_marks_sideScan_placeholder.GetChild(i).gameObject);
            S_ScanDistanceMarks?.Clear();
        }

    }


    private void DrawDownLook_ViewDigits()
    {
        var resRect = new Vector2(ResultSonarImage.rectTransform.rect.width, ResultSonarImage.rectTransform.rect.height);
        var rScale = RadiusPercent / 100f * 2;

        float norm = Mathf.Lerp(resRect.y / resRect.x, 1f, 0f);
        int vFontSize = (int)(FontSize);
        float minAbs = Mathf.Min(resRect.x, resRect.y);
        float min = minAbs / ((MaxDisplayRange - MinDisplayRange) / MaxDisplayRange);
        Vector2 center = new Vector2(resRect.x, resRect.y) * (Vector2)CentrePercent / 100f;

        if (S_ScanDistanceMarks.Count != (4) || S_ScanDistanceMarks == null)
        {
            ClearS_ScanTextMarks();
            CreateTextS_ScanMarks(4);
        }

        var scanRange = MaxDisplayRange - MinDisplayRange;

        //Проверка на активацию в зависимости от минимального расстояния

        void sm(Text mark, string text, Vector2 coord, Vector2 offsetDir, Vector2 offset_sign)
        {
            var bounds = new Vector2(mark.preferredWidth + 4f, mark.preferredHeight + 4f) / 2f;
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = vFontSize;
            var dCoord = coord + offsetDir * bounds.x * offset_sign.x + Vector2.Perpendicular(offsetDir) * bounds.y * offset_sign.y;

            mark.rectTransform.localPosition = dCoord;
            mark.color = GridColor;
            mark.gameObject.SetActive(true);
        }
        void Draw(List<Text> gridMarks, Vector2 resWH, Vector2 Cent, float z)
        {

            if ((ShowGrid==true &&ScanMode == ScanMode.SideScan && PolarView == false))
            {
                Vector2 dir, offset_val, dCoord, offset_sign;
                float dist;
                string text;

                
                for (int i = 0; i < S_ScanDistanceMarks.Count; i++)
                {
                    float C = S_ScanDistanceMarks.Count;

                    dir = new Vector2(Mathf.Cos((-ImageRotation + 90f) * Mathf.Deg2Rad), Mathf.Sin((-ImageRotation + 90f ) * Mathf.Deg2Rad));
                    dir.Normalize();

                    offset_val = new Vector2(0f, -60f);// -markWidth);
                    offset_sign = new Vector2(0f, 0f);
                    dist = z*minAbs/2f*(1f * (1f - ((i + 1f) / (C + 1f))));
                    text = (Mathf.Round(2f * (MinDisplayRange + scanRange * i / Rings)) / 2f).ToString();

                    string dim = "m";
                    float rangeVal = Mathf.Abs((MaxScanRange) * (1f-(i + 1f) / (C + 1f)));
                    text = Mathf.Round(rangeVal).ToString() + dim;
                    dCoord = new Vector2(Cent.x, Cent.y) + dir * dist - Vector2.Perpendicular(dir) * offset_val.y;
                    dir = Vector2.Perpendicular(dir);
                    sm(gridMarks[i], text, dCoord, dir, offset_sign);

                    
                }
            }
            else
            {
                for (int k = 0; k < gridMarks.Count; k++)
                {
                    gridMarks[k].gameObject.SetActive(false);
                }
            }
        }
        Draw(S_ScanDistanceMarks, resRect, center, rScale);
        
    }





    private void ClearTextMarks()
    {
        if (text_marks_placeholder == null)
        {
            text_marks_placeholder = new GameObject("TextMarksPlaceholder").transform;
            text_marks_placeholder.transform.SetParent(ResultSonarImage.rectTransform, false);
        }
        else
        {

            for (int i = text_marks_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(text_marks_placeholder.GetChild(i).gameObject);
            RingsDistanceMarks?.Clear();
            GridDistanceMarks?.Clear();
        }
        if (zoom_text_marks_placeholder == null)
        {
            zoom_text_marks_placeholder = new GameObject("ZoomTextMarksPlaceholder").transform;
            zoom_text_marks_placeholder.transform.SetParent(ZoomResultImage.rectTransform, false);
        }
        else
        {
            for (int i = zoom_text_marks_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(zoom_text_marks_placeholder.GetChild(i).gameObject);
            Zoom_RingsDistanceMarks.Clear();
            Zoom_GridDistanceMarks.Clear();
        }

    }

    private void CreateTextRingsMarks(int size)
    {
        RingsDistanceMarks = new List<Text> { };
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("RingMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            RingsDistanceMarks.Add(tmp);
            go.transform.SetParent(text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
        Zoom_RingsDistanceMarks = new List<Text> { };
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("ZoomRingMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            Zoom_RingsDistanceMarks.Add(tmp);
            go.transform.SetParent(zoom_text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
    }

    private void CreateTextGridMarks(int size)
    {
        GridDistanceMarks = new List<Text> { };
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("GridMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            GridDistanceMarks.Add(tmp);
            go.transform.SetParent(text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }

        Zoom_GridDistanceMarks = new List<Text> { };
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("ZoomGridMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            Zoom_GridDistanceMarks.Add(tmp);
            go.transform.SetParent(zoom_text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
    }

    private void DrawCartesianViewDigits()
    {
        if (Rings == 0) return;
        var resRect = new Vector2(ResultSonarImage.rectTransform.rect.width, ResultSonarImage.rectTransform.rect.height);
        var rScale = RadiusPercent / 100f * 2;

        float norm = Mathf.Lerp(resRect.y / resRect.x, 1f, 0f);
        int vFontSize = (int)(FontSize);
        float minAbs = Mathf.Min(resRect.x, resRect.y);
        Vector2 center = new Vector2(resRect.x, resRect.y) * (Vector2)CentrePercent / 100f;

        if (RingsDistanceMarks.Count != ((Rings) * 2 + Lines + 2 + 1) || RingsDistanceMarks == null || Zoom_RingsDistanceMarks == null)
        {
            ClearTextMarks();
            CreateTextRingsMarks(Rings * 2 + Lines + 2 + 1);
        }
        var markWidth = RingsDistanceMarks[0].rectTransform.rect.width / 5f;
        var scanRange = MaxDisplayRange - MinDisplayRange;

        void sm(Text mark, string text, Vector2 coord, Vector2 dir, Vector2 offset_sign)
        {
            //var bounds = mark.textBounds.extents;
            var bounds = new Vector2(mark.preferredWidth, mark.preferredHeight);
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = vFontSize;

            var dCoord = coord + dir * offset_sign.x * bounds.x + Vector2.Perpendicular(dir) * (offset_sign.y * bounds.y);

            mark.rectTransform.localPosition = dCoord;
            mark.color = GridColor;
            mark.gameObject.SetActive(true);
        }

        void Draw(List<Text> ringsMarks, Vector2 resWH, Vector2 Cent, float z)
        {
            if ((ringsMarks == Zoom_RingsDistanceMarks && AcousticZoom == false))
            {

                for (int k = 0; k < ringsMarks.Count; k++)
                {
                    ringsMarks[k].gameObject.SetActive(false);
                }
                return;
            }
            if ((ShowGrid == true && Rings != 0 && ShowDigits && PolarView == false&& ScanMode == ScanMode.ForwardLooking))
            {
                Vector2 dir, offset_val, dCoord, offset_sign;
                float dist;
                string text;

                //setZeroMark(ringsMarks, 0, Cent, Vector2.down, markWidth/2, 0);
                dir = Vector2.down;
                offset_val = new Vector2(0f, 0f);
                offset_sign = new Vector2(1f, 0f);
                text = MinDisplayRange.ToString();

                dCoord = Cent + dir * offset_val.x + Vector2.Perpendicular(dir) * offset_val.y;
                dCoord.x = Mathf.Clamp(dCoord.x, -resRect.x / 2 - offset_val.x, resRect.x / 2 + offset_val.x);
                dCoord.y = Mathf.Clamp(dCoord.y, -resRect.y / 2 - offset_val.x, resRect.y / 2 + offset_val.x);

                sm(ringsMarks[0], text, dCoord, dir, offset_sign);
                //sm(ringsMarks[ringsMarks.Count - 2], text, dCoord, dir);
                for (int i = 0; i < Rings; i++)
                {
                    //Right
                    int k = i + 1;
                    dir = new Vector2(Mathf.Cos((-ImageRotation + 90f - HorizontalFieldOfView / 2f) * Mathf.Deg2Rad), Mathf.Sin((-ImageRotation + 90f - HorizontalFieldOfView / 2f) * Mathf.Deg2Rad));
                    dir.Normalize();

                    offset_val = new Vector2(0f, 0f);// -markWidth); 
                    offset_sign = new Vector2(0f, -0.6f);
                    dist = z * minAbs * k / Rings / 2f + offset_val.x;
                    text = (Mathf.Round(2f * (MinDisplayRange + scanRange * k / Rings)) / 2f).ToString();
                    dCoord = Cent + dir * (dist) + Vector2.Perpendicular(dir) * offset_val.y;
                    //if (dist < 0 || dist > z * minAbs / 2 || dist < z * minAbs / 2f * (MinScanRange - 0.1f) / MaxDisplayRange)
                    //    ringsMarks[k].gameObject.SetActive(false);
                    //else
                    sm(ringsMarks[k], text, dCoord, dir, offset_sign);

                    //Left
                    dir = new Vector2(Mathf.Cos((-ImageRotation + 90f + HorizontalFieldOfView / 2f) * Mathf.Deg2Rad), Mathf.Sin((-ImageRotation + 90f + HorizontalFieldOfView / 2f) * Mathf.Deg2Rad));
                    dir.Normalize();

                    offset_val = new Vector2(0f, 0f);// markWidth);

                    offset_sign = new Vector2(0f, 0.6f);
                    dist = z * minAbs * k / Rings / 2f + offset_val.x;
                    text = (Mathf.Round(2f * (MinDisplayRange + scanRange * k / Rings)) / 2f).ToString();
                    dCoord = Cent + dir * (dist) + Vector2.Perpendicular(dir) * offset_val.y;

                    //if (dist < 0 || dist > z * minAbs / 2 || dist < z * minAbs / 2f * (MinScanRange - 0.1f) / MaxDisplayRange)
                    //    ringsMarks[k+Rings].gameObject.SetActive(false);
                    //else
                    sm(ringsMarks[k + Rings], text, dCoord, dir, offset_sign);

                    //setMark(ringsMarks, k + Rings, Cent, dir, 0f, markWidth, z);
                }

                float S1_angle = ImageRotation + (90 - HorizontalFieldOfView / 2);
                float S2_angle = ImageRotation + (90 + HorizontalFieldOfView / 2);
                for (int k = 0; k < Lines + 2; k++)
                {
                    float a = S1_angle + HorizontalFieldOfView * (k / (Lines + 1f));
                    dir = new Vector2(-Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    dir.Normalize();


                    offset_val = new Vector2(markWidth, 0f);
                    dist = z * minAbs / 2f + offset_val.x;
                    offset_sign = new Vector2(1f, 1f);

                    float b = (MirrorImage == true)? -1:1;
                    text = (b*(Mathf.Round(2f * (a - S1_angle)) / 2f - HorizontalFieldOfView / 2f)).ToString() + "°";
                    dCoord = Cent + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;
                    dir = Vector2.zero;
                    //dir.= -dir.x;
                    sm(ringsMarks[k + 2 * Rings + 1], text, dCoord, dir, offset_sign);
                }
            }
            else
            {
                for (int k = 0; k < ringsMarks.Count; k++)
                {
                    ringsMarks[k].gameObject.SetActive(false);
                }
            }
        }
        Vector2 relUV;
        Draw(RingsDistanceMarks, resRect, center, rScale);

        var zoomRect = new Vector2(ZoomResultImage.rectTransform.rect.width, ZoomResultImage.rectTransform.rect.height);

        var Boxcenter = (MouseCoords - Vector2.one / 2f) * resRect;
        float zoom = ZoomParams.x / 100f;
        relUV = (center - Boxcenter) / zoom;
        Draw(Zoom_RingsDistanceMarks, zoomRect, relUV, rScale * 1f / zoom);
    }

    private void DrawPolarViewDigits()
    {
        if (Rings == 0) return;
        var resRect = new Vector2(ResultSonarImage.rectTransform.rect.width, ResultSonarImage.rectTransform.rect.height);
        var rScale = RadiusPercent / 100f * 2;

        float norm = Mathf.Lerp(resRect.y / resRect.x, 1f, 0f);
        int vFontSize = (int)(FontSize);
        float minAbs = Mathf.Min(resRect.x, resRect.y);
        float min = minAbs / ((MaxDisplayRange - MinDisplayRange) / MaxDisplayRange);
        Vector2 center = new Vector2(resRect.x, resRect.y) * new Vector2(0f, -50f) / 100f;

        if (GridDistanceMarks.Count != (Rings * 4 + 2) || GridDistanceMarks == null)
        {
            ClearTextMarks();
            CreateTextGridMarks(Rings * 4 + 2);
        }

        var markWidth = 0f;
        var scanRange = MaxDisplayRange - MinDisplayRange;

        //Проверка на активацию в зависимости от минимального расстояния

        void sm(Text mark, string text, Vector2 coord, Vector2 offsetDir, Vector2 offset_sign)
        {
            var bounds = new Vector2(mark.preferredWidth + 4f, mark.preferredHeight + 4f) / 2f;
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = vFontSize;
            Debug.Log(bounds);
            var dCoord = coord + offsetDir * bounds.x * offset_sign.x + Vector2.Perpendicular(offsetDir) * bounds.y * offset_sign.y;

            mark.rectTransform.localPosition = dCoord;
            mark.color = GridColor;
            mark.gameObject.SetActive(true);
        }
        void Draw(List<Text> gridMarks, Vector2 resWH, Vector2 Cent, float z)
        {
            if ((gridMarks == Zoom_GridDistanceMarks && AcousticZoom == false))
            {

                for (int k = 0; k < gridMarks.Count; k++)
                {
                    gridMarks[k].gameObject.SetActive(false);
                }
                return;
            }
            if (ShowGrid && Rings != 0 && ShowDigits && PolarView == true)
            {
                Vector2 dir, offset_val, dCoord, offset_sign;
                float dist;
                string text;

                dir = Vector2.left;
                offset_val = new Vector2(-4f, 2f);
                offset_sign = new Vector2(1f, 1f);
                text = MinDisplayRange.ToString();

                dCoord = new Vector2(Cent.x, resWH.y / 2f) + dir * offset_val.x + Vector2.Perpendicular(dir) * offset_val.y;
                dCoord.x = Mathf.Clamp(dCoord.x, -resRect.x / 2 - offset_val.x, resRect.x / 2 + offset_val.x);
                dCoord.y = Mathf.Clamp(dCoord.y, -resRect.y / 2 - offset_val.x, resRect.y / 2 + offset_val.x);

                dir = Vector2.Perpendicular(dir);
                sm(gridMarks[gridMarks.Count - 1], text, dCoord, dir, offset_sign);


                offset_sign = new Vector2(1f, -1f);
                dir = Vector2.up;
                dCoord = new Vector2(-resWH.x / 2f, Cent.y) + dir * offset_val.x + Vector2.Perpendicular(dir) * offset_val.y;
                dCoord.x = Mathf.Clamp(dCoord.x, -resRect.x / 2 - offset_val.x, resRect.x / 2 + offset_val.x);
                dCoord.y = Mathf.Clamp(dCoord.y, -resRect.y / 2 - offset_val.x, resRect.y / 2 + offset_val.x);

                sm(gridMarks[gridMarks.Count - 2], text, dCoord, dir, offset_sign);
                //setMarkWithText(gridMarks, "0", 4 * Rings , new Vector2(Cent.x, resWH.y / 2f), Vector2.left, -(z/norm*minAbs/Rings/2f)+w/2, w, z/norm);
                //setZeroMark(gridMarks, 1, new Vector2(-resWH.x / 2f, Cent.y), Vector2.up, w / 2, w);

                for (int i = 0; i < Rings; i++)
                {

                    int k = i + 1;
                    dir = Vector2.up;
                    offset_val = new Vector2(0f, 0f);// -markWidth);
                    offset_sign = new Vector2(-1f, -1f);
                    dist = 2f * z * minAbs * k / Rings / 2f + offset_val.x;
                    text = (Mathf.Round(2f * (MinDisplayRange + scanRange * k / Rings)) / 2f).ToString();

                    dCoord = new Vector2(-resWH.x / 2f, Cent.y) + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;
                    dir = Vector2.Perpendicular(dir);
                    sm(gridMarks[k], text, dCoord, dir, offset_sign);
                    //Vertical+
                    //setMark(gridMarks, k , new Vector2(-resWH.x / 2f, Cent.y), Vector2.up, -markWidth, -3f*markWidth/2f , 2f*z);
                    //Vertical-
                    //setMark(gridMarks, k + 3 * Rings, new Vector2(-resWH.x / 2f, Cent.y), Vector2.down, -w / 2, w / 2, 2f * z);
                    //(Mathf.Round((a) * 2f) / 2f).ToString()
                    float a = HorizontalFieldOfView / 2f * ((float)(k) / (float)(Rings));
                    a = Mathf.Round(a * 2f) / 2f;

                    //Horisontal+

                    dir = Vector2.right;
                    offset_val = new Vector2(0f, 0f);// -markWidth);
                    offset_sign = new Vector2(-1f, -1f);
                    dist = z / norm * minAbs * k / Rings / 2f + offset_val.x;
                    text = a.ToString() + "°";

                    dCoord = new Vector2(Cent.x, resWH.y / 2f) + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;

                    sm(gridMarks[k + Rings], text, dCoord, dir, offset_sign);

                    //setMarkWithText(gridMarks, a.ToString(), k+Rings, new Vector2(Cent.x, resWH.y / 2f), Vector2.right, -3* markWidth/2 , -markWidth, );
                    //Horisontal-

                    dir = Vector2.left;
                    dCoord = new Vector2(Cent.x, resWH.y / 2f) + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;
                    offset_sign = new Vector2(-1f, 1f);
                    text = (-a).ToString() + "°";
                    sm(gridMarks[k + 2 * Rings], text, dCoord, dir, offset_sign);

                    //setMarkWithText(gridMarks, (-a).ToString(), k + 2*Rings, new Vector2(Cent.x, resWH.y / 2f), Vector2.left, -3*markWidth/2 , markWidth , z / norm);
                }
            }
            else
            {
                for (int k = 0; k < gridMarks.Count; k++)
                {
                    gridMarks[k].gameObject.SetActive(false);
                }
            }
        }
        Vector2 relUV;
        Draw(GridDistanceMarks, resRect, center, 1f);

        var zoomRect = new Vector2(ZoomResultImage.rectTransform.rect.width, ZoomResultImage.rectTransform.rect.height - markWidth);

        var Boxcenter = (MouseCoords - Vector2.one / 2f) * resRect;
        float zoom = ZoomParams.x / 100f;
        relUV = (center - Boxcenter) / zoom;
        Draw(Zoom_GridDistanceMarks, zoomRect, relUV, 1f / zoom);
    }

    private void DrawMarksOnUI()
    {
        if (MarksList.Count == 0)
        {
            ClearMarksOnUI();
            return;
        }
        if (MarksGoList.Count != MarksList.Count || MarksGoList.Contains(null))
        {
            ClearMarksOnUI();
            CreateMarksOnUI(MarksList.Count);
        }
        if (ShowMark == false)
        {
            foreach (var m in MarksGoList)
            {
                m.gameObject.SetActive(false);
            }

            foreach (var m in Zoom_MarksGoList)
            {
                m.gameObject.SetActive(false);
            }

            return;
        }
        void Draw(List<Image> mGoList, Vector2 center, float zoom)
        {

            int iter = 0;
            foreach (var m in MarksList)
            {
                var rect = m.rect;
                rect.x -= 0.5f;
                rect.y -= 0.5f;
                if (MarkSprites.ContainsKey(m.type) == false)
                {
                    Debug.Log("No Such Type. Define " + m.type + " type in Type Creator.");
                    continue;
                }

                mGoList[iter].color = m.color;

                mGoList[iter].sprite = MarkSprites[m.type];
                mGoList[iter].rectTransform.localPosition = center + (new Vector2(rect.x, rect.y) * ResultDimensions) / zoom;
                mGoList[iter].rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
                mGoList[iter].gameObject.SetActive(true);
                iter++;
            }

        }

        Vector2 c = ResultDimensions * (Vector2)CentrePercent / 100f;

        Draw(MarksGoList, new Vector2(0f, 0f), 1f);
        if (AcousticZoom == false)
        {

            foreach (var m in Zoom_MarksGoList)
            {
                m.gameObject.SetActive(false);
            }
        }
        else
        {
            var zoomRect = new Vector2(ZoomResultImage.rectTransform.rect.width, ZoomResultImage.rectTransform.rect.height);
            float zoom = ZoomParams.x / 100f;
            var Boxcenter = (MouseCoords - Vector2.one / 2f) * ResultDimensions;
            var relUV = (c - Boxcenter) / zoom;
            Draw(Zoom_MarksGoList, relUV, zoom);
        }
    }
    private void ClearMarksOnUI()
    {
        if (marks_placeholder == null)
        {
            marks_placeholder = new GameObject("MarksPlaceholder").transform;
            marks_placeholder.transform.SetParent(ResultSonarImage.rectTransform, false);
        }
        else
        {
            for (int i = marks_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(marks_placeholder.GetChild(i).gameObject);
            MarksGoList.Clear();
        }

        if (zoom_marks_placeholder == null)
        {
            zoom_marks_placeholder = new GameObject("ZoomMarksPlaceholder").transform;
            zoom_marks_placeholder.transform.SetParent(ZoomResultImage.rectTransform, false);
        }
        else
        {
            for (int i = zoom_marks_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(zoom_marks_placeholder.GetChild(i).gameObject);
            Zoom_MarksGoList.Clear();
        }
    }


    private void CreateMarksOnUI(int count)
    {

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("mark_image" + i);
            var img = go.AddComponent<Image>() as Image;
            MarksGoList.Add(img);
            go.transform.SetParent(marks_placeholder);
            go.transform.localScale = Vector3.one;
            go.SetActive(false);

        }

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("zoom_mark_image" + i);
            var img = go.AddComponent<Image>() as Image;
            Zoom_MarksGoList.Add(img);
            go.transform.SetParent(zoom_marks_placeholder);
            go.transform.localScale = Vector3.one;
            go.SetActive(false);
        }
    }


    //SideScan Ruler
    private void DrawSideScanRuler()
    {
        var resRect = new Vector2(ResultDimensions.x, ResultDimensions.y);

        if (SideScanRulerSettings.RulerScale == 0) SideScanRulerSettings.RulerScale = 1;
        if (SideScanRulerSettings.RulerScale != 0 && SideScan_RulerText.Count != (SideScanRulerSettings.RulerScale) || SideScan_RulerText == null)
        {
            CreateRulerTextList(SideScanRulerSettings.RulerScale);
        }
        Draw(resRect, resRect / 2f, 1f);

        void Draw(Vector2 resWH, Vector2 Cent, float z)
        {
            Vector2 dir, offset_val, dCoord, offset_sign;
            string text;

            dir = Vector2.right;
            offset_val = new Vector2(0f, 10f);
            offset_sign = new Vector2(0f, 0f);

            int C = SideScan_RulerText.Count;
            for (int i = 0; i < C; i++)
            {
                offset_sign = new Vector2(0f, 0f);
                float xCoord = 1.0f;
                dCoord = new Vector2(0.95f * resWH.x * (xCoord * (i) / (C - 1) - 0.5f), -resWH.y / 2) + Vector2.Perpendicular(dir) * offset_val.y;

                string dim = "m";
                float rangeVal = Mathf.Abs((MaxScanRange) * ((1 - ((float)i / (C - 1)) * 2)));
                text = Mathf.Round(rangeVal).ToString() + dim;
                sm(SideScan_RulerText[i], text, dCoord, dir, offset_sign);
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
            mark.color = SideScanRulerSettings.DigitsColor;
            mark.gameObject.SetActive(true);
        }
    }

    private void CreateRulerTextList(int size)
    {
        ClearRulerTextList();

        SideScan_RulerText = new List<Text>();
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("RulerMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            SideScan_RulerText.Add(tmp);
            go.transform.SetParent(sidescan_ruler_text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
    }

    private void ClearRulerTextList()
    {

        if (sidescan_ruler_text_marks_placeholder == null)
        {
            sidescan_ruler_text_marks_placeholder = new GameObject("SideScanMarksPlaceholder").transform;
            sidescan_ruler_text_marks_placeholder.SetParent(SideScanResult.transform, false);
            SideScan_RulerText?.Clear();
        }
        else
        {
            for (int i = sidescan_ruler_text_marks_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(sidescan_ruler_text_marks_placeholder.GetChild(i).gameObject);
        }
    }
    //WaterColumn Ruler
    private void DrawSnippetsRuler()
    {
        var resRect = new Vector2(ResultDimensions.x, ResultDimensions.y);

        if (SnippetsRulerSettings.RulerScale == 0) SnippetsRulerSettings.RulerScale = 1;
        if (SnippetsRulerSettings.RulerScale != 0 && Snippets_RulerText.Count != (SnippetsRulerSettings.RulerScale) || Snippets_RulerText == null)
        {
            CreateSnippetsRulerTextList(SnippetsRulerSettings.RulerScale);
        }
        Draw(resRect, resRect / 2f, 1f);

        void Draw(Vector2 resWH, Vector2 Cent, float z)
        {
            Vector2 dir, offset_val, dCoord, offset_sign;
            string text;

            dir = Vector2.right;
            offset_val = new Vector2(0f, 10f);
            offset_sign = new Vector2(0f, 0f);

            int C = Snippets_RulerText.Count;
            for (int i = 0; i < C; i++)
            {
                offset_sign = new Vector2(0f, 0f);
                float xCoord = 1.0f;
                dCoord = new Vector2(0.95f * resWH.x * (xCoord * (i) / (C - 1) - 0.5f), -resWH.y / 2) + Vector2.Perpendicular(dir) * offset_val.y;

                string dim = "m";
                float rangeVal = Mathf.Abs((MaxScanRange) * ((1 - ((float)i / (C - 1)) * 2)));
                text = Mathf.Round(rangeVal).ToString() + dim;
                sm(Snippets_RulerText[i], text, dCoord, dir, offset_sign);
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
            mark.color = SideScanRulerSettings.DigitsColor;
            mark.gameObject.SetActive(true);
        }
    }

    private void CreateSnippetsRulerTextList(int size)
    {
        ClearSnippetsRulerTextList();

        Snippets_RulerText = new List<Text>();
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("RulerMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            Snippets_RulerText.Add(tmp);
            go.transform.SetParent(snippets_ruler_text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
    }

    private void ClearSnippetsRulerTextList()
    {

        if (snippets_ruler_text_marks_placeholder == null)
        {
            snippets_ruler_text_marks_placeholder = new GameObject("SnippetsMarksPlaceholder").transform;
            snippets_ruler_text_marks_placeholder.SetParent(SnippetsResult.transform, false);
            Snippets_RulerText?.Clear();
        }
        else
        {
            for (int i = snippets_ruler_text_marks_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(snippets_ruler_text_marks_placeholder.GetChild(i).gameObject);
        }
    }


    //WaterColumnMarks
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

            float ScrollTime = (float)display.Scroll * (1f/FrameRate) * (960f / (float)ScrollAmount) / 1000f;
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

                if (display.Event.TextList[i].uvOffset > 1)
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
                display.Event.TextList[i].uvOffset += ScrollAmount / (960f);

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
            if (display.Event.TextList[0].uvOffset <= 1f) display.Event.TextList.Insert(0, display.Event.TextPool.Pop());
            display.eventTimer -= display.Event.EventInterval;
            display.Event.TextList[0].text1.gameObject.SetActive(true);
            display.Event.TextList[0].uvOffset = 0f;
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

        ClearTextPool(WaterColumnDisplay);

        CreatePoolOf(WaterColumnDisplay);

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
        string name = "WaterColumnMarksPlaceholder";
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

    //WaterColumn Horisontal Grid Marks

    private void DrawWaterColumnRuler()
    {
        var resRect = new Vector2(ResultDimensions.x, ResultDimensions.y);

        if (WaterColumnDisplay.GridCount <= 0) WaterColumnDisplay.GridCount = 1;
        if (WaterColumnDisplay.GridCount > 0 && WaterColumn_RulerText.Count != (WaterColumnDisplay.GridCount) || WaterColumn_RulerText == null)
        {
            CreateWaterColumnRulerTextList(WaterColumnDisplay.GridCount);
        }
        Draw(resRect, resRect / 2f, 1f);

        void Draw(Vector2 resWH, Vector2 Cent, float z)
        {
            Vector2 dir, offset_val, dCoord, offset_sign;
            string text;

            dir = Vector2.left;
            offset_val = new Vector2(5f, 0f);
            offset_sign = new Vector2(1f, -1f);

            int C = WaterColumn_RulerText.Count;
            for (int i = 0; i < C; i++)
            {
                float xCoord = 1.0f;
                dCoord = new Vector2(resWH.x/2, resWH.y*(xCoord * (1f-((i+1f)/(C+1f))) - 0.5f)) - Vector2.Perpendicular(dir) * offset_val.y;

                string dim = "m";
                float rangeVal = Mathf.Abs((MaxScanRange) * (i+1f)/(C+1f));
                text = Mathf.Round(rangeVal).ToString() + dim;
                sm(WaterColumn_RulerText[i], text, dCoord, dir, offset_sign);
            }
        }

        void sm(Text mark, string text, Vector2 coord, Vector2 offsetDir, Vector2 offset_sign)
        {
            var bounds = new Vector2(mark.preferredWidth +0f, mark.preferredHeight) / 2f;
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = RulerFontSize;

            var dCoord = coord + offsetDir * bounds.x * offset_sign.x + Vector2.Perpendicular(offsetDir) * bounds.y * offset_sign.y;

            mark.rectTransform.localPosition = dCoord;
            mark.color = GridColor;
            mark.gameObject.SetActive(true);
        }
    }

    private void CreateWaterColumnRulerTextList(int size)
    {
        ClearWaterColumnRulerTextList();

        WaterColumn_RulerText = new List<Text>();
        for (int i = 0; i < size; i++)
        {
            var go = new GameObject("RulerMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            WaterColumn_RulerText.Add(tmp);
            go.transform.SetParent(watercolumn_ruler_text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
    }

    private void ClearWaterColumnRulerTextList()
    {

        if (watercolumn_ruler_text_marks_placeholder == null)
        {
            watercolumn_ruler_text_marks_placeholder = new GameObject("WaterColumnRulerMarksPlaceholder").transform;
            watercolumn_ruler_text_marks_placeholder.SetParent(WaterColumnResult.transform, false);
            WaterColumn_RulerText?.Clear();
        }
        else
        {
            for (int i = watercolumn_ruler_text_marks_placeholder.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(watercolumn_ruler_text_marks_placeholder.GetChild(i).gameObject);
        }
    }


    protected override void Cleanup()
    {
        // Cleanup code

        CoreUtils.Destroy(pseudo3dMaterial);
        CoreUtils.Destroy(dotMaterial);
        if (pseudo3D_Result != null) pseudo3D_Result.Release();
        if (pseudo3D_buffer != null) pseudo3D_buffer.Dispose();
        if (pseudo3D_Positions != null) pseudo3D_Positions.Release();

        if (distanceBuffer != null)
            distanceBuffer.Dispose();
        distanceTexture?.Release();
        distribution_Transit1?.Release();
        distribution_Transit2?.Release();
        Result?.Release();
        if (zoom_distanceBuffer != null)
            zoom_distanceBuffer.Dispose();
        if (zoom_DistanceTexture != null)
            zoom_DistanceTexture?.Release();

        if (sidescan_result != null) sidescan_result.Release();
        if (ruler != null) ruler.Release();
        if (sidescan_scroll != null) sidescan_scroll.Release();
        if (sidescan_scroll_temp != null) sidescan_scroll_temp.Release();

        if (snippets_result != null) snippets_result.Release();
        if (snippets_scroll != null) snippets_scroll.Release();
        if (snippets_scroll_temp != null) snippets_scroll_temp.Release();
        zoom_distribution_Transit1?.Release();
        zoom_distribution_Together?.Release();
        Zoom_Result?.Release();
        ClearTextMarks();
        ClearRulerTextList();
        ClearSnippetsRulerTextList();
        ClearTextPool(WaterColumnDisplay);
        ClearWaterColumnRulerTextList();
    }

    private void SerializeMarkSpriteDictionaryIfPossible()
    {
        MarkSprites = new Dictionary<MarkType, Sprite>();
        foreach (var m in Type_Creator.TypesList)
        {
            if (MarkSprites.ContainsKey(m.type))
            {
                MarkSprites[m.type] = m.sprite;
                continue;
            }
            MarkSprites.Add(m.type, m.sprite);

        }
    }
}

