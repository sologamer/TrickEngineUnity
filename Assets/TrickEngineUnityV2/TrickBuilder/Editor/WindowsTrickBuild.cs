using UnityEditor;

public class WindowsTrickBuild : TrickBuild
{
    public static void Build()
    {
        var session = new WindowsTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.manifest.BuildVersion + args.config.BuildVersionOffset);
        string fullPathAndName = $"{args.manifest.OutputDirectory}{args.config.AppName}.exe";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }
}