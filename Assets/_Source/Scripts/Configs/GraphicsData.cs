using System;
using UnityEngine;

[Serializable]
public class GraphicsData
{
    public const int CURRENT_VERSION = 1;

    public int version = CURRENT_VERSION;

    public int displayIndex;
    public int screenMode = (int)FullScreenMode.FullScreenWindow;
    public int resolutionIndex;
    public int refreshRateIndex;
    public int vSync;
    public int frameLimit = -1;

    public int antiAliasing = 3;
    public int taaQuality = 1;
    public int smaaQuality = 2;
    public bool dithering = true;

    public bool dynamicResolution;
    public int screenPercentage = 100;
    public int upscaler = (int)GraphicsUpscalerMode.Off;
    public int dlssMode;
    public int fsr2Mode;
    public float upscaleSharpness = 0.5f;

    public int ssao = 2;
    public int ssgi;
    public int ssr = 2;
    public bool transparentSSR;
    public int contactShadows = 1;
    public bool screenSpaceShadows;

    public int shadowQuality = 2;
    public int shadowResolution = 2;
    public int shadowDistance = 100;
    public int shadowCascades = 4;
    public int shadowFiltering = 1;

    public int fog = 1;
    public bool volumetricFog = true;
    public int volumetricQuality = 1;
    public int volumetricClouds;

    public int motionBlur;
    public int vignette;

    public int textureQuality;
    public int anisotropicFiltering = 1;
    public float lodBias = 1f;

    public GraphicsData Clone()
    {
        return (GraphicsData)MemberwiseClone();
    }

    public void CopyFrom(GraphicsData source)
    {
        if (source == null)
        {
            return;
        }

        version = source.version;
        displayIndex = source.displayIndex;
        screenMode = source.screenMode;
        resolutionIndex = source.resolutionIndex;
        refreshRateIndex = source.refreshRateIndex;
        vSync = source.vSync;
        frameLimit = source.frameLimit;
        antiAliasing = source.antiAliasing;
        taaQuality = source.taaQuality;
        smaaQuality = source.smaaQuality;
        dithering = source.dithering;
        dynamicResolution = source.dynamicResolution;
        screenPercentage = source.screenPercentage;
        upscaler = source.upscaler;
        dlssMode = source.dlssMode;
        fsr2Mode = source.fsr2Mode;
        upscaleSharpness = source.upscaleSharpness;
        ssao = source.ssao;
        ssgi = source.ssgi;
        ssr = source.ssr;
        transparentSSR = source.transparentSSR;
        contactShadows = source.contactShadows;
        screenSpaceShadows = source.screenSpaceShadows;
        shadowQuality = source.shadowQuality;
        shadowResolution = source.shadowResolution;
        shadowDistance = source.shadowDistance;
        shadowCascades = source.shadowCascades;
        shadowFiltering = source.shadowFiltering;
        fog = source.fog;
        volumetricFog = source.volumetricFog;
        volumetricQuality = source.volumetricQuality;
        volumetricClouds = source.volumetricClouds;
        motionBlur = source.motionBlur;
        vignette = source.vignette;
        textureQuality = source.textureQuality;
        anisotropicFiltering = source.anisotropicFiltering;
        lodBias = source.lodBias;
    }
}

public enum GraphicsUpscalerMode
{
    Off = 0,
    CatmullRom = 1,
    CAS = 2,
    FSR1 = 3,
    TAAU = 4,
    DLSS = 5,
    FSR2 = 6,
    STP = 7
}
