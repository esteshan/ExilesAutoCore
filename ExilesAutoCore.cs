using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using ExileCore2;
using ExileCore2.Shared.Helpers;
using ExilesAutoCore.Rules;
using ExilesAutoCore.State;
using ExilesAutoCore.Ui;
using ImGuiNET;
using Newtonsoft.Json;

namespace ExilesAutoCore;

public sealed class ExilesAutoCore : BaseSettingsPlugin<ExilesAutoCoreSettings>
{
    private readonly RuleBuilder _ruleBuilder = new();
    private readonly ComboBuilder _comboBuilder = new();
    private readonly Engine _engine = new();

    // Transient confirmation/error shown in the profile bar after an import/export, with its expiry.
    private string _profileBarStatus = "";
    private Vector4 _profileBarStatusColor;
    private DateTime _profileBarStatusUntil;

    // A native file dialog runs on its own STA thread (WinForms requires STA). We must NOT block the
    // render thread waiting on it — that freezes the whole overlay — so we poll for completion each frame.
    private Thread _dialogThread;
    private volatile string _dialogResultPath;
    private volatile bool _dialogComplete;
    private bool _dialogIsImport;
    private Profile _dialogExportProfile;

    // The DLL-embedded zip of included_builds is extracted into the user's builds folder once per session.
    private const string IncludedBuildsResource = "ExilesAutoCore.included_builds.zip";
    private bool _seededIncludedBuilds;

    // Class -> ascendancies, used to scaffold the builds/<Class>/<Ascendancy> folder tree that the
    // import/export dialogs open into. Source: https://poe2db.tw/us/Ascendancy_class
    private static readonly Dictionary<string, string[]> AscendanciesByClass = new()
    {
        ["Warrior"] = new[] { "Titan", "Warbringer", "Smith of Kitava" },
        ["Witch"] = new[] { "Infernalist", "Blood Mage", "Lich", "Abyssal Lich" },
        ["Ranger"] = new[] { "Deadeye", "Pathfinder" },
        ["Monk"] = new[] { "Martial Artist", "Invoker", "Acolyte of Chayula" },
        ["Mercenary"] = new[] { "Tactician", "Witchhunter", "Gemling Legionnaire" },
        ["Sorceress"] = new[] { "Stormweaver", "Chronomancer", "Disciple of Varashta" },
        ["Huntress"] = new[] { "Amazon", "Spirit Walker", "Ritualist" },
        ["Druid"] = new[] { "Oracle", "Shaman" },
    };

    public override bool Initialise()
    {
        // Scaffold builds/<Class>/<Ascendancy> up front so the folders exist the first time the user
        // opens an import/export dialog.
        EnsureBuildsFolder();
        return base.Initialise();
    }

    // builds/ lives under the plugin's ConfigDirectory (C:\ExileCore\config\ExilesAutoCore\builds) — a
    // stable, user-findable location that survives recompiles, unlike the volatile Plugins\Temp run dir.
    // Creates the full class/ascendancy tree if missing and returns the folder.
    private string EnsureBuildsFolder()
    {
        var baseDir = string.IsNullOrEmpty(ConfigDirectory) ? DirectoryFullName : ConfigDirectory;
        var root = Path.Combine(baseDir, "builds");
        try
        {
            foreach (var (className, ascendancies) in AscendanciesByClass)
            {
                foreach (var ascendancy in ascendancies)
                {
                    Directory.CreateDirectory(Path.Combine(root, className, ascendancy));
                }
            }
        }
        catch
        {
            // Permissions or a read-only location — fall back to the base directory below.
        }

        var resolved = Directory.Exists(root) ? root : baseDir;
        SeedIncludedBuilds(resolved);
        return resolved;
    }

    // Extracts the builds bundled in the DLL (included_builds.zip) into the user's builds folder. Runs once
    // per session and never overwrites a file the user already has, so their own builds/edits are safe.
    private void SeedIncludedBuilds(string buildsRoot)
    {
        if (_seededIncludedBuilds)
        {
            return;
        }

        _seededIncludedBuilds = true; // even on failure — don't retry every dialog open

        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IncludedBuildsResource);
            if (stream == null)
            {
                return; // no bundled builds in this build
            }

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                // Skip directory entries (empty Name) and the bundle's README.
                if (string.IsNullOrEmpty(entry.Name) || entry.FullName.Equals("README.txt", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                var destination = Path.Combine(buildsRoot, relative);

                // Never clobber a build the user already has.
                if (File.Exists(destination))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: false);
            }
        }
        catch
        {
            // A corrupt/locked resource must never stop the plugin loading.
        }
    }

    public override void DrawSettings()
    {
        // Only show the configuration menu while the plugin itself is enabled (its checkbox is ticked).
        if (!Settings.Enable)
        {
            ImGui.TextColored(Color.Gray.ToImguiVec4(), "Enable ExilesAutoCore (tick it in the plugin list) to configure it.");
            return;
        }

        // Single-threaded ImGui draw, so a static flag is enough to thread the mode into the editor.
        ConditionEditor.AdvancedMode = Settings.AdvancedMode;

        Theme.Push();
        try
        {
            base.DrawSettings();

            DrawProfileBar();
            var profile = ActiveProfile();

            // A fresh snapshot each frame the settings window is open, used to light the live status dots.
            var state = new GameState(GameController, Settings.MaxMonsterRange);

            if (ImGui.CollapsingHeader("Automation rules", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawAutomationStatus();
                _ruleBuilder.Draw(profile.Rules, state);
            }

            if (ImGui.CollapsingHeader("Combos", ImGuiTreeNodeFlags.DefaultOpen))
            {
                _comboBuilder.Draw(profile.Combos, state);
            }
        }
        finally
        {
            Theme.Pop();
        }
    }

    public override void Render()
    {
        // The engine runs every frame (not just when settings are open), but only when the plugin and
        // automation are both armed and we're in a valid in-game state.
        if (!Settings.Enable || !Settings.EnableAutomation)
        {
            return;
        }

        var state = new GameState(GameController, Settings.MaxMonsterRange);
        if (!state.IsValid)
        {
            return;
        }

        _engine.Tick(GameController, state, ActiveProfile(), Settings);
    }

    // Ensures at least one profile exists — migrating any pre-profiles rules/combos into a "Default"
    // one — clamps the active index, and returns the active profile.
    private Profile ActiveProfile()
    {
        if (Settings.Profiles.Count == 0)
        {
            Settings.Profiles.Add(new Profile
            {
                Name = "Default",
                Rules = new List<SkillRule>(Settings.Rules),
                Combos = new List<Combo>(Settings.Combos),
            });
            Settings.Rules.Clear();
            Settings.Combos.Clear();
        }

        if (Settings.ActiveProfileIndex < 0 || Settings.ActiveProfileIndex >= Settings.Profiles.Count)
        {
            Settings.ActiveProfileIndex = 0;
        }

        return Settings.Profiles[Settings.ActiveProfileIndex];
    }

    private void DrawProfileBar()
    {
        ActiveProfile(); // make sure a profile exists before building the dropdown

        var names = Settings.Profiles.Select(p => p.Name).ToArray();
        var index = Settings.ActiveProfileIndex;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Profile", ref index, names, names.Length))
        {
            Settings.ActiveProfileIndex = index;
        }

        ImGui.SameLine();
        if (ImGui.Button("New profile"))
        {
            Settings.Profiles.Add(new Profile { Name = $"Profile {Settings.Profiles.Count + 1}" });
            Settings.ActiveProfileIndex = Settings.Profiles.Count - 1;
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete profile") && Settings.Profiles.Count > 1)
        {
            Settings.Profiles.RemoveAt(Settings.ActiveProfileIndex);
            Settings.ActiveProfileIndex = 0;
        }

        // Pick up the result of a file dialog opened on a previous frame (non-blocking).
        ProcessPendingDialog();

        var dialogBusy = _dialogThread != null;
        ImGui.SameLine();
        ImGui.BeginDisabled(dialogBusy);
        if (ImGui.Button("Export profile"))
        {
            BeginFileDialog(isImport: false, profile: ActiveProfile());
        }

        ImGui.SameLine();
        if (ImGui.Button("Import profile"))
        {
            BeginFileDialog(isImport: true, profile: null);
        }

        ImGui.EndDisabled();
        if (dialogBusy)
        {
            ImGui.SameLine();
            ImGui.TextColored(Color.Gray.ToImguiVec4(), "(file dialog open…)");
        }

        if (_profileBarStatus.Length > 0 && DateTime.UtcNow < _profileBarStatusUntil)
        {
            ImGui.TextColored(_profileBarStatusColor, _profileBarStatus);
        }

        var active = Settings.Profiles[Settings.ActiveProfileIndex];
        ImGui.SetNextItemWidth(200);
        ImGui.InputText("Profile name", ref active.Name, 40);

        ImGui.SetNextItemWidth(420);
        ImGui.InputText("Build link", ref active.ShowcaseUrl, 400);
        if (IsOpenableUrl(active.ShowcaseUrl))
        {
            ImGui.SameLine();
            if (ImGui.Button("Open build guide"))
            {
                OpenUrl(active.ShowcaseUrl);
            }
        }

        ImGui.Separator();
    }

    private void SetProfileBarStatus(string text, Vector4 color)
    {
        _profileBarStatus = text;
        _profileBarStatusColor = color;
        _profileBarStatusUntil = DateTime.UtcNow.AddSeconds(5);
    }

    // Kicks off a native file dialog on a background STA thread and returns immediately. The result is
    // collected later by ProcessPendingDialog so the render thread is never blocked.
    private void BeginFileDialog(bool isImport, Profile profile)
    {
        if (_dialogThread != null)
        {
            return;
        }

        _dialogIsImport = isImport;
        _dialogExportProfile = profile;
        _dialogResultPath = null;
        _dialogComplete = false;

        var suggestedName = profile != null ? SanitizeFileName(profile.Name) : null;
        var initialDir = EnsureBuildsFolder(); // computed here — DirectoryFullName is render-thread safe
        _dialogThread = new Thread(() =>
        {
            try
            {
                _dialogResultPath = RunFileDialog(save: !isImport, suggestedName: suggestedName, initialDir: initialDir);
            }
            finally
            {
                _dialogComplete = true;
            }
        });
        _dialogThread.SetApartmentState(ApartmentState.STA);
        _dialogThread.IsBackground = true;
        _dialogThread.Start();
    }

    // Once the dialog thread has finished, apply its result (export/import) on the render thread.
    private void ProcessPendingDialog()
    {
        if (_dialogThread == null || !_dialogComplete)
        {
            return;
        }

        _dialogThread.Join(); // already complete — returns immediately
        _dialogThread = null;

        var path = _dialogResultPath;
        if (path == null)
        {
            return; // user cancelled
        }

        if (_dialogIsImport)
        {
            ImportProfileFromPath(path);
        }
        else
        {
            ExportProfileToPath(_dialogExportProfile, path);
        }

        _dialogExportProfile = null;
    }

    // Writes the profile to the chosen .json file so it can be shared as a file.
    private void ExportProfileToPath(Profile profile, string path)
    {
        try
        {
            var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(path, json);
            SetProfileBarStatus($"Exported to {Path.GetFileName(path)}", Color.Lime.ToImguiVec4());
        }
        catch (Exception e)
        {
            SetProfileBarStatus($"Export failed: {e.Message}", Color.Salmon.ToImguiVec4());
        }
    }

    // Reads a profile from the chosen .json file and adds it as a new profile. Any failure leaves Settings
    // untouched and reports the reason in the profile bar.
    private void ImportProfileFromPath(string path)
    {
        try
        {
            var profile = JsonConvert.DeserializeObject<Profile>(File.ReadAllText(path));
            if (profile == null)
            {
                SetProfileBarStatus("Import failed: file did not contain a profile.", Color.Salmon.ToImguiVec4());
                return;
            }

            // Guard against nulls so the rest of the plugin can assume these lists always exist.
            profile.Rules ??= new List<SkillRule>();
            profile.Combos ??= new List<Combo>();
            profile.Name = UniqueProfileName(string.IsNullOrWhiteSpace(profile.Name) ? "Imported profile" : profile.Name);

            Settings.Profiles.Add(profile);
            Settings.ActiveProfileIndex = Settings.Profiles.Count - 1;
            SetProfileBarStatus($"Imported profile \"{profile.Name}\"", Color.Lime.ToImguiVec4());
        }
        catch (Exception e)
        {
            // Malformed JSON or unreadable file — surface it, change nothing.
            SetProfileBarStatus($"Import failed: {e.Message}", Color.Salmon.ToImguiVec4());
        }
    }

    // Shows the actual WinForms dialog (already on an STA thread). A topmost owner form keeps the dialog in
    // front of the game overlay. Returns the chosen path, or null if the user cancelled.
    private static string RunFileDialog(bool save, string suggestedName, string initialDir)
    {
        using var owner = new System.Windows.Forms.Form
        {
            TopMost = true,
            ShowInTaskbar = false,
            StartPosition = System.Windows.Forms.FormStartPosition.Manual,
            Location = new System.Drawing.Point(-4000, -4000),
            Size = new System.Drawing.Size(1, 1),
        };
        owner.Show();

        try
        {
            if (save)
            {
                using var dialog = new System.Windows.Forms.SaveFileDialog
                {
                    Title = "Export profile",
                    Filter = "ExilesAutoCore profile (*.json)|*.json",
                    DefaultExt = "json",
                    AddExtension = true,
                    InitialDirectory = initialDir,
                    FileName = string.IsNullOrWhiteSpace(suggestedName) ? "profile" : suggestedName,
                };
                return dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
            }

            using var openDialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Import profile",
                Filter = "ExilesAutoCore profile (*.json)|*.json",
                CheckFileExists = true,
                InitialDirectory = initialDir,
            };
            return openDialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK ? openDialog.FileName : null;
        }
        finally
        {
            owner.Close();
        }
    }

    // Strip characters that aren't valid in a file name so the profile name can seed the export dialog.
    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "profile";
        }

        var cleaned = string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
        return cleaned.Length == 0 ? "profile" : cleaned;
    }

    // Appends " (imported)" / a counter so an imported profile never silently shadows an existing name.
    private string UniqueProfileName(string desired)
    {
        var existing = Settings.Profiles.Select(p => p.Name).ToHashSet();
        if (!existing.Contains(desired))
        {
            return desired;
        }

        var candidate = $"{desired} (imported)";
        var n = 2;
        while (existing.Contains(candidate))
        {
            candidate = $"{desired} (imported {n++})";
        }

        return candidate;
    }

    // Only absolute http/https links are openable — never hand an arbitrary string to the shell.
    private static bool IsOpenableUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static void OpenUrl(string url)
    {
        if (!IsOpenableUrl(url))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void DrawAutomationStatus()
    {
        if (!Settings.EnableAutomation)
        {
            ImGui.TextColored(Color.Gray.ToImguiVec4(), "Automation: OFF — turn on \"Enable automation\" above to press keys.");
        }
        else if (_engine.PauseReason is "" or "Ready")
        {
            ImGui.TextColored(Color.Lime.ToImguiVec4(), "Automation: ON (running)");
        }
        else
        {
            ImGui.TextColored(Color.Orange.ToImguiVec4(), $"Automation: ON but paused — {_engine.PauseReason}");
        }

        ImGui.Text($"Last action: {_engine.LastAction}");
        ImGui.Separator();
    }
}
