using UnityEditor;

public class OsxTrickBuild : TrickBuild
{
    protected override BuildPlayerOptions? OnPreBuild(TrickBuildConfig config,
        TrickBuildManifest manifest, string customBuildId = "")
    {
        string fullPathAndName = $"{manifest.OutputDirectory}{config.AppName}.app";
        return new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = fullPathAndName,
            targetGroup = BuildTargetGroup.Standalone,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None,
        };
    }

    protected override void OnPostBuild(TrickBuildConfig config, TrickBuildManifest manifest, string customBuildId = "")
    {
        
    }

    public static void Build() => new OsxTrickBuild().TryBuild();
}