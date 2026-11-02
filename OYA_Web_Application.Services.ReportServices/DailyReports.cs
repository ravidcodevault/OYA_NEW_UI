using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GemBox.Spreadsheet;
using Microsoft.Extensions.Configuration;
using OYA_Web_Application.Models.Data.Reports;

namespace OYA_Web_Application.Services.ReportServices;

public class DailyReports
{
    private readonly IConfiguration _configuration;

    private static string path = "";

    private static string outputPath = "";

    private string connectionString = "";

    private static string qodbcConnString = "DSN=Quickbooks Data;OLE DB Services=-2;";

    private static DateTime now = DateTime.Now;

    private static string logPath = "C:\\LOG\\Helias Sales Report\\" + DateTime.Now.ToString().Replace(":", "").Replace("/", "");

    private static string template = "Helias Sales Report - Template v24 Contractor.xlsx";

    private static string mainSheetName = "OYASales2024Letter net sales";

    private static string mainStartCell = "G2";

    private static string accountsCol = "F";

    private static string prevYearCol = "Z";

    private static string year2DatePercentCol = "Y";

    private static int internationalRow = 41;

    private static int otherRow = 26;

    private static int canadaRow = 28;

    private static int mexicoRow = 29;

    private static int trinidadTobagoRow = 30;

    private DateTime startOfLastYear = new DateTime(DateTime.Now.Year - 1, 1, 1);

    private DateTime endOfLastYear = new DateTime(DateTime.Now.Year - 1, DateTime.Now.Month, DateTime.Now.Day);

    private static string internationalSheetName = "OYASales2024Letter net sales 2";

    private static string internationalStartCell = "G2";

    private static string canadaSheetName = "OYASls24 Canada table";

    private static string westSheetName = "OYASls24 West table";

    private static string centralSheetName = "OYASls24 Central table";

    private static string eastSheetName = "OYASls24 East table";

    private static string dateCell = "P1";

    private static string startCell = "D3";

    private static string canadaEndCell = "U47";

    private static string westEndCell = "U41";

    private static string centralEndCell = "U61";

    private static string eastEndCell = "U65";

    private static string[] oyaClosedList = new string[14]
    {
        "OYA Affinity Beauty Concepts", "OYA Vintage Salon Supplies", "OYA Beauty Sales", "OYA GF Distributors", "OYA Independent SS", "OYA Kevin Ray Distributor Group", "OYA Lunica Beauty LLC", "OYA New England Salon Solutions", "OYA PA Beauty Supply", "OYA Princess Beauty Supply",
        "OYA Sweis", "OYA Hair Whisper", "OYA Vintage Canada", "OYA Beauty Van-Mexico"
    };

    private static string[] oyaJobList = new string[53]
    {
        "OYA ISCS:ISCS Derek Moser", "OYA ISCS:ISCS Michelle Fanning", "OYA M Beauty:Andrea's", "OYA M Beauty:Ariel Derrick (Residence)", "OYA M Beauty:Breanna Taylor (Residence)", "OYA M Beauty:Elite Salon", "OYA M Beauty:Flaunt Salon", "OYA M Beauty:Fran Medina (Residence)", "OYA M Beauty:GAL Studios", "OYA M Beauty:Irene Fernandez",
        "OYA M Beauty:Jagged Edge", "OYA M Beauty:Karen Poulsen", "OYA M Beauty:Koolfx Salon at Mesa Royale", "OYA M Beauty:Linda Reese (Residence)", "OYA M Beauty:Marc Reid Salon @ Steven Paul Salon", "OYA M Beauty:Mira Bella Hair Salon", "OYA M Beauty:R Salon Day Spa - Donna Raffanti", "OYA M Beauty:Salon Boutique - Arrowhead", "OYA M Beauty:Schwahn Salon", "OYA M Beauty:Shaggy's",
        "OYA M Beauty:Shear Creations", "OYA M Beauty:Stasyss Salon", "OYA M Beauty:Sublime Salon", "OYA Unique Salon Concepts:Lisa Gagne", "OYA Uberbeauty808", "OYA C.K. ColorHouse", "OYA Store Professional", "OYA Store Consumer", "OYA M Beauty", "OYA Josephs College",
        "OYA Blaze Salon", "OYA ProSalon Distribution Group", "OYA Central States Supply", "OYA Elite Salon Distributors USA OHIO", "OYA Discover Salon Services", "OYA Mid America Beauty Supply", "OYA Economy", "OYA Elite Salon Services", "OYA Valley Beauty Supply", "OYA Unique Salon Concepts",
        "OYA Style Care", "OYA ISCS", "OYA Prime Salon Services", "OYA Aidan Laura Aine Ltd - ALA", "OYA Desbo Distribution Inc.", "OYA SOID Distributors, P.R.", "OYA De Bleu Beauty Supply", "OYA Binani Desbo Group", "OYA Braliza LLC", "OYA NCN Beauty LLC",
        "OYA NCN Beauty LLC Venezuela", "OYA NCN Beauty LLC Ecuador", "OYA Brothers Beauty Products"
    };

    private ExcelWorksheet oSheet;

    private ExcelWorksheet sheetInternational;

    private ExcelWorksheet sheetCanada;

    private ExcelWorksheet sheetWest;

    private ExcelWorksheet sheetCentral;

    private ExcelWorksheet sheetEast;

