using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WebGLBuilder
{
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes found in Build Settings.");

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "WebGLBuild",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(
                $"WebGL build failed: {report.summary.result}, errors: {report.summary.totalErrors}");

        Console.WriteLine(
            $"WEBGL_BUILD_OK size={report.summary.totalSize} path={options.locationPathName}");
    }
}
