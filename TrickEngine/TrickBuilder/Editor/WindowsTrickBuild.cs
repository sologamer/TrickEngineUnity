using UnityEditor;

public class WindowsTrickBuild : TrickBuild
{
    protected override BuildPlayerOptions? OnPreBuild(TrickBuildConfig config,
        TrickBuildManifest manifest, string customBuildId = "")
    {
        string fullPathAndName = $"{manifest.OutputDirectory}{config.AppName}.exe";
        return new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = fullPathAndName,
            targetGroup = BuildTargetGroup.Standalone,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
    }

    protected override void OnPostBuild(TrickBuildConfig config, TrickBuildManifest manifest, string customBuildId = "")
    {
        
    }

    public static void Build() => new WindowsTrickBuild().TryBuild();
}