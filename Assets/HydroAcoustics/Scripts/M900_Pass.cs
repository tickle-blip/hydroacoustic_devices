using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

using System.Collections.Generic;
using System;

using TMPro;
class M900_Pass : CustomPass
{
    #region Fields
    //[HideInInspector] public RawImage Test_img3;
    //[HideInInspector] public RawImage Test_img1;
    //public RawImage distance_img;
    public Vector2Int ResultDimensions = new Vector2Int(1920, 1080);
    public RawImage ResultSonarImage;
    public RawImage ZoomResultImage;
    public Camera Camera_UI;
    public Camera Zoom_Camera_UI;

    public TypeCreator Type_Creator;
    /////-------------"UI Fields"------------------/////
    [HideInInspector] public float FrameRate;
    [Range(0f, 1f)]
    public float Gain = 0.25f;
    [Range(0f, 10f)]
    public float TVG = 1f;

    [Range(360f, 1440f)]
    [SerializeField] private int _Resolution = 720; // Change via SetResolution

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

    public int SmoothRadius;
    public bool Smoothing;

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
    public bool AcousticZoom = false;
    public bool PolarView = false;

    public List<Mark> MarksList;
    
    public float ImageRotation = 0;
    public Vector2Int CentrePercent; //Change via SetPositionX, SetPositionY
    public int RadiusPercent = 50; //Change via SetRadius

    public Vector2 MouseCoords = new Vector2(0.5f, 0.5f); // Mouse Coords image UV space  . (0,0) - left bottom corner
    public Vector2Int ZoomWindowRect = new Vector2Int(300, 500); //Zoom window Width, Height
    public Vector2Int ZoomParams = new Vector2Int(50, 1); // Zoom %, Zoom Factor

    /////--------------"Scripting Fields"------------------/////

    public Font CustomFont;

    public Dictionary<MarkType, Sprite> MarkSprites;

    [Range(0f, 3f)]
    public float LinesThickness = 1f; //Rings and grid thickness
    [Range(1, 10)]
    public int BoxSize = 5;
    [Range(1f, 60f)]
    public int FontSize = 20;

    [Range(10, 25f)]
    public float VerticalFieldOfView = 20f;
    [Range(90f, 130f)]
    public float HorizontalFieldOfView = 115f;

    [Range(2f, 300f)]
    public float NoiseTresholdForMaxFOV = 60f;
    [Range(0f, 300f)]
    public float NoiseTresholdForMinFOV = 0f;

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

    //public float ScanSpeed = 10f;

    public Camera bakingCamera = null;

    public Transform shipTransform = null;
    
    /////-------------"Private Fields"------------------/////
    
    //UI Marks Fields
    private List<Text> RingsDistanceMarks, Zoom_RingsDistanceMarks;
    private List<Text> GridDistanceMarks, Zoom_GridDistanceMarks;
    private List<Image> MarksGoList;
    private List<Image> Zoom_MarksGoList;
    private Transform marks_placeholder;
    private Transform zoom_marks_placeholder;

    private Transform text_marks_placeholder;
    private Transform zoom_text_marks_placeholder;
    //Final Texture
    [HideInInspector] public RTHandle Result, Zoom_Result;

    private GraphicsFormat vectorFormat;
    private GraphicsFormat vector4Format;
    //Texture Handles
    private RenderTexture distanceTexture;
    private RenderTexture zoom_DistanceTexture;
    private RTHandle distribution_Transit1, distribution_Transit2, distribution_Together;
    private RTHandle zoom_distribution_Transit1, zoom_distribution_Transit2, zoom_distribution_Together;
    private ComputeBuffer distanceBuffer, zoom_distanceBuffer;

    //Shader Fields

    private ShaderTagId[] shaderTags;

    private ComputeShader DistributionComputer;
    private ComputeShader InterpolationComputer;
    private ComputeShader BlurComputer;

    private Material overrideMaterial = null;
    private Material terrainOverrideMaterial = null;

    //Shader Hash Handles
    private int handleClearBuffer;
    private int handleNoiseDistributionMain;
    private int handleDistributionMain;
    private int handleRenderInPolar;
    private int handleRenderInCartesian;

    private int handleBlurHor;
    private int handleBlurVer;

    private int handleRemap;
    private int handle_Zoom_RenderInPolar;
    private int handle_Zoom_RenderInCartesian;


