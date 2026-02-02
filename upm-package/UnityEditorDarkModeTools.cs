using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditorDarkMode.Tools
{
    internal static class UnityEditorDarkModeTools
    {
        private const string PACKAGE_NAME = "com.0x7c13.unityeditor-darkmode";
        private const string DISABLED_PLUGINS_FOLDER = "Plugins~";
        private const string ENABLED_PLUGINS_FOLDER = "com.0x7c13.unityeditor-darkmode";
        private const string MENU_ROOT = "Dark Mode";
        private const string ENABLE_MENU = MENU_ROOT + "/Import To Enable";
        private const string DISABLE_MENU = MENU_ROOT + "/Delete To Disable";

        [MenuItem(ENABLE_MENU, priority = 0)]
        private static void EnableDarkMode()
        {
            SetEnable(true);
        }

        [MenuItem(DISABLE_MENU, priority = 1)]
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

            var disabledFolder = Path.Combine(packageRootPath, DISABLED_PLUGINS_FOLDER);
            return Directory.Exists(disabledFolder);
        }

        [MenuItem(DISABLE_MENU, true)]
        private static bool ValidateDisableDarkMode()
        {
            var projectPluginsPath = GetProjectPluginsPath();

            if (string.IsNullOrEmpty(projectPluginsPath))
            {
                return false;
            }

            var enabledFolder = Path.Combine(projectPluginsPath, ENABLED_PLUGINS_FOLDER);
            return Directory.Exists(enabledFolder);
        }

        private static void SetEnable(bool enabled)
        {
            var packageRootPath = GetPackageRootPath();
            var projectPluginsPath = GetProjectPluginsPath();

            if (string.IsNullOrEmpty(packageRootPath) || Directory.Exists(packageRootPath) == false)
            {
                Debug.LogError($"[Dark Mode] Failed to locate '{DISABLED_PLUGINS_FOLDER}'");
                return;
            }

            if (string.IsNullOrEmpty(projectPluginsPath))
            {
                Debug.LogError("[Dark Mode] Failed to locate 'Assets/Plugins'.");
                return;
            }

            if (Directory.Exists(projectPluginsPath) == false)
            {
                if (enabled)
                {
                    Directory.CreateDirectory(projectPluginsPath);
                }
                else
                {
                    return;
                }
            }

            TogglePluginsFolder(packageRootPath, projectPluginsPath, enabled);
        }

        private static string GetProjectPluginsPath()
        {
            return Path.Combine(Application.dataPath, "Plugins").Replace('\\', '/');
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

        private static void TogglePluginsFolder(string packageRootPath, string projectPluginsPath, bool enabled)
        {
            var disabledFolderPath = Path.Combine(packageRootPath, DISABLED_PLUGINS_FOLDER);
            var enabledFolderPath = Path.Combine(projectPluginsPath, ENABLED_PLUGINS_FOLDER);

            if (enabled && Directory.Exists(disabledFolderPath))
            {
                if (Directory.Exists(enabledFolderPath))
                {
                    Directory.Delete(enabledFolderPath, true);
                }

                Directory.CreateDirectory(enabledFolderPath);

                var disabledFolder = new DirectoryInfo(disabledFolderPath);

                foreach (FileInfo srcFile in disabledFolder.GetFiles())
                {
                    var destFilePath = Path.Combine(enabledFolderPath, srcFile.Name);
                    File.Copy(srcFile.FullName, destFilePath, true);
                }

                var msg = "The plugin to enable Dark Mode has been imported into " +
                    $"'Assets/Plugins/{ENABLED_PLUGINS_FOLDER}'\n." +
                    $"It is safe for your version control system to ignore the folder '{ENABLED_PLUGINS_FOLDER}'.\n" +
                    "Please restart the Unity Editor to fully enable Dark Mode support.";

                Debug.LogWarning($"[Dark Mode] Enabled. {msg}");

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                EditorUtility.DisplayDialog("Dark Mode Enabled", msg, "OK");
            }
            else if (enabled == false && Directory.Exists(enabledFolderPath))
            {
                EditorUtility.DisplayDialog(
                      "How to disable Dark Mode?"
                    , $"It is NOT possible to delete the folder 'Assets/Plugins/{ENABLED_PLUGINS_FOLDER}' " +
                      "while Unity Editor is running.\n" +
                      "Please exit Unity Editor then delete the folder manually."
                    , "I understand"
                );
            }
        }
    }
}
