using UnityEditor;

public class AndroidTrickBuild : TrickBuild, IFastLaneModule
{
    protected override BuildPlayerOptions? OnPreBuild(TrickBuildConfig config,
        TrickBuildManifest manifest, string customBuildId = "")
    {
        PlayerSettings.Android.keyaliasName = config.Android.AndroidAliasName;
        PlayerSettings.Android.keyaliasPass = config.Android.AndroidAliasPass;
        PlayerSettings.Android.keystoreName = config.Android.AndroidKeyStorePath;
        PlayerSettings.Android.keystorePass = config.Android.AndroidKeyStorePass;
        
        string fullPathAndName = $"{manifest.OutputDirectory}{config.AppName}.aab";
        return new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = fullPathAndName,
            targetGroup = BuildTargetGroup.Android,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };
    }

    protected override void OnPostBuild(TrickBuildConfig config, TrickBuildManifest manifest, string customBuildId = "")
    {
        
    }

    public static void BuildBundle() => new AndroidTrickBuild().TryBuild("bundle");
    public static void BuildApk() => new AndroidTrickBuild().TryBuild("apk");

    public void HandleUpload()
    {
        
    }
}