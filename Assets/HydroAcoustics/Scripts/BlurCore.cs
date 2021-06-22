using UnityEngine;

static class BlurCore
{

    private static ComputeBuffer weightsBuffer;

    private static float[] OneDimensinalKernel(int radius, float sigma)
    {
        float[] kernelResult = new float[radius * 2 + 1];
        float sum = 0.0f;
        for (int t = 0; t < radius; t++)
        {
            double newBlurWalue = 0.39894 * Mathf.Exp(-0.5f * t * t / (sigma * sigma)) / sigma;
            kernelResult[radius + t] = (float)newBlurWalue;
            kernelResult[radius - t] = (float)newBlurWalue;
            if (t != 0)
                sum += (float)newBlurWalue * 2.0f;
            else
                sum += (float)newBlurWalue;
        }
        // normalize kernels
        for (int k = 0; k < radius * 2 + 1; k++)
        {
            kernelResult[k] /= sum;
        }
        return kernelResult;
    }
    public static ComputeBuffer GetWeights(int radius)
    {

        if (weightsBuffer != null)
            weightsBuffer.Dispose();

        float sigma = ((int)radius) / 1.5f;

        weightsBuffer = new ComputeBuffer((int)radius * 2 + 1, sizeof(float));
        float[] blurWeights = OneDimensinalKernel((int)radius, sigma);
        weightsBuffer.SetData(blurWeights);

        return weightsBuffer;
    }
}