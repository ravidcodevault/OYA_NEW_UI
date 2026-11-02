using System.Data;
using OYA_Web_Application.Models;

using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;
using SqlDataReader = Microsoft.Data.SqlClient.SqlDataReader;

namespace OYA_Web_Application.Models.Services
{
    public class DatabaseEntryRepository
    {
        private readonly string _connectionString;

        public DatabaseEntryRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection was not found.");
        }

        // =====================================================
        // GET ALL
        // =====================================================

        public async Task<List<DatabaseEntryModel>> GetAllAsync()
        {
            var result = new List<DatabaseEntryModel>();

            const string sql = @"
                SELECT
                    [Customer],
                    [Sheet],
                    [Row],
                    [region],
                    [territory],
                    [HQ],
                    [code],
                    [start],
                    [name],
                    [terr],
                    [dsc],
                    [store],
                    [de],
                    [PrevYearTotal],
                    [Plan],
                    [InternationalRow],
                    [WestRow],
                    [EastRow],
                    [CentralRow],
                    [IsActive]
                FROM [Quickbooks].[dbo].[Distributor]
                WHERE [Customer] IS NOT NULL
                  AND LTRIM(RTRIM([Customer])) <> ''
                ORDER BY [name];
            ";

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(sql, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new DatabaseEntryModel
                {
                    Customer = GetString(reader, "Customer"),
                    Sheet = GetString(reader, "Sheet"),
                    Row = GetDouble(reader, "Row"),

                    Region = GetString(reader, "region"),
                    Territory = GetString(reader, "territory"),
                    HQ = GetString(reader, "HQ"),
                    Code = GetString(reader, "code"),

                    Start = GetDouble(reader, "start"),
                    Name = GetString(reader, "name"),

                    Terr = GetDouble(reader, "terr"),
                    Dsc = GetDouble(reader, "dsc"),
                    Store = GetDouble(reader, "store"),
                    De = GetDouble(reader, "de"),

                    PrevYearTotal =
                        GetDouble(reader, "PrevYearTotal"),

                    Plan =
                        GetDouble(reader, "Plan"),

                    InternationalRow =
                        GetInt(reader, "InternationalRow"),

                    WestRow =
                        GetInt(reader, "WestRow"),

                    EastRow =
                        GetInt(reader, "EastRow"),

                    CentralRow =
                        GetInt(reader, "CentralRow"),

                    IsActive =
                        GetBool(reader, "IsActive")
                });
            }