    //Fields for Fps
    private float time;
    private Vector4 zoomRect;
    private Vector2Int zoomDim;

    private int aspect;
    private int zoom_Resolution;
    #endregion

    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return overrideMaterial; yield return terrainOverrideMaterial; }

    public void SetPositionX(int X)
    {
        var relCP = CentrePercent - new Vector2(X, CentrePercent.y);

        MouseCoords = MouseCoords - relCP / 100f;
        CentrePercent.x = X;

        if (MarksList.Count == 0) return;
        foreach(var m in MarksList)
        {
            m.rect.x = m.rect.x - relCP.x / 100f;
        }
    }

    public void SetPositionY(int Y)
    {
        var relCP = CentrePercent - new Vector2(CentrePercent.x, Y);

        MouseCoords = MouseCoords - relCP / 100f;
        CentrePercent.y = Y;

        if (MarksList.Count == 0) return;
        foreach (var m in MarksList)
        {
            m.rect.y = m.rect.y - relCP.y / 100f;
        }
    }

    public void SetRadius(int R)
    {
        if (R == 0) R = 1;
        if (RadiusPercent == 0) RadiusPercent = 1;
        var relRP = (float)RadiusPercent / (float)R;
        var mCoordsCentered = MouseCoords - new Vector2(0.5f, 0.5f) - (Vector2)CentrePercent / 100f;
        mCoordsCentered /= (relRP);
        MouseCoords = mCoordsCentered + new Vector2(0.5f, 0.5f) + (Vector2)CentrePercent / 100f;

        ZoomParams.x = (int)(ZoomParams.x / relRP);
        ZoomParams.x = Mathf.Clamp(ZoomParams.x, 1, 1000);

        RadiusPercent = R;
        if (MarksList.Count == 0) return;
        foreach (var m in MarksList)
        {
            mCoordsCentered = new Vector2(m.rect.x,m.rect.y) - new Vector2(0.5f, 0.5f) - (Vector2)CentrePercent / 100f;
            mCoordsCentered /= (relRP);
            m.rect.x = (mCoordsCentered + new Vector2(0.5f, 0.5f) + (Vector2)CentrePercent / 100f).x;
            m.rect.y = (mCoordsCentered + new Vector2(0.5f, 0.5f) + (Vector2)CentrePercent / 100f).y;
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

        DistributionComputer = (ComputeShader)Resources.Load("ComputeShaders/M900/M900_DistributionComputer");
        InterpolationComputer = (ComputeShader)Resources.Load("ComputeShaders/M900/M900_InterpolationComputer");

        BlurComputer = (ComputeShader)Resources.Load("ComputeShaders/Shared/BlurComputer");


        handleClearBuffer = DistributionComputer.FindKernel("ClearBuffer");

        handleNoiseDistributionMain = DistributionComputer.FindKernel("MapNoiseToQuadSonar");
        handleDistributionMain = DistributionComputer.FindKernel("MapDepthToQuadSonar");
        handleRemap = InterpolationComputer.FindKernel("RemapQuad");
        handleRenderInPolar = InterpolationComputer.FindKernel("RenderInPolar");
        handleRenderInCartesian = InterpolationComputer.FindKernel("RenderInCartesian");

        handleBlurHor = BlurComputer.FindKernel("HorzBlurCs");
        handleBlurVer = BlurComputer.FindKernel("VertBlurCs");

        handle_Zoom_RenderInPolar = InterpolationComputer.FindKernel("Zoom_RenderInPolar");

        handle_Zoom_RenderInCartesian = InterpolationComputer.FindKernel("Zoom_RenderInCartesian");
    }

    private bool IsNextUpdateAvaliable()
    {
        time += Time.deltaTime;
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

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if (bakingCamera == null)
        {
            Debug.Log("Set up baking Camera via inspector");
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

        vectorFormat = GraphicsFormat.R32_SFloat;
        vector4Format = GraphicsFormat.R16G16B16A16_SFloat;

        SerializeMarkSpriteDictionaryIfPossible();

        bakingCamera.usePhysicalProperties = true;

        bakingCamera.enabled = false;
        SetUpCameraData(Camera_UI);
        SetUpCameraData(Zoom_Camera_UI);
        InitSHaders_CashNameHandles();

        AllocateTexturesIfNeeded();
        time = 0.5f;

        marks_placeholder = ResultSonarImage.transform.Find("MarksPlaceholder");
        zoom_marks_placeholder = ZoomResultImage.transform.Find("ZoomMarksPlaceholder");
        text_marks_placeholder = ResultSonarImage.transform.Find("TextMarksPlaceholder");
        zoom_text_marks_placeholder = ZoomResultImage.transform.Find("ZoomTextMarksPlaceholder");

        //MarksList = new List<Mark>();
        MarksGoList = new List<Image>();
        Zoom_MarksGoList = new List<Image>();

        RingsDistanceMarks = new List<Text>();
        GridDistanceMarks = new List<Text>();
        Zoom_RingsDistanceMarks = new List<Text>();
        Zoom_GridDistanceMarks = new List<Text>();
        ClearTextMarks();

    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
    {
        if (!bakingCamera || !shipTransform || !terrainOverrideMaterial || !overrideMaterial || !distanceTexture || !Camera_UI )
            return;

        if (camera.camera.cameraType == CameraType.SceneView)
            return;

        if (camera.camera != Camera_UI)
            return;

        if (IsNextUpdateAvaliable() == false)
            return;

        AllocateTexturesIfNeeded();
        ///////////----Render Objects With Override Materials
        CoreUtils.SetRenderTarget(cmd, distanceTexture, ClearFlag.All, clearColor: Color.black);
        RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, distanceTexture,new Vector4(0f, 0f, 1f, 1f));

        ///////////----Remap Camera Texture To Distance Distribution
        SetVar_DistributionComputer(cmd);

        cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", distanceBuffer);
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count + 1023) / 1024, 1, 1);

        CoreUtils.SetRenderTarget(cmd, distribution_Transit1, ClearFlag.All, clearColor: Color.black);
        cmd.SetComputeBufferParam(DistributionComputer, handleNoiseDistributionMain, "DistanceBuffer", distanceBuffer);
        cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Source", distanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Destination", distribution_Transit1);

        cmd.DispatchCompute(DistributionComputer, handleNoiseDistributionMain, (distanceTexture.width + 15) / 16,
               (distanceTexture.height + 15) / 16, 1);
        
        cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", distanceBuffer);
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count+1023) / 1024,1, 1);

        CoreUtils.SetRenderTarget(cmd, distribution_Transit2, ClearFlag.All, clearColor: Color.black);
        cmd.SetComputeBufferParam(DistributionComputer, handleDistributionMain, "DistanceBuffer", distanceBuffer);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Source", distanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Destination", distribution_Transit2);
        
        cmd.DispatchCompute(DistributionComputer, handleDistributionMain, (distanceTexture.width + 15) / 16,
               (distanceTexture.height + 15) / 16, 1);



        ///////////----Connect Noise and True distr. to one texture
        SetVar_DisplayAndZoomParams(cmd);
        SetVar_SonarParams(cmd);
        SetVar_SonarGridAndColor(cmd);

        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Noise", distribution_Transit1);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Source", distribution_Transit2);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Destination", distribution_Together);
        cmd.DispatchCompute(InterpolationComputer, handleRemap, (distribution_Together.referenceSize.x + 15) / 16,
               (distribution_Together.referenceSize.y + 15) / 16, 1);
        
        if (Smoothing == true)
            BlurDistribution(cmd);

        ////////////----Render to Polar and Cartesian
        CoreUtils.SetRenderTarget(cmd, Result, ClearFlag.Color, clearColor: Color.clear);
        if (PolarView == true)
        {
            cmd.SetComputeTextureParam(InterpolationComputer, handleRenderInPolar, "Source", distribution_Together);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRenderInPolar, "Destination", Result);
            cmd.DispatchCompute(InterpolationComputer, handleRenderInPolar, (ResultDimensions.x + 31) / 32,
                   (ResultDimensions.y + 31) / 32, 1);
        }
        else
        {
            cmd.SetComputeTextureParam(InterpolationComputer, handleRenderInCartesian, "Source", distribution_Together);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRenderInCartesian, "Destination", Result);
            cmd.DispatchCompute(InterpolationComputer, handleRenderInCartesian, (ResultDimensions.x + 31) / 32,
                   (ResultDimensions.y + 31) / 32, 1);
        }


        if (AcousticZoom== true)
        {
            CoreUtils.SetRenderTarget(cmd, zoom_DistanceTexture, ClearFlag.All, clearColor: Color.black);
            RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, zoom_DistanceTexture, zoomRect);
            
            ///////////----Render Depth To Planar Texture
            cmd.SetComputeVectorParam(InterpolationComputer, "OriginTexWH", (Vector2)Result.referenceSize);
            
            cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", zoom_distanceBuffer);
            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (zoom_distanceBuffer.count + 1023) / 1024, 1, 1);
            
            CoreUtils.SetRenderTarget(cmd, zoom_distribution_Transit1, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeBufferParam(DistributionComputer, handleNoiseDistributionMain, "DistanceBuffer", zoom_distanceBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Source", zoom_DistanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Destination", zoom_distribution_Transit1);

            cmd.DispatchCompute(DistributionComputer, handleNoiseDistributionMain, (zoom_DistanceTexture.width + 15) / 16,
                    (zoom_DistanceTexture.height + 15) / 16, 1);

            
            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (zoom_distanceBuffer.count + 1023) / 1024, 1, 1);
            
            CoreUtils.SetRenderTarget(cmd, zoom_distribution_Transit2, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeBufferParam(DistributionComputer, handleDistributionMain, "DistanceBuffer", zoom_distanceBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Source", zoom_DistanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Destination", zoom_distribution_Transit2);

            cmd.DispatchCompute(DistributionComputer, handleDistributionMain, (zoom_DistanceTexture.width + 15) / 16,
                    (zoom_DistanceTexture.height + 15) / 16, 1);

            
            CoreUtils.SetRenderTarget(cmd, zoom_distribution_Together, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Noise", zoom_distribution_Transit1);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Source", zoom_distribution_Transit2);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Destination", zoom_distribution_Together);
            cmd.DispatchCompute(InterpolationComputer, handleRemap, (zoom_distribution_Together.referenceSize.x + 15) / 16,
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

        if (ResultSonarImage != null)
        {
            ResultSonarImage.texture = Result;
            ResultSonarImage.SetNativeSize();
        }
        if (ZoomResultImage != null && Zoom_Result!=null)
        {
            ZoomResultImage.gameObject.SetActive(AcousticZoom);
            ZoomResultImage.texture = Zoom_Result;
            ZoomResultImage.SetNativeSize();
        }


        DrawMarksOnUI();
        if (PolarView == false )
            DrawCartesianViewDigits();
        else
            DrawPolarViewDigits();

        /*if (distance_img != null)
            distance_img.texture = Result;
        if (Test_img3 != null)
            Test_img3.texture = zoom_DistanceTexture;
        if (Test_img1 != null&& zoom_distribution_Together!=null)
            Test_img1.texture = zoom_distribution_Together;*/
    }

    private void BlurDistribution(CommandBuffer cmd)
    {
        //SetParams
        var weights = BlurCore.GetWeights(SmoothRadius);
        cmd.SetComputeBufferParam(BlurComputer, handleBlurHor, "gWeights", weights);
        cmd.SetComputeBufferParam(BlurComputer, handleBlurVer, "gWeights", weights);
        cmd.SetComputeIntParam(BlurComputer, "blurRadius", (int)SmoothRadius);

        //SetTextures
        cmd.SetComputeTextureParam(BlurComputer, handleBlurHor, "source", distribution_Together);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurHor, "horBlurOutput", distribution_Transit1);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurVer, "horBlurOutput", distribution_Transit1);
        cmd.SetComputeTextureParam(BlurComputer, handleBlurVer, "verBlurOutput", distribution_Together);

        //DispatchShaders
        cmd.DispatchCompute(BlurComputer, handleBlurHor, (distribution_Together.referenceSize.x + 1023) / 1024, distribution_Together.referenceSize.y, 1);
        cmd.DispatchCompute(BlurComputer, handleBlurVer, distribution_Together.referenceSize.x , (distribution_Together.referenceSize.y+1023)/1024, 1);
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
        cmd.SetComputeFloatParam(DistributionComputer, "TopNoiseTreshold", NoiseTresholdForMaxFOV);
        cmd.SetComputeFloatParam(DistributionComputer, "BottomNoiseTreshold", NoiseTresholdForMinFOV);
    }
    
    private void SetVar_DisplayAndZoomParams(CommandBuffer cmd)
    {
        var offset = (Vector2)CentrePercent * 0.01f;
        var scale = 2f * RadiusPercent / 100f;

        cmd.SetComputeVectorParam(InterpolationComputer, "Offset", offset);
        cmd.SetComputeFloatParam(InterpolationComputer, "Scale_factor", scale);

        cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle1", zoomRect.x);
        cmd.SetComputeFloatParam(DistributionComputer, "Zoom_angle2", zoomRect.z+zoomRect.x);

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
        cmd.SetComputeFloatParam(InterpolationComputer, "MinimumRange", Mathf.Clamp01(MinScanRange/ MaxScanRange));
    }

    private void SetVar_SonarGridAndColor(CommandBuffer cmd)
    {

        cmd.SetComputeIntParam(InterpolationComputer, "GridCount", ShowGrid == true ? 2 : 0);
        cmd.SetComputeIntParam(InterpolationComputer, "LinesCount", Lines);
        cmd.SetComputeIntParam(InterpolationComputer, "Rings", Rings);
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

        cmd.SetComputeFloatParam(InterpolationComputer, "Gamma", Gamma);
        cmd.SetComputeVectorArrayParam(InterpolationComputer, "_Colors", _cols);
        cmd.SetComputeIntParam(InterpolationComputer, "_ColorsCount", _cols.Length);
        cmd.SetComputeVectorParam(InterpolationComputer, "_BackgroundColor", BackgroundColor);
        cmd.SetComputeVectorParam(InterpolationComputer, "_GridColor", GridColor);

    }

    private void CheckIfMouseIsInImageBounds()
    {
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
            Vector2 d1 = boxCentRelative - (Vector2)ZoomWindowRect/2f * ZoomParams.x / 100f;
            Vector2 d2 = boxCentRelative + (Vector2)ZoomWindowRect/2f * ZoomParams.x / 100f;
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
            Vector2 d1 = boxC - (Vector2)ZoomWindowRect/2f * ZoomParams.x / 100f;
            Vector2 d2 = boxC + (Vector2)ZoomWindowRect/2f * ZoomParams.x / 100f;
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
        float y = PaniniProjectionScreenPosition(2*x-1)+0.5f;
        //loop until completion
        int iter = 0;
        while (Mathf.Abs(yTarget - y) > xTolerance && iter <100 )
        {
            if (yTarget > y)
                lower = percent;
            else
                upper = percent;
            
            percent = (upper + lower) / 2;
            y = PaniniProjectionScreenPosition( 2*percent -1f)+0.5f;
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
        float upscale = HorizontalFieldOfView <= 115? Mathf.Lerp(0.65f, 0.82f, (HorizontalFieldOfView - 90) / 25): Mathf.Lerp(0.82f, 1.01f, (HorizontalFieldOfView - 115) / 15);

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
        //bakingCamera.fieldOfView = Mathf.Lerp(VerticalFieldOfView, 179f, (90f - ScanAreaLookAngle) / 90f);
        bakingCamera.farClipPlane = MaxScanRange;
        bakingCamera.focalLength = 57f;

        float _horisontalFieldOfView = FieldOfViewToSensorSize(HorizontalFieldOfView, bakingCamera.focalLength);
        float _verticalFieldOfView = FieldOfViewToSensorSize(VerticalFieldOfView, bakingCamera.focalLength);
        bakingCamera.sensorSize = new Vector2(_horisontalFieldOfView, _verticalFieldOfView);
        bakingCamera.gateFit = Camera.GateFitMode.None;
        //bakingCamera.rect = new Rect((float)(i - 1) / CamCountAproximation, 0, 1f / CamCountAproximation, 1);


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
        var height = Mathf.Min(1 - 0f, 1f);
        var r = new Rect(rect.x, 0f, width, height);
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
        cmd.SetGlobalMatrix("_ViewProjMatrix", vp);
        cmd.SetGlobalMatrix("_InvViewProjMatrix", vp.inverse);
        cmd.SetGlobalMatrix("_CameraViewProjMatrix", vp);
        cmd.SetGlobalVector("_WorldSpaceCameraPos", Vector3.zero);
        cmd.SetGlobalVector("_ProjectionParams", new Vector4(bakingCamera.nearClipPlane, bakingCamera.farClipPlane, 1 / bakingCamera.nearClipPlane, 1 / bakingCamera.farClipPlane));
    }

    private void AllocateTexturesIfNeeded()
    {
        aspect = Mathf.CeilToInt(bakingCamera.sensorSize.x / bakingCamera.sensorSize.y);
        if (Result == null || ResultDimensions != Result.referenceSize && ResultDimensions != Vector2Int.zero)
        {
            Result?.Release();
            Result = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
                name: "Result", enableRandomWrite: true
            );
        }

        if (distribution_Transit1 == null|| _Resolution != distribution_Together.referenceSize.x && _Resolution != 0 )
        {
            if (distanceBuffer !=null) distanceBuffer.Dispose();
            distanceBuffer = new ComputeBuffer(_Resolution * _Resolution , sizeof(float), ComputeBufferType.Default);

            if (distanceTexture != null) distanceTexture?.Release();
            distanceTexture = new RenderTexture(_Resolution * aspect / 2, _Resolution, 16, vector4Format);
            distanceTexture.filterMode = FilterMode.Point;
            distanceTexture.name = "DistanceTexture";

            distribution_Transit1?.Release();
            distribution_Transit1 = RTHandles.Alloc(
            _Resolution, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: vectorFormat,
            name: "distribution_Transit1", enableRandomWrite: true
            );
            distribution_Transit2?.Release();
            distribution_Transit2 = RTHandles.Alloc(
            _Resolution, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: vectorFormat,
            name: "distribution_Transit2", enableRandomWrite: true
            );
            distribution_Together?.Release();
            distribution_Together = RTHandles.Alloc(
               _Resolution, _Resolution , dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
               name: "distribution_Together", enableRandomWrite: true, autoGenerateMips: false);


        }

        if (AcousticZoom == true)
        {
            zoom_Resolution =  (int)(Mathf.Clamp(_Resolution * ZoomParams.y, 360, 1440));
            zoomRect = CalculateZoomCameraRect();
            zoomDim = new Vector2Int(Mathf.FloorToInt(zoom_Resolution* (zoomRect.z)),zoom_Resolution);
            if (zoom_DistanceTexture == null || zoomDim.x * (int)(aspect / 2f) != zoom_DistanceTexture.width && _Resolution != 0 && ZoomParams.y != 0 && zoomDim.x != 0)
            {
                if (zoom_DistanceTexture != null) zoom_DistanceTexture.Release();
                zoom_DistanceTexture = new RenderTexture(zoomDim.x * (int)(aspect /2f), zoomDim.y, 16, vector4Format);
                zoom_DistanceTexture.filterMode = FilterMode.Point;
                zoom_DistanceTexture.name = "Zoom_DistanceTexture";
            }
            if ((zoom_distribution_Transit1 == null || zoomDim.x != zoom_distribution_Transit1.referenceSize.x) && _Resolution != 0 && ZoomParams.y != 0 && zoomDim.x != 0)
            {
                if (zoom_distanceBuffer != null) zoom_distanceBuffer.Release();
                zoom_distanceBuffer = new ComputeBuffer(Mathf.RoundToInt(zoomDim.x*zoomDim.y), sizeof(float), ComputeBufferType.Default);
                
                zoom_distribution_Transit1?.Release();
                zoom_distribution_Transit1 = RTHandles.Alloc(
                    zoomDim.x,zoomDim.y, dimension: TextureDimension.Tex2D,
                    colorFormat: vectorFormat,
                    name: "Zoom_distribution_Transit1", enableRandomWrite: true
                );

                zoom_distribution_Transit2?.Release();
                zoom_distribution_Transit2 = RTHandles.Alloc(
                  zoomDim.x, zoomDim.y, dimension: TextureDimension.Tex2D, colorFormat: vectorFormat,
                   name: "Zoom_distribution_Transit2", enableRandomWrite: true, autoGenerateMips: false);

                zoom_distribution_Together?.Release();
                zoom_distribution_Together = RTHandles.Alloc(
                zoomDim.x, zoomDim.y, dimension: TextureDimension.Tex2D,
                colorFormat: vectorFormat,
                name: "Zoom_distribution_Together", enableRandomWrite: true);
            }

            if ((Zoom_Result == null || ZoomWindowRect != Zoom_Result.referenceSize) && ZoomWindowRect != Vector2Int.zero)
            {
                Zoom_Result?.Release();
                Zoom_Result = RTHandles.Alloc(
                    ZoomWindowRect.x, ZoomWindowRect.y, dimension: TextureDimension.Tex2D, colorFormat: vector4Format,
                    name: "zoom_Result", enableRandomWrite: true
                );
            }
        }
    }

    private void ClearTextMarks()
    {
        if (text_marks_placeholder == null)
        {
            text_marks_placeholder = new GameObject("TextMarksPlaceholder").transform;
            text_marks_placeholder.transform.SetParent(ResultSonarImage.rectTransform, false);
        }
        else{

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

        if (RingsDistanceMarks.Count != ((Rings) * 2+Lines+2+1) || RingsDistanceMarks == null || Zoom_RingsDistanceMarks == null)
        {
            ClearTextMarks();
            CreateTextRingsMarks(Rings * 2 + Lines+2+1);
        }
        var markWidth = RingsDistanceMarks[0].rectTransform.rect.width / 5f;
        var scanRange = MaxDisplayRange- MinDisplayRange;
        
        void sm(Text mark, string text,Vector2 coord, Vector2 dir,Vector2 offset_sign)
        {
            //var bounds = mark.textBounds.extents;
            var bounds = new Vector2(mark.preferredWidth,mark.preferredHeight);
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = vFontSize;
            
            var dCoord = coord + dir*offset_sign.x*bounds.x + Vector2.Perpendicular(dir) * (offset_sign.y*bounds.y);

            mark.rectTransform.localPosition = dCoord;
            mark.color = GridColor;
            mark.gameObject.SetActive(true);
        }

        void Draw(List<Text> ringsMarks, Vector2 resWH, Vector2 Cent, float z)
        {
            if((ringsMarks == Zoom_RingsDistanceMarks && AcousticZoom == false))
            {

                for (int k = 0; k < ringsMarks.Count; k++)
                {
                    ringsMarks[k].gameObject.SetActive(false);
                }
                return;
            }
            if ((ShowGrid == true && Rings != 0 && ShowDigits && PolarView == false))
            {
                Vector2 dir, offset_val, dCoord,offset_sign;
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
                    dir = new Vector2(Mathf.Cos((-ImageRotation+ 90f - HorizontalFieldOfView / 2f) * Mathf.Deg2Rad), Mathf.Sin((-ImageRotation + 90f - HorizontalFieldOfView / 2f) * Mathf.Deg2Rad));
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
                        sm(ringsMarks[k+Rings], text, dCoord, dir,offset_sign);

                    //setMark(ringsMarks, k + Rings, Cent, dir, 0f, markWidth, z);
                }

                float S1_angle = ImageRotation + (90 - HorizontalFieldOfView / 2);
                float S2_angle = ImageRotation + (90 + HorizontalFieldOfView / 2);
                for (int k=0; k < Lines+2; k++)
                {
                    float a = S1_angle + HorizontalFieldOfView * (k / (Lines + 1f));
                    dir = new Vector2(-Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    dir.Normalize();


                    offset_val = new Vector2(markWidth,0f );
                    dist = z * minAbs / 2f + offset_val.x;
                    offset_sign = new Vector2(1f, 1f);
                    text = (Mathf.Round(2f * (a - S1_angle)) / 2f - HorizontalFieldOfView / 2f).ToString() + "°";
                    dCoord = Cent + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;
                    dir = Vector2.zero;
                    //dir.= -dir.x;
                    sm(ringsMarks[k+2*Rings+1], text, dCoord, dir, offset_sign);
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
        float zoom = ZoomParams.x / 100f ;
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
        Vector2 center = new Vector2(resRect.x, resRect.y) * new Vector2(0f,-50f) / 100f;

        if (GridDistanceMarks.Count != (Rings * 4 + 2) || GridDistanceMarks == null)
        {
            ClearTextMarks();
            CreateTextGridMarks(Rings * 4+2);
        }

        var markWidth = 0f;
        var scanRange = MaxDisplayRange-MinDisplayRange;

        //Проверка на активацию в зависимости от минимального расстояния

        void sm(Text mark, string text, Vector2 coord, Vector2 offsetDir,Vector2 offset_sign)
        {
            var bounds = new Vector2(mark.preferredWidth+4f, mark.preferredHeight+4f)/2f;
            mark.text = text;
            mark.font = CustomFont;
            mark.fontSize = vFontSize;
            Debug.Log(bounds);
            var dCoord = coord + offsetDir * bounds.x* offset_sign.x + Vector2.Perpendicular(offsetDir) * bounds.y* offset_sign.y;

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
            if (ShowGrid && Rings != 0 && ShowDigits && PolarView == true )
            {
                Vector2 dir, offset_val, dCoord,offset_sign;
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
                    dist = 2f*z * minAbs * k / Rings / 2f + offset_val.x; 
                    text = (Mathf.Round(2f * (MinDisplayRange + scanRange * k / Rings)) / 2f).ToString();

                    dCoord = new Vector2(-resWH.x / 2f, Cent.y) + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;
                    dir = Vector2.Perpendicular(dir);
                    sm(gridMarks[k], text, dCoord, dir, offset_sign);
                    //Vertical+
                    //setMark(gridMarks, k , new Vector2(-resWH.x / 2f, Cent.y), Vector2.up, -markWidth, -3f*markWidth/2f , 2f*z);
                    //Vertical-
                    //setMark(gridMarks, k + 3 * Rings, new Vector2(-resWH.x / 2f, Cent.y), Vector2.down, -w / 2, w / 2, 2f * z);
                    //(Mathf.Round((a) * 2f) / 2f).ToString()
                    float a = HorizontalFieldOfView/2f * ((float)(k) / (float)(Rings));
                    a = Mathf.Round(a * 2f) / 2f;

                    //Horisontal+

                    dir = Vector2.right;
                    offset_val = new Vector2(0f, 0f);// -markWidth);
                    offset_sign = new Vector2(-1f, -1f);
                    dist = z / norm * minAbs * k / Rings / 2f + offset_val.x;
                    text = a.ToString() + "°";

                    dCoord = new Vector2(Cent.x, resWH.y / 2f) + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;

                    sm(gridMarks[k+Rings], text, dCoord, dir, offset_sign);

                    //setMarkWithText(gridMarks, a.ToString(), k+Rings, new Vector2(Cent.x, resWH.y / 2f), Vector2.right, -3* markWidth/2 , -markWidth, );
                    //Horisontal-

                    dir = Vector2.left;
                    dCoord = new Vector2(Cent.x, resWH.y / 2f) + dir * dist + Vector2.Perpendicular(dir) * offset_val.y;
                    offset_sign = new Vector2(-1f, 1f);
                    text = (-a).ToString() + "°";
                    sm(gridMarks[k+2*Rings], text, dCoord, dir, offset_sign);

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
        
        var zoomRect = new Vector2(ZoomResultImage.rectTransform.rect.width, ZoomResultImage.rectTransform.rect.height-markWidth);

        var Boxcenter = (MouseCoords - Vector2.one / 2f) * resRect;
        float zoom = ZoomParams.x / 100f ;
        relUV = (center - Boxcenter) / zoom;
        Draw(Zoom_GridDistanceMarks, zoomRect, relUV, 1f / zoom);
    }
    private void DrawMarksOnUI()
    {
        if (MarksList.Count == 0) return;
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
                mGoList[iter].rectTransform.localPosition =center+ (new Vector2(rect.x, rect.y) * ResultDimensions)/zoom;
                mGoList[iter].rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
                mGoList[iter].gameObject.SetActive(true);
                iter++;
            }

        }

        Vector2 c = ResultDimensions * (Vector2)CentrePercent / 100f;

        Draw(MarksGoList,new Vector2(0f,0f),1f);
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
            var relUV = (c - Boxcenter)/zoom;
            Draw(Zoom_MarksGoList,relUV,zoom);
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
            var img =  go.AddComponent<Image>() as Image;
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

    protected override void Cleanup()
    {
        // Cleanup code
        if (distanceBuffer != null)
            distanceBuffer.Release();
        distanceTexture?.Release();
        distribution_Transit1?.Release();
        distribution_Transit2?.Release();
        Result?.Release();
        if (zoom_distanceBuffer != null)
            zoom_distanceBuffer.Dispose();
        if (zoom_DistanceTexture!=null)
            zoom_DistanceTexture?.Release();
        zoom_distribution_Transit1?.Release();
        zoom_distribution_Together?.Release();
        Zoom_Result?.Release();
        ClearTextMarks();
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

