using UnityEditor;

public class AndroidTrickBuild : TrickBuild, IFastLaneModule
{
    public static void BuildBundle()
    {
        var session = new AndroidTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.manifest.BuildVersion + args.config.BuildVersionOffset);
        string fullPathAndName = $"{args.manifest.OutputDirectory}{args.config.AppName}.aab";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
    }

    public static void BuildApk()
    {
        var session = new AndroidTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.manifest.BuildVersion + args.config.BuildVersionOffset);
        string fullPathAndName = $"{args.manifest.OutputDirectory}{args.config.AppName}.apk";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
    }

    public void HandleUpload()
    {
        
    }
}