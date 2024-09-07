using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using WaveTracker.Audio;
using WaveTracker.Tracker;
using WaveTracker.UI;

namespace WaveTracker {
    public static class SaveLoad {
        /// <summary>
        /// Returns true if the current module is saved
        /// </summary>
        public static bool IsSaved { get { return !App.CurrentModule.IsDirty; } }

        /// <summary>
        /// The full filepath of the current file
        /// </summary>
        public static string CurrentFilepath { get; private set; }

        /// <summary>
        /// Maximum length of the recent files list
        /// </summary>
        private const int MAX_RECENT_FILES = 10;

        /// <summary>
        /// Holds paths to the last recently opened files
        /// </summary>
        private static List<string> recentFilePaths = new List<string>();

        /// <summary>
        /// The path of the folder containing themes
        /// </summary>
        public static string ThemeFolderPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaveTracker", "Themes"); } }
        
        /// <summary>
        /// The path of the folder containing the app settings
        /// </summary>
        public static string SettingsFolderPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaveTracker"); } }
        
        /// <summary>
        /// The path of the configuration file
        /// </summary>
        public static string SettingsPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaveTracker", "wtpref"); } }
        
        /// <summary>
        /// The path of the folder containing all saved paths
        /// </summary>
        public static string PathsFolderPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WaveTracker", "path"); } }
        
        /// <summary>
        /// The current filename, including the extension
        /// </summary>
        public static string FileName { get { return CurrentFilepath == "" ? "Untitled.wtm" : Path.GetFileName(CurrentFilepath); } }
        
        /// <summary>
        /// The current filename, without the extension
        /// </summary>
        public static string FileNameWithoutExtension { get { return CurrentFilepath == "" ? "Untitled" : Path.GetFileNameWithoutExtension(CurrentFilepath); } }
        public static int savecooldown = 0;

        /// <summary>
        /// Writes the current module to <c>path</c>
        /// </summary>
        /// <param name="path"></param>
        private static void WriteTo(string path) {
            Debug.WriteLine("Saving to: " + path);
            Stopwatch stopwatch = Stopwatch.StartNew();

            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                Serializer.Serialize(fs, App.CurrentModule);
            }

            stopwatch.Stop();
            App.CurrentModule.OnSaveModule();
            Debug.WriteLine("saved in " + stopwatch.ElapsedMilliseconds + " ms");
            return;
        }

        public static void SaveFileVoid() {
            SaveFile();
        }

        public static bool SaveFile() {
            if (savecooldown == 0) {
                savecooldown = 4;
                if (!File.Exists(CurrentFilepath)) {
                    return SaveFileAs();
                } else {
                    WriteTo(CurrentFilepath);
                    return true;
                }
            }
            savecooldown = 4;
            return false;
        }

        public static void NewFile() {
            if (Input.focus != null) {
                return;
            }
            Playback.Stop();
            if (!IsSaved) {
                DoSaveChangesDialog(NewFileUnsavedChangesCallback);
            } else {
                OpenANewFile();
            }
        }

        private static void NewFileUnsavedChangesCallback(string ret) {
            if (ret == "Yes") {
                if (!File.Exists(CurrentFilepath)) {
                    SaveFileAs();
                } else {
                    WriteTo(CurrentFilepath);
                }
                SaveFile();
                OpenANewFile();
            }
            if (ret == "No") {
                OpenANewFile();
            }
            if (ret == "Cancel") {
                return;
            }
        }

        private static void OpenANewFile() {
            CurrentFilepath = "";
            App.CurrentModule = new WTModule();
            App.CurrentSongIndex = 0;
            App.PatternEditor.OnSwitchSong(true);
            Playback.Goto(0, 0);
            WaveBank.currentWaveID = 0;
            WaveBank.lastSelectedWave = 0;
            ChannelManager.Reset();
            ChannelManager.UnmuteAllChannels();
            ChannelManager.PreviewChannel.SetWave(0);
        }

        public static void SaveFileAsVoid() {
            SaveFileAs();
        }

        public static bool SaveFileAs() {
            if (Input.focus != null) {
                return false;
            }
            Playback.Stop();
            if (SetFilePathThroughSaveAsDialog(out string filepath)) {
                CurrentFilepath = filepath;
                Debug.WriteLine("Saving as: " + CurrentFilepath);
                WriteTo(CurrentFilepath);
                Debug.WriteLine("Saved as: " + CurrentFilepath);
                App.CurrentModule.OnSaveModule();
                return true;
            }
            return false;
        }

        public static void OpenFile() {
            if (Input.internalDialogIsOpen) {
                return;
            }
            Playback.Stop();
            if (savecooldown == 0) {
                if (!IsSaved) {
                    DoSaveChangesDialog(OpenUnsavedCallback);
                } else {
                    ChooseFileToOpenAndLoad();
                }
            }
            savecooldown = 4;
        }

        public static WaveTracker.UI.Menu CreateFileMenu() {
            return new WaveTracker.UI.Menu([
                new MenuOption("New", NewFile),
                new MenuOption("Open...", OpenFile),
                new MenuOption("Save", SaveFileVoid),
                new MenuOption("Save As...", SaveFileAsVoid),
                null,
                new MenuOption("Export as WAV...", Dialogs.exportDialog.Open),
                null,
                new SubMenu("Recent Files", CreateRecentFilesMenu()),
                null,
                new MenuOption("Exit", App.ExitApplication),
            ]);
        }