    private static string[] oyaList = new string[40]
    {
        "OYA Vintage Salon Supplies", "OYA Kevin Ray Distributor Group", "OYA Uberbeauty808", "OYA Josephs College", "OYA Blaze Salon", "OYA ProSalon Distribution Group", "OYA Central States Supply", "OYA Blast of Beauty", "OYA New England Salon Solutions", "OYA Elite Salon Services",
        "OYA Economy", "OYA ISCS", "OYA ISCS - Other", "ISCS Derek Moser", "ISCS Michelle Fanning", "OYA Prime Salon Services", "OYA Affinity Beauty Concepts", "OYA Hair Whisper", "OYA Vintage Canada", "OYA Beauty Van-Mexico",
        "OYA Independent SS", "OYA Sweis", "OYA PA Beauty Supply", "OYA GF Distributors", "OYA Princess Beauty Supply", "OYA Kmac2", "OYA Lunica Beauty LLC", "OYA Unique Salon Concepts", "OYA Mid America Beauty Supply", "OYA Beauty Sales",
        "OYA Style Care", "OYA  Aidan Laura Aine Ltd", "Total OYA M Beauty", "OYA M Beauty - Sample Sub Account", "Flaunt Salon", "Irene Fernandez", "Karen Poulsen", "Sassy Chic", "Schwahn Salon", "OYA Jagged Edge"
    };

    private List<string> oyaContractorList = new List<string>();

    private List<Distributor> distributorList = new List<Distributor>();

    private List<Contractor> contractorList = new List<Contractor>();

    private List<Sales> contractorsThisMonthList = new List<Sales>();

    private List<Sales> OverallOyaSales = new List<Sales>();

    private List<Sales> OverallPinoSales = new List<Sales>();

    private decimal internetThisMonthToDate;

    private decimal contractorsThisMonthToDate;

    private int pinoCustomerCount;

    private byte[] content;

    private static readonly Dictionary<int, (string col, string colTerritory)> MonthColumnMap = new Dictionary<int, (string, string)>
    {
        {
            1,
            ("L", "G")
        },
        {
            2,
            ("M", "H")
        },
        {
            3,
            ("N", "I")
        },
        {
            4,
            ("O", "J")
        },
        {
            5,
            ("P", "K")
        },
        {
            6,
            ("Q", "L")
        },
        {
            7,
            ("R", "M")
        },
        {
            8,
            ("S", "N")
        },
        {
            9,
            ("T", "O")
        },
        {
            10,
            ("U", "P")
        },
        {
            11,
            ("V", "Q")
        },
        {
            12,
            ("W", "R")
        }
    };

    private static readonly Dictionary<string, decimal> QBSalonsData = new Dictionary<string, decimal>
    {
        {
            Normalize("Alexandra Shirzad Shirzad"),
            52.00m
        },
        {
            Normalize("Ashley Kowalczyk"),
            303.75m
        },
        {
            Normalize("Beach Bum Tanz LLC MILLER"),
            25.00m
        },
        {
            Normalize("Becky Box"),
            226.15m
        },
        {
            Normalize("Brandie Rounds"),
            25.00m
        },
        {
            Normalize("Chem-Steel Distro Roger Cribb"),
            162.44m
        },
        {
            Normalize("CHESNEY TIPTON Tipton"),
            175.00m
        },
        {
            Normalize("Colton Gieselman"),
            35.85m
        },
        {
            Normalize("Deb parker Deb parker"),
            9.50m
        },
        {
            Normalize("Denee Clifford"),
            71.70m
        },
        {
            Normalize("Diana S Kurzka"),
            21.00m
        },
        {
            Normalize("DS Embodyment Salon & Spa"),
            248.50m
        },
        {
            Normalize("DS Hair Venue"),
            146.00m
        },
        {
            Normalize("DS Jordan Deisbeck"),
            455.50m
        },
        {
            Normalize("DS Spa Isbell"),
            114.35m
        },
        {
            Normalize("DSCA InnerChi, LLC"),
            790.65m
        },
        {
            Normalize("DSCA Joni Nauton"),
            183.40m
        },
        {
            Normalize("DSCA Penny Colbert"),
            119.50m
        },
        {
            Normalize("DSCO Tanya Auville"),
            269.05m
        },
        {
            Normalize("DSGA Shawon Pinder"),
            288.00m
        },
        {
            Normalize("DSID Rebecca Williams"),
            334.30m
        },
        {
            Normalize("DSNV Chris The Hair Guy"),
            463.74m
        },
        {
            Normalize("DSUT Steur and Friends"),
            166.40m
        },
        {
            Normalize("Jillian Vissa"),
            76.45m
        },
        {
            Normalize("Julie Brahier"),
            420.65m
        },
        {
            Normalize("karen axsom"),
            69.00m
        },
        {
            Normalize("Karen Baez Baez"),
            209.05m
        },
        {
            Normalize("Katie Barton Katie Barton"),
            116.00m
        },
        {
            Normalize("Marina Guerrero"),
            372.00m
        },
        {
            Normalize("marladietch@hotmail.com"),
            57.30m
        },
        {
            Normalize("OLIVIA HOWELL-CORA & COMPANY OLIVIA HOWEL"),
            57.00m
        },
        {
            Normalize("Patricia Wheat"),
            15.50m
        },
        {
            Normalize("Ricardo Moya"),
            0.00m
        },
        {
            Normalize("Tiffany Sears"),
            62.50m
        },
        {
            Normalize("TLP HOLDINGS LLC POULIOT"),
            30.00m
        },
        {
            Normalize("Trade Show - OYA"),
            10322.00m
        },
        {
            Normalize("TRANSITIONS OF INDIANA-INDIANAPOLIS Brack"),
            198.00m
        },
        {
            Normalize("Trudy Peterson"),
            252.65m
        },
        {
            Normalize("Virginia Cady"),
            138.00m
        },
        {
            Normalize("Yeny Marcela Garcia"),
            478.00m
        }
    };

