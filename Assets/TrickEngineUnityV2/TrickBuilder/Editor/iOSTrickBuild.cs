using UnityEditor;

public class iOSTrickBuild : TrickBuild, IFastLaneModule
{
    public static void Build()
    {
        var session = new iOSTrickBuild();
        if (!session.FindArgs(out var args)) return;
        session.SetVersion(args.manifest.BuildVersion + args.config.BuildVersionOffset);
        string fullPathAndName = $"{args.manifest.OutputDirectory}{args.config.AppName}.app";
        session.StartBuild(fullPathAndName, BuildTargetGroup.iOS, BuildTarget.iOS, BuildOptions.None);
    }

    public void HandleUpload()
    {
        
    }
}