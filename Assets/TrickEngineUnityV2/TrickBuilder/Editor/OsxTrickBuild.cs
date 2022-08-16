using UnityEditor;

public class OsxTrickBuild : TrickBuild
{
    public static void Build()
    {
        var session = new OsxTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.manifest.BuildVersion + args.config.BuildVersionOffset);
        string fullPathAndName = $"{args.manifest.OutputDirectory}{args.config.AppName}.app";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, BuildOptions.None);
    }
}