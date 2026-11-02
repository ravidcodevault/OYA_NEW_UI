using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using GemBox.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Microsoft.Office.Interop.Excel;
using OYA_Web_Application.Models.Data.Reports;

namespace OYA_Web_Application.Services.ReportServices;

public class TerritoryReports
{
    private readonly IConfiguration _configuration;

    private static string path = "";

    private static string outputPath = "";

    private string connectionString = " ";

    private static string qodbcConnString = "DSN=Quickbooks Data;OLE DB Services=-2;";

    private static DateTime now = DateTime.Now;

    private static string logPath = "C:\\LOG\\Helias Sales Report\\" + DateTime.Now.ToString().Replace(":", "").Replace("/", "");

    private static string template = "Helias Sales Report - Territory.xlsx";

    private static string mainSheetName = "OYASales2024Letter net sales";

    private static string mainStartCell = "G2";

    private static string accountsCol = "F";

    private static string prevYearCol = "Z";

    private static int internationalRow = 41;

    private static int salonsDirectRow = 35;

    private static int publicDirectRow = 37;

    private static int allOyaRow = 39;

    private static int pinoRow = 40;

    private static int heliasRow = 41;

    private static int otherRow = 26;

    private static int canadaRow = 28;

    private static int mexicoRow = 29;

    private static int trinidadTobagoRow = 30;

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

    private string mainEndCell = prevYearCol + heliasRow;

    private string internationalEndCell = prevYearCol + internationalRow;

    private string allOyaAcctsCell = accountsCol + allOyaRow;

    private string pinoAcctsCell = accountsCol + pinoRow;

    private string heliasAcctsCell = accountsCol + heliasRow;

    private List<Sales> oyaThisMonthList = new List<Sales>();

    private List<Sales> pinoThisMonthList = new List<Sales>();

    private List<Sales> otherThisMonthList = new List<Sales>();

    private List<Sales> contractorsThisMonthList = new List<Sales>();

    private List<Sales> OverallSales = new List<Sales>();

    private decimal oyaThisMonthToDate;

    private decimal pinoThisMonthToDate;

    private decimal otherThisMonthToDate;

    private decimal internetThisMonthToDate;

    private decimal contractorsThisMonthToDate;

    private List<Sales> oyaLastYearToDateList = new List<Sales>();

    private List<Sales> pinoLastYearToDateList = new List<Sales>();

    private List<Sales> otherLastYearToDateList = new List<Sales>();

    private decimal oyaLastYearToDate;

    private decimal pinoLastYearToDate;

    private decimal otherLastYearToDate;

    private int pinoCustomerCount;

    private byte[] content;

