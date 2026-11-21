using System;
using System.IO;

namespace BlackMarket.Save
{
    /// <summary>
    /// Writes and reads the save file safely:
    /// 1. write to "save.json.tmp", 2. read it back and check it, 3. move the current save to "save.json.bak",
    /// 4. move the temp file into place. A crash at any step leaves either the old save or the backup intact,
    /// and a broken new file never replaces a good one.
    ///
    /// Plain C#: the JSON conversion is passed in, so this is testable without Unity.
    /// </summary>
    public sealed class SaveStore
    {
        private readonly string path;
        private readonly Func<SaveData, string> serialize;
        private readonly Func<string, SaveData> deserialize;

        public SaveStore(string directory, Func<SaveData, string> serialize, Func<string, SaveData> deserialize, string fileName = "save.json")
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("A save folder is required.", nameof(directory));
            path = Path.Combine(directory, fileName);
            this.serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            this.deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
        }

        public string FilePath => path;
        public string BackupPath => path + ".bak";
        private string TempPath => path + ".tmp";

        /// <summary>Returns false (and leaves existing files untouched) if the data is invalid or can't be written.</summary>
        public bool Write(SaveData data, out string error)
        {
            error = null;
            try
            {
                if (SaveValidator.Validate(data).Count > 0)
                {
                    error = "Refused to save invalid data: " + string.Join(" ", SaveValidator.Validate(data));
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllText(TempPath, serialize(data));

                // Prove the file we just wrote can be loaded before it replaces anything.
                SaveData check = deserialize(File.ReadAllText(TempPath));
                if (SaveValidator.Validate(check).Count > 0)
                {
                    File.Delete(TempPath);
                    error = "Written save failed verification.";
                    return false;
                }

                if (File.Exists(path))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Move(path, BackupPath);
                }

                File.Move(TempPath, path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>Loads the save, falling back to the backup. Returns null when there is no usable save (new game).</summary>
        public SaveData Read(out string note)
        {
            note = null;
            SaveData main = TryLoad(path);
            if (main != null) return main;

            SaveData backup = TryLoad(BackupPath);
            if (backup != null)
            {
                note = File.Exists(path) ? "Save file was damaged; restored the backup." : "Restored the backup save.";
                return backup;
            }

            if (File.Exists(path) || File.Exists(BackupPath)) note = "Save files were unreadable; starting a new game (files kept).";
            return null;
        }

        public bool Exists => File.Exists(path) || File.Exists(BackupPath);

        private SaveData TryLoad(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                SaveData data = deserialize(File.ReadAllText(file));
                return SaveValidator.Validate(data).Count == 0 ? data : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
