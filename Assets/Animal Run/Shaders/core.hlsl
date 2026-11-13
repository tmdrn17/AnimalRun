#ifndef DISSOLVE_EDGE_GLOW_CORE_INCLUDED
#define DISSOLVE_EDGE_GLOW_CORE_INCLUDED

// Deterministic hash: each 3D grid cell always returns the same pseudo-random value.
float dissolveHash(float3 p)
{
    p = frac(p * 0.1031);
    p = p + dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

// Smooth trilinear value noise in object space.
float dissolveValueNoise(float3 p)
{
    float3 cell = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float x00 = lerp(
        dissolveHash(cell + float3(0.0, 0.0, 0.0)),
        dissolveHash(cell + float3(1.0, 0.0, 0.0)),
        f.x
    );
    float x10 = lerp(
        dissolveHash(cell + float3(0.0, 1.0, 0.0)),
        dissolveHash(cell + float3(1.0, 1.0, 0.0)),
        f.x
    );
    float x01 = lerp(
        dissolveHash(cell + float3(0.0, 0.0, 1.0)),
        dissolveHash(cell + float3(1.0, 0.0, 1.0)),
        f.x
    );
    float x11 = lerp(
        dissolveHash(cell + float3(0.0, 1.0, 1.0)),
        dissolveHash(cell + float3(1.0, 1.0, 1.0)),
        f.x
    );

    return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
}

// Combine broad shapes and fine details, then remap to the 0-1 range.
float dissolveNoise(float3 p)
{
    float n = dissolveValueNoise(p) * 0.6;
    n += dissolveValueNoise(p * 2.03 + float3(11.7, 3.1, 7.9)) * 0.28;
    n += dissolveValueNoise(p * 4.01 + float3(5.3, 17.2, 2.4)) * 0.12;
    return saturate((n - 0.2) / 0.6);
}

float4 sgEffect(SgInput i)
{
    float3 lit = _BaseColor.rgb * i.lighting;

    // Object-space 3D noise avoids visible seams at mesh UV boundaries.
    float noise = dissolveNoise(i.positionOS * _NoiseScale);
    noise = lerp(noise, saturate(0.5 - i.positionOS.y), _Sweep);

    // Values below the threshold are clipped away.
    float cut = _Threshold * 1.01;
    float keep = step(cut, noise);

    // A narrow band just above the cut becomes the glowing burn edge.
    float edge = (1.0 - smoothstep(cut, cut + _EdgeWidth, noise))
               * keep
               * saturate(_Threshold * 20.0);

    float3 color = lerp(lit, _EdgeColor.rgb * _EdgeGlow, edge);
    return float4(color, keep);
}

#endif // DISSOLVE_EDGE_GLOW_CORE_INCLUDED
