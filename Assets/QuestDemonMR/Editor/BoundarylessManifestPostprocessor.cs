using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace QuestDemonMR.Editor
{
    /// <summary>
    /// Declares the application as a fully boundaryless passthrough experience.
    /// Meta permits this only for apps that never switch to an immersive mode.
    /// </summary>
    public sealed class BoundarylessManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string FeatureName = "com.oculus.feature.BOUNDARYLESS_APP";
        // Meta's OVRGradleGeneration runs at 99999 and can create numbered
        // incremental copies of Android resources, so run after it.
        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            var manifestPath = Path.Combine(basePath, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Generated Android manifest was not found.", manifestPath);

            XNamespace android = "http://schemas.android.com/apk/res/android";
            var document = XDocument.Load(manifestPath);
            var root = document.Root ?? throw new InvalidOperationException("Generated Android manifest has no root element.");
            var feature = root.Elements("uses-feature")
                .FirstOrDefault(element => (string)element.Attribute(android + "name") == FeatureName);
            if (feature == null)
            {
                feature = new XElement("uses-feature");
                root.AddFirst(feature);
            }
            feature.SetAttributeValue(android + "name", FeatureName);
            feature.SetAttributeValue(android + "required", "true");
            document.Save(manifestPath);
            RemoveNumberedGeneratedCopies(Path.Combine(basePath, "src", "main"));
            Debug.Log($"QDMR_BOUNDARYLESS_MANIFEST_OK path={manifestPath}");
        }

        private static void RemoveNumberedGeneratedCopies(string generatedRoot)
        {
            if (!Directory.Exists(generatedRoot)) return;
            var removed = 0;
            foreach (var path in Directory.GetFiles(generatedRoot, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var split = name.LastIndexOf(' ');
                if (split < 0 || !int.TryParse(name[(split + 1)..], out _)) continue;
                var original = Path.Combine(Path.GetDirectoryName(path) ?? generatedRoot,
                    name[..split] + Path.GetExtension(path));
                if (!File.Exists(original)) continue;
                File.Delete(path);
                removed++;
            }
            if (removed > 0) Debug.Log($"QDMR_CLEAN_ANDROID_RESOURCES count={removed}");
        }
    }
}
