using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace QuestDemonMR.Editor
{
    // Run after generation, not only before Unity creates/copies Android files.
    // Preserve numbered conflict copies for inspection; never touch the original.
    public sealed class GradleDuplicateGuard : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => int.MaxValue;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var root = Path.GetFullPath(path);
            var allowed = Path.GetFullPath("Library/Bee/Android/Prj").TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var export = Environment.GetEnvironmentVariable("QDMR_GRADLE_EXPORT");
            var isExplicitExport = !string.IsNullOrEmpty(export) &&
                (Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v184-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v185-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v186-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v187-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v188-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v189-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1810-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1811-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1812-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1813-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1814-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1815-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1816-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1817-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1818-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1819-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1820-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v190-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v191-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v192-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v199-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1910-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1911-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1912-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1913-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1914-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1915-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1916-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1917-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1918-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1919-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v1920-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v200-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v201-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v202-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v203-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v204-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v205-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v206-export.", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v198-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v197-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v196-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v195-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v194-", StringComparison.Ordinal) ||
                 Path.GetFullPath(export).StartsWith("/private/tmp/qdmr-v193-", StringComparison.Ordinal)) &&
                root == Path.Combine(Path.GetFullPath(export), "unityLibrary");
            if (!root.StartsWith(allowed, StringComparison.Ordinal) && !isExplicitExport)
                throw new InvalidOperationException("Unexpected generated Gradle directory: " + root);
            var quarantine = Path.GetFullPath("Verification/GradleQuarantine/" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var count = QuarantineNumberedCopies(Path.Combine(root, "libs"), quarantine + "/libs") +
                        QuarantineNumberedCopies(Path.Combine(root, "src/main/res"), quarantine + "/res") +
                        QuarantineNumberedCopies(Path.Combine(root, "src/main/assets/bin/Data"), quarantine + "/data");
            if (count > 0) Debug.Log($"QDMR_GRADLE_QUARANTINE count={count} path={quarantine}");
        }
        public static int QuarantineNumberedCopies(string source, string destination)
        {
            if (!Directory.Exists(source)) return 0;
            var count = 0;
            foreach (var path in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var stem = Path.GetFileNameWithoutExtension(path);
                var match = Regex.Match(stem, @"^(.*) [0-9]+$");
                if (!match.Success) continue;
                var original = Path.Combine(Path.GetDirectoryName(path), match.Groups[1].Value + Path.GetExtension(path));
                if (!File.Exists(original)) continue;
                var target = Path.Combine(destination, Path.GetRelativePath(source, path));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Move(path, target); count++;
            }
            return count;
        }
        public static void RecoverAndBuild()
        {
            new GradleDuplicateGuard().OnPostGenerateGradleAndroidProject(
                Path.GetFullPath("Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary"));
            QuestDemonProjectBuilder.BuildAndroidMeasurementPrepared();
        }
    }
}
