using UnityEditor;

public class iOSTrickBuild : TrickBuild, IFastLaneModule
{
    protected override BuildPlayerOptions? OnPreBuild(TrickBuildConfig config,
        TrickBuildManifest manifest, string customBuildId = "")
    {
        string fullPathAndName = $"{manifest.OutputDirectory}{config.AppName}.app";
        return new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = fullPathAndName,
            targetGroup = BuildTargetGroup.iOS,
            target = BuildTarget.iOS,
            options = BuildOptions.None,
        };
    }

    protected override void OnPostBuild(TrickBuildConfig config, TrickBuildManifest manifest, string customBuildId = "")
    {
        
    }

    public static void Build() => new iOSTrickBuild().TryBuild();

    public void HandleUpload()
    {
        
    }
}