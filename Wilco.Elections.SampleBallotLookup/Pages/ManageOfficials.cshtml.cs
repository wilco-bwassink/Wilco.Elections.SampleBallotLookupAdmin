using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Linq;

namespace Wilco.Elections.SampleBallotLookup.Pages
{
    public class ManageOfficialsModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ManageOfficialsModel> _logger;
        private readonly string _connectionString;

        public ManageOfficialsModel(IConfiguration config, ILogger<ManageOfficialsModel> logger)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("ElectionsDb");
        }

        [BindProperty(SupportsGet = true)]
        public string? TableName { get; set; } = "County"; // default

        // Kept for single-key fallback if needed
        [BindProperty]
        public string? Id { get; set; }

        // For composite keys (hidden inputs posted from the view)
        [BindProperty]
        public Dictionary<string, string?> Keys { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string?> Fields { get; set; } = new();

        public string? StatusMessage { get; set; }

        public List<ColumnDef> Columns { get; private set; } = new();
        public List<Dictionary<string, object?>> Rows { get; private set; } = new();

        // Multiple key columns supported; view posts all via Keys[...]
        public IReadOnlyList<string> KeyColumns => Columns.Where(c => c.IsKey).Select(c => c.ColumnName).ToList();

        // Helper fallback used in a few places
        public string KeyColumn => KeyColumns.FirstOrDefault() ?? "District_ID";

        public class ColumnDef
        {
            public string ColumnName { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public bool IsKey { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsReadOnly => IsIdentity || ReadOnly;
            public bool ReadOnly { get; set; }
            public string? DisplayName { get; set; }
            public string InputType { get; set; } = "text";
            public int Order { get; set; } = 1000;
        }

        private static readonly Dictionary<string, List<ColumnDef>> Preferred = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Federal"] = new()
            {
                new ColumnDef{ ColumnName="District_ID",       DisplayName="ID",             IsKey=true,  IsIdentity=true, ReadOnly=true, Order=0 },
                new ColumnDef{ ColumnName="District_Name",     DisplayName="District Name", InputType="text",   Order=1 },
                new ColumnDef{ ColumnName="District_Sort_Order",DisplayName="District Sort",InputType="number", Order=2, DataType="int" },
                new ColumnDef{ ColumnName="Description",       DisplayName="Description",   InputType="textarea",Order=3 },
                new ColumnDef{ ColumnName="Office_Sort_Order", DisplayName="Office Sort",   InputType="number", Order=4, DataType="int" },
                new ColumnDef{ ColumnName="Appointed_Official",DisplayName="Official",      IsKey=true, InputType="text",   Order=5 },
                new ColumnDef{ ColumnName="Web_Site",          DisplayName="Website",       InputType="url",    Order=6 }
            },
            ["State"] = new()
            {
                new ColumnDef{ ColumnName="District_ID",       DisplayName="ID",             IsKey=true,  IsIdentity=true, ReadOnly=true, Order=0 },
                new ColumnDef{ ColumnName="District_Name",     DisplayName="District Name", InputType="text",   Order=1 },
                new ColumnDef{ ColumnName="District_Sort_Order",DisplayName="District Sort",InputType="number", Order=2, DataType="int" },
                new ColumnDef{ ColumnName="Description",       DisplayName="Description",   InputType="textarea",Order=3 },
                new ColumnDef{ ColumnName="Office_Sort_Order", DisplayName="Office Sort",   InputType="number", Order=4, DataType="int" },
                new ColumnDef{ ColumnName="Appointed_Official",DisplayName="Official",      IsKey=true, InputType="text",   Order=5 },
                new ColumnDef{ ColumnName="Web_Site",          DisplayName="Website",       InputType="url",    Order=6 },
                new ColumnDef{ ColumnName="LinkID",            DisplayName="Link ID",       InputType="number", Order=7, DataType="int" }
            },
            ["County"] = new()
            {
                new ColumnDef{ ColumnName="District_ID",       DisplayName="ID",            IsKey=true,  IsIdentity=true, ReadOnly=true, Order=0 },
                new ColumnDef{ ColumnName="District_Name",     DisplayName="District Name", InputType="text",   Order=1 },
                new ColumnDef{ ColumnName="District_Sort_Order",DisplayName="District Sort",InputType="number", Order=2, DataType="int" },
                new ColumnDef{ ColumnName="Description",       DisplayName="Description",   IsKey=true, InputType="textarea",Order=3 },
                new ColumnDef{ ColumnName="Office_Sort_Order", DisplayName="Office Sort",   InputType="number", Order=4, DataType="int" },
                new ColumnDef{ ColumnName="Appointed_Official",DisplayName="Official",      InputType="text",   Order=5 },
                new ColumnDef{ ColumnName="Web_Site",          DisplayName="Website",       InputType="url",    Order=6 }
            }
        };

        public void OnGet()
        {
            Load();
        }

        public IActionResult OnPostUpdate()
        {
            try
            {
                LoadColumns();
                if (Columns.Count == 0) return Page();

                var (schema, table) = ResolveSchemaAndTable();
                using var con = new SqlConnection(_connectionString);
                con.Open();

                // Updatable columns in this post
                var updatable = Columns.Where(c => !c.IsReadOnly && Fields.ContainsKey(c.ColumnName)).ToList();
                if (updatable.Count == 0)
                {
                    StatusMessage = "Nothing to update.";
                    LoadRows();
                    return Page();
                }

                // SET clause
                var setClauses = string.Join(", ", updatable.Select((c, i) => $"[{c.ColumnName}] = @p{i}"));

                // WHERE using all key columns (composite key support)
                var keyCols = KeyColumns;
                if (keyCols.Count == 0)
                {
                    keyCols = new List<string> { KeyColumn }; // fallback
                }
                var where = string.Join(" AND ", keyCols.Select((k, i) => $"[{k}] = @k{i}"));

                using var cmd = new SqlCommand($"UPDATE [{schema}].[{table}] SET {setClauses} WHERE {where}", con);

                // SET params
                for (int i = 0; i < updatable.Count; i++)
                {
                    var col = updatable[i];
                    cmd.Parameters.AddWithValue($"@p{i}", ConvertToDbValue(col, Fields[col.ColumnName]));
                }

                // KEY params (typed)
                for (int i = 0; i < keyCols.Count; i++)
                {
                    var k = keyCols[i];
                    var keyCol = Columns.First(c => c.ColumnName.Equals(k, StringComparison.OrdinalIgnoreCase));
                    Keys.TryGetValue(k, out var raw);
                    cmd.Parameters.AddWithValue($"@k{i}", ConvertToDbValue(keyCol, raw));
                }

                var affected = cmd.ExecuteNonQuery();
                StatusMessage = affected == 1 ? "Row updated." : $"Updated {affected} rows.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update failed");
                StatusMessage = $"Update failed: {ex.Message}";
            }

            Load();
            return Page();
        }

        public IActionResult OnPostInsert()
        {
            try
            {
                LoadColumns();
                if (Columns.Count == 0) return Page();

                var (schema, table) = ResolveSchemaAndTable();
                using var con = new SqlConnection(_connectionString);
                con.Open();

                var insertable = Columns.Where(c => !c.IsReadOnly && Fields.ContainsKey(c.ColumnName)).ToList();
                if (insertable.Count == 0)
                {
                    StatusMessage = "Nothing to insert.";
                    LoadRows();
                    return Page();
                }

                var colList = string.Join(", ", insertable.Select(c => $"[{c.ColumnName}]"));
                var paramList = string.Join(", ", insertable.Select((c, i) => $"@p{i}"));
                using var cmd = new SqlCommand($"INSERT INTO [{schema}].[{table}] ({colList}) VALUES ({paramList})", con);
                for (int i = 0; i < insertable.Count; i++)
                {
                    var col = insertable[i];
                    cmd.Parameters.AddWithValue($"@p{i}", ConvertToDbValue(col, Fields[col.ColumnName]));
                }
                var affected = cmd.ExecuteNonQuery();
                StatusMessage = affected == 1 ? "Row inserted." : $"Inserted {affected} rows.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Insert failed");
                StatusMessage = $"Insert failed: {ex.Message}";
            }

            Load();
            return Page();
        }

        public IActionResult OnPostDelete()
        {
            try
            {
                LoadColumns();
                if (Columns.Count == 0) return Page();

                var (schema, table) = ResolveSchemaAndTable();
                using var con = new SqlConnection(_connectionString);
                con.Open();

                // WHERE using all key columns (composite key support)
                var keyCols = KeyColumns;
                if (keyCols.Count == 0)
                {
                    keyCols = new List<string> { KeyColumn }; // fallback
                }
                var where = string.Join(" AND ", keyCols.Select((k, i) => $"[{k}] = @k{i}"));

                using var cmd = new SqlCommand($"DELETE FROM [{schema}].[{table}] WHERE {where}", con);

                // KEY params (typed)
                for (int i = 0; i < keyCols.Count; i++)
                {
                    var k = keyCols[i];
                    var keyCol = Columns.First(c => c.ColumnName.Equals(k, StringComparison.OrdinalIgnoreCase));

                    string? raw = null;
                    if (Keys != null && Keys.TryGetValue(k, out var v)) raw = v;
                    else if (k.Equals(KeyColumn, StringComparison.OrdinalIgnoreCase)) raw = Id;

                    cmd.Parameters.AddWithValue($"@k{i}", ConvertToDbValue(keyCol, raw));
                }

                var affected = cmd.ExecuteNonQuery();
                StatusMessage = affected == 1 ? "Row deleted." : $"Deleted {affected} rows.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete failed");
                StatusMessage = $"Delete failed: {ex.Message}";
            }

            Load();
            return Page();
        }

        private void Load()
        {
            LoadColumns();
            LoadRows();
        }

        private void LoadColumns()
        {
            Columns.Clear();
            if (string.IsNullOrWhiteSpace(TableName)) return;

            var (schema, table) = ResolveSchemaAndTable();

            // Base: introspect columns from DB
            var discovered = new List<ColumnDef>();
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using var cmd = new SqlCommand(@"
                    SELECT c.COLUMN_NAME, c.DATA_TYPE,
                           CASE WHEN k.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsKey,
                           COLUMNPROPERTY(OBJECT_ID(CONCAT(@schema,'.',@table)), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    LEFT JOIN (
                        SELECT ku.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                          ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        WHERE tc.TABLE_SCHEMA = @schema AND tc.TABLE_NAME = @table AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ) k ON c.COLUMN_NAME = k.COLUMN_NAME
                    WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
                    ORDER BY c.ORDINAL_POSITION;", con);
                cmd.Parameters.AddWithValue("@schema", schema);
                cmd.Parameters.AddWithValue("@table", table);
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    discovered.Add(new ColumnDef
                    {
                        ColumnName = rdr.GetString(0),
                        DataType = rdr.GetString(1),
                        IsKey = rdr.GetInt32(2) == 1,
                        IsIdentity = rdr.IsDBNull(3) ? false : rdr.GetInt32(3) == 1
                    });
                }
            }

            // Apply preferred overrides (order, input types, labels, types)
            if (Preferred.TryGetValue(TableName!, out var prefs))
            {
                // Merge: only include columns present in prefs (keeps grid concise and in your specified order)
                var map = discovered.ToDictionary(d => d.ColumnName, StringComparer.OrdinalIgnoreCase);
                Columns = prefs
                    .Where(p => map.ContainsKey(p.ColumnName))
                    .Select(p =>
                    {
                        var d = map[p.ColumnName];
                        d.DisplayName = p.DisplayName ?? d.ColumnName;
                        d.InputType = string.IsNullOrEmpty(p.InputType) ? GuessInputType(d) : p.InputType;
                        d.ReadOnly = p.ReadOnly;
                        d.IsKey = p.IsKey || d.IsKey;
                        d.IsIdentity = p.IsIdentity || d.IsIdentity;
                        if (!string.IsNullOrEmpty(p.DataType)) d.DataType = p.DataType;
                        d.Order = p.Order;
                        return d;
                    })
                    .OrderBy(x => x.Order)
                    .ToList();
            }
            else
            {
                // Fallback: use discovered columns with guessed input types
                Columns = discovered.Select(d => { d.InputType = GuessInputType(d); return d; }).ToList();
            }
        }

        private string GuessInputType(ColumnDef col)
        {
            var type = col.DataType?.ToLowerInvariant() ?? "";
            return type switch
            {
                "bit" => "checkbox",
                "int" or "bigint" or "smallint" or "tinyint" or "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "number",
                "date" or "datetime" or "datetime2" or "smalldatetime" => "date",
                _ when col.ColumnName.Equals("Web_Site", StringComparison.OrdinalIgnoreCase) => "url",
                _ when col.ColumnName.Equals("Description", StringComparison.OrdinalIgnoreCase) => "textarea",
                _ when col.ColumnName.Equals("Appointed_Official", StringComparison.OrdinalIgnoreCase) => "text",
                _ => "text"
            };
        }

        private void LoadRows()
        {
            Rows.Clear();
            if (Columns.Count == 0) return;
            var (schema, table) = ResolveSchemaAndTable();

            // Prefer Office_Sort_Order if it exists then fall back to keys
            var hasOfficeSort = Columns.Any(c => c.ColumnName.Equals("Office_Sort_Order", StringComparison.OrdinalIgnoreCase));

            var keyOrder = KeyColumns.Count > 0 ? string.Join(", ", KeyColumns.Select(k => $"[{k}]") ) : $"[{Columns.First().ColumnName}]";
            var orderBy = hasOfficeSort ? "[Office_Sort_Order], " + keyOrder : keyOrder;

            using var con = new SqlConnection(_connectionString);
            con.Open();
            using var cmd = new SqlCommand(
                $"SELECT {string.Join(", ", Columns.Select(c => "[" + c.ColumnName + "]"))} FROM [{schema}].[{table}] ORDER BY {orderBy}", con);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < rdr.FieldCount; i++)
                {
                    row[rdr.GetName(i)] = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
                }
                Rows.Add(row);
            }
        }

        private (string schema, string table) ResolveSchemaAndTable()
        {
            var table = (TableName ?? "County").Trim();
            return ("Elections", table);
        }

        private object? ConvertToDbValue(ColumnDef col, string? raw)
        {
            if (raw == null) return DBNull.Value;
            try
            {
                switch ((col.DataType ?? "").ToLowerInvariant())
                {
                    case "bit":
                        if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)) return true;
                        if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase)) return false;
                        return string.IsNullOrWhiteSpace(raw) ? (object?)DBNull.Value : (object?)(raw == "1");
                    case "int":
                    case "bigint":
                    case "smallint":
                    case "tinyint":
                        if (int.TryParse(raw, out var i)) return i; return DBNull.Value;
                    case "decimal":
                    case "numeric":
                    case "money":
                    case "smallmoney":
                        if (decimal.TryParse(raw, out var d)) return d; return DBNull.Value;
                    case "float":
                    case "real":
                        if (double.TryParse(raw, out var f)) return f; return DBNull.Value;
                    case "date":
                    case "datetime":
                    case "datetime2":
                    case "smalldatetime":
                        if (DateTime.TryParse(raw, out var dt)) return dt; return DBNull.Value;
                    default:
                        return string.IsNullOrWhiteSpace(raw) ? (object?)DBNull.Value : raw;
                }
            }
            catch
            {
                return DBNull.Value;
            }
        }
    }
}
