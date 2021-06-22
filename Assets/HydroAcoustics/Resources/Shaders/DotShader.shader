Shader "Unlit/DotShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
			#pragma geometry geo
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
			uniform StructuredBuffer<float4> buffer;       
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4x4 _CameraViewProjMatrix;
			float4x4 _CameraViewMatrix;
			float4x4 _CameraProjMatrix;
			float4x4 _CameraInvProjMatrix;
			float4 _PrParams;
			float2 SurfaceLevels;
			float r(float2 co)
			{
				float a = 12.9898;
				float b = 78.233;
				float c = 43758.5453;
				float dt = dot(co.xy, float2(a, b));
				float sn = dt % 3.14;
				return frac(sin(sn) * c);
			}

			float remap(float a, float b, float c, float d, float x) {
				float t = (x - c) / (d - c);
				return lerp(a, b, t);
			}

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				float dist: TEXCOORD2;
				float col:COLOR;
			};
			
			struct v2g {
				float4 vertex : SV_POSITION;
				float dist : TEXCOORD2;
				float col : COLOR;
			};
			struct g2f {
				float4 vertex : SV_POSITION;
				float col : COLOR;
			};


            v2g vert (uint id : SV_VertexID)
            {

				float4 pos = float4(buffer[id].xyz, 1);
                
				v2g o;
				//o.vertex = mul(_CameraViewProjMatrix, pos);
				o.vertex = pos;
				float4 vPos = mul(_CameraViewMatrix, pos);
				float dist = length(vPos.xyz)*_PrParams.w;
				o.dist = dist;


				float s = remap(0, 1, SurfaceLevels.x, SurfaceLevels.y, pos.y);
				o.col = s;
				return o;
            }

			[maxvertexcount(27)]
			void geo(point v2g  IN[1], inout TriangleStream<g2f> triStream)
			{
				g2f o;
				//float4 pos = IN[0].vertex + IN[0].vertex *0.01* r(IN[0].vertex.xy);
				//o.vertex = mul(_CameraViewProjMatrix, IN[0].vertex);
				//o.col = IN[0].col;
				//pointStream.Append(o);
				float4 p = IN[0].vertex;
				float k = (1 - IN[0].dist);
				float4 viewP = mul(_CameraViewMatrix, p);
				float size = lerp(0.04, 0.1, k);
				for (int i = 0; i <= 10*k; i++)
				{
					float3 rand = float3(r(p.xy * 5 + i), r(p.yz * 12 + i), r(p.xy*i + 2));
					float4 centPos = viewP + float4(rand, 0);

					for (int k = 0; k < 3; k++) 
					{
						float sn, cs;
						sincos(2*UNITY_PI * k /3 , sn, cs);
						float4 v = centPos + size*float4(sn,cs,0,0 );
						o.vertex = mul(_CameraProjMatrix, v);
						o.col = IN[0].col;
						triStream.Append(o);
					}
					triStream.RestartStrip();
					
				}
			}
            
			fixed4 frag (g2f i) : SV_Target
            {

				float3 BotCol = float3(0.1,0.8,0.1);
				float3 MidCol = float3(1, 1, 0);
				float3 UpCol = float3(1, 0, 0);
                // sample the texture
				float middle=0.5;
				float factor = i.col;
				float3 col = lerp(BotCol, MidCol, clamp((factor) / middle, 0, 1))*step(factor,middle) +lerp(MidCol, UpCol, clamp((factor - middle) / middle, 0, 1))*step(middle, factor);
				//col = i.col;
                // apply fog
                return float4(col,1);
            }
            ENDCG
        }
    }
}