            return result;
        }


        // =====================================================
        // INSERT
        // =====================================================

        public async Task<bool> InsertAsync(
            DatabaseEntryModel distributor)
        {
            if (distributor == null)
            {
                throw new ArgumentNullException(
                    nameof(distributor));
            }

            if (string.IsNullOrWhiteSpace(
                distributor.Customer))
            {
                throw new ArgumentException(
                    "Customer is required.",
                    nameof(distributor.Customer));
            }

            if (string.IsNullOrWhiteSpace(
                distributor.Name))
            {
                throw new ArgumentException(
                    "Distributor name is required.",
                    nameof(distributor.Name));
            }

            const string sql = @"
                INSERT INTO [Quickbooks].[dbo].[Distributor]
                (
                    [Customer],
                    [Sheet],
                    [Row],
                    [region],
                    [territory],
                    [HQ],
                    [code],
                    [start],
                    [name],
                    [terr],
                    [dsc],
                    [store],
                    [de],
                    [PrevYearTotal],
                    [Plan],
                    [InternationalRow],
                    [WestRow],
                    [EastRow],
                    [CentralRow],
                    [IsActive]
                )
                VALUES
                (
                    @Customer,
                    @Sheet,
                    @Row,
                    @Region,
                    @Territory,
                    @HQ,
                    @Code,
                    @Start,
                    @Name,
                    @Terr,
                    @Dsc,
                    @Store,
                    @De,
                    @PrevYearTotal,
                    @Plan,
                    @InternationalRow,
                    @WestRow,
                    @EastRow,
                    @CentralRow,
                    @IsActive
                );
            ";

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(sql, connection);


            // =================================================
            // STRING PARAMETERS
            // =================================================

            AddStringParameter(
                command,
                "@Customer",
                distributor.Customer);

            AddStringParameter(
                command,
                "@Sheet",
                distributor.Sheet);

            AddStringParameter(
                command,
                "@Region",
                distributor.Region);

            AddStringParameter(
                command,
                "@Territory",
                distributor.Territory);

            AddStringParameter(
                command,
                "@HQ",
                distributor.HQ);

            AddStringParameter(
                command,
                "@Code",
                distributor.Code);

            AddStringParameter(
                command,
                "@Name",
                distributor.Name);


            // =================================================
            // DOUBLE PARAMETERS
            // =================================================

            AddDoubleParameter(
                command,
                "@Row",
                distributor.Row);

            AddDoubleParameter(
                command,
                "@Start",
                distributor.Start);

            AddDoubleParameter(
                command,
                "@Terr",
                distributor.Terr);

            AddDoubleParameter(
                command,
                "@Dsc",
                distributor.Dsc);

            AddDoubleParameter(
                command,
                "@Store",
                distributor.Store);

            AddDoubleParameter(
                command,
                "@De",
                distributor.De);

            AddDoubleParameter(
                command,
                "@PrevYearTotal",
                distributor.PrevYearTotal);

            AddDoubleParameter(
                command,
                "@Plan",
                distributor.Plan);


            // =================================================
            // INTEGER PARAMETERS
            // =================================================

            AddIntParameter(
                command,
                "@InternationalRow",
                distributor.InternationalRow);

            AddIntParameter(
                command,
                "@WestRow",
                distributor.WestRow);

            AddIntParameter(
                command,
                "@EastRow",
                distributor.EastRow);

            AddIntParameter(
                command,
                "@CentralRow",
                distributor.CentralRow);


            // =================================================
            // BIT
            // =================================================

            command.Parameters.Add(
                new SqlParameter(
                    "@IsActive",
                    SqlDbType.Bit)
                {
                    Value = distributor.IsActive
                });


            // =================================================
            // EXECUTE
            // =================================================

            var affectedRows =
                await command.ExecuteNonQueryAsync();

            return affectedRows == 1;
        }


        // =====================================================
        // STRING PARAMETER
        // =====================================================

        private static void AddStringParameter(
            SqlCommand command,
            string name,
            string? value)
        {
            command.Parameters.Add(
                new SqlParameter(
                    name,
                    SqlDbType.NVarChar,
                    255)
                {
                    Value =
                        string.IsNullOrWhiteSpace(value)
                            ? DBNull.Value
                            : value.Trim()
                });
        }


        // =====================================================
        // DOUBLE PARAMETER
        // =====================================================

        private static void AddDoubleParameter(
            SqlCommand command,
            string name,
            double? value)
        {
            command.Parameters.Add(
                new SqlParameter(
                    name,
                    SqlDbType.Float)
                {
                    Value =
                        value.HasValue
                            ? value.Value
                            : DBNull.Value
                });
        }


        // =====================================================
        // INT PARAMETER
        // =====================================================

        private static void AddIntParameter(
            SqlCommand command,
            string name,
            int? value)
        {
            command.Parameters.Add(
                new SqlParameter(
                    name,
                    SqlDbType.Int)
                {
                    Value =
                        value.HasValue
                            ? value.Value
                            : DBNull.Value
                });
        }


        // =====================================================
        // GET STRING
        // =====================================================

        private static string? GetString(
            SqlDataReader reader,
            string column)
        {
            var index =
                reader.GetOrdinal(column);

            if (reader.IsDBNull(index))
            {
                return null;
            }

            return Convert.ToString(
                reader.GetValue(index));
        }


        // =====================================================
        // GET DOUBLE
        // =====================================================

        private static double? GetDouble(
            SqlDataReader reader,
            string column)
        {
            var index =
                reader.GetOrdinal(column);

            if (reader.IsDBNull(index))
            {
                return null;
            }

            return Convert.ToDouble(
                reader.GetValue(index));
        }


        // =====================================================
        // GET INT
        // =====================================================

        private static int? GetInt(
            SqlDataReader reader,
            string column)
        {
            var index =
                reader.GetOrdinal(column);

            if (reader.IsDBNull(index))
            {
                return null;
            }

            return Convert.ToInt32(
                reader.GetValue(index));
        }


        // =====================================================
        // GET BOOL
        // =====================================================

        private static bool GetBool(
            SqlDataReader reader,
            string column)
        {
            var index =
                reader.GetOrdinal(column);

            if (reader.IsDBNull(index))
            {
                return false;
            }

            return Convert.ToBoolean(
                reader.GetValue(index));
        }
    }
}