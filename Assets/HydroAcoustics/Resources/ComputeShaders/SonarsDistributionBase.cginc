
float4 GetDimensionsFrom(in Texture2D tex) {
	float2 a;
	tex.GetDimensions(a.x, a.y);
	return float4(1 / a.x, 1 / a.y, a.x, a.y);
}

float remap(float a, float b, float c, float d, float x) {
	float t = (x - c) / (d - c);
	return lerp(a, b, t);
}

//Noise Functions//
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

float fractal(float2 p)
{
	//float r =smoothNoise(p) + smoothNoise(p*2.) + smoothNoise(p*4.) + smoothNoise(p*8.);
	//r /= 5;
	//return r;
	return smoothNoise(p)*.5333 + smoothNoise(p*2.)*.2667 + smoothNoise(p*4.)*.1333 + smoothNoise(p*8.)*.0667;
}

float warpedNoise(float2 p, float4 WorldPos) {

	float2 m = float2((WorldPos.xz / 100));//vec2(sin(iTime*0.5), cos(iTime*0.5));
	float x = fractal(p + m);
	float y = fractal(p + m.yx + x);
	float z = fractal(p - m - x + y);
	return fractal(p + float2(x, y) + float2(y, z) + float2(z, x) + length(float3(x, y, z))*0.25);

}
//-----//

//Функция распределения https://www.shadertoy.com/view/llBSWc
float bias(float x, float b) {
	b = -log2(1.0 - b);
	return 1.0 - pow(abs(1.0 - pow(x, 1. / b)), b);
}



//BezierSolving(Not In Use)//
float2 CurveSolve(float2 cp0, float2 cp1, float2 cp2, float2 cp3, float t) {
	float2 cps[4] = { cp0, cp1, cp2, cp3 };
	float2 pos = float2(0, 0);
	pos =
		pow(1.0f - t, 3.0f) * cp0 +
		3.0f * pow(1.0f - t, 2.0f) * t * cp1 +
		3.0f * (1.0f - t) * pow(t, 2.0f) * cp2 +
		pow(t, 3.0f) * cp3;

	return pos;
}

float BeizerYFromX(float xTarget, float x1, float y1, float x2, float y2) {
	float xTolerance = 0.001; //adjust as you please

	float lower = 0;
	float upper = 1;
	float percent = (upper + lower) / 2;

	//get initial x
	float x = CurveSolve(float2(0, 0), float2(x1, y1), float2(x2, y2), float2(1, 1), percent).x;

	//loop until completion
	while (abs(xTarget - x) > xTolerance) {
		if (xTarget > x)
			lower = percent;
		else
			upper = percent;

		percent = (upper + lower) / 2;
		x = CurveSolve(float2(0, 0), float2(x1, y1), float2(x2, y2), float2(1, 1), percent).x;
	}
	//we're within tolerance of the desired x value.
	//return the y value.
	return CurveSolve(float2(0, 0), float2(x1, y1), float2(x2, y2), float2(1, 1), percent).y;
};

float getRandomBeizerEase(float seed, float xNorm1, float xNorm2) {

	float Ynorm1 = sqrt(1 - xNorm1 * xNorm1);
	float Ynorm2 = sqrt(1 - xNorm2 * xNorm2);

	return BeizerYFromX(seed, xNorm1, Ynorm1, xNorm2, Ynorm2);
}
//-----//

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

float2 GetNoiseUVrelativeToWorld(float2 source_uv, float4 worldPosRot, float scale, float patternScroll) {

	float2 uv = (source_uv*scale + worldPosRot.xz / patternScroll);
	uv = rotateUV(uv, worldPosRot.w);
	return uv;
}


//Панини проекция для коррекции искажений из-за широкого угла обзора. Исправляется только х координата.//
float2 PaniniProjection(float2 V, float d, float s)
{	// src: http://tksharpless.net/vedutismo/Pannini/panini.pdf
	//
	// The Cartesian coordinates of a point on the cylinder are 
	//	: (x, y, z) = (sinPhi, tanTheta, -cosPhi)
	//
	// The distance from projection center to view plane is 	
	//	: d + 1.0f
	//
	// The distance from projection center to the parallel plane containing the cylinder point is 
	//	: d + cosPhi
	//
	// Mapping from sphere (or cylinder in this case) to plane is
	//	: h = S * sinPhi
	//	: v = S * tanTheta
	// where S = (d+1)/(d+cosPhi);
	const float XZLength = sqrt(V.x * V.x + 1.0);
	const float sinPhi = V.x / XZLength;
	const float tanTheta = V.y / XZLength;
	const float cosPhi = sqrt(1.0 - sinPhi * sinPhi);
	const float S = (d + 1.0) / (d + cosPhi);
	return S * float2(sinPhi, lerp(tanTheta, tanTheta / cosPhi, s));
}

float2 PaniniProjectionScreenPosition(float2 screenPosition, float horisontalFOV)
{
	const float fovH = horisontalFOV * (3.14159265359 / 180.);
	const float D = 3;
	const float S = 1;
	//вручную подобрал коэфициенты для углов обзора 90-120 градусов.
	const float upscale = lerp(0.65, 0.82, (horisontalFOV - 90) / 25)*step(horisontalFOV, 115) + lerp(0.82, 1.01, (horisontalFOV - 115) / 15)*step(115.1, horisontalFOV);//082
	const float2 unproject = tan(0.5f * fovH);
	const float2 project = 1.0f / unproject;

	// unproject the screenspace position, get the viewSpace xy and use it as direction
	const float2 viewDirection = screenPosition * unproject;
	const float2 paniniPosition = PaniniProjection(viewDirection, D, S);

	// project & upscale the panini position 
	return paniniPosition * project * upscale;
}
//-------//

float PaniniYfromX(float yTarget,float FOV) {
	float xTolerance = 0.0001f; //adjust as you please

	float lower = 0;
	float upper = 1;
	float percent = (upper + lower) / 2;

	//get initial x
	float x = percent;
	float y = PaniniProjectionScreenPosition(2 * float2(x, 0) - 1, FOV).x + 0.5;
	//loop until completion
	int iter = 0;
	while (abs(yTarget - y) > xTolerance && iter < 100)
	{
		if (yTarget > y)
			lower = percent;
		else
			upper = percent;

		percent = (upper + lower) / 2;
		y = PaniniProjectionScreenPosition(2 * float2(percent, 0) - 1, FOV).x + 0.5;
		iter++;
	}
	//we're within tolerance of the desired x value.
	//return the y value.
	return percent;
};
