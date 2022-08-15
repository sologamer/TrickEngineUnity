using UnityEditor;

public class WindowsTrickBuild : TrickBuild
{
    public static void Build()
    {
        var session = new WindowsTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.BuildVersion);
        string fullPathAndName = $"{args.OutputDirectory}{args.AppName}.exe";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }
}