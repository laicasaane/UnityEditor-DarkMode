using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditorDarkMode.Tools
{
    internal static class UnityEditorDarkModeTools
    {
        private const string PACKAGE_NAME = "com.0x7c13.unityeditor-darkmode";
        private const string MENU_ROOT = "Dark Mode";
        private const string ENABLE_MENU = MENU_ROOT + "/Enable";
        private const string DISABLE_MENU = MENU_ROOT + "/Disable";

        [MenuItem(ENABLE_MENU)]
        private static void EnableDarkMode()
        {
            SetEnable(true);
        }

        [MenuItem(DISABLE_MENU)]
        private static void DisableDarkMode()
        {
            SetEnable(false);
        }

        [MenuItem(ENABLE_MENU, true)]
        private static bool ValidateEnableDarkMode()
        {
            var packageRootPath = GetPackageRootPath();

            if (string.IsNullOrEmpty(packageRootPath))
            {
                return false;
            }

            var disabledFolder = Path.Combine(packageRootPath, "Plugins~");
            return Directory.Exists(disabledFolder);
        }

        [MenuItem(DISABLE_MENU, true)]
        private static bool ValidateDisableDarkMode()
        {
            var packageRootPath = GetPackageRootPath();

            if (string.IsNullOrEmpty(packageRootPath))
            {
                return false;
            }

            var enabledFolder = Path.Combine(packageRootPath, "Plugins");
            return Directory.Exists(enabledFolder);
        }

        private static void SetEnable(bool enabled)
        {
            var packageRootPath = GetPackageRootPath();

            if (string.IsNullOrEmpty(packageRootPath))
            {
                Debug.LogError("[Dark Mode] Could not find package root path.");
                return;
            }

            TogglePluginsFolder(packageRootPath, enabled);
        }

        private static string GetPackageRootPath()
        {
            var guidStrings = AssetDatabase
                .FindAssets($"t:{nameof(AssemblyDefinitionAsset)}")
                .AsSpan();

            var guidStringsLength = guidStrings.Length;

            if (guidStringsLength < 1)
            {
                return string.Empty;
            }

            for (var i = 0; i < guidStringsLength; i++)
            {
                var asmdefGuidString = guidStrings[i];
                var asmdefPath = AssetDatabase.GUIDToAssetPath(asmdefGuidString).Replace('\\', '/');
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(asmdefPath);
                var asmdefAsset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmdefPath);

                if (package == null
                    || asmdefAsset == false
                    || package.name.Equals(PACKAGE_NAME) == false
                )
                {
                    continue;
                }

                return package.resolvedPath;
            }

            return string.Empty;
        }

        private static void TogglePluginsFolder(string rootPath, bool enabled)
        {
            var disabledFolder = Path.Combine(rootPath, "Plugins~");
            var enabledFolder = Path.Combine(rootPath, "Plugins");
            var enabledMetaFile = Path.Combine(rootPath, "Plugins.meta");

            if (enabled && Directory.Exists(disabledFolder))
            {
                if (Directory.Exists(enabledFolder))
                {
                    Directory.Delete(enabledFolder, true);
                }

                Directory.Move(disabledFolder, enabledFolder);
                File.WriteAllText(enabledMetaFile, GetPluginsFolderMetaContent(), Encoding.UTF8);

                if (Directory.Exists(disabledFolder))
                {
                    Directory.Delete(disabledFolder, true);
                }

                Debug.Log("[Dark Mode] Enabled. Please restart the Unity Editor to fully enable Dark Mode support.");
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Dark Mode Enabled",
                    "Please restart the Unity Editor to fully enable Dark Mode support.",
                    "OK"
                );
            }
            else if (enabled == false && Directory.Exists(enabledFolder))
            {
                if (Directory.Exists(disabledFolder))
                {
                    Directory.Delete(disabledFolder, true);
                }

                Directory.Move(enabledFolder, disabledFolder);

                if (Directory.Exists(enabledFolder))
                {
                    Directory.Delete(enabledFolder, true);
                }

                if (File.Exists(enabledMetaFile))
                {
                    File.Delete(enabledMetaFile);
                }

                Debug.Log("[Dark Mode] Disabled. Please restart the Unity Editor to fully disable Dark Mode support.");
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Dark Mode Disabled",
                    "Please restart the Unity Editor to fully disable Dark Mode support.",
                    "OK"
                );
            }
        }

        private static string GetPluginsFolderMetaContent()
        {
            var sb = new StringBuilder();

            sb.AppendLine("fileFormatVersion: 2");
            sb.AppendLine($"guid: {Guid.NewGuid():N}");
            sb.AppendLine("folderAsset: yes");
            sb.AppendLine("DefaultImporter:");
            sb.AppendLine("  externalObjects: {}");
            sb.AppendLine("  userData: ");
            sb.AppendLine("  assetBundleName: ");
            sb.AppendLine("  assetBundleVariant: ");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
