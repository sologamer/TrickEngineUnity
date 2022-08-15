using UnityEditor;

public class AndroidTrickBuild : TrickBuild, IFastLaneModule
{
    public static void BuildBundle()
    {
        var session = new AndroidTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.BuildVersion);
        string fullPathAndName = $"{args.OutputDirectory}{args.AppName}.aab";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
    }

    public static void BuildApk()
    {
        var session = new AndroidTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.BuildVersion);
        string fullPathAndName = $"{args.OutputDirectory}{args.AppName}.apk";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
    }

    public void HandleUpload()
    {
        
    }
}