    public DailyReports(IConfiguration configuration)
    {
        _configuration = configuration;
        path = _configuration["TemplatePath"];
        outputPath = _configuration["ReportOutputPath"];
        connectionString = _configuration.GetConnectionString("DefaultConnection");
    }

    private void FetchData()
    {
        using SqlConnection connection = new SqlConnection(connectionString);
        using SqlCommand command = new SqlCommand("dbo.USP_GetSalesDataForReports", connection);
        command.CommandType = CommandType.StoredProcedure;
        connection.Open();
        command.Parameters.Add("@thisYear", SqlDbType.Int).Value = DateTime.Now.Year;
        command.Parameters.Add("@thisMonth", SqlDbType.Int).Value = DateTime.Now.Month;
        SqlDataAdapter adapt = new SqlDataAdapter(command);
        DataSet ds = new DataSet();
        adapt.Fill(ds);
        DataTable overallSalesTable = ds.Tables[0];
        foreach (DataRow _row in overallSalesTable.Rows)
        {
            Sales item = new Sales();
            item.Customer = Convert.ToString(_row["Customer"]);
            item.Class = Convert.ToString(_row["ClassRefFullName"]);
            item.Month = Convert.ToInt32(_row["Month"]);
            item.Year = Convert.ToInt32(_row["Year"]);
            item.Rep = Convert.ToString(_row["SalesRepRefFullName"]);
            item.CustomerType = Convert.ToString(_row["CustomerTypeRefFullName"]);
            item.TxnDate = Convert.ToDateTime(_row["TxnDate"]);
            if (_row["Amount"] != DBNull.Value)
            {
                item.Amount = Convert.ToDecimal(_row["Amount"]);
            }
            if (item.Customer == "OYA PESA S.A., Guatemala" &&
                item.Month == 8 &&
                item.Year == 2026)
            {
                Console.WriteLine("========== SQL -> C# ==========");
                Console.WriteLine($"Customer : {item.Customer}");
                Console.WriteLine($"Amount   : {item.Amount}");
                Console.WriteLine($"TxnDate  : {item.TxnDate}");
                Console.WriteLine($"Class    : {item.Class}");
                Console.WriteLine("===============================");
            }
            OverallOyaSales.Add(item);
        }
        overallSalesTable = ds.Tables[1];
        foreach (DataRow _row2 in overallSalesTable.Rows)
        {
            Sales item2 = new Sales();
            item2.Customer = Convert.ToString(_row2["Customer"]);
            item2.Class = Convert.ToString(_row2["ClassRefFullName"]);
            item2.Month = Convert.ToInt32(_row2["Month"]);
            item2.Year = Convert.ToInt32(_row2["Year"]);
            item2.Rep = Convert.ToString(_row2["SalesRepRefFullName"]);
            item2.CustomerType = Convert.ToString(_row2["CustomerTypeRefFullName"]);
            item2.TxnDate = Convert.ToDateTime(_row2["TxnDate"]);
            if (_row2["Amount"] != DBNull.Value)
            {
                item2.Amount = Convert.ToDecimal(_row2["Amount"]);
            }
            OverallPinoSales.Add(item2);
        }
    }

    private int GetRowByLabel(ExcelWorksheet sheet, string label)
    {
        for (int num = 1; num <= sheet.Rows.Count; num++)
        {
            string val = sheet.Cells["B" + num].Value?.ToString();
            if (!string.IsNullOrEmpty(val) && val.Equals(label, StringComparison.OrdinalIgnoreCase))
            {
                return num;
            }
        }
        throw new Exception(label + " row not found");
    }

