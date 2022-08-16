using UnityEditor;

public class LinuxTrickBuild : TrickBuild
{
    public static void Build()
    {
        var session = new LinuxTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.manifest.BuildVersion + args.config.BuildVersionOffset);
        string fullPathAndName = $"{args.manifest.OutputDirectory}{args.config.AppName}.x64";
        session.StartBuild(fullPathAndName, BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64, BuildOptions.None);
    }
}