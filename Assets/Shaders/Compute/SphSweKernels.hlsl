#ifndef SPH_SWE_KERNELS_INCLUDED
#define SPH_SWE_KERNELS_INCLUDED

static const float SphSwePi = 3.14159265358979323846f;

float EvaluatePoly6(float squaredDistance, float effectiveRadius)
{
    if (squaredDistance < 0.0f || effectiveRadius <= 0.0f)
    {
        return 0.0f;
    }
    
    float squaredRadius = effectiveRadius * effectiveRadius;
    
    if (squaredDistance >= squaredRadius)
    {
        return 0.0f;
    }
    
    float radiusFourthPower = squaredRadius * squaredRadius;
    float radiusEighthPower = radiusFourthPower * radiusFourthPower;
    float coefficient = 4.0f / (SphSwePi * radiusEighthPower);
    float radiusDifference = squaredRadius - squaredDistance;
    
    return coefficient * radiusDifference * radiusDifference * radiusDifference;
}

float2 EvaluateSpikyGradient(float2 positionDifference, float effectiveRadius)
{
    if (effectiveRadius <= 0.0f)
    {
        return float2(0.0f, 0.0f);
    }

    float squaredDistance =positionDifference.x * positionDifference.x + positionDifference.y * positionDifference.y;

    if (squaredDistance <= 0.0f)
    {
        return float2(0.0f, 0.0f);
    }

    float squaredRadius = effectiveRadius * effectiveRadius;

    if (squaredDistance >= squaredRadius)
    {
        return float2(0.0f, 0.0f);
    }

    float distance = sqrt(squaredDistance);
    float radiusDifference = effectiveRadius - distance;
    float radiusFourthPower = squaredRadius * squaredRadius;
    float radiusFifthPower = radiusFourthPower * effectiveRadius;
    float coefficient = -30.0f / (SphSwePi * radiusFifthPower);
    float gradientScale = coefficient * radiusDifference * radiusDifference / distance;

    return positionDifference * gradientScale;
}

float EvaluateViscosityLaplacian(float squaredDistance, float effectiveRadius)
{
    if (squaredDistance < 0.0f || effectiveRadius <= 0.0f)
    {
        return 0.0f;
    }

    float squaredRadius = effectiveRadius * effectiveRadius;

    if (squaredDistance >= squaredRadius)
    {
        return 0.0f;
    }

    float distance = sqrt(squaredDistance);
    float radiusFourthPower = squaredRadius * squaredRadius;
    float radiusFifthPower = radiusFourthPower * effectiveRadius;
    float coefficient = 20.0f / (3.0f * SphSwePi * radiusFifthPower);

    return coefficient * (effectiveRadius - distance);
}

#endif