using UnityEditor;

public class LinuxTrickBuild : TrickBuild
{
    protected override BuildPlayerOptions? OnPreBuild(TrickBuildConfig config,
        TrickBuildManifest manifest, string customBuildId = "")
    {
        string fullPathAndName = $"{manifest.OutputDirectory}{config.AppName}.x64";
        return new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = fullPathAndName,
            targetGroup = BuildTargetGroup.Standalone,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None,
        };
    }

    protected override void OnPostBuild(TrickBuildConfig config, TrickBuildManifest manifest, string customBuildId = "")
    {
        
    }

    public static void Build() => new LinuxTrickBuild().TryBuild();
}