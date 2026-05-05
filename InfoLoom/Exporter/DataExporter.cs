using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Colossal.PSI.Environment;
using Game.City;
using Game.Simulation;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.WorkforceData;
using InfoLoomTwo.Systems.WorkplacesData;
using Unity.Entities;

namespace InfoLoomTwo.Exporter
{
    /// <summary>
    /// Utility class for exporting InfoLoom system data to CSV files.
    /// Files are written to: %AppData%\..\LocalLow\Colossal Order\Cities Skylines II\ModsData\InfoLoomTwo\
    /// File naming: {dataType}_{cityName}_{dayOfYear}_{year}.csv
    /// Old files beyond the configured retention limit are automatically pruned.
    /// </summary>
    public static class DataExporter
    {
        /// <summary>Output directory path, initialized on first use via <see cref="EnsureOutputDirectory"/>.</summary>
        public static string OutputPath { get; private set; }

        private static bool s_Initialized;

        /// <summary>
        /// Ensures the output directory exists. Must be called before any export.
        /// Called automatically by <see cref="ExportCsv"/>.
        /// </summary>
        public static void EnsureOutputDirectory()
        {
            if (s_Initialized)
                return;

            OutputPath = Path.Combine(EnvPath.kUserDataPath, "ModsData", "InfoLoomTwo");

            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);

            s_Initialized = true;
            Mod.log.Info($"[DataExporter] Output path: {OutputPath}");
        }

        /// <summary>
        /// Exports data to a CSV file. Creates a new timestamped file each call and prunes old files.
        /// </summary>
        /// <param name="dataType">Short identifier used as the filename prefix, e.g. "workforce".</param>
        /// <param name="header">CSV header row, e.g. "level,employed,unemployed".</param>
        /// <param name="rows">Enumerable of CSV data rows (no newlines needed).</param>
        /// <param name="cityName">City name to embed in the filename. Pass null to omit.</param>
        /// <param name="timestamp">Timestamp to embed in the filename. Pass null to use the current wall-clock time.</param>
        /// <param name="maxFilesToKeep">Maximum number of files to retain for this data type. Oldest are deleted first.</param>
        /// <param name="replaceExisting">When true, writes to a fixed file ({dataType}.csv), overwriting any previous export. When false, a timestamped file is created and old files are pruned.</param>
        public static void ExportCsv(
            string dataType,
            string header,
            IEnumerable<string> rows,
            string cityName = null,
            DateTime? timestamp = null,
            int maxFilesToKeep = 5,
            bool replaceExisting = false)
        {
            EnsureOutputDirectory();

            string filePath;
            if (replaceExisting)
            {
                // Fixed filename — always overwrite, no pruning needed
                filePath = Path.Combine(OutputPath, $"{dataType}.csv");
            }
            else
            {
                DateTime ts = timestamp ?? DateTime.Now;
                string citySegment = string.IsNullOrEmpty(cityName)
                    ? string.Empty
                    : "_" + SanitizeFileName(cityName);
                filePath = Path.Combine(OutputPath, $"{dataType}{citySegment}_{ts:yyyyMMdd_HHmmss}.csv");
            }

            try
            {
                using (var sw = new StreamWriter(filePath, false, new UTF8Encoding(true)))
                {
                    sw.WriteLine(header);
                    foreach (string row in rows)
                        sw.WriteLine(row);
                }

                Mod.log.Info($"[DataExporter] Exported '{dataType}' to {filePath}");
                if (!replaceExisting)
                    PruneOldFiles(dataType, maxFilesToKeep);
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[DataExporter] Failed to export '{dataType}': {ex.Message}");
            }
        }

        /// <summary>
        /// Convenience overload that resolves city name and in-game date via the ECS world.
        /// </summary>
        public static void ExportCsv(
            World world,
            string dataType,
            string header,
            IEnumerable<string> rows,
            int maxFilesToKeep = 5,
            bool replaceExisting = false)
        {
            string cityName = null;
            DateTime? gameDate = null;

            try
            {
                var cityConfig = world?.GetExistingSystemManaged<CityConfigurationSystem>();
                if (cityConfig != null)
                    cityName = cityConfig.cityName;

                var timeSystem = world?.GetExistingSystemManaged<TimeSystem>();
                if (timeSystem != null)
                    gameDate = timeSystem.GetCurrentDateTime();
            }
            catch
            {
                // Best-effort; fall back to wall-clock time and no city name
            }

            ExportCsv(dataType, header, rows, cityName, gameDate, maxFilesToKeep, replaceExisting);
        }

        /// <summary>
        /// Deletes the oldest files matching the given data type prefix when count exceeds <paramref name="maxFilesToKeep"/>.
        /// </summary>
        private static void PruneOldFiles(string dataType, int maxFilesToKeep)
        {
            if (maxFilesToKeep <= 0)
                return;

            try
            {
                var dir = new DirectoryInfo(OutputPath);
                FileInfo[] files = dir
                    .GetFiles($"{dataType}*.csv")
                    .OrderBy(f => f.CreationTimeUtc)
                    .ToArray();

                int toDelete = files.Length - maxFilesToKeep;
                for (int i = 0; i < toDelete; i++)
                {
                    Mod.log.Info($"[DataExporter] Pruning old export: {files[i].Name}");
                    files[i].Delete();
                }
            }
            catch (Exception ex)
            {
                Mod.log.Warn($"[DataExporter] Could not prune old files for '{dataType}': {ex.Message}");
            }
        }

        /// <summary>Strips characters that are invalid in Windows file/folder names.</summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            return sb.ToString();
        }

        // ─── High-level orchestration (called directly from Settings buttons) ──────

        public static void ExportAll()
        {
            var s = Mod.setting;
            if (s == null) return;
            if (s.exportWorkforce)    ExportWorkforce();
            if (s.exportDemographics) ExportDemographics();
            if (s.exportWorkplaces)   ExportWorkplaces();
        }

        public static void ExportWorkforce()
        {
            var s = Mod.setting;
            if (s == null) return;
            var world = World.DefaultGameObjectInjectionWorld;
            var system = world?.GetExistingSystemManaged<WorkforceSystem>();
            if (system == null || !system.m_Results.IsCreated || system.m_Results.Length == 0)
            {
                Mod.log.Warn("[DataExporter] workforce: no data available.");
                return;
            }

            // Temporarily override to city-wide if requested
            Unity.Entities.Entity savedDistrict = system.SelectedDistrict;
            bool didOverride = s.exportWorkforceCityWide && savedDistrict != Unity.Entities.Entity.Null;
            if (didOverride)
            {
                system.SelectedDistrict = Unity.Entities.Entity.Null;
                system.RecalculateNow();
            }

            const string header = "level,total,worker,unemployed,unemploymentRate,employable,outside,underemployed,homeless";
            var rows = new List<string>(system.m_Results.Length);
            string[] levelNames = { "Uneducated", "PoorlyEducated", "Educated", "WellEducated", "HighlyEducated", "Totals" };
            for (int i = 0; i < system.m_Results.Length; i++)
            {
                WorkforcesInfo r = system.m_Results[i];
                string levelName = i < levelNames.Length ? levelNames[i] : i.ToString();
                rows.Add($"{levelName},{r.Total},{r.Worker},{r.Unemployed},{r.UnemploymentRate:F2},{r.Employable},{r.Outside},{r.Under},{r.Homeless}");
            }

            ExportCsv(world, "workforce", header, rows, s.exportFilesRetentionCount, s.exportReplaceExisting);

            if (didOverride)
                system.SelectedDistrict = savedDistrict;
        }

        public static void ExportDemographics()
        {
            var s = Mod.setting;
            if (s == null) return;
            var world = World.DefaultGameObjectInjectionWorld;
            var system = world?.GetExistingSystemManaged<Demographics>();
            if (system == null)
            {
                Mod.log.Warn("[DataExporter] demographics: system not found.");
                return;
            }

            // If the user wants city-wide data but a district is currently selected,
            // temporarily clear the selection, recalculate, then restore afterwards.
            Unity.Entities.Entity savedDistrict = system.SelectedDistrict;
            bool didOverride = s.exportDemoCityWide && savedDistrict != Unity.Entities.Entity.Null;
            if (didOverride)
            {
                system.SelectedDistrict = Unity.Entities.Entity.Null;
                system.UpdateDemographics();
            }

            // Build a single CSV. Each row has: grouping, label, total, [employment cols], [education cols]
            string header = BuildDemoHeader(s);
            var rows = new List<string>();

            // Per-age detail (1yr groups) — mirrors UI's finest-grained view
            if (s.exportDemoPerAge && system.m_Results.IsCreated && system.m_Results.Length > 0)
            {
                for (int i = 0; i < system.m_Results.Length; i++)
                {
                    PopulationDetailedGroupInfo r = system.m_Results[i];
                    rows.Add(BuildDemoRow("1yr", r.Age.ToString(), r.Total,
                        r.Work, r.School1, r.School2, r.School3, r.School4, r.Unemployed, r.Retired,
                        r.Uneducated, r.PoorlyEducated, r.Educated, r.WellEducated, r.HighlyEducated, r.ChildOrTeenWithNoSchool, s));
                }
            }

            // 5-year groups — mirrors UI's Five-Year grouping strategy
            if (s.exportDemoFiveYear && system.m_FiveYearDetails.IsCreated && system.m_FiveYearDetails.Length > 0)
            {
                for (int i = 0; i < system.m_FiveYearDetails.Length; i++)
                {
                    PopulationFiveYearGroupInfo r = system.m_FiveYearDetails[i];
                    string label = r.Age + 4 >= 120 ? r.Age + "-120" : r.Age + "-" + (r.Age + 4);
                    rows.Add(BuildDemoRow("5yr", label, r.Total,
                        r.Work, r.School1, r.School2, r.School3, r.School4, r.Unemployed, r.Retired,
                        r.Uneducated, r.PoorlyEducated, r.Educated, r.WellEducated, r.HighlyEducated, r.ChildOrTeenWithNoSchool, s));
                }
            }

            // 10-year groups — mirrors UI's Ten-Year grouping strategy
            if (s.exportDemoTenYear && system.m_TenYearDetails.IsCreated && system.m_TenYearDetails.Length > 0)
            {
                for (int i = 0; i < system.m_TenYearDetails.Length; i++)
                {
                    PopulationTenYearGroupInfo r = system.m_TenYearDetails[i];
                    string label = r.Age + 9 >= 120 ? r.Age + "-120" : r.Age + "-" + (r.Age + 9);
                    rows.Add(BuildDemoRow("10yr", label, r.Total,
                        r.Work, r.School1, r.School2, r.School3, r.School4, r.Unemployed, r.Retired,
                        r.Uneducated, r.PoorlyEducated, r.Educated, r.WellEducated, r.HighlyEducated, r.ChildOrTeenWithNoSchool, s));
                }
            }

            // Lifecycle groups (Child/Teen/Adult/Elderly) — mirrors UI's Lifecycle grouping
            if (s.exportDemoLifecycle && system.m_LifecycleDetails.IsCreated && system.m_LifecycleDetails.Length > 0)
            {
                string[] groupNames = { "Child", "Teen", "Adult", "Elderly" };
                for (int i = 0; i < system.m_LifecycleDetails.Length; i++)
                {
                    PopulationLifecycleInfo r = system.m_LifecycleDetails[i];
                    string label = i < groupNames.Length ? groupNames[i] : r.Group.ToString();
                    rows.Add(BuildDemoRow("lifecycle", label, r.Total,
                        r.Work, r.School1, r.School2, r.School3, r.School4, r.Unemployed, r.Retired,
                        r.Uneducated, r.PoorlyEducated, r.Educated, r.WellEducated, r.HighlyEducated, r.ChildOrTeenWithNoSchool, s));
                }
            }

            // City-wide totals appended as extra rows (grouping="totals", label=metric name, total=value, rest blank)
            if (s.exportDemoTotals && system.m_Totals.IsCreated && system.m_Totals.Length > 0)
            {
                string[] totalsNames = { "AllCitizens", "Locals", "Tourists", "Commuters", "Students", "Workers", "OldestCitizenAge", "MovingAways", "DeadCitizens", "HomelessCitizens" };
                for (int i = 0; i < system.m_Totals.Length; i++)
                {
                    string metric = i < totalsNames.Length ? totalsNames[i] : i.ToString();
                    rows.Add(BuildDemoTotalsRow(metric, system.m_Totals[i], s));
                }
            }

            if (rows.Count > 0)
                ExportCsv(world, "demographics", header, rows, s.exportFilesRetentionCount, s.exportReplaceExisting);
            else
                Mod.log.Warn("[DataExporter] demographics: no groupings enabled, nothing to export.");

            // Restore the original district selection — the next OnUpdate() will recalculate for the UI.
            if (didOverride)
                system.SelectedDistrict = savedDistrict;
        }

        /// <summary>Builds the CSV header with grouping + label columns followed by enabled data columns.</summary>
        private static string BuildDemoHeader(Setting s)
        {
            var cols = new List<string> { "grouping", "label", "total" };
            if (s.exportDemoEmploymentCols)
                cols.AddRange(new[] { "work", "school1_elementary", "school2_highschool", "school3_college", "school4_university", "unemployed", "retired" });
            if (s.exportDemoEducationCols)
                cols.AddRange(new[] { "uneducated", "poorlyEducated", "educated", "wellEducated", "highlyEducated", "childOrTeenNoSchool" });
            return string.Join(",", cols);
        }

        /// <summary>Builds a single data row for a demographics grouping section.</summary>
        private static string BuildDemoRow(
            string grouping, string label, int total,
            int work, int school1, int school2, int school3, int school4, int unemployed, int retired,
            int uneducated, int poorlyEd, int educated, int wellEd, int highlyEd, int childNoSchool,
            Setting s)
        {
            var cols = new List<string> { grouping, label, total.ToString() };
            if (s.exportDemoEmploymentCols)
            {
                cols.Add(work.ToString());
                cols.Add(school1.ToString());
                cols.Add(school2.ToString());
                cols.Add(school3.ToString());
                cols.Add(school4.ToString());
                cols.Add(unemployed.ToString());
                cols.Add(retired.ToString());
            }
            if (s.exportDemoEducationCols)
            {
                cols.Add(uneducated.ToString());
                cols.Add(poorlyEd.ToString());
                cols.Add(educated.ToString());
                cols.Add(wellEd.ToString());
                cols.Add(highlyEd.ToString());
                cols.Add(childNoSchool.ToString());
            }
            return string.Join(",", cols);
        }

        /// <summary>Builds a totals row — data columns are left blank since totals are single-value metrics.</summary>
        private static string BuildDemoTotalsRow(string metric, int value, Setting s)
        {
            int extraCols = 0;
            if (s.exportDemoEmploymentCols) extraCols += 7; // work, school1-4, unemployed, retired
            if (s.exportDemoEducationCols)  extraCols += 6; // uneducated, poorlyEd, educated, wellEd, highlyEd, childNoSchool
            string blanks = extraCols > 0 ? new string(',', extraCols) : string.Empty;
            return "totals," + metric + "," + value + blanks;
        }

        public static void ExportWorkplaces()
        {
            var s = Mod.setting;
            if (s == null) return;
            var world = World.DefaultGameObjectInjectionWorld;
            var system = world?.GetExistingSystemManaged<WorkplacesSystem>();
            if (system == null || !system.m_Results.IsCreated || system.m_Results.Length == 0)
            {
                Mod.log.Warn("[DataExporter] workplaces: no data available.");
                return;
            }

            // Temporarily override to city-wide if requested
            Unity.Entities.Entity savedDistrict = system.SelectedDistrict;
            bool didOverride = s.exportWorkplacesCityWide && savedDistrict != Unity.Entities.Entity.Null;
            if (didOverride)
            {
                system.SelectedDistrict = Unity.Entities.Entity.Null;
                system.RecalculateNow();
            }

            const string header = "level,total,service,commercial,leisure,extractor,industrial,office,employee,open,commuter";
            var rows = new List<string>(system.m_Results.Length);
            for (int i = 0; i < system.m_Results.Length; i++)
            {
                WorkplacesInfo r = system.m_Results[i];
                rows.Add($"{r.Level},{r.Total},{r.Service},{r.Commercial},{r.Leisure},{r.Extractor},{r.Industrial},{r.Office},{r.Employee},{r.Open},{r.Commuter}");
            }

            ExportCsv(world, "workplaces", header, rows, s.exportFilesRetentionCount, s.exportReplaceExisting);

            if (didOverride)
                system.SelectedDistrict = savedDistrict;
        }
    }
}
