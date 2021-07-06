	Shader "Renderers/SBP__DistanceShader"
{
    Properties
    {
		Reflectivity("Reflectivity", Range(0.0, 1.0))=1
		Wideness("Wideness", Range(0.0, 1.0)) = 0.1
		Bezier1("Bezier1", Range(0.0, 1.0)) = 1
    }

    HLSLINCLUDE
			// List all the attributes needed in your shader (will be passed to the vertex shader)
			 // you can see the complete list of these attributes in VaryingMesh.hlsl
	#define ATTRIBUTES_NEED_TEXCOORD0
	#define ATTRIBUTES_NEED_NORMAL
	#define ATTRIBUTES_NEED_TANGENT

	// List all the varyings needed in your fragment shader
	#define VARYINGS_NEED_TEXCOORD0
	#define VARYINGS_NEED_POSITION_WS
	#define VARYINGS_NEED_TANGENT_TO_WORLD

	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"

    #pragma target 5.0
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    // #pragma enable_d3d11_debug_symbols

    //enable GPU instancing support
    #pragma multi_compile_instancing

	float fowCutOut;
	sampler2D sampler_DistanceTex;
		float CalculateDistanceInFoW(float3 worldPos)
		{
			float3 positionVS = mul(_ViewMatrix, float4(worldPos, 1)).xyz;
			float ViewProjection = abs((normalize(positionVS).z));
			float Distance = saturate(sqrt(dot(positionVS, positionVS))*(_ProjectionParams.w));
		
			return Distance;
		}


		float rand(float2 co)
		{
			float a = 12.9898;
			float b = 78.233;
			float c = 43758.5453;
			float dt = dot(co.xy, float2(a, b));
			float sn = dt % 3.14;
			return frac(sin(sn) * c);
		}

		float smoothNoise(float2 p) {

			float2 i = floor(p); p -= i; p *= p * (3. - p - p);
			float2 m1 = mul(float2x2(frac(sin(float4(0, 1, 27, 28) + i.x + i.y*27.) * 100000.)), float2(1. - p.y, p.y));
			float2 m2 = float2(1. - p.x, p.x);
			return dot(m1, m2);
		}
	
    ENDHLSL

    SubShader
    {


		Pass
		{
			Name "SecondPass"
			Tags { "LightMode" = "SecondPass" }

			Blend Off
			ZWrite Off
			ZTest LEqual

			Cull Back

			HLSLPROGRAM

			// Toggle the alpha test
			#define _ALPHATEST_ON

			// Toggle transparency
			// #define _SURFACE_TYPE_TRANSPARENT

			// Toggle fog on transparent
			#define _ENABLE_FOG_ON_TRANSPARENT


			texture2D<float3> _DistanceTex;
			float MaxScanRange;
			float ShowSBSolid;
			float Reflectivity, Wideness, Bezier1, Bezier2;
			
			void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
			{
				// Write back the data to the output structures
				ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
				ZERO_INITIALIZE(SurfaceData, surfaceData);

				float Distance = CalculateDistanceInFoW(fragInputs.positionRWS);

				//(r,g,b)->(Distance,Reflectivity,Wideness)
				float3 SampledTerrainData = _DistanceTex.Sample(s_trilinear_clamp_sampler, fragInputs.positionSS.xy / 720).rgb;

				//Perform depth test
				float deltaVal = Distance - SampledTerrainData.r;
				deltaVal = clamp(deltaVal * step(-deltaVal, 0), 0, 1);

				float delta = clamp(-sign(Distance - SampledTerrainData.r), 0, 1);
				float seed = smoothNoise(fragInputs.positionSS.xy / 720 * 50);
				float clip = lerp(seed*(1 - ShowSBSolid), 1, delta)* step(deltaVal, SampledTerrainData.b / MaxScanRange);
				
				//Check If object is below the terrain  //kind of Depth test
				float isTerrain = step(clip, 0.5);

				float3 objCol = float3(Distance, Reflectivity , Wideness);
				float3 terCol = float3(SampledTerrainData.r, SampledTerrainData.g , min(deltaVal*MaxScanRange, SampledTerrainData.b));
				float3 col = lerp(objCol, terCol, isTerrain);

				surfaceData.color = col;
				builtinData.opacity = Bezier1;
			}

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"

			#pragma vertex Vert
			#pragma fragment Frag

			ENDHLSL
			}
    }
}
