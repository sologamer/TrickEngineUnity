using UnityEditor;

public class OsxTrickBuild : TrickBuild
{
    public static void Build()
    {
        var session = new OsxTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.BuildVersion);
        string fullPathAndName = $"{args.OutputDirectory}{args.AppName}.app";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, BuildOptions.None);
    }
}