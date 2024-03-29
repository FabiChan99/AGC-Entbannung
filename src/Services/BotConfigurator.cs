﻿#region

using AGC_Entbannungssystem.Helpers;
using DisCatSharp.Entities;
using Newtonsoft.Json.Linq;

#endregion

namespace AGC_Entbannungssystem.Services;

public class BotConfigurator
{
    private static JObject LoadConfig()
    {
        try
        {
            string json = File.ReadAllText("config.json");
            return JObject.Parse(json);
        }
        catch (Exception)
        {
            Console.WriteLine("The configuration file could not be loaded. Please check the config.");
            Console.WriteLine("Press any key to exit the program.");
            Console.WriteLine("Current Working Directory: " + Directory.GetCurrentDirectory());
            Console.ReadKey();
            Environment.Exit(12);
            return null;
        }
    }

    public static string GetConfig(string category, string key)
    {
        JObject config = LoadConfig();

        try
        {
            string value = config[category][key].Value<string>();
            return !string.IsNullOrEmpty(value) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    public static void SetConfig(string category, string key, string value)
    {
        JObject config = LoadConfig();

        if (config[category] == null)
        {
            config[category] = new JObject();
        }

        config[category][key] = value;

        File.WriteAllText("config.json", config.ToString());
    }

    public static DiscordColor GetEmbedColor()
    {
        string fallbackColor = "000000";
        string colorString;

        try
        {
            string colorConfig = GetConfig("EmbedConfig", "DefaultEmbedColor");
            if (colorConfig != null && colorConfig.StartsWith("#"))
                colorConfig = colorConfig.Substring(1);

            if (string.IsNullOrEmpty(colorConfig) || !HexCheck.IsHexColor(colorConfig))
            {
                colorString = fallbackColor;
                return new DiscordColor(colorString);
            }

            colorString = colorConfig;
        }
        catch
        {
            colorString = fallbackColor;
        }

        return new DiscordColor(colorString);
    }
}