using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TrickCore;
using UnityEditor.Build.Reporting;

public abstract class TrickBuild
{
    private string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
    }

    protected void StartBuild(string targetDir, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, BuildOptions buildOptions)
    {
        Console.WriteLine($"[{GetType().Name}] Building:{targetDir} buildTargetGroup:{buildTargetGroup} buildTarget:{buildTarget}");

        bool switchResult = EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        if (!switchResult)
        {
            Console.WriteLine($"[{GetType().Name}] Unable to change Build Target to: {buildTarget} Exiting...");
            return;
        }

        Console.WriteLine($"[{GetType().Name}] Starting to build for target {buildTarget}");
        BuildReport buildReport = BuildPipeline.BuildPlayer(GetEnabledScenes(), targetDir, buildTarget, buildOptions);
        BuildSummary buildSummary = buildReport.summary;
        if (buildSummary.result == BuildResult.Succeeded)
        {
            Console.WriteLine($"[{GetType().Name}] Build Success took {buildSummary.totalTime.TotalSeconds:F1}s (size: {buildSummary.totalSize} bytes)");
        }
        else
        {
            Console.WriteLine($"[{GetType().Name}] Build Failed took {buildSummary.totalTime.TotalSeconds:F1}s (totalErrors:{buildSummary.totalErrors})");
        }
    }

    /// <summary>
    /// A version in format of 1.0000, parsed to 10000
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    protected int ToAndroidVersion(string v)
    {
        string[] split = v.Split('.');
        return split.Length == 2 && int.TryParse(string.Join(null, split), out var version) ? version : 0;
    }

    protected string GetVersion(int version)
    {
        int length = version.ToString().Length;
        string s = version.ToString();
        const int leadingZeros = 4;
        if (length <= 4) return "0." + version.ToString("D4");
        int diff = length - leadingZeros;
        int m = Math.Max(1, diff);
        if (s.Length > m) s = s.Insert(m, ".");
        return s;
    }
    
    protected void SetVersion(int buildVersion)
    {
        PlayerSettings.bundleVersion = GetVersion(buildVersion);
        PlayerSettings.iOS.buildNumber = GetVersion(buildVersion);
        PlayerSettings.Android.bundleVersionCode = buildVersion;
        
        Console.WriteLine($"[{GetType().Name}] Version set to {PlayerSettings.bundleVersion} (code={buildVersion})");
    }

    protected bool FindArgs(out (TrickBuildConfig config, TrickBuildManifest manifest) tuple)
    {
        tuple = default;
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        List<string> argumentInOrder =
            commandLineArgs.Skip(
                    Array.FindIndex(commandLineArgs, s => string.Equals(s, "-executeMethod", StringComparison.OrdinalIgnoreCase)) +
                    2)
                .TakeWhile(x => !x.StartsWith("-")).ToList();

        if (argumentInOrder.Count < 2)
        {
            Console.WriteLine($"[{GetType().Name}] ERROR: FindArgs failed to parse. Expected -executeMethod <config file> <manifest base64>");
            return false;
        }

        if (!File.Exists(argumentInOrder[0]))
        {
            Console.WriteLine($"The file '{argumentInOrder[0]}' doesn't exists");
            return false;
        }
        var text = File.ReadAllText(argumentInOrder[0]);
        
        Console.WriteLine("TODO-REMOVE: [Config]: " + text);
        var config = text.DeserializeJson<TrickBuildConfig>();
        var manifest = argumentInOrder[1].DeserializeJsonBase64<TrickBuildManifest>();
        Console.WriteLine("TODO-REMOVE: [Manifest]: " + manifest.SerializeToJson(true, true));

        tuple = (config, manifest);
        return true;
    }
}


/// <summary>
/// From a config file, user static settings
/// </summary>
[JsonObject]
public class TrickBuildConfig
{
    public string AppName;
    public int BuildVersionOffset;

    public AndroidTrickBuildConfig Android = new AndroidTrickBuildConfig();
}

[JsonObject]
public class AndroidTrickBuildConfig
{
    public string AndroidKeyStorePath;
    public string AndroidKeyStorePass;
    public string AndroidAliasName;
    public string AndroidAliasPass;
}

/// <summary>
/// From the builder (jenkins)
/// </summary>
[JsonObject]
public class TrickBuildManifest
{
    public int BuildVersion;
    public string OutputDirectory;
}