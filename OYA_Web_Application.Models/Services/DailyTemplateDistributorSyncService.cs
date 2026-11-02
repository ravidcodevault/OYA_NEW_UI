using System.Globalization;
using System.Threading.Channels;
using GemBox.Spreadsheet;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace OYA_Web_Application.Models.Services;

public sealed class DailyTemplateDistributorSyncService : BackgroundService
{
    private const string UpdateSql = @"
UPDATE [dbo].[Distributor]
SET
    [Sheet] = CASE WHEN COALESCE(@InternationalRow, [InternationalRow]) IS NOT NULL THEN N'Foreign' ELSE @Sheet END,
    [Row] = @Row,
    [region] = @Region,
    [territory] = @Territory,
    [code] = @Code,
    [start] = @Start,
    [name] = @Name,
    [terr] = @Terr,
    [dsc] = @Dsc,
    [store] = @Store,
    [de] = @De,
    [Plan] = @Plan,
    [InternationalRow] = COALESCE(@InternationalRow, [InternationalRow]),
    [WestRow] = COALESCE(@WestRow, [WestRow]),
    [CentralRow] = COALESCE(@CentralRow, [CentralRow]),
    [EastRow] = COALESCE(@EastRow, [EastRow])
WHERE [name] = @ExistingName
AND EXISTS
(
    SELECT [Sheet], [Row], [region], [territory], [code], [start], [name],
           [terr], [dsc], [store], [de], [Plan], [InternationalRow],
           [WestRow], [CentralRow], [EastRow]
    EXCEPT
    SELECT CASE WHEN COALESCE(@InternationalRow, [InternationalRow]) IS NOT NULL THEN N'Foreign' ELSE @Sheet END,
           @Row, @Region, @Territory, @Code, @Start, @Name,
           @Terr, @Dsc, @Store, @De, @Plan,
           COALESCE(@InternationalRow, [InternationalRow]),
           COALESCE(@WestRow, [WestRow]),
           COALESCE(@CentralRow, [CentralRow]),
           COALESCE(@EastRow, [EastRow])
);";

    private const string InsertSql = @"
INSERT INTO [dbo].[Distributor]
(
    [Customer], [Sheet], [Row], [region], [territory], [code], [start],
    [name], [terr], [dsc], [store], [de], [Plan], [InternationalRow],
    [WestRow], [CentralRow], [EastRow], [IsActive]
)
VALUES
(
    NULL, CASE WHEN @InternationalRow IS NOT NULL THEN N'Foreign' ELSE @Sheet END,
    @Row, @Region, @Territory, @Code, @Start,
    @Name, @Terr, @Dsc, @Store, @De, @Plan, @InternationalRow,
    @WestRow, @CentralRow, @EastRow, 1
);";

    private readonly ILogger<DailyTemplateDistributorSyncService> _logger;
    private readonly string? _dailyTemplatePath;
    private readonly string? _internationalTemplatePath;
    private readonly string? _territoryTemplatePath;
    private readonly IReadOnlyList<string> _watchedTemplatePaths;
    private readonly string _connectionString;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly Channel<bool> _changes = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public DailyTemplateDistributorSyncService(
        IConfiguration configuration,
        ILogger<DailyTemplateDistributorSyncService> logger)
    {
        _logger = logger;
        _dailyTemplatePath = configuration["FilePaths:DailyReportTemplate"];
        _internationalTemplatePath = configuration["FilePaths:InternationalReport"];
        _territoryTemplatePath = configuration["FilePaths:TerritoryReportTemplate"]
            ?? configuration["FilePaths:TerritoryReport"];
        _watchedTemplatePaths = new[]
            {
                _dailyTemplatePath,
                _internationalTemplatePath,
                _territoryTemplatePath
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection was not found.");
    }

    public async Task<int> SyncNowAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_dailyTemplatePath))
        {
            throw new InvalidOperationException(
                "FilePaths:DailyReportTemplate is not configured.");
        }

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_dailyTemplatePath))
            {
                throw new FileNotFoundException(
                    "The configured daily report template was not found.",
                    _dailyTemplatePath);
            }

            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
            var rows = ReadConfigurationRows(_dailyTemplatePath);
            var templateMappings = ReadSecondaryTemplateMappings();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);
            var existingNames = await ReadExistingNamesAsync(
                connection,
                (SqlTransaction)transaction,
                cancellationToken);
            var mappingOnlyUpdates = ApplySecondaryTemplateMappings(
                rows,
                templateMappings,
                existingNames);

            var inserted = 0;
            var updated = 0;
            var unchanged = 0;
            var skipped = 0;
            foreach (var row in rows)
            {
                var key = NormalizeName(row.Name);
                var hasExisting = existingNames.TryGetValue(key, out var matches);
                if (hasExisting && matches!.Count != 1)
                {
                    _logger.LogWarning(
                        "Skipping distributor '{Name}': multiple database rows have this name.",
                        row.Name);
                    skipped++;
                    continue;
                }

                if (!hasExisting && string.IsNullOrWhiteSpace(row.Code) && !row.Start.HasValue)
                {
                    _logger.LogWarning(
                        "Skipping new distributor '{Name}': the template has no code or start date to distinguish it from a heading.",
                        row.Name);
                    skipped++;
                    continue;
                }

                await using var command = new SqlCommand(
                    hasExisting ? UpdateSql : InsertSql,
                    connection,
                    (SqlTransaction)transaction);

                if (hasExisting)
                {
                    AddString(command, "@ExistingName", matches![0]);
                }

                AddString(command, "@Sheet", row.Sheet);
                command.Parameters.Add("@Row", System.Data.SqlDbType.Float).Value = row.Row;
                AddString(command, "@Region", row.Region);
                AddString(command, "@Territory", row.Territory);
                AddString(command, "@Code", row.Code);
                AddDouble(command, "@Start", row.Start);
                AddString(command, "@Name", row.Name);
                AddDouble(command, "@Terr", row.Terr);
                AddDouble(command, "@Dsc", row.Dsc);
                AddDouble(command, "@Store", row.Store);
                AddDouble(command, "@De", row.De);
                AddDouble(command, "@Plan", row.Plan);
                AddInt(command, "@InternationalRow", row.InternationalRow);
                AddInt(command, "@WestRow", row.WestRow);
                AddInt(command, "@CentralRow", row.CentralRow);
                AddInt(command, "@EastRow", row.EastRow);

                var affected = await command.ExecuteNonQueryAsync(cancellationToken);
                if (affected > 1 || (!hasExisting && affected != 1))
                {
                    throw new InvalidOperationException(
                        $"Distributor sync expected one row for '{row.Name}' but affected {affected}.");
                }

                if (!hasExisting)
                {
                    inserted++;
                    AddIdentity(existingNames, row.Name, row.Name);
                }
                else if (affected == 1)
                {
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }

            foreach (var mapping in mappingOnlyUpdates)
            {
                var column = mapping.Target switch
                {
                    "InternationalRow" => "InternationalRow",
                    "WestRow" => "WestRow",
                    "CentralRow" => "CentralRow",
                    "EastRow" => "EastRow",
                    _ => throw new InvalidOperationException("Unsupported template mapping column.")
                };
                var sheetUpdate = mapping.Target == "InternationalRow"
                    ? ", [Sheet] = N'Foreign'"
                    : string.Empty;
                var changed = mapping.Target == "InternationalRow"
                    ? $"([{column}] IS NULL OR [{column}] <> @Row OR [Sheet] IS NULL OR [Sheet] <> N'Foreign')"
                    : $"([{column}] IS NULL OR [{column}] <> @Row)";
                await using var command = new SqlCommand(
                    $"UPDATE [dbo].[Distributor] SET [{column}] = @Row{sheetUpdate} WHERE [name] = @ExistingName AND {changed};",
                    connection,
                    (SqlTransaction)transaction);
                AddString(command, "@ExistingName", mapping.Customer);
                command.Parameters.Add("@Row", System.Data.SqlDbType.Int).Value = mapping.Row;
                var affected = await command.ExecuteNonQueryAsync(cancellationToken);
                if (affected > 1)
                {
                    throw new InvalidOperationException(
                        $"Mapping sync matched multiple rows for '{mapping.Customer}'.");
                }
                updated += affected;
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "Distributor sync: {Inserted} inserted, {Updated} updated, {Unchanged} unchanged, {Skipped} skipped.",
                inserted,
                updated,
                unchanged,
                skipped);
            return rows.Count;
        }
        catch
        {
            throw;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private List<TemplateRowMapping> ReadSecondaryTemplateMappings()
    {
        var mappings = new List<TemplateRowMapping>();
        if (!string.IsNullOrWhiteSpace(_internationalTemplatePath))
        {
            mappings.AddRange(ReadInternationalMappings(_internationalTemplatePath));
        }

        if (!string.IsNullOrWhiteSpace(_territoryTemplatePath))
        {
            mappings.AddRange(ReadTerritoryMappings(_territoryTemplatePath));
        }

        return mappings;
    }

    private static async Task<Dictionary<string, List<string>>> ReadExistingNamesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        await using var command = new SqlCommand(
            "SELECT [name] FROM [dbo].[Distributor] WITH (UPDLOCK, HOLDLOCK) WHERE [name] IS NOT NULL;",
            connection,
            transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader["name"]?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var key = NormalizeName(name);
            if (!result.TryGetValue(key, out var matches))
            {
                matches = new List<string>();
                result.Add(key, matches);
            }

            matches.Add(name);
        }

        return result;
    }

    private static void AddIdentity(
        Dictionary<string, List<string>> identities,
        string identity,
        string customer)
    {
        var key = NormalizeName(identity);
        if (!identities.TryGetValue(key, out var customers))
        {
            customers = new List<string>();
            identities.Add(key, customers);
        }

        if (!customers.Contains(customer, StringComparer.OrdinalIgnoreCase))
        {
            customers.Add(customer);
        }
    }

    private List<DatabaseMappingUpdate> ApplySecondaryTemplateMappings(
        List<DistributorTemplateRow> rows,
        IEnumerable<TemplateRowMapping> mappings,
        Dictionary<string, List<string>> existingCustomers)
    {
        var rowsByName = rows
            .GroupBy(row => NormalizeName(row.Name))
            .ToDictionary(group => group.Key, group => group.ToList());

        var mappingOnlyUpdates = new List<DatabaseMappingUpdate>();
        foreach (var mapping in mappings)
        {
            var key = NormalizeName(mapping.Name) switch
            {
                "desbodistribution" => "desbodistributioninc",
                "beautycenter" => "beautycenternicaragua",
                "americanbeautysupply" => "americanbeautysupplypanama",
                "debluebeautysupply" => "debleubeautysupply",
                "josephscollege" => "josephs",
                "midamericabeauty" => "midamerica",
                "2alternatives" => string.Empty,
                var normalized => normalized
            };
            if (key.Length == 0)
            {
                continue;
            }

            if (rowsByName.TryGetValue(key, out var templateMatches))
            {
                if (templateMatches.Count == 1)
                {
                    SetMapping(templateMatches[0], mapping);
                    continue;
                }

                _logger.LogWarning(
                    "Skipping {TemplateType} row {Row} for '{Name}': the daily template contains multiple matching customers.",
                    mapping.Target,
                    mapping.Row,
                    mapping.Name);
                continue;
            }

            if (existingCustomers.TryGetValue(key, out var customerMatches) &&
                customerMatches.Count == 1)
            {
                mappingOnlyUpdates.Add(
                    new DatabaseMappingUpdate(customerMatches[0], mapping.Row, mapping.Target));
                continue;
            }

            _logger.LogWarning(
                "Skipping {TemplateType} row {Row} for '{Name}': no unique Distributor record matches this template row.",
                mapping.Target,
                mapping.Row,
                mapping.Name);
        }

        return mappingOnlyUpdates;
    }

    private static void SetMapping(
        DistributorTemplateRow distributor,
        TemplateRowMapping mapping)
    {
        switch (mapping.Target)
        {
            case "InternationalRow":
                distributor.InternationalRow = mapping.Row;
                break;
            case "WestRow":
                distributor.WestRow = mapping.Row;
                break;
            case "CentralRow":
                distributor.CentralRow = mapping.Row;
                break;
            case "EastRow":
                distributor.EastRow = mapping.Row;
                break;
        }
    }

    private static List<TemplateRowMapping> ReadInternationalMappings(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "The configured International report template was not found.",
                path);
        }

        var workbook = ExcelFile.Load(path);
        if (workbook.Worksheets.Count < 1)
        {
            throw new InvalidDataException(
                "The International report template has no worksheet.");
        }

        var sheet = workbook.Worksheets[0];
        if (!string.Equals(CellText(sheet, "A", 2), "name", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(CellText(sheet, "C", 2), "year", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Worksheet '{sheet.Name}' does not match the expected International report layout.");
        }

        var mappings = new List<TemplateRowMapping>();
        for (var rowNumber = 3; rowNumber <= sheet.Rows.Count; rowNumber++)
        {
            if (!IsPreviousYearRow(CellText(sheet, "C", rowNumber)))
            {
                continue;
            }

            var name = CellText(sheet, "A", rowNumber);
            if (!string.IsNullOrWhiteSpace(name) && !IsPlaceholder(name))
            {
                mappings.Add(new TemplateRowMapping(name, rowNumber, "InternationalRow"));
            }
        }

        return mappings;
    }

    private static List<TemplateRowMapping> ReadTerritoryMappings(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "The configured Territory report template was not found.",
                path);
        }

        var workbook = ExcelFile.Load(path);
        if (workbook.Worksheets.Count < 3)
        {
            throw new InvalidDataException(
                "The Territory report template must contain West, Central, and East worksheets.");
        }

        var mappings = new List<TemplateRowMapping>();
        foreach (var sheet in workbook.Worksheets)
        {
            var target = sheet.Name.Contains("west", StringComparison.OrdinalIgnoreCase)
                ? "WestRow"
                : sheet.Name.Contains("central", StringComparison.OrdinalIgnoreCase)
                    ? "CentralRow"
                    : sheet.Name.Contains("east", StringComparison.OrdinalIgnoreCase)
                        ? "EastRow"
                        : null;

            if (target is null)
            {
                continue;
            }

            if (!string.Equals(CellText(sheet, "A", 2), "name", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(CellText(sheet, "C", 2), "year", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Worksheet '{sheet.Name}' does not match the expected Territory report layout.");
            }

            for (var rowNumber = 3; rowNumber <= sheet.Rows.Count; rowNumber++)
            {
                if (!IsPreviousYearRow(CellText(sheet, "C", rowNumber)))
                {
                    continue;
                }

                var name = CellText(sheet, "A", rowNumber);
                if (!string.IsNullOrWhiteSpace(name) && !IsPlaceholder(name))
                {
                    mappings.Add(new TemplateRowMapping(name, rowNumber, target));
                }
            }
        }

        if (!mappings.Any(mapping => mapping.Target == "WestRow") ||
            !mappings.Any(mapping => mapping.Target == "CentralRow") ||
            !mappings.Any(mapping => mapping.Target == "EastRow"))
        {
            throw new InvalidDataException(
                "The Territory template must include West, Central, and East worksheets.");
        }

        return mappings;
    }

    private static bool IsPreviousYearRow(string? value) =>
        string.Equals(value?.Trim(), "prev.", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value?.Trim(), "prev", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeName(string value) =>
        new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_dailyTemplatePath))
        {
            _logger.LogWarning(
                "Template sync is disabled because FilePaths:DailyReportTemplate is not configured.");
            return;
        }

        var watchers = new List<FileSystemWatcher>();
        try
        {
            foreach (var fullPath in _watchedTemplatePaths)
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                {
                    _logger.LogWarning(
                        "Cannot watch configured template path {TemplatePath} because its directory is unavailable.",
                        fullPath);
                    continue;
                }

                var watcher = new FileSystemWatcher(directory)
                {
                    Filter = Path.GetFileName(fullPath),
                    NotifyFilter = NotifyFilters.LastWrite |
                                   NotifyFilters.Size |
                                   NotifyFilters.FileName
                };

                FileSystemEventHandler changed = (_, _) => QueueSync();
                RenamedEventHandler renamed = (_, _) => QueueSync();
                watcher.Changed += changed;
                watcher.Created += changed;
                watcher.Renamed += renamed;
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }

            await TrySyncWithRetryAsync(stoppingToken);

            while (await _changes.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_changes.Reader.TryRead(out _))
                {
                    // Coalesce file-system save notifications.
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                while (_changes.Reader.TryRead(out _))
                {
                    // Wait for the last write event before reading the workbooks.
                }

                await TrySyncWithRetryAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        finally
        {
            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }
        }
    }

    private void QueueSync() => _changes.Writer.TryWrite(true);

    private async Task TrySyncWithRetryAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                var count = await SyncNowAsync(cancellationToken);
                _logger.LogInformation(
                    "Synchronized {Count} distributor configuration rows from the configured report templates.",
                    count);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (attempt < 4)
            {
                _logger.LogWarning(
                    exception,
                    "Report template sync attempt {Attempt} failed; retrying.",
                    attempt);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Report template sync failed. Database rows were not committed.");
            }
        }
    }

    private List<DistributorTemplateRow> ReadConfigurationRows(string path)
    {
        var workbook = ExcelFile.Load(path);
        if (workbook.Worksheets.Count < 2)
        {
            throw new InvalidDataException(
                "The daily report template must contain its Main and International worksheets.");
        }

        var rows = new List<DistributorTemplateRow>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var duplicateNames = new HashSet<string>(StringComparer.Ordinal);

        ReadSheet(workbook.Worksheets[0], "Main", rows, names, duplicateNames);
        ReadSheet(workbook.Worksheets[1], "International", rows, names, duplicateNames);

        foreach (var name in duplicateNames)
        {
            _logger.LogWarning(
                "Skipping distributor name '{Name}': it appears more than once in the daily template.",
                name);
        }
        rows.RemoveAll(row => duplicateNames.Contains(NormalizeName(row.Name)));

        if (rows.Count == 0)
        {
            throw new InvalidDataException(
                "No distributor configuration rows were found in the daily report template.");
        }

        return rows;
    }

    private void ReadSheet(
        ExcelWorksheet sheet,
        string sheetKey,
        List<DistributorTemplateRow> rows,
        HashSet<string> names,
        HashSet<string> duplicateNames)
    {
        var regionHeader = CellText(sheet, "A", 1);
        var territoryHeader = CellText(sheet, "B", 1);
        var nameHeader = CellText(sheet, "F", 1);

        if (!string.Equals(regionHeader, "region", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(territoryHeader, "territory", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(nameHeader, "name", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Worksheet '{sheet.Name}' does not match the expected customer configuration layout.");
        }

        for (var rowNumber = 2; rowNumber <= sheet.Rows.Count; rowNumber++)
        {
            var region = CellText(sheet, "A", rowNumber);
            if (!IsDistributorRegion(sheetKey, region))
            {
                continue;
            }

            var name = CellText(sheet, "F", rowNumber);
            if (string.IsNullOrWhiteSpace(name) || IsPlaceholder(name))
            {
                continue;
            }

            var key = NormalizeName(name);
            if (key.Length == 0 || !names.Add(key))
            {
                duplicateNames.Add(key);
                continue;
            }

            rows.Add(new DistributorTemplateRow
            {
                Sheet = sheetKey,
                Row = rowNumber,
                Region = region,
                Territory = CellText(sheet, "B", rowNumber),
                Code = CellText(sheet, "C", rowNumber),
                Start = CellNumber(sheet, "E", rowNumber),
                Name = name,
                Terr = CellNumber(sheet, "G", rowNumber),
                Dsc = CellNumber(sheet, "H", rowNumber),
                Store = CellNumber(sheet, "I", rowNumber),
                De = CellNumber(sheet, "J", rowNumber),
                Plan = CellNumber(sheet, "AB", rowNumber)
            });
        }
    }

    private static bool IsDistributorRegion(string sheetKey, string? region) =>
        sheetKey == "Main"
            ? region?.StartsWith("USA ", StringComparison.OrdinalIgnoreCase) == true
            : region is not null &&
              (region.Equals("Canada", StringComparison.OrdinalIgnoreCase) ||
               region.Equals("Mexico / Central", StringComparison.OrdinalIgnoreCase) ||
               region.Equals("Caribic", StringComparison.OrdinalIgnoreCase) ||
               region.Equals("South America", StringComparison.OrdinalIgnoreCase));

    private static string? CellText(ExcelWorksheet sheet, string column, int row)
    {
        var value = sheet.Cells[$"{column}{row}"].Value;
        if (value is null)
        {
            return null;
        }

        if (value is double doubleValue)
        {
            return doubleValue.ToString("0.################", CultureInfo.InvariantCulture);
        }

        if (value is decimal decimalValue)
        {
            return decimalValue.ToString("0.################", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
    }

    private static double? CellNumber(ExcelWorksheet sheet, string column, int row)
    {
        var text = CellText(sheet, column, row);
        if (string.IsNullOrWhiteSpace(text) || IsPlaceholder(text))
        {
            return null;
        }

        if (double.TryParse(
                text,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var value))
        {
            return value;
        }

        if (double.TryParse(
                text,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture,
                out value))
        {
            return value;
        }

        throw new InvalidDataException(
            $"Template cell {column} contains a non-numeric configuration value.");
    }

    private static bool IsPlaceholder(string value) =>
        value.Trim().All(character => character is '-' or '—' or '–');

    private static void AddString(SqlCommand command, string name, string? value)
    {
        command.Parameters.Add(name, System.Data.SqlDbType.NVarChar, 255)
            .Value = string.IsNullOrWhiteSpace(value)
                ? DBNull.Value
                : value.Trim();
    }

    private static void AddDouble(SqlCommand command, string name, double? value)
    {
        command.Parameters.Add(name, System.Data.SqlDbType.Float)
            .Value = value.HasValue ? value.Value : DBNull.Value;
    }

    private static void AddInt(SqlCommand command, string name, int? value)
    {
        command.Parameters.Add(name, System.Data.SqlDbType.Int)
            .Value = value.HasValue ? value.Value : DBNull.Value;
    }

    private sealed class TemplateRowMapping(string name, int row, string target)
    {
        public string Name { get; } = name;
        public int Row { get; } = row;
        public string Target { get; } = target;
    }

    private sealed class DatabaseMappingUpdate(string customer, int row, string target)
    {
        public string Customer { get; } = customer;
        public int Row { get; } = row;
        public string Target { get; } = target;
    }

    private sealed class DistributorTemplateRow
    {
        public required string Sheet { get; init; }
        public required int Row { get; init; }
        public string? Region { get; init; }
        public string? Territory { get; init; }
        public string? Code { get; init; }
        public double? Start { get; init; }
        public required string Name { get; init; }
        public double? Terr { get; init; }
        public double? Dsc { get; init; }
        public double? Store { get; init; }
        public double? De { get; init; }
        public double? Plan { get; init; }
        public int? InternationalRow { get; set; }
        public int? WestRow { get; set; }
        public int? CentralRow { get; set; }
        public int? EastRow { get; set; }
    }
}