    public TerritoryReports(IConfiguration configuration)
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
        System.Data.DataTable overallSalesTable = ds.Tables[0];
        foreach (DataRow _row in overallSalesTable.Rows)
        {
            Sales item = new Sales();
            item.Customer = Convert.ToString(_row["Customer"]);
            item.Class = Convert.ToString(_row["ClassRefFullName"]);
            item.Month = Convert.ToInt32(_row["Month"]);
            item.TxnDate = Convert.ToDateTime(_row["TxnDate"]);
            item.Year = Convert.ToInt32(_row["Year"]);
            item.Rep = Convert.ToString(_row["SalesRepRefFullName"]);
            item.CustomerType = Convert.ToString(_row["CustomerTypeRefFullName"]);
            if (_row["Amount"] != DBNull.Value)
            {
                item.Amount = Convert.ToDecimal(_row["Amount"]);
            }
            OverallSales.Add(item);
        }
    }

    private void GenerateExcel()
    {
        int thisMonth = now.Month;
        string col = "";
        try
        {
            ExcelFile oWB = ExcelFile.Load(path + template);
            sheetWest = oWB.Worksheets[0];
            sheetCentral = oWB.Worksheets[1];
            sheetEast = oWB.Worksheets[2];
            distributorList = GetDistributor();
            DateTime today = DateTime.Today;
            DateTime startOfCurrentYear = new DateTime(today.Year, 1, 1);
            DateTime startOfLastYear = new DateTime(today.Year - 1, 1, 1);
            DateTime endOfLastYear = new DateTime(today.Year - 1, today.Month, today.Day);
            sheetWest.Cells["A" + 1].Value = $"Net sales {DateTime.Now.Year} in US$";
            sheetWest.Cells["P" + 1].Value = DateTime.Now.ToString("MMMM dd, yyyy") ?? "";
            sheetCentral.Cells["A" + 1].Value = $"Net sales {DateTime.Now.Year} in US$";
            sheetCentral.Cells["P" + 1].Value = DateTime.Now.ToString("MMMM dd, yyyy") ?? "";
            sheetEast.Cells["A" + 1].Value = $"Net sales {DateTime.Now.Year} in US$";
            sheetEast.Cells["P" + 1].Value = DateTime.Now.ToString("MMMM dd, yyyy") ?? "";
            string colTerritory;
            for (int num = 1; num <= 12; num++)
            {
                thisMonth = num;
                switch (thisMonth)
                {
                    case 1:
                        col = "L";
                        colTerritory = "G";
                        break;
                    case 2:
                        col = "M";
                        colTerritory = "H";
                        break;
                    case 3:
                        col = "N";
                        colTerritory = "I";
                        break;
                    case 4:
                        col = "O";
                        colTerritory = "J";
                        break;
                    case 5:
                        col = "P";
                        colTerritory = "K";
                        break;
                    case 6:
                        col = "Q";
                        colTerritory = "L";
                        break;
                    case 7:
                        col = "R";
                        colTerritory = "M";
                        break;
                    case 8:
                        col = "S";
                        colTerritory = "N";
                        break;
                    case 9:
                        col = "T";
                        colTerritory = "O";
                        break;
                    case 10:
                        col = "U";
                        colTerritory = "P";
                        break;
                    case 11:
                        col = "V";
                        colTerritory = "Q";
                        break;
                    case 12:
                        col = "W";
                        colTerritory = "R";
                        break;
                    default:
                        colTerritory = "G";
                        break;
                }
                distributorList.Where((Distributor distributor) => distributor.WestRow > 0).ToList().ForEach(delegate (Distributor distributor)
                {
                    if (thisMonth <= now.Month)
                    {
                        List<Sales> list = OverallSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).ToList();
                        sheetWest.Cells[colTerritory + (distributor.WestRow + 1)].Value = GetSalesByCustomer(distributor.Customer, OverallSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).ToList());
                        decimal? num2 = OverallSales.Where((Sales sales) => sales.TxnDate >= startOfCurrentYear && sales.TxnDate <= today && sales.Customer.ToLower() == distributor.Customer.ToLower()).Sum((Sales sales) => sales.Amount);
                        decimal? num3 = OverallSales.Where((Sales sales) => sales.TxnDate >= startOfLastYear && sales.TxnDate <= endOfLastYear && sales.Customer.ToLower() == distributor.Customer.ToLower()).Sum((Sales sales) => sales.Amount);
                        sheetWest.Cells["U" + distributor.WestRow].Value = num3;
                        decimal num4 = default(decimal);
                        decimal? num5 = num3;
                        if (!((num5.GetValueOrDefault() == default(decimal)) & num5.HasValue))
                        {
                            num4 = (num2.Value - num3.Value) / num3.Value * 100m;
                        }
                        sheetWest.Cells["T" + (distributor.WestRow + 1)].Value = Math.Round(num4) + " %";
                    }
                    sheetWest.Cells[colTerritory + distributor.WestRow].Value = GetSalesByCustomer(distributor.Customer, OverallSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year != now.Year).ToList());
                });
                distributorList.Where((Distributor distributor) => distributor.CentralRow > 0).ToList().ForEach(delegate (Distributor distributor)
                {
                    if (thisMonth <= now.Month)
                    {
                        sheetCentral.Cells[colTerritory + (distributor.CentralRow + 1)].Value = GetSalesByCustomer(distributor.Customer, OverallSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).ToList());
                        decimal? num2 = OverallSales.Where((Sales sales) => sales.TxnDate >= startOfCurrentYear && sales.TxnDate <= today && sales.Customer.ToLower() == distributor.Customer.ToLower()).Sum((Sales sales) => sales.Amount);
                        decimal? num3 = OverallSales.Where((Sales sales) => sales.TxnDate >= startOfLastYear && sales.TxnDate <= endOfLastYear && sales.Customer.ToLower() == distributor.Customer.ToLower()).Sum((Sales sales) => sales.Amount);
                        sheetCentral.Cells["U" + distributor.CentralRow].Value = num3;
                        decimal num4 = default(decimal);
                        decimal? num5 = num3;
                        if (!((num5.GetValueOrDefault() == default(decimal)) & num5.HasValue))
                        {
                            num4 = (num2.Value - num3.Value) / num3.Value * 100m;
                        }
                        sheetCentral.Cells["T" + (distributor.CentralRow + 1)].Value = Math.Round(num4) + " %";
                    }
                    sheetCentral.Cells[colTerritory + distributor.CentralRow].Value = GetSalesByCustomer(distributor.Customer, OverallSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year != now.Year).ToList());
                });
                distributorList.Where((Distributor distributor) => distributor.EastRow > 0).ToList().ForEach(delegate (Distributor distributor)
                {
                    if (thisMonth <= now.Month)
                    {
                        sheetEast.Cells[colTerritory + (distributor.EastRow + 1)].Value = GetSalesByCustomer(distributor.Customer, OverallSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year == now.Year).ToList());
                        decimal? num2 = OverallSales.Where((Sales sales) => sales.TxnDate >= startOfCurrentYear && sales.TxnDate <= today && sales.Customer.ToLower() == distributor.Customer.ToLower()).Sum((Sales sales) => sales.Amount);
                        decimal? num3 = OverallSales.Where((Sales sales) => sales.TxnDate >= startOfLastYear && sales.TxnDate <= endOfLastYear && sales.Customer.ToLower() == distributor.Customer.ToLower()).Sum((Sales sales) => sales.Amount);
                        sheetEast.Cells["U" + distributor.EastRow].Value = num3;
                        decimal num4 = default(decimal);
                        decimal? num5 = num3;
                        if (!((num5.GetValueOrDefault() == default(decimal)) & num5.HasValue))
                        {
                            num4 = (num2.Value - num3.Value) / num3.Value * 100m;
                        }
                        sheetEast.Cells["T" + (distributor.EastRow + 1)].Value = Math.Round(num4) + " %";
                    }
                    sheetEast.Cells[colTerritory + distributor.EastRow].Value = GetSalesByCustomer(distributor.Customer, OverallSales.Where((Sales sales) => sales.Month == thisMonth && sales.Year != now.Year).ToList());
                });
            }
            sheetWest.Cells["P1"].Value = DateTime.Now.ToString("MMMM dd, yyyy");
            sheetCentral.Cells["P1"].Value = DateTime.Now.ToString("MMMM dd, yyyy");
            sheetEast.Cells["P1"].Value = DateTime.Now.ToString("MMMM dd, yyyy");
            using MemoryStream memoryStream = new MemoryStream();
            oWB.Save(memoryStream, SaveOptions.XlsxDefault);
            content = memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Territory report generation failed. Check the configured template files and report data source.", ex);
        }
    }

    public byte[] DownloadExcel()
    {
        try
        {
            FetchData();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Territory report data could not be loaded.", ex);
        }

        SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
        SpreadsheetInfo.FreeLimitReached += delegate (object? sender, FreeLimitEventArgs e9)
        {
            e9.FreeLimitReachedAction = FreeLimitReachedAction.Stop;
        };
        GenerateExcel();
        return content;
    }

    private static void CopyToDailyReport(Application sourceXL, Workbook sourceWb, string sourceWorksheetName, string rep, string begCell, string endCell)
    {
        try
        {
            object defaultArg = Type.Missing;
            Workbook copyWb = null;
            Worksheet copyWs = null;
            string fileName = "";
            string date = now.ToString("MMMM dd, yyyy");
            copyWb = sourceXL.Workbooks.Open(path + "Helias Sales Report" + fileName + ".xlsx", 0, false, 5, "", "", false, XlPlatform.xlWindows, "", true, false, 0, true, false, false);
            copyWs = (Worksheet)copyWb.Worksheets[sourceWorksheetName];
            if (now.Day < 10)
            {
                copyWs.PageSetup.RightHeader = "&" + 12 + "&\"Arial\"per " + now.ToString("MMMM d, yyyy");
            }
            else
            {
                copyWs.PageSetup.RightHeader = "&" + 12 + "&\"Arial\"per " + now.ToString("MMMM d, yyyy");
            }
            ((_Worksheet)(Worksheet)sourceWb.Worksheets[sourceWorksheetName]).get_Range((object)begCell, (object)endCell).Copy(Type.Missing);
            ((_Worksheet)copyWs).get_Range((object)begCell, (object)endCell).PasteSpecial(XlPasteType.xlPasteValues, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
            copyWb.Save();
            copyWb.Close(defaultArg, defaultArg, defaultArg);
            Console.WriteLine(logPath + "_updated_salesreport_" + rep + ".txt");
            LogFile(logPath + "_updated_salesreport_" + rep + ".txt", "Succeefully updated Helias Sales Report.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(logPath + "_update_salesreport_" + rep + "_error.txt");
            LogFile(logPath + "_update_salesreport_" + rep + "_error.txt", ex.ToString());
        }
    }

    private static void CopyToSalesRep(Application oXL, Workbook oWB, string template, string rep, string begCell, string endCell)
    {
        try
        {
            object defaultArg = Type.Missing;
            Workbook wb = null;
            Worksheet ws = null;
            string date = now.ToString("MMMM dd, yyyy");
            string fileName = " - " + rep;
            wb = oXL.Workbooks.Open(path + "Helias Sales Report" + fileName + ".xlsx", 0, false, 5, "", "", false, XlPlatform.xlWindows, "", true, false, 0, true, false, false);
            ws = (Worksheet)wb.Worksheets[template];
            ((_Worksheet)(Worksheet)oWB.Worksheets[template]).get_Range((object)begCell, (object)endCell).Copy(Type.Missing);
            ((_Worksheet)ws).get_Range((object)begCell, (object)endCell).PasteSpecial(XlPasteType.xlPasteValues, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
            wb.Save();
            wb.Close(defaultArg, defaultArg, defaultArg);
            Console.WriteLine(logPath + "_updated_salesreport_" + rep + ".txt");
            LogFile(logPath + "_updated_salesreport_" + rep + ".txt", "Succeefully updated Helias Sales Report.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(logPath + "_update_salesreport_" + rep + "_error.txt");
            LogFile(logPath + "_update_salesreport_" + rep + "_error.txt", ex.ToString());
        }
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

    private static void LogFile(string file, string message)
    {
        if (!string.IsNullOrEmpty(file) || !string.IsNullOrEmpty(message))
        {
            Console.WriteLine(message ?? "");
        }
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
        using (SqlCommand com = new SqlCommand("SELECT Customer, WestRow, EastRow, CentralRow, Sheet FROM Distributor WHERE WestRow > 0 OR EastRow > 0 OR CentralRow > 0;", conn))
        {
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read())
            {
                distributorList.Add(new Distributor
                {
                    Customer = reader["Customer"].ToString(),
                    WestRow = reader["WestRow"] == DBNull.Value ? 0 : Convert.ToInt32(reader["WestRow"]),
                    EastRow = reader["EastRow"] == DBNull.Value ? 0 : Convert.ToInt32(reader["EastRow"]),
                    CentralRow = reader["CentralRow"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CentralRow"]),
                    Sheet = Convert.ToString(reader["Sheet"])
                });
            }
            reader.Close();
        }
        conn.Close();
        return distributorList;
    }
}
