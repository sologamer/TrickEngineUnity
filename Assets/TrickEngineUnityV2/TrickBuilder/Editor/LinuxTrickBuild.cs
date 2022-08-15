using UnityEditor;

public class LinuxTrickBuild : TrickBuild
{
    public static void Build()
    {
        var session = new LinuxTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.BuildVersion);
        string fullPathAndName = $"{args.OutputDirectory}{args.AppName}.x64";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64, BuildOptions.None);
    }
}