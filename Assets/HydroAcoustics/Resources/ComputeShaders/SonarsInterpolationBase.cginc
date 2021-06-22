
float4 GetDimensionsFrom(in Texture2D tex) {
	float2 a;
	tex.GetDimensions(a.x, a.y);
	return float4(1 / a.x, 1 / a.y, a.x, a.y);
}

float remap(float a, float b, float c, float d, float x) {
	float t = (x - c) / (d - c);
	return lerp(a, b, t);
}


float2 GetDestUV(float2 coord, float4 s_dim, float4 d_dim) {
	return coord * s_dim.zw* d_dim.xy;
}

float2 UVFromPolar(float2 cosin, float radius) {
	float2 uv = float2(cosin.x*radius, cosin.y*radius);
	uv /= 2;
	uv += 0.5;
	return uv;
}

float2 rotateUV(float2 uv, float degrees)
{
	const float UNITY_PI = 3.1415;
	const float Deg2Rad = (UNITY_PI * 2.0) / 360.0;
	float rotationRadians = degrees * Deg2Rad;
	float s = sin(rotationRadians);
	float c = cos(rotationRadians);
	float2x2 rotationMatrix = float2x2(c, -s, s, c);
	uv -= 0.5;
	uv = mul(rotationMatrix, uv);
	uv += 0.5;
	return uv;
}

float2 Scale_UV(float2 uv, float2 offset, float scale, float4 d_dim) {
	uv -= offset * d_dim.zw;
	uv /= scale;
	return uv;
}

void GetAngleCoordAndRadiusUV(float2 uv, out float  r, out float  a)
{
	r = length(uv) * 2;
	a = atan2(-uv.y, uv.x)* 57.2958 + 180;// in degrees
}

//Line SDF//
float LineSegment(float2 p, float2 a, float2 b) {
	float2 pa = p - a;
	float2 ba = b - a;
	float h = clamp(dot(pa, ba) / dot(ba, ba), 0, 1);
	return length(pa - ba * h);
}
float LineSharpen(float d, float w, float2 res) {
	float e = 1 / min(res.x, res.y);
	return 1 - smoothstep(-e, e, d - w);
}
//-----//

//Box SDF//
float sdBox(float2 pos, float2 b, float r)
{
	float2 d = abs(pos) - b;
	float l = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
	return clamp(r - abs(l), 0, 1);
}

//Digit Drawing in Shader//
float DigitBin(const int x)
{
	return x == 0 ? 480599.0 : x == 1 ? 139810.0 : x == 2 ? 476951.0 : x == 3 ? 476999.0 : x == 4 ? 350020.0 : x == 5 ? 464711.0 : x == 6 ? 464727.0 : x == 7 ? 476228.0 : x == 8 ? 481111.0 : x == 9 ? 481095.0 : 0.0;
}

float PrintValue(float2 vStringCoords, float fValue, float fMaxDigits, float fDecimalPlaces)
{
	if ((vStringCoords.y < 0.0) || (vStringCoords.y >= 1.0)) return 0.0;

	bool bNeg = (fValue < 0.0);
	fValue = abs(fValue);

	float fLog10Value = log2(abs(fValue)) / log2(10.0);
	float fBiggestIndex = max(floor(fLog10Value), 0.0);
	float fDigitIndex = fMaxDigits - floor(vStringCoords.x);
	float fCharBin = 0.0;
	if (fDigitIndex > (-fDecimalPlaces - 1.01)) {
		if (fDigitIndex > fBiggestIndex) {
			if ((bNeg) && (fDigitIndex < (fBiggestIndex + 1.5))) fCharBin = 1792.0;
		}
		else {
			if (fDigitIndex == -1.0) {
				if (fDecimalPlaces > 0.0) fCharBin = 2.0;
			}
			else {
				float fReducedRangeValue = fValue;
				if (fDigitIndex < 0.0) { fReducedRangeValue = frac(fValue); fDigitIndex += 1.0; }
				float fDigitValue = (abs(fReducedRangeValue / (pow(10.0, fDigitIndex))));
				fCharBin = DigitBin(int(floor(fDigitValue% 10.0)));
			}
		}
	}
	return floor((fCharBin / pow(2.0, floor(frac(vStringCoords.x) * 4.0) + (floor(vStringCoords.y * 5.0) * 4.0))) % 2.0);
}
//-------//