    private void GenerateExcelv2()
    {
        ExcelFile oWB = ExcelFile.Load(path + template);
        oSheet = oWB.Worksheets[0];
        sheetInternational = oWB.Worksheets[1];
        int salonsDirectRow = GetRowByLabel(oSheet, "Salons Direct");
        int publicDirectRow = salonsDirectRow + 2;
        int allOyaRow = salonsDirectRow + 4;
        int pinoRow = salonsDirectRow + 5;
        int heliasRow = salonsDirectRow + 6;
        string mainEndCell = prevYearCol + heliasRow;
        string internationalEndCell = prevYearCol + internationalRow;
        string allOyaAcctsCell = accountsCol + allOyaRow;
        string pinoAcctsCell = accountsCol + pinoRow;
        string heliasAcctsCell = accountsCol + heliasRow;
        distributorList = GetDistributor();
        contractorList = GetContractors();
        oyaContractorList = contractorList.Select((Contractor contractor) => contractor.Code).ToList();
        DateTime today = DateTime.Today;
        DateTime startOfCurrentYear = new DateTime(today.Year, 1, 1);
        DateTime startOfLastYear = new DateTime(today.Year - 1, 1, 1);
        DateTime endOfLastYear = new DateTime(today.Year - 1, today.Month, today.Day);
        try
        {
            for (int month = 1; month <= now.Month; month++)
            {
                if (!MonthColumnMap.ContainsKey(month))
                {
                    continue;
                }
                (string, string) tuple = MonthColumnMap[month];
                string col = tuple.Item1;
                string colTerritory = tuple.Item2;
                List<Sales> currentMonthOya = OverallOyaSales.Where((Sales sales) => sales.Month == month && sales.Year == now.Year).ToList();
                List<Sales> currentMonthPino = OverallPinoSales.Where((Sales sales) => sales.Month == month && sales.Year == now.Year).ToList();
                distributorList.ForEach(delegate (Distributor distributor)
                {
                    ExcelWorksheet excelWorksheet = ((distributor.Sheet == "Main") ? oSheet : sheetInternational);
                    decimal salesByCustomer = GetSalesByCustomer(distributor.Customer, currentMonthOya);
                    decimal salesByCustomer2 = GetSalesByCustomer(distributor.Customer, OverallOyaSales.Where((Sales sales) => sales.Year != now.Year).ToList());
                    decimal valueOrDefault = OverallOyaSales.Where((Sales sales) => sales.Customer == distributor.Customer && sales.TxnDate >= startOfCurrentYear && sales.TxnDate <= today).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    decimal valueOrDefault2 = OverallOyaSales.Where((Sales sales) => sales.Customer == distributor.Customer && sales.TxnDate >= startOfLastYear && sales.TxnDate <= endOfLastYear).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    decimal num = ((valueOrDefault2 == 0m) ? 0m : ((valueOrDefault - valueOrDefault2) / valueOrDefault2 * 100m));
                    excelWorksheet.Cells[col + distributor.Row].Value = salesByCustomer;
                    excelWorksheet.Cells["K" + distributor.Row].Value = salesByCustomer2;
                    excelWorksheet.Cells[year2DatePercentCol + distributor.Row].Value = Math.Round(num) + " %";
                    excelWorksheet.Cells[prevYearCol + distributor.Row].Value = valueOrDefault2;
                });
                contractorList.ForEach(delegate (Contractor contractor)
                {
                    decimal valueOrDefault = OverallOyaSales.Where((Sales sales) => sales.Rep == contractor.Code && sales.Month == month && sales.Year == now.Year).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    decimal valueOrDefault2 = OverallOyaSales.Where((Sales sales) => sales.Rep == contractor.Code && sales.Year != now.Year).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    decimal valueOrDefault3 = OverallOyaSales.Where((Sales sales) => sales.Rep == contractor.Code && sales.Year != now.Year && sales.TxnDate <= endOfLastYear).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    sheetInternational.Cells[col + contractor.Row].Value = valueOrDefault;
                    sheetInternational.Cells["K" + contractor.Row].Value = valueOrDefault2;
                    sheetInternational.Cells[prevYearCol + contractor.Row].Value = valueOrDefault3;
                });
                oSheet.Cells[col + pinoRow].Value = currentMonthPino.Sum((Sales sales) => sales.Amount);
                oSheet.Cells[col + allOyaRow].Value = currentMonthOya.Sum((Sales sales) => sales.Amount);
                oSheet.Cells[col + salonsDirectRow].Value = currentMonthOya.Where((Sales sales) => sales.Class?.ToLower() == "salons direct").Sum((Sales sales) => sales.Amount);
                oSheet.Cells[col + publicDirectRow].Value = currentMonthOya.Where((Sales sales) => sales.Class?.ToLower() == "public direct" || sales.Class?.ToLower() == "amazon").Sum((Sales sales) => sales.Amount);
                pinoCustomerCount = currentMonthPino.Select((Sales sales) => sales.Customer).Distinct().Count();
                oSheet.Cells[allOyaAcctsCell].Value = distributorList.Count + " accts";
                oSheet.Cells[pinoAcctsCell].Value = pinoCustomerCount + " accts";
                oSheet.Cells[heliasAcctsCell].Value = distributorList.Count + pinoCustomerCount + " accts";
            }
            IEnumerable<Sales> lastYear = OverallOyaSales.Where((Sales sales) => sales.Year != now.Year);
            oSheet.Cells[prevYearCol + allOyaRow].Value = lastYear.Where((Sales sales) => sales.TxnDate <= endOfLastYear).Sum((Sales sales) => sales.Amount);
            oSheet.Cells[prevYearCol + pinoRow].Value = OverallPinoSales.Where((Sales sales) => sales.Year != now.Year && sales.TxnDate <= endOfLastYear).Sum((Sales sales) => sales.Amount);
            oSheet.Cells["K" + allOyaRow].Value = lastYear.Sum((Sales sales) => sales.Amount);
            using MemoryStream ms = new MemoryStream();
            oWB.Save(ms, SaveOptions.XlsxDefault);
            content = ms.ToArray();
        }
        catch
        {
        }
    }

