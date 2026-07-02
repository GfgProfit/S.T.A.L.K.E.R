using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GraphicsSettingsStorage
{
    private const string SAVES_FOLDER = "saves";
    private const string GLOBAL_FOLDER = "global";
    private const string GRAPHICS_FILE_NAME = "graphics.json";

    public static GraphicsData LoadGraphicsSettings()
    {
        string savePath = GetGraphicsSavePath();

        try
        {
            if (File.Exists(savePath) == false)
            {
                GraphicsData defaultData = CreateDefaultGraphicsData();
                SaveGraphicsSettings(defaultData);
                return defaultData;
            }

            string json = File.ReadAllText(savePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                GraphicsData defaultData = CreateDefaultGraphicsData();
                SaveGraphicsSettings(defaultData);
                return defaultData;
            }

            GraphicsData data = JsonUtility.FromJson<GraphicsData>(json);

            if (data == null || data.version != GraphicsData.CURRENT_VERSION)
            {
                GraphicsData defaultData = CreateDefaultGraphicsData();
                SaveGraphicsSettings(defaultData);
                return defaultData;
            }

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Graphics settings file is invalid and will be recreated. Path: {savePath}. Error: {exception.Message}");
            GraphicsData defaultData = CreateDefaultGraphicsData();
            SaveGraphicsSettings(defaultData);
            return defaultData;
        }
    }

    public static bool SaveGraphicsSettings(GraphicsData data)
    {
        if (data == null)
        {
            return false;
        }

        string savePath = GetGraphicsSavePath();

        try
        {
            string directoryPath = Path.GetDirectoryName(savePath);

            if (Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }

            data.version = GraphicsData.CURRENT_VERSION;
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Graphics settings file cannot be saved. Path: {savePath}. Error: {exception.Message}");
            return false;
        }
    }

    public static string GetGraphicsSavePath()
    {
        DirectoryInfo rootDirectory = Directory.GetParent(Application.dataPath);
        string rootPath = rootDirectory != null ? rootDirectory.FullName : Application.dataPath;
        return Path.Combine(rootPath, SAVES_FOLDER, GLOBAL_FOLDER, GRAPHICS_FILE_NAME);
    }

    public static GraphicsData CreateDefaultGraphicsData()
    {
        GraphicsData data = new GraphicsData();
        data.screenMode = (int)Screen.fullScreenMode;
        data.vSync = QualitySettings.vSyncCount;
        data.frameLimit = Application.targetFrameRate;
        data.textureQuality = QualitySettings.globalTextureMipmapLimit;
        data.anisotropicFiltering = (int)QualitySettings.anisotropicFiltering;
        data.lodBias = QualitySettings.lodBias;

        List<Resolution> resolutions = GraphicsSettingsApplier.GetResolutionList();
        Resolution currentResolution = Screen.currentResolution;

        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == currentResolution.width && resolutions[i].height == currentResolution.height)
            {
                data.resolutionIndex = i;
                break;
            }
        }

        List<RefreshRate> refreshRates = GraphicsSettingsApplier.GetRefreshRates(resolutions[data.resolutionIndex]);

        for (int i = 0; i < refreshRates.Count; i++)
        {
            if (refreshRates[i].numerator == currentResolution.refreshRateRatio.numerator && refreshRates[i].denominator == currentResolution.refreshRateRatio.denominator)
            {
                data.refreshRateIndex = i;
                break;
            }
        }

        return data;
    }
}