        private static MenuItemBase[] CreateRecentFilesMenu() {
            if (recentFilePaths.Count == 0) {
                return [new MenuOption("Clear", recentFilePaths.Clear)];
            } else {
                MenuItemBase[] menu = new MenuItemBase[recentFilePaths.Count + 2];
                menu[0] = new MenuOption("Clear", recentFilePaths.Clear);
                menu[1] = null;
                for (int i = 0; i < recentFilePaths.Count; i++) {
                    menu[i + 2] = new MenuOption(i + 1 + ". " + recentFilePaths[i], TryToLoadFile, recentFilePaths[i]);
                }
                return menu;
            }
        }

        public static void ReadRecentFiles() {
            recentFilePaths = ReadPaths("recent", new string[0]).ToList();
        }

        public static void AddPathToRecentFiles(string path) {
            recentFilePaths.Remove(path);
            recentFilePaths.Insert(0, path);
            if (recentFilePaths.Count > MAX_RECENT_FILES) {
                recentFilePaths.RemoveAt(recentFilePaths.Count - 1);
            }
            SavePaths("recent", recentFilePaths.ToArray());
        }

        private static void TryToLoadFile(string path) {
            string currentPath = CurrentFilepath;

            if (LoadFile(path)) {
                Visualizer.GenerateWaveColors();
                App.CurrentModule.OnSaveModule();
                App.CurrentSongIndex = 0;
                App.PatternEditor.OnSwitchSong(true);
                WaveBank.currentWaveID = 0;
                WaveBank.lastSelectedWave = 0;
                Playback.Goto(0, 0);
                ChannelManager.Reset();
                ChannelManager.UnmuteAllChannels();
                ChannelManager.PreviewChannel.SetWave(0);
                CurrentFilepath = path;
                AddPathToRecentFiles(CurrentFilepath);
            } else {
                Dialogs.MessageDialog.Show("Could not open " + Path.GetFileName(path), "Error");
                CurrentFilepath = currentPath;
            }
        }

        private static void ChooseFileToOpenAndLoad() {
            if (SetFilePathThroughOpenDialog(out string filepath)) {
                TryToLoadFile(filepath);
            }
        }

        public static bool LoadFile(string path) {
            if (!File.Exists(path)) {
                return false;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try {
                using (FileStream fs = new FileStream(path, FileMode.Open)) {
                    fs.Position = 0;
                    App.CurrentModule = Serializer.Deserialize<WTModule>(fs);
                }
            } catch {
                return false;
            }
            stopwatch.Stop();
            Debug.WriteLine("opened in " + stopwatch.ElapsedMilliseconds + " ms");
            CurrentFilepath = path;
            return true;
        }

        public static void DoSaveChangesDialog(Action<string> callback) {
            Dialogs.MessageDialog.Show("Save changes to " + FileName + "?", new[] { "Yes", "No", "Cancel" }, callback);
        }

        private static void OpenUnsavedCallback(string result) {
            if (result == "Yes") {
                if (SaveFile()) {
                    ChooseFileToOpenAndLoad();
                }
            }
            if (result == "No") {
                ChooseFileToOpenAndLoad();
            }
        }

        private static bool SetFilePathThroughOpenDialog(out string filepath) {
            string ret = "";
            filepath = CurrentFilepath;

            var dialog = new OpenFileDialog {
                Title = "Open File",
                Filters = new List<FileDialogFilter> {
                    new FileDialogFilter { Name = "WaveTracker modules", Extensions = { "wtm" } }
                },
                Directory = ReadPath("openwtm", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
            };

            var result = dialog.ShowAsync(App.MainWindow).Result;

            if (result != null && result.Length > 0) {
                ret = result[0];
                SavePath("openwtm", Path.GetDirectoryName(ret));
                filepath = ret;
                return true;
            }
            return false;
        }

        private static bool SetFilePathThroughSaveAsDialog(out string filepath) {
            string ret = "";
            filepath = CurrentFilepath;

            var dialog = new SaveFileDialog {
                Title = "Save File As",
                DefaultExtension = "wtm",
                Filters = new List<FileDialogFilter> {
                    new FileDialogFilter { Name = "WaveTracker modules", Extensions = { "wtm" } }
                },
                InitialFileName = FileName
            };

            var result = dialog.ShowAsync(App.MainWindow).Result;

            if (!string.IsNullOrWhiteSpace(result)) {
                ret = result;
                SavePath("savewtm", Path.GetDirectoryName(ret));
                AddPathToRecentFiles(ret);
                filepath = ret;
                return true;
            }
            return false;
        }

        public static void SavePath(string pathName, string path) {
            Directory.CreateDirectory(PathsFolderPath);
            File.WriteAllText(Path.Combine(PathsFolderPath, pathName + ".path"), path);
        }

        public static string ReadPath(string pathName, string defaultPath) {
            string filepath = Path.Combine(PathsFolderPath, pathName + ".path");
            if (File.Exists(filepath)) {
                return File.ReadAllLines(filepath)[0];
            } else {
                SavePath(pathName, defaultPath);
                return defaultPath;
            }
        }

        public static void SavePaths(string pathName, string[] paths) {
            Directory.CreateDirectory(PathsFolderPath);
            File.WriteAllLines(Path.Combine(PathsFolderPath, pathName + ".path"), paths);
        }

        public static string[] ReadPaths(string pathName, string[] defaultPaths) {
            string filepath = Path.Combine(PathsFolderPath, pathName + ".path");
            if (File.Exists(filepath)) {
                return File.ReadAllLines(filepath);
            } else {
                SavePaths(pathName, defaultPaths);
                return defaultPaths;
            }
        }
    }
}