    private void GenerateExcel()
    {

        Console.WriteLine("********** ENTERED GenerateExcel **********");



        int thisMonth = now.Month;
     

        Console.WriteLine("Loading workbook...");
        ExcelFile oWB = ExcelFile.Load(path + template);
        Console.WriteLine("Workbook Loaded Successfully");

        Console.WriteLine("Worksheet Count = " + oWB.Worksheets.Count);

        for (int i = 0; i < oWB.Worksheets.Count; i++)
        {
            Console.WriteLine($"Sheet {i}: {oWB.Worksheets[i].Name}");
        }
        oSheet = oWB.Worksheets[0];
        sheetInternational = oWB.Worksheets[1];


        Console.WriteLine("Main Sheet = " + oSheet.Name);
        Console.WriteLine("International Sheet = " + sheetInternational.Name);

        Console.WriteLine("A1 Main = " + oSheet.Cells["A1"].Value);
        Console.WriteLine("B1 Main = " + oSheet.Cells["B1"].Value);
        Console.WriteLine("B3 Main = " + oSheet.Cells["B3"].Value);

        Console.WriteLine("A1 International = " + sheetInternational.Cells["A1"].Value);
        Console.WriteLine("B1 International = " + sheetInternational.Cells["B1"].Value);
        Console.WriteLine("B7 International = " + sheetInternational.Cells["B7"].Value);


        int salonsDirectRow = GetRowByLabel(oSheet, "Salons Direct");
        int publicDirectRow = salonsDirectRow + 2;
        int allOyaRow = salonsDirectRow + 4;
        int pinoRow = salonsDirectRow + 5;
        int heliasRow = salonsDirectRow + 6;
        string mainEndCell = prevYearCol + heliasRow;
        string internationalEndCell = prevYearCol + internationalRow;
        string allOyaAcctsCell = accountsCol + allOyaRow;
        string pinoAcctsCell = accountsCol + pinoRow;
        string heliasAcctsCell = accountsCol + heliasRow;
        distributorList = (from distributor in GetDistributor()
                           where distributor.Row > 0
                           orderby distributor.Row
                           select distributor).ToList();

        Console.WriteLine("Distributor Count = " + distributorList.Count);

        foreach (var d in distributorList)
        {
            Console.WriteLine($"{d.Customer} | {d.Sheet} | {d.Row}");
        }
        contractorList = GetContractors();
        oyaContractorList = contractorList.Select((Contractor contractor) => contractor.Code).ToList();
        try
        {
            for (int i9 = 1; i9 <= now.Month; i9++)
            {
                thisMonth = i9;
                (string, string) tuple = MonthColumnMap[thisMonth];
                string col = tuple.Item1;
                string colTerritory = tuple.Item2;
                DateTime today = DateTime.Today;
                DateTime startOfCurrentYear = new DateTime(today.Year, 1, 1);
                DateTime startOfLastYear = new DateTime(today.Year - 1, 1, 1);
                DateTime endOfLastYear = new DateTime(today.Year - 1, today.Month, today.Day);
                List<Sales> currentMonthData = OverallOyaSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).ToList();
                List<Sales> lastYearData = OverallOyaSales.Where((Sales sales) => sales.Year != now.Year).ToList();
                List<Sales> currentYtdData = OverallOyaSales.Where((Sales sales) => sales.TxnDate >= startOfCurrentYear && sales.TxnDate <= today).ToList();
                List<Sales> previousYtdData = OverallOyaSales.Where((Sales sales) => sales.TxnDate >= startOfLastYear && sales.TxnDate <= endOfLastYear).ToList();
                distributorList.ForEach(delegate (Distributor distributor)
                {
                    ExcelWorksheet excelWorksheet = ((distributor.Sheet == "Main") ? oSheet : sheetInternational);

                    excelWorksheet.Cells[col + distributor.Row].Value =
                        GetSalesByCustomer(
                            distributor.Customer,
                            currentMonthData.Where(sales =>
                                sales.Customer.Trim().Equals(distributor.Customer.Trim(),
                                StringComparison.OrdinalIgnoreCase))
                            .ToList());

                    excelWorksheet.Cells["K" + distributor.Row].Value =
                        GetSalesByCustomer(
                            distributor.Customer,
                            lastYearData.Where(sales =>
                                sales.Customer.Trim().Equals(distributor.Customer.Trim(),
                                StringComparison.OrdinalIgnoreCase))
                            .ToList());

                    decimal currentYTD = currentYtdData
                        .Where(sales => sales.Customer.Trim().Equals(distributor.Customer.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                        .Sum(sales => sales.Amount)
                        .GetValueOrDefault();

                    decimal previousYTD = previousYtdData
                        .Where(sales => sales.Customer.Trim().Equals(distributor.Customer.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                        .Sum(sales => sales.Amount)
                        .GetValueOrDefault();

                    decimal growth = 0;

                    if (previousYTD != 0)
                        growth = (currentYTD - previousYTD) / previousYTD * 100;

                    excelWorksheet.Cells[year2DatePercentCol + distributor.Row].Value = Math.Round(growth) + " %";
                    excelWorksheet.Cells[prevYearCol + distributor.Row].Value = previousYTD;

                    // DEBUG
                    if (distributor.Customer.Contains("PESA"))
                    {
                        Console.WriteLine("====================================");
                        Console.WriteLine("Distributor : " + distributor.Customer);
                        Console.WriteLine("Sheet       : " + distributor.Sheet);
                        Console.WriteLine("Row         : " + distributor.Row);
                        Console.WriteLine("Month Total : " + GetSalesByCustomer(distributor.Customer, currentMonthData));
                        Console.WriteLine("Current YTD : " + currentYTD);
                        Console.WriteLine("PreviousYTD : " + previousYTD);
                        Console.WriteLine("====================================");
                    }
                });
                contractorList.ForEach(delegate (Contractor contractor)
                {
                    decimal valueOrDefault = currentMonthData.Where((Sales sales) => sales.Rep.Equals(contractor.Code)).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    sheetInternational.Cells[col + contractor.Row].Value = valueOrDefault;
                    decimal valueOrDefault2 = previousYtdData.Where((Sales sales) => sales.Rep.Equals(contractor.Code)).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    sheetInternational.Cells[prevYearCol + contractor.Row].Value = valueOrDefault2;
                    decimal valueOrDefault3 = lastYearData.Where((Sales sales) => sales.Rep.Equals(contractor.Code)).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                    sheetInternational.Cells["K" + contractor.Row].Value = valueOrDefault3;
                });
                oSheet.Cells[col + pinoRow].Value = OverallPinoSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).Sum((Sales sales) => sales.Amount);
                oSheet.Cells[col + allOyaRow].Value = OverallOyaSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).Sum((Sales sales) => sales.Amount);
                decimal salonsDirectSales = OverallOyaSales.Where((Sales sales) => sales.Class.ToLower() == "salons direct" && sales.Month == thisMonth && sales.Year == now.Year).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                decimal publicDirectSales = OverallOyaSales.Where((Sales sales) => sales.Class.ToLower() == "public direct" && sales.Month == thisMonth && sales.Year == now.Year).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                decimal amazonSales = OverallOyaSales.Where((Sales sales) => sales.Class.ToLower() == "amazon" && sales.Month == thisMonth && sales.Year == now.Year).Sum((Sales sales) => sales.Amount).GetValueOrDefault();
                oSheet.Cells[col + salonsDirectRow].Value = salonsDirectSales;
                oSheet.Cells[col + publicDirectRow].Value = publicDirectSales + amazonSales;
                oSheet.Cells[col + allOyaRow].Value = OverallOyaSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).Sum((Sales sales) => sales.Amount);
                oSheet.Cells[col + pinoRow].Value = OverallPinoSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).Sum((Sales sales) => sales.Amount);
                pinoCustomerCount = OverallPinoSales.DistinctBy((Sales sales) => sales.Customer).Count();
                oSheet.Cells[allOyaAcctsCell].Value = distributorList.Count() + " accts";
                oSheet.Cells[pinoAcctsCell].Value = pinoCustomerCount + "  accts";
                oSheet.Cells[heliasAcctsCell].Value = distributorList.Count() + pinoCustomerCount + " accts";
            }
            List<Sales> lastYearOya = OverallOyaSales.Where((Sales sales) => sales.Year != now.Year && sales.TxnDate <= this.endOfLastYear).ToList();
            List<Sales> lastYearPino = OverallPinoSales.Where((Sales sales) => sales.Year != now.Year && sales.TxnDate <= this.endOfLastYear).ToList();
            oSheet.Cells[prevYearCol + allOyaRow].Value = lastYearOya.Sum((Sales sales) => sales.Amount).GetValueOrDefault();
            oSheet.Cells[prevYearCol + pinoRow].Value = lastYearPino.Sum((Sales sales) => sales.Amount).GetValueOrDefault();
            Dictionary<string, decimal> lastYearClassWise = (from sales in lastYearOya
                                                             group sales by (sales.Class != null) ? sales.Class.ToLower() : "").ToDictionary((IGrouping<string, Sales> grouping) => grouping.Key, (IGrouping<string, Sales> source) => source.Sum((Sales sales) => sales.Amount).GetValueOrDefault());
            decimal salonsDirectLastYear = lastYearClassWise.GetValueOrDefault("salons direct");
            decimal publicDirectLastYear = lastYearClassWise.GetValueOrDefault("public direct");
            decimal amazonLastYear = lastYearClassWise.GetValueOrDefault("amazon");
            oSheet.Cells[prevYearCol + salonsDirectRow].Value = salonsDirectLastYear;
            oSheet.Cells[prevYearCol + publicDirectRow].Value = publicDirectLastYear + amazonLastYear;
            oSheet.Cells["K" + salonsDirectRow].Value = salonsDirectLastYear;
            oSheet.Cells["K" + publicDirectRow].Value = publicDirectLastYear + amazonLastYear;
            oSheet.Cells["K" + pinoRow].Value = lastYearPino.Sum((Sales sales) => sales.Amount).GetValueOrDefault();
            oSheet.Cells["K" + allOyaRow].Value = lastYearOya.Sum((Sales sales) => sales.Amount).GetValueOrDefault();
            if (now.Year == 2026)
            {
                oSheet.Cells[prevYearCol + allOyaRow].Value = OverallOyaSales.Where((Sales sales) => sales.TxnDate <= this.endOfLastYear && sales.Year != now.Year).Sum((Sales sales) => sales.Amount);
                oSheet.Cells[prevYearCol + pinoRow].Value = OverallPinoSales.Where((Sales sales) => sales.TxnDate <= this.endOfLastYear && sales.Year != now.Year).Sum((Sales sales) => sales.Amount);
                oSheet.Cells[prevYearCol + publicDirectRow].Value = OverallOyaSales.Where((Sales sales) => sales.Class.ToLower() == "internet order" && sales.CustomerType.ToLower() != "other" && sales.Year != now.Year && sales.TxnDate <= this.endOfLastYear).Sum((Sales sales) => sales.Amount);
                oSheet.Cells[prevYearCol + salonsDirectRow].Value = OverallOyaSales.Where((Sales sales) => sales.CustomerType.ToLower() == "other" && sales.Year != now.Year && sales.TxnDate <= this.endOfLastYear).Sum((Sales sales) => sales.Amount);
                IEnumerable<Sales> lastYear = OverallOyaSales.Where((Sales sales) => sales.Year != now.Year);
                oSheet.Cells["K" + salonsDirectRow].Value = lastYear.Where((Sales sales) => sales.CustomerType.ToLower() == "other").Sum((Sales sales) => sales.Amount);
                oSheet.Cells["K" + publicDirectRow].Value = lastYear.Where((Sales sales) => sales.Class.ToLower() == "internet order" && sales.CustomerType.ToLower() != "other").Sum((Sales sales) => sales.Amount);
                oSheet.Cells["K" + pinoRow].Value = OverallPinoSales.Where((Sales sales) => sales.Year != now.Year).Sum((Sales sales) => sales.Amount);
                oSheet.Cells["K" + allOyaRow].Value = lastYear.Sum((Sales sales) => sales.Amount);
            }
            using MemoryStream memoryStream = new MemoryStream();
            oWB.Save(memoryStream, SaveOptions.XlsxDefault);
            content = memoryStream.ToArray();
        }
        catch (Exception)
        {
        }
    }

    public byte[] DownloadExcel()
    {
        FetchData();
        SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
        SpreadsheetInfo.FreeLimitReached += delegate (object? sender, FreeLimitEventArgs e9)
        {
            e9.FreeLimitReachedAction = FreeLimitReachedAction.ContinueAsTrial;
        };
        GenerateExcel();
        return content;
    }

    private static decimal GetSalesByCustomer(string customer, IList<Sales> list)
    {
        decimal value = default(decimal);
        List<Sales> filteredList = list.Where((Sales sales) => sales.Customer.ToLower().Equals(customer.ToLower())).ToList();
        if (filteredList.Count() > 0)
        {
            return filteredList.Sum((Sales sales) => sales.Amount).GetValueOrDefault();
        }
        return value;
    }

    public List<Contractor> GetContractors()
    {
        List<Contractor> contractorList = new List<Contractor>();
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();
        using (SqlCommand com = new SqlCommand("SELECT Code, Row, Sheet, Contractor FROM Contractor", conn))
        {
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read())
            {
                if (reader["Code"] != DBNull.Value)
                {
                    contractorList.Add(new Contractor
                    {
                        Code = reader["Code"].ToString(),
                        Row = Convert.ToInt32(reader["Row"]),
                        Sheet = reader["Sheet"].ToString(),
                        Label = reader["Contractor"].ToString()
                    });
                }
            }
            reader.Close();
        }
        conn.Close();
        return contractorList;
    }

    public List<Distributor> GetDistributor()
    {
        List<Distributor> distributorList = new List<Distributor>();
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();
        using (SqlCommand com = new SqlCommand("SELECT Customer, Sheet, Row FROM Distributor WHERE IsActive = 1 ORDER BY Sheet, Row", conn))
        {
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read())
            {
                distributorList.Add(new Distributor
                {
                    Customer = Convert.ToString(reader["Customer"]),
                    Row = reader["Row"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Row"]),
                    Sheet = reader["Sheet"] == DBNull.Value ? "" : reader["Sheet"].ToString()
                });
            }
            reader.Close();
        }
        conn.Close();
        return distributorList;
    }

    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }
        string value = input.ToLower().Trim();
        int commaIndex = value.IndexOf(",");
        if (commaIndex > 0)
        {
            value = value.Substring(0, commaIndex);
        }
        value = Regex.Replace(value, "\\s+", " ");
        return value.Trim();
    }

    public static void CompareCustomerTotals(List<Sales> overallOyaSales, int thisMonth, int year)
    {
        List<Sales> systemData = overallOyaSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == year).ToList();
        var systemGrouped = (from sales in systemData
                             group sales by Normalize(sales.Customer)).ToDictionary((IGrouping<string, Sales> grouping) => grouping.Key, (IGrouping<string, Sales> source) => new
                             {
                                 Total = source.Sum((Sales sales) => sales.Amount).GetValueOrDefault(),
                                 Count = source.Count(),
                                 MinDate = source.Min((Sales sales) => sales.TxnDate),
                                 MaxDate = source.Max((Sales sales) => sales.TxnDate),
                                 Classes = (from sales in source
                                            group sales by sales.Class ?? "NULL").ToDictionary((IGrouping<string, Sales> cg) => cg.Key, (IGrouping<string, Sales> cg) => cg.Sum((Sales sales) => sales.Amount))
                             });
        Console.WriteLine("Customer | QB | System | Diff | Count | DateRange");
        Console.WriteLine("------------------------------------------------------------------");
        decimal totalDiff = default(decimal);
        foreach (KeyValuePair<string, decimal> qb in QBSalonsData)
        {
            bool exists = systemGrouped.ContainsKey(qb.Key);
            decimal systemValue = (exists ? systemGrouped[qb.Key].Total : 0m);
            decimal diff = qb.Value - systemValue;
            totalDiff += diff;
            if (!(diff != 0m))
            {
                continue;
            }
            int count = (exists ? systemGrouped[qb.Key].Count : 0);
            string minDate = (exists ? systemGrouped[qb.Key].MinDate.ToShortDateString() : "-");
            string maxDate = (exists ? systemGrouped[qb.Key].MaxDate.ToShortDateString() : "-");
            Console.WriteLine($"{qb.Key} | {qb.Value} | {systemValue} | {diff} | {count} | {minDate} - {maxDate}");
            if (!exists)
            {
                continue;
            }
            Console.WriteLine("   Class Breakdown:");
            foreach (KeyValuePair<string, decimal?> cls in systemGrouped[qb.Key].Classes)
            {
                Console.WriteLine($"      {cls.Key} : {cls.Value}");
            }
        }
        Console.WriteLine("------------------------------------------------------------------");
        Console.WriteLine("TOTAL DIFF: " + totalDiff);
        Console.WriteLine("Month: " + thisMonth);
        HashSet<string> qbKeys = QBSalonsData.Keys.ToHashSet();
        var extraCustomers = (from keyValuePair in Enumerable.Where(systemGrouped, keyValuePair => !qbKeys.Contains(keyValuePair.Key))
                              select new
                              {
                                  Customer = keyValuePair.Key,
                                  Total = keyValuePair.Value.Total,
                                  Count = keyValuePair.Value.Count
                              } into anon
                              orderby anon.Total descending
                              select anon).Take(20).ToList();
        Console.WriteLine("\nTop System Customers NOT in QB:");
        foreach (var item in extraCustomers)
        {
            Console.WriteLine($"{item.Customer} | {item.Total} | {item.Count}");
        }
        List<KeyValuePair<string, decimal>> missingInSystem = QBSalonsData.Where<KeyValuePair<string, decimal>>((KeyValuePair<string, decimal> keyValuePair) => !systemGrouped.ContainsKey(keyValuePair.Key)).ToList();
        Console.WriteLine("\nQB Customers Missing in System:");
        foreach (KeyValuePair<string, decimal> item2 in missingInSystem)
        {
            Console.WriteLine($"{item2.Key} | {item2.Value}");
        }
    }

    public static void FindExactMissing309(List<Sales> overallOyaSales, int thisMonth, int year)
    {
        List<Sales> data = overallOyaSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == year).ToList();
        var matches = (from sales in data.Where(delegate (Sales sales)
        {
            string key = Normalize(sales.Customer);
            string value = sales.Class?.Trim().ToLower();
            return QBSalonsData.ContainsKey(key) && string.IsNullOrWhiteSpace(value);
        })
                       select new { sales.Customer, sales.Amount, sales.Class, sales.TxnDate }).ToList();
        decimal total = matches.Sum(anon => anon.Amount).GetValueOrDefault();
        Console.WriteLine("---- EXACT MISSING (SHOULD BE SALONS) ----");
        foreach (var m9 in matches)
        {
            Console.WriteLine($"{m9.Customer} | {m9.Amount} | {m9.Class ?? "NULL"} | {m9.TxnDate}");
        }
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("TOTAL: " + total);
    }

    public static void AuditTransactionClassification(List<Sales> overallOyaSales, int thisMonth, int year)
    {
        List<Sales> data = overallOyaSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == year).ToList();
        Console.WriteLine("---- POTENTIAL CLASSIFICATION ISSUES ----");
        Console.WriteLine("Customer | Amount | Class | Issue");
        Console.WriteLine("----------------------------------------------------------");
        foreach (Sales x9 in data)
        {
            string normalizedCustomer = Normalize(x9.Customer);
            string cls = x9.Class?.Trim().ToLower();
            string issue = null;
            if (string.IsNullOrWhiteSpace(cls))
            {
                issue = "NULL CLASS";
            }
            else if (cls != "salons direct" && cls != "public direct" && cls != "amazon")
            {
                issue = "UNKNOWN CLASS";
            }
            if (QBSalonsData.ContainsKey(normalizedCustomer) && cls != "salons direct")
            {
                issue = ((issue == null) ? "" : (issue + " | ")) + "SHOULD BE SALONS";
            }
            if (issue != null)
            {
                Console.WriteLine($"{x9.Customer} | {x9.Amount} | {x9.Class ?? "NULL"} | {issue}");
            }
        }
    }

    public static void SummarizeClassificationIssues(List<Sales> overallOyaSales, int thisMonth, int year)
    {
        List<Sales> data = overallOyaSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == year).ToList();
        var issues = (from anon in data.Select(delegate (Sales sales)
        {
            string key = Normalize(sales.Customer);
            string text = sales.Class?.Trim().ToLower();
            string text2 = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                text2 = "NULL CLASS";
            }
            else if (text != "salons direct" && text != "public direct" && text != "amazon")
            {
                text2 = "UNKNOWN CLASS";
            }
            if (QBSalonsData.ContainsKey(key) && text != "salons direct")
            {
                text2 = ((text2 == null) ? "" : (text2 + " | ")) + "SHOULD BE SALONS";
            }
            return new
            {
                Customer = sales.Customer,
                Amount = sales.Amount,
                Issue = text2
            };
        })
                      where anon.Issue != null
                      group anon by anon.Issue into grouping
                      select new
                      {
                          Issue = grouping.Key,
                          Total = grouping.Sum(anon => anon.Amount),
                          Count = grouping.Count()
                      } into anon
                      orderby anon.Total descending
                      select anon).ToList();
        Console.WriteLine("\n---- ISSUE SUMMARY ----");
        foreach (var i9 in issues)
        {
            Console.WriteLine($"{i9.Issue} | {i9.Total} | {i9.Count}");
        }
    }
}
