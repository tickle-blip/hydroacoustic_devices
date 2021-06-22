using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

using System.Collections.Generic;
using System;


public enum Resolution
{
    VeryLow,
    Low,
    Medium,
    High,
    Ultra
}

class GKO_Pass : CustomPass
{
    #region Fields
    //public RawImage Test_img;
    //public RawImage Test_img1;
    
    //public RawImage distance_img;
    public Vector2Int ResultDimensions = new Vector2Int(1920, 1080);
    public Camera Camera_UI;
    public Camera Zoom_Camera_UI;
    public RawImage ResultSonarImage;
    public RawImage ZoomResultImage;

    public TypeCreator Type_Creator;
    /////-------------"UI Fields"------------------/////

    [Range(0f, 1f)]
    public float Gain = 0.25f;
    [Range(2f,300f)]
    [SerializeField] private float MaxScanRange = 10f; //Change via SetRange
    [Range(0f, 720)]
    [SerializeField] private int _Resolution = 90; // Change via SetResolution
    [Range(0f, 360f)]
    [SerializeField] private int ScanZoneWideness = 360; //Change via SetWideness
    [Range(0f, 360f)]
    [SerializeField] private int ScanLookAngle = 0; // Change via SetScanAngle

    [Range(0f, 1f)]
    public float Sensitivity = 0; 
    [Range(0f, 2f)]
    public float Contrast = 1f;

    public bool Interpolation;

    public Vector3[] Colors = { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1) }; //Color Palette. colors in range (0.0 - 1.0)

    public Color BackgroundColor = Color.black;
    public Color GridColor = Color.grey;
    public Color BoxColor = Color.red;

    [Range(0f, 10f)]
    public int Rings = 4;

    public int Sector1_angle = 30, Sector2_angle = 60;

    public bool ShowDigits = true;
    public bool ShowRings = true;
    public bool ShowSector = true;
    public bool ShowCross = true;
    public bool ShowGrid = false;
    public bool AcousticZoom = false;

    public bool ShowMark;
    public List<Mark> MarksList;
    public Vector2Int CentrePercent; //Change via SetPositionX, SetPositionY
    public int RadiusPercent= 50; //Change via SetRadius

    public Vector2 MouseCoords = new Vector2(0.5f, 0.5f); // Mouse Coords image UV space  . (0,0) - left bottom corner
    public Vector2Int ZoomWindowRect = new Vector2Int(300, 500); //Zoom window Width, Height
    public Vector2Int ZoomParams = new Vector2Int(50, 1); // Zoom %, Zoom Factor

    /////--------------"Scripting Fields"------------------/////

    public Dictionary<MarkType, Sprite> MarkSprites;
    public Font CustomFont;

    [Range(0f, 3f)]
    public float LinesThickness = 1f; //Rings and grid thickness
    [Range(1, 10)]
    public int BoxSize = 5;
    [Range(1f, 60f)]
    public int FontSize = 20;

    [Range(10f, 30f)]
    public float VerticalFieldOfView = 30f;


    [Range(0f, 1f)]
    public float BlindZoneRadius = 0.25f;//In Meters [0,1]
    [Range(0.01f, 150f)]
    public float Noise1_Scale = 1f;
    [Range(0.01f, 150f)]
    public float Noise2_Scale = 1f;
    [Range(0.01f, 1f)]
    public float Noise1_Bias = 0.5f;
    [Range(0.01f, 1f)]
    public float Noise2_Bias = 0.5f;

    [Range(0.5f, 200f)]
    public float NoisePatternScroll = 30f;

    [Range(0f, 2f)]
    public float Noise_Gain_Multiplier = 0.5f;

    [Range(0f, 1f)]
    public float Noise_Reflectivity_Min = 0.5f;
    [Range(0f, 1f)]
    public float Noise_Reflectivity_Max = 0.5f;

    
    public float ScanSpeed = 10f;
    [Range(6, 12f)]
    public int CamerasCount=6;
    //public Texture2D NoiseMain; //Noise texture for internal fake randomness
    

    public Camera bakingCamera = null;

    public Transform shipTransform = null;

    /////-------------"Private Fields"------------------/////

    private List<Text> RingsDistanceMarks, Zoom_RingsDistanceMarks;
    private List<Text> GridDistanceMarks, Zoom_GridDistanceMarks;

    private Transform text_marks_placeholder;
    private Transform zoom_text_marks_placeholder;

    private List<Image> MarksGoList;
    private List<Image> Zoom_MarksGoList;
    private Transform marks_placeholder;
    private Transform zoom_marks_placeholder;

    //Final Texture
    [HideInInspector] public RTHandle Result, Zoom_Result;
    //Texture Handles
    private RenderTexture DistanceTexture;//public for debuging
    private RenderTexture Zoom_DistanceTexture; 
    private RTHandle distribution_Raw, distribution_Raw_Noise, distribution_Fixed, distribution_Interpolated_On_Quad;
    private RTHandle Zoom_distribution_Raw, Zoom_distribution_Raw_Noise, Zoom_distribution_Fixed, Zoom_distribution_Interpolated_On_Quad;
    private ComputeBuffer distanceBuffer, Zoom_distanceBuffer;
    private GraphicsFormat VectorFormat;
    private GraphicsFormat Vector4Format;

    private ComputeShader DistributionComputer;
    private ComputeShader InterpolationComputer;

    private Material overrideMaterial = null;
    private Material terrainOverrideMaterial = null;

    private int handleClearTexturePortion;
    private int handleClearBuffer;

    private int handleNoiseDistributionMain;
    private int handleDistributionMain;
    private int handleRemapInterpolate;
    private int handleRenderInPolar;
    private int handleRemap;
    private int handle_Zoom_RenderInPolar;
    private ShaderTagId[] shaderTags;

    //fields for angles varying
    private float time, currentRotAngle, dir, temp_angle, temp_angleStep;
    private float dt;
    private float cosL;
    private bool mustClear;
    private int aspect;
    private float camera_x;
    private float camera_width;
    #endregion

    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return overrideMaterial; yield return terrainOverrideMaterial; }

    public void SetPositionX(int X)
    {
        var relCP = CentrePercent - new Vector2(X, CentrePercent.y);

        MouseCoords = MouseCoords - relCP / 100f;
        CentrePercent.x = X;

        if (MarksList.Count == 0) return;
        foreach (var m in MarksList)
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
            mCoordsCentered = new Vector2(m.rect.x, m.rect.y) - new Vector2(0.5f, 0.5f) - (Vector2)CentrePercent / 100f;
            mCoordsCentered /= (relRP);
            m.rect.x = (mCoordsCentered + new Vector2(0.5f, 0.5f) + (Vector2)CentrePercent / 100f).x;
            m.rect.y = (mCoordsCentered + new Vector2(0.5f, 0.5f) + (Vector2)CentrePercent / 100f).y;
        }
    }
    
    public void SetWideness(int wideness)
    {
        var _angle = ScanZoneWideness * time + ScanLookAngle - ScanZoneWideness / 2;

        ScanZoneWideness = wideness;
        time = (_angle - ScanLookAngle + ScanZoneWideness / 2) / (ScanZoneWideness + 0.01f);
        time = Mathf.Clamp01(time);
    }

    public void SetLookAngle(int lookAngle)
    {
        var _angle = ScanZoneWideness * time + ScanLookAngle - ScanZoneWideness / 2;

        ScanLookAngle = lookAngle;
        time = (_angle - ScanLookAngle + ScanZoneWideness / 2) / (ScanZoneWideness + 0.01f);
        if (_angle > ScanLookAngle - ScanZoneWideness / 2 && _angle < ScanLookAngle + ScanZoneWideness / 2)
        {

            time = (1 + Mathf.Sign(time)) / 2;
        }

    }

    public void SetResolution(Resolution res)
    {
        int value=60;
        switch (res)
        {
            case Resolution.VeryLow:
                value = 60;
                break;
            case Resolution.Low:
                value = 120;
                break;
            case Resolution.Medium:
                value = 180;
                break;
            case Resolution.High:
                value = 360;
                break;
            case Resolution.Ultra:
                value = 480;
                break;
        }
        float angleStep = 360f / (float)value;
        var _angle = ScanZoneWideness * time + ScanLookAngle - ScanZoneWideness / 2;
        _angle -= angleStep;
        var angle_rounded = dir == 1f ? Mathf.Floor(_angle / angleStep) * angleStep : (Mathf.Ceil(_angle / angleStep) * angleStep);

        time = (angle_rounded - ScanLookAngle + ScanZoneWideness / 2) / (ScanZoneWideness + 0.01f);
        _Resolution = value;
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

    public enum ZoneDirection
    {
        Up,
        Down,
        Left,
        Right,
        Center
    }

    public void SetScanZone(ZoneDirection zone)
    {
        switch (zone)
        {
            case ZoneDirection.Up:
                SetWideness(180);
                SetLookAngle(0);
                break;
            case ZoneDirection.Down:
                SetWideness(180);
                SetLookAngle(180);
                break;
            case ZoneDirection.Left:
                SetWideness(180);
                SetLookAngle(270);
                break;
            case ZoneDirection.Right:
                SetWideness(180);
                SetLookAngle(90);
                break;
            case ZoneDirection.Center:
                SetWideness(360);
                SetLookAngle(0);
                break;
        }
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
    private void InitSHaders_CashNameHandles()
    {
        //overrideMaterial = CoreUtils.CreateEngineMaterial((Shader)Resources.Load("SolidDistanceShader"));
        //terrainOverrideMaterial = CoreUtils.CreateEngineMaterial((Shader)Resources.Load("TerrainDistanceGraph"));
        //overrideMaterial = (Material)Resources.Load("Materials/GKO/GKO_Solid_Mat");
        overrideMaterial = (Material)Resources.Load("Materials/Shared/Shared_Solid_Mat");
        terrainOverrideMaterial = (Material)Resources.Load("Materials/Shared/Shared_Terrain_Mat");
        DistributionComputer = (ComputeShader)Resources.Load("ComputeShaders/GKO/GKO_DistributionComputer");
        InterpolationComputer = (ComputeShader)Resources.Load("ComputeShaders/GKO/GKO_InterpolationComputer");

        //Запоминаем id кернелей чтобы по ним потом запускать компут шейдеры
        handleClearTexturePortion = DistributionComputer.FindKernel("ClearTexturePortion");
        handleClearBuffer = DistributionComputer.FindKernel("ClearBuffer");

        handleNoiseDistributionMain = DistributionComputer.FindKernel("MapNoiseToQuadSonar");
        handleDistributionMain = DistributionComputer.FindKernel("MapDepthToQuadSonar");
        handleRemapInterpolate = InterpolationComputer.FindKernel("RemapInterpolateQuad");
        handleRemap = InterpolationComputer.FindKernel("RemapQuad");
        handleRenderInPolar = InterpolationComputer.FindKernel("RenderInPolar");

        handle_Zoom_RenderInPolar = InterpolationComputer.FindKernel("Zoom_RenderInPolar");

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
        //Выключает пост-процесинг в ЮИ камерах.
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
            Debug.Log("Set up cameras via inspector");
            return;
        }
        if (Camera_UI == null)
        {
            Debug.Log("Set up UI cameras via inspector");
            return;
        }

        if (Type_Creator == null)
            return;
        VectorFormat = GraphicsFormat.R32_SFloat;
        Vector4Format = GraphicsFormat.R16G16B16A16_SFloat;

        SerializeMarkSpriteDictionaryIfPossible();

        bakingCamera.usePhysicalProperties = true;
        bakingCamera.enabled = false;
        SetUpCameraData(Camera_UI);
        SetUpCameraData(Zoom_Camera_UI);

        InitSHaders_CashNameHandles();

        AllocateTexturesIfNeeded();

        marks_placeholder = ResultSonarImage.transform.Find("MarksPlaceholder");
        zoom_marks_placeholder = ZoomResultImage.transform.Find("ZoomMarksPlaceholder");
        text_marks_placeholder = ResultSonarImage.transform.Find("TextMarksPlaceholder");
        zoom_text_marks_placeholder = ZoomResultImage.transform.Find("ZoomTextMarksPlaceholder");
        dt = 0f;
        time = 0.5f;
        currentRotAngle = ScanLookAngle;
        dir = -1f;
        mustClear = false;
        RingsDistanceMarks = new List<Text>();
        GridDistanceMarks = new List<Text>();
        Zoom_RingsDistanceMarks = new List<Text>();
        Zoom_GridDistanceMarks = new List<Text>();

        MarksGoList = new List<Image>();
        Zoom_MarksGoList = new List<Image>();
        ClearDMarks();
    }

    protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
    {
        if (!bakingCamera || !shipTransform || !terrainOverrideMaterial || !overrideMaterial || !DistanceTexture || !Camera_UI)
            return;

        if (camera.camera.cameraType == CameraType.SceneView)
            return;

        if (camera.camera != Camera_UI) return;
        if (IsNextScanSectorAvaliable(out float angle))
            currentRotAngle = angle;
        else
            return;
        if (mustClear == true)
            CoreUtils.SetRenderTarget(cmd, distribution_Raw, ClearFlag.All, clearColor: Color.black);
        //Debug.Log("GKORRunnig2");


        var HorisontalFoV = 360f / CamerasCount;
        camera_width = HorisontalFoV / 360f;
        camera_x = Mathf.Clamp01(Mathf.Ceil(currentRotAngle / HorisontalFoV) / CamerasCount - camera_width);

        AllocateTexturesIfNeeded();

        ///////////----Render Objects With Override Materials
        CoreUtils.SetRenderTarget(cmd, DistanceTexture, ClearFlag.All, clearColor: Color.black);
        RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, DistanceTexture);

        ///////////----Render Depth To Planar Texture
        SetVar_DistributionComputer(cmd);

        
        
        //Clear Distance Buffer
        cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", distanceBuffer);
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count + 1023) / 1024, 1, 1);

        //Render Noise To "distribution_Raw_Noise" texture
        CoreUtils.SetRenderTarget(cmd, distribution_Raw_Noise, ClearFlag.All, clearColor: Color.black);
        cmd.SetComputeBufferParam(DistributionComputer, handleNoiseDistributionMain, "DistanceBuffer", distanceBuffer);
        cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Source", DistanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Destination", distribution_Raw_Noise);

        cmd.DispatchCompute(DistributionComputer, handleNoiseDistributionMain, (DistanceTexture.width + 15) / 16,
              (DistanceTexture.height + 15) / 16, 1);



        //cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", distanceBuffer);
        
        //Clear Distance Buffer to write to it again
        cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (distanceBuffer.count + 1023) / 1024, 1, 1);

        //Render Distance Distribution To "distribution_Raw" Texture
        CoreUtils.SetRenderTarget(cmd, distribution_Raw, ClearFlag.All, clearColor: Color.black);
        cmd.SetComputeBufferParam(DistributionComputer, handleDistributionMain, "DistanceBuffer", distanceBuffer);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Source", DistanceTexture);
        cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Destination", distribution_Raw);
        
        cmd.DispatchCompute(DistributionComputer, handleDistributionMain, (DistanceTexture.width + 15) / 16,
               (DistanceTexture.height + 15) / 16, 1);
        


        ///////////----Re-Render Depth To Stable Planar Texture
        
        //Set Shader Variables
        SetVar_StepAngleParams(cmd);
        SetVar_ScanDisplayParams(cmd);
        SetVar_SonarGridAndColor(cmd);

        //Combine Noise and Distribution textures together --> рендерит не все изображение, а полоску шириной "DiscreteAngleStep" в новую текстуру
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Noise", distribution_Raw_Noise);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Source", distribution_Raw);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Destination", distribution_Fixed);
        cmd.DispatchCompute(InterpolationComputer, handleRemap, (distribution_Fixed.referenceSize.x + 15) / 16,
               (distribution_Fixed.referenceSize.y + 15) / 16, 1);

        //Интерполирует изображение, удаляет лишнее когда ScanZoneWideness < 360.
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemapInterpolate, "Source", distribution_Fixed);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRemapInterpolate, "Destination", distribution_Interpolated_On_Quad);
        cmd.DispatchCompute(InterpolationComputer, handleRemapInterpolate, (distribution_Interpolated_On_Quad.referenceSize.x + 15) / 16,
               (distribution_Interpolated_On_Quad.referenceSize.y + 15) / 16, 1);


        ////////////----Render To Final Texture with Grid
        CoreUtils.SetRenderTarget(cmd, Result, ClearFlag.Color, clearColor: Color.clear);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderInPolar, "Source", distribution_Interpolated_On_Quad);
        cmd.SetComputeTextureParam(InterpolationComputer, handleRenderInPolar, "Destination", Result);
        cmd.DispatchCompute(InterpolationComputer, handleRenderInPolar, (ResultDimensions.x + 31) / 32,
               (ResultDimensions.y + 31) / 32, 1);


        if (AcousticZoom)
        {
            CoreUtils.SetRenderTarget(cmd, Zoom_DistanceTexture, ClearFlag.All, clearColor: Color.black);
            RenderFromCamera(renderContext, cmd, cullingResult, bakingCamera, Zoom_DistanceTexture);

            ///////////----Render Depth To Planar Texture
            cmd.SetComputeVectorParam(InterpolationComputer, "OriginTexWH", (Vector2)Result.referenceSize);

            CoreUtils.SetRenderTarget(cmd, Zoom_distribution_Raw_Noise, ClearFlag.All, clearColor: Color.black);

            cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", Zoom_distanceBuffer);
            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (Zoom_distanceBuffer.count + 1023) / 1024, 1, 1);

            cmd.SetComputeBufferParam(DistributionComputer, handleNoiseDistributionMain, "DistanceBuffer", Zoom_distanceBuffer);
            cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Source", Zoom_DistanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleNoiseDistributionMain, "Destination", Zoom_distribution_Raw_Noise);
            cmd.DispatchCompute(DistributionComputer, handleNoiseDistributionMain, (Zoom_DistanceTexture.width + 15) / 16,
                    (Zoom_DistanceTexture.height + 15) / 16, 1);



            cmd.SetComputeBufferParam(DistributionComputer, handleClearBuffer, "DistanceBuffer", Zoom_distanceBuffer);
            cmd.DispatchCompute(DistributionComputer, handleClearBuffer, (Zoom_distanceBuffer.count + 1023) / 1024, 1, 1);

            cmd.SetComputeBufferParam(DistributionComputer, handleDistributionMain, "DistanceBuffer", Zoom_distanceBuffer);

            CoreUtils.SetRenderTarget(cmd, Zoom_distribution_Raw, ClearFlag.All, clearColor: Color.black);

            cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Source", Zoom_DistanceTexture);
            cmd.SetComputeTextureParam(DistributionComputer, handleDistributionMain, "Destination", Zoom_distribution_Raw);

            cmd.DispatchCompute(DistributionComputer, handleDistributionMain, (Zoom_DistanceTexture.width + 15) / 16,
                    (Zoom_DistanceTexture.height + 15) / 16, 1);


            cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Noise", Zoom_distribution_Raw_Noise);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Source", Zoom_distribution_Raw);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRemap, "Destination", Zoom_distribution_Fixed);
            cmd.DispatchCompute(InterpolationComputer, handleRemap, (Zoom_distribution_Fixed.referenceSize.x + 15) / 16,
                   (Zoom_distribution_Fixed.referenceSize.y + 15) / 16, 1);

            cmd.SetComputeTextureParam(InterpolationComputer, handleRemapInterpolate, "Source", Zoom_distribution_Fixed);
            cmd.SetComputeTextureParam(InterpolationComputer, handleRemapInterpolate, "Destination", Zoom_distribution_Interpolated_On_Quad);
            cmd.DispatchCompute(InterpolationComputer, handleRemapInterpolate, (Zoom_distribution_Interpolated_On_Quad.referenceSize.x + 15) / 16,
                    (Zoom_distribution_Interpolated_On_Quad.referenceSize.y + 15) / 16, 1);

            ////////////----Render Grid On Zoom Texture
            CoreUtils.SetRenderTarget(cmd, Zoom_Result, ClearFlag.Color, clearColor: Color.clear);
            cmd.SetComputeTextureParam(InterpolationComputer, handle_Zoom_RenderInPolar, "Source", Zoom_distribution_Interpolated_On_Quad);
            cmd.SetComputeTextureParam(InterpolationComputer, handle_Zoom_RenderInPolar, "Destination", Zoom_Result);
            cmd.DispatchCompute(InterpolationComputer, handle_Zoom_RenderInPolar, (ZoomWindowRect.x + 31) / 32,
                    (ZoomWindowRect.y + 31) / 32, 1);
        }
        DrawMarksOnUI();
        DrawDigits();
        if (ResultSonarImage != null)
        {

            ResultSonarImage.texture = Result;
            ResultSonarImage.SetNativeSize();
        }

        if (ZoomResultImage != null && Zoom_Result != null && AcousticZoom)
        {
            ZoomResultImage.gameObject.SetActive(true);
            ZoomResultImage.texture = Zoom_Result;
            ZoomResultImage.SetNativeSize();
        }
        else
            ZoomResultImage.gameObject.SetActive(false);
        
          /*
        if (distance_img != null)
            distance_img.texture = DistanceTexture;
        
        if (Test_img != null)
            Test_img.texture = Zoom_DistanceTexture;
        if (Test_img1 != null)
            Test_img1.texture = distribution_Raw;*/
    }

    float prevt=0;
    private bool IsNextScanSectorAvaliable(out float angle_rounded)
    {
        mustClear = false;
        float angleStep = 360f / _Resolution;
        float _angle;
        float t_min = angleStep / 360f;
        //dt += dir * Time.deltaTime/_Resolution;
        dt = -prevt + Time.timeSinceLevelLoad;
        
        dt = dt > t_min ? t_min : dt; 
        prevt = Time.timeSinceLevelLoad;
        time +=dir*dt*angleStep/ ScanZoneWideness*ScanSpeed; ;


        if (ScanZoneWideness == 360f)
        {
            dir = 1;
            time = Mathf.Abs(time) > 1 ? time = 0 : time;


            _angle = ScanZoneWideness * time;

            angle_rounded = Mathf.Ceil(_angle / angleStep) * angleStep;

        }
        else
        {
            time = Mathf.Clamp(time, 0.01f, 0.99f);
            _angle = ScanZoneWideness * time + ScanLookAngle - ScanZoneWideness / 2;
            angle_rounded = dir == 1f ? Mathf.Ceil(_angle / angleStep) * angleStep : (Mathf.Floor(_angle / angleStep) * angleStep);
            if (time <= 0.01f || time >= 0.99f)
            {
                dir *= -1;
                angle_rounded = dir == 1f ? Mathf.Ceil(_angle / angleStep) * angleStep : (Mathf.Floor(_angle / angleStep) * angleStep);
                if (angle_rounded >= 360f && dir == -1) angle_rounded = angle_rounded - 360f;
                if (angle_rounded < 0f && dir == -1) angle_rounded = angle_rounded + 360f;
                if (angle_rounded > 360f && dir == 1) angle_rounded = angle_rounded - 360f;
                if (angle_rounded <= 0f && dir == 1) angle_rounded = angle_rounded + 360f;


                mustClear = true;
                return true;
            }
        }

        if (angle_rounded >= 360f && dir == -1) angle_rounded = angle_rounded - 360f;
        if (angle_rounded < 0f && dir == -1) angle_rounded = angle_rounded + 360f;
        if (angle_rounded > 360f && dir == 1) angle_rounded = angle_rounded - 360f;
        if (angle_rounded <= 0f && dir == 1) angle_rounded = angle_rounded + 360f;

        if (time == 0)
        {
            mustClear = true;
            return true;
        }
        return angle_rounded != currentRotAngle;
    }

    private void SetVar_DistributionComputer(CommandBuffer cmd)
    {
        
        cmd.SetComputeVectorParam(DistributionComputer, "WorldPosRot", new Vector4(shipTransform.position.x,
                                                                                              shipTransform.position.y,
                                                                                             shipTransform.position.z,
                                                                                              shipTransform.localEulerAngles.y));
        cmd.SetComputeFloatParam(DistributionComputer, "MaxDistance", MaxScanRange);
        cmd.SetComputeVectorParam(DistributionComputer, "Noise12_Scale_Bias", new Vector4(Noise1_Scale, Noise2_Scale,Noise1_Bias,Noise2_Bias));
        cmd.SetComputeVectorParam(DistributionComputer, "Noise_Refl_Min_Max", new Vector2(Noise_Reflectivity_Min, Noise_Reflectivity_Max));
        cmd.SetComputeFloatParam(DistributionComputer, "Noise_Gain_Mult", Noise_Gain_Multiplier);
        cmd.SetComputeFloatParam(DistributionComputer, "NoisePatternScroll", NoisePatternScroll);
        cmd.SetComputeFloatParam(DistributionComputer, "BlindZoneRadius", BlindZoneRadius / MaxScanRange);
        cmd.SetComputeFloatParam(DistributionComputer, "Gain", Gain);

        cmd.SetComputeFloatParam(DistributionComputer, "CurrentRotAngle", currentRotAngle);
        cmd.SetComputeFloatParam(DistributionComputer, "DiscreteAngleStep", dir * 360f / _Resolution);

        cmd.SetComputeFloatParam(DistributionComputer, "uv_X", camera_x);
        cmd.SetComputeFloatParam(DistributionComputer, "uv_W", camera_width);



    }

    private void SetVar_ScanDisplayParams(CommandBuffer cmd)
    {
        var offset = (Vector2)CentrePercent * 0.01f;
        var scale = 2f * RadiusPercent / 100f;

        cmd.SetComputeVectorParam(InterpolationComputer, "Offset", offset);
        cmd.SetComputeFloatParam(InterpolationComputer, "Scale_factor", scale);

        cmd.SetComputeVectorParam(InterpolationComputer, "MouseCoords", MouseCoords);
        cmd.SetComputeVectorParam(InterpolationComputer, "ZoomBox", (Vector2)ZoomWindowRect);
        cmd.SetComputeVectorParam(InterpolationComputer, "ZoomParams", (Vector2)ZoomParams);
    }

    private void SetVar_StepAngleParams(CommandBuffer cmd)
    {
        cmd.SetComputeFloatParam(InterpolationComputer, "CurrentRotAngle", currentRotAngle);
        cmd.SetComputeFloatParam(InterpolationComputer, "DiscreteAngleStep", dir * 360f / _Resolution);

        cmd.SetComputeFloatParam(InterpolationComputer, "ScanLookAngle", ScanLookAngle);
        cmd.SetComputeFloatParam(InterpolationComputer, "ScanZoneWideness", ScanZoneWideness);
    }

    private void SetVar_SonarGridAndColor(CommandBuffer cmd)
    {

        if (Interpolation)
            cmd.SetComputeFloatParam(InterpolationComputer, "Interpolation", 1);
        else
            cmd.SetComputeFloatParam(InterpolationComputer, "Interpolation", 0);

        cmd.SetComputeIntParam(InterpolationComputer, "CrossCount", ShowCross == true ? 2 : 0);
        cmd.SetComputeIntParam(InterpolationComputer, "GridCount", ShowGrid == true ? 2 : 0);
        cmd.SetComputeIntParam(InterpolationComputer, "Rings", Rings);
        cmd.SetComputeIntParam(InterpolationComputer, "Sector1_angle", Sector1_angle);
        cmd.SetComputeIntParam(InterpolationComputer, "Sector2_angle", Sector2_angle);

        cmd.SetComputeFloatParam(InterpolationComputer, "ApplyZoom", AcousticZoom == true ? 1f : 0);
        cmd.SetComputeFloatParam(InterpolationComputer, "ShowRings", ShowRings == true ? 1f : 0);
        cmd.SetComputeFloatParam(InterpolationComputer, "ShowSector", ShowSector == true ? 1f : 0);
        cmd.SetComputeFloatParam(InterpolationComputer, "MaxDistanceMark", bakingCamera.farClipPlane / cosL);

        cmd.SetComputeFloatParam(InterpolationComputer, "Thickness", LinesThickness / 1000f);
        cmd.SetComputeIntParam(InterpolationComputer, "FontSize", FontSize);
        cmd.SetComputeFloatParam(InterpolationComputer, "MaxDistanceMark", bakingCamera.farClipPlane / cosL);

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
        cmd.SetComputeVectorParam(InterpolationComputer, "_BackgroundColor", BackgroundColor);
        cmd.SetComputeVectorParam(InterpolationComputer, "_GridColor", GridColor);

    }

    private float FieldOfViewToSensorSize(float fieldOfView, float focalL)
    {
        return Mathf.Tan(fieldOfView * Mathf.PI / 360.0f) * 2.0f * focalL;
    }
    private void RenderFromCamera(ScriptableRenderContext renderContext, CommandBuffer cmd, CullingResults cullingResult, Camera view, RenderTexture target_RT)
    {
        view.targetTexture = target_RT;
        view.TryGetCullingParameters(out var cullingParams);
        cullingParams.cullingOptions = CullingOptions.ShadowCasters;
        cullingResult = renderContext.Cull(ref cullingParams);
        
        bakingCamera.farClipPlane = MaxScanRange;
        bakingCamera.focalLength = 57f;

        
        float _horisontalFieldOfView = FieldOfViewToSensorSize(camera_width*360f, bakingCamera.focalLength);
        bakingCamera.transform.eulerAngles = new Vector3(0f, (camera_width / 2f + camera_x) *360f, 0f);
        float _verticalFieldOfView = FieldOfViewToSensorSize(VerticalFieldOfView, bakingCamera.focalLength);
       
        bakingCamera.sensorSize = new Vector2(_horisontalFieldOfView, _verticalFieldOfView);
        bakingCamera.gateFit = Camera.GateFitMode.None;

        SetCameraMatrices(cmd);

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
        //Если нет какой-то текстуры или во время игры сменилось разрешение, то метод пересоздает текстуры.
        var zoom_Resolution = _Resolution*ZoomParams.y;
        aspect = Mathf.FloorToInt(bakingCamera.sensorSize.x / bakingCamera.sensorSize.y);
        
        if (Result == null || ResultDimensions != Result.referenceSize && ResultDimensions != Vector2Int.zero)
        {
            Result?.Release();
            Result = RTHandles.Alloc(
                ResultDimensions.x, ResultDimensions.y, dimension: TextureDimension.Tex2D, colorFormat: Vector4Format,
                name: "Result", enableRandomWrite: true
            );

            distribution_Interpolated_On_Quad = RTHandles.Alloc(
                1366, 768, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
                name: "Distribution Interpolated", enableRandomWrite: true);
        }

        if (_Resolution != 0 && distribution_Raw == null || _Resolution != distribution_Raw.referenceSize.x )
        {
            distanceBuffer?.Dispose();
            distanceBuffer = new ComputeBuffer(_Resolution * _Resolution, sizeof(float), ComputeBufferType.Default);

            distribution_Raw?.Release();
            distribution_Raw = RTHandles.Alloc(
            _Resolution, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: VectorFormat,
            name: "Distribution Raw", enableRandomWrite: true
            );
            distribution_Raw_Noise?.Release();
            distribution_Raw_Noise = RTHandles.Alloc(
            _Resolution, _Resolution, dimension: TextureDimension.Tex2D,
            colorFormat: VectorFormat,
            name: "Distribution Raw Noise", enableRandomWrite: true
            );
            distribution_Fixed?.Release();
            distribution_Fixed = RTHandles.Alloc(
               _Resolution, _Resolution, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
               name: "Distribution Fixed", enableRandomWrite: true, autoGenerateMips: false);


            DistanceTexture?.Release();
            DistanceTexture = new RenderTexture(_Resolution * aspect, _Resolution, 16, Vector4Format);
            DistanceTexture.filterMode = FilterMode.Point;
            DistanceTexture.name = "DistanceTexture";

        }

        if (AcousticZoom)
        {
            if ((Zoom_distribution_Raw == null || zoom_Resolution != Zoom_distribution_Raw.referenceSize.x) && _Resolution != 0 && ZoomParams.y != 0)
            {
                if (Zoom_DistanceTexture != null) Zoom_DistanceTexture.Release();
                Zoom_DistanceTexture = new RenderTexture(zoom_Resolution * aspect, zoom_Resolution, 16, Vector4Format);
                Zoom_DistanceTexture.filterMode = FilterMode.Point;
                Zoom_DistanceTexture.name = "Zoom_DistanceTexture";

                Zoom_distanceBuffer?.Dispose();
                Zoom_distanceBuffer = new ComputeBuffer(zoom_Resolution* zoom_Resolution, sizeof(float), ComputeBufferType.Default);

                Zoom_distribution_Raw?.Release();
                Zoom_distribution_Raw = RTHandles.Alloc(
                    zoom_Resolution, zoom_Resolution, dimension: TextureDimension.Tex2D,
                    colorFormat: VectorFormat,
                    name: "Zoom Distribution Raw", enableRandomWrite: true
                );

                Zoom_distribution_Raw_Noise?.Release();
                Zoom_distribution_Raw_Noise = RTHandles.Alloc(
                    zoom_Resolution, zoom_Resolution, dimension: TextureDimension.Tex2D,
                    colorFormat: VectorFormat,
                    name: "Zoom Distribution Raw Noise", enableRandomWrite: true
                );
                Zoom_distribution_Fixed?.Release();
                Zoom_distribution_Fixed = RTHandles.Alloc(
                   zoom_Resolution, zoom_Resolution, dimension: TextureDimension.Tex2D, colorFormat: VectorFormat,
                   name: "Zoom_Distribution Fixed", enableRandomWrite: true, autoGenerateMips: false);

            }
            if (Zoom_distribution_Interpolated_On_Quad == null)
            {
                Zoom_distribution_Interpolated_On_Quad?.Release();
                Zoom_distribution_Interpolated_On_Quad = RTHandles.Alloc(
                1366, 768, dimension: TextureDimension.Tex2D,
                colorFormat: VectorFormat,
                name: "Zoom Distribution Interpolated", enableRandomWrite: true);
            }
            if ((Zoom_Result == null || ZoomWindowRect != Zoom_Result.referenceSize) && ZoomWindowRect != Vector2Int.zero)
            {
                Zoom_Result?.Release();
                Zoom_Result = RTHandles.Alloc(
                    ZoomWindowRect.x, ZoomWindowRect.y, dimension: TextureDimension.Tex2D, colorFormat: Vector4Format,
                    name: "zoom_Result", enableRandomWrite: true
                );
            }
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
            var go = new GameObject("RingMark " + i);
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
        for (int i = 0; i < size + 2; i++)
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
        for (int i = 0; i < size + 2; i++)
        {
            var go = new GameObject("GridMark " + i);
            var tmp = go.AddComponent<Text>() as Text;
            Zoom_GridDistanceMarks.Add(tmp);
            go.transform.SetParent(zoom_text_marks_placeholder);
            go.transform.localScale = Vector3.one;
            tmp.alignment = TextAnchor.MiddleCenter;
            go.SetActive(false);
        }
    }
    
    private void DrawDigits()
    {
        var resRect = new Vector2(ResultSonarImage.rectTransform.rect.width, ResultSonarImage.rectTransform.rect.height);
        var rScale = RadiusPercent / 100f * 2;

        float norm = Mathf.Lerp(resRect.y / resRect.x, 1f, 0f);
        int vFontSize = (int)(FontSize);
        var min = Mathf.Min(resRect.x, resRect.y);
        Vector2 center = new Vector2(resRect.x, resRect.y) * (Vector2)CentrePercent / 100f;

        if (RingsDistanceMarks.Count != Rings * 4 || RingsDistanceMarks == null || GridDistanceMarks == null)
        {
            ClearDMarks();
            CreateTextRingsMarks(Rings * 4);
            CreateTextGridMarks(Rings * 4);
        }
        var w = RingsDistanceMarks[0].rectTransform.rect.width / 5f;
        var maxD = bakingCamera.farClipPlane;



        void setMark(List<Text> marksList, int index, Vector2 c, Vector2 v, float offset1, float offset2, float scale)
        {
            float i = index % Rings + 1;
            var dCoord = c + v * (scale * min * i / Rings / 2f + offset1) + (v.x + v.y) * Vector2.Perpendicular(v) * offset2;

            marksList[index - 1].text = (Mathf.Round(2f * maxD * i / Rings) / 2f).ToString();
            marksList[index - 1].rectTransform.localPosition = dCoord;
            marksList[index - 1].font = CustomFont;
            marksList[index - 1].fontSize = vFontSize;
            marksList[index - 1].color = GridColor;
            marksList[index - 1].gameObject.SetActive(true);
        }
        void setZeroMark(List<Text> marksList, int index, Vector2 c, Vector2 v, float offset1, float offset2)
        {
            var dCoord = c + v * offset1 + (v.x + v.y) * Vector2.Perpendicular(v) * offset2;
            dCoord.x = Mathf.Clamp(dCoord.x, -resRect.x / 2 - offset1, resRect.x / 2 + offset1);
            dCoord.y = Mathf.Clamp(dCoord.y, -resRect.y / 2 - offset1, resRect.y / 2 + offset1);

            marksList[marksList.Count - index - 1].text = "0";
            marksList[marksList.Count - index - 1].rectTransform.localPosition = dCoord;
            marksList[marksList.Count - index - 1].fontSize = vFontSize;
            marksList[marksList.Count - index - 1].color = GridColor;
            marksList[marksList.Count - index - 1].gameObject.SetActive(true);
        }

        void Draw(List<Text> ringsMarks, List<Text> gridMarks, Vector2 resWH, Vector2 Cent, float z)
        {
            if ((ShowRings|| ShowSector) == true && Rings != 0 && ShowGrid == false && ShowDigits)
            {
                for (int k = 1; k <= Rings; k++)
                {
                    //Horisontal+
                    setMark(ringsMarks, k, Cent, Vector2.right, -w, -w / 2, z);
                    //Horisontal-
                    setMark(ringsMarks, k + Rings, Cent, Vector2.left, -w, -w / 2, z);
                    //Vertical+
                    setMark(ringsMarks, k + 2 * Rings, Cent, Vector2.up, -w + w / 3, -w, z);
                    //Vertical-
                    setMark(ringsMarks, k + 3 * Rings, Cent, Vector2.down, -w + w / 3, -w, z);
                }
            }
            else
            {
                for (int k = 0; k < 4*Rings; k++)
                {
                    ringsMarks[k].gameObject.SetActive(false);
                }
            }
            if (ShowGrid == true && Rings != 0 && ShowDigits)
            {
                setZeroMark(gridMarks, 0, new Vector2(Cent.x, resWH.y / 2f), Vector2.left, -w / 2, -w);
                setZeroMark(gridMarks, 1, new Vector2(-resWH.x / 2f, Cent.y), Vector2.up, -w / 2, -w);

                for (int k = 1; k <= Rings; k++)
                {
                    //Horisontal+
                    setMark(gridMarks, k, new Vector2(Cent.x, resWH.y / 2f), Vector2.right, -w, -w, z);
                    //Horisontal-
                    setMark(gridMarks, k + Rings, new Vector2(Cent.x, resWH.y / 2f), Vector2.left, -w, -w, z);
                    //Vertical+
                    setMark(gridMarks, k + 2 * Rings, new Vector2(-resWH.x / 2f, Cent.y), Vector2.up, -w / 2, -w, z);
                    //Vertical-
                    setMark(gridMarks, k + 3 * Rings, new Vector2(-resWH.x / 2f, Cent.y), Vector2.down, -w / 2, -w, z);
                }
            }
            else
            {
                for (int k = 0; k < 4*Rings+2; k++)
                {
                    gridMarks[k].gameObject.SetActive(false);
                }
            }
        }
        Draw(RingsDistanceMarks, GridDistanceMarks, resRect, center, rScale);


        var zoomRect = new Vector2(ZoomResultImage.rectTransform.rect.width, ZoomResultImage.rectTransform.rect.height);

        var Boxcenter = (MouseCoords - Vector2.one / 2f) * resRect;
        float zoom = ZoomParams.x / 100f * 2f;
        Vector2 relUV = (center - Boxcenter) / zoom;
        Draw(Zoom_RingsDistanceMarks, Zoom_GridDistanceMarks, zoomRect, relUV, rScale * 1f / zoom);
    }
    private void ClearDMarks()
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
            RingsDistanceMarks.Clear();
            GridDistanceMarks.Clear();
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

    private void DrawMarksOnUI()
    {
        if (MarksGoList ==null ||  MarksGoList.Count != MarksList.Count || MarksGoList.Contains(null))
        {
            ClearMarksOnUI();
            CreateMarksOnUI(MarksList.Count);
        }
        //
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
            //
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
    protected override void Cleanup()
    {
        // Cleanup code

        distanceBuffer?.Dispose();
        DistanceTexture?.Release();
        distribution_Raw?.Release();
        distribution_Interpolated_On_Quad?.Release();
        Result?.Release();

        Zoom_distanceBuffer?.Dispose();
        if (Zoom_DistanceTexture!=null)
            Zoom_DistanceTexture?.Release();
        Zoom_distribution_Raw?.Release();
        Zoom_distribution_Interpolated_On_Quad?.Release();
        Zoom_Result?.Release();

        ClearDMarks();
    }


}
