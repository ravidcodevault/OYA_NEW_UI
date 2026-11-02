using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using GemBox.Spreadsheet;
using Microsoft.Extensions.Configuration;

namespace OYA_Web_Application.Services.ReportServices;

public class CommissionReport
{
    private readonly IConfiguration _configuration;

    private readonly string path = "";

    private readonly string commissionTemplate = "";

    private readonly string connectionString = " ";

    private static string CommissionCustomerNameCol = "A";

    private static string CommissionTypeCol = "B";

    private static string CommissionShipStateCol = "C";

    private static string CommissionDateCol = "D";

    private static string CommissionNumCol = "E";

    private static string CommissionCustomerCol = "F";

    private static string CommissionItemCol = "G";

    private static string CommissionItemDescriptionCol = "H";

    private static string CommissionQtyCol = "I";

    private static string CommissionSalesCol = "J";

    private static string CommissionAmountCol = "K";

    private static string CommissionFirstOrderCol = "L";

    private static string CommissionRateCol = "M";

    private static string CommissionCommissionCol = "N";

    private static string CommissionContractorCol = "O";

    private static string CommissionShipCityCol = "P";

    private static string CommissionTotalDenotion = "Q";

    private static int CommissionStartRow = 2;

    private byte[] content;

    private List<CommissionData> commissionDataList = new List<CommissionData>();

    private List<ContractorCustomer> contractorCustomerList = new List<ContractorCustomer>();

    public CommissionReport(IConfiguration configuration)
    {
        _configuration = configuration;
        if (configuration != null)
        {
            path = _configuration["TemplatePath"];
            commissionTemplate = _configuration["FilePaths:CommissionReportTemplate"];
            connectionString = _configuration.GetConnectionString("DefaultConnection");
        }
    }

    private void FetchDetailsData(string contrator, string startDate, string endDate)
    {
        using SqlConnection connection = new SqlConnection(connectionString);
        using SqlCommand command = new SqlCommand("GetCommisionReport", connection);
        command.CommandType = CommandType.StoredProcedure;
        connection.Open();
        command.Parameters.Add("@contractor", SqlDbType.VarChar).Value = contrator;
        command.Parameters.Add("@startDate", SqlDbType.VarChar).Value = startDate;
        command.Parameters.Add("@endDate", SqlDbType.VarChar).Value = endDate;
        SqlDataAdapter adapt = new SqlDataAdapter(command);
        DataSet ds = new DataSet();
        adapt.Fill(ds);
        DataTable commissionTable = ds.Tables[0];
        commissionDataList.Clear();
        foreach (DataRow _row in commissionTable.Rows)
        {
            CommissionData commissionData = new CommissionData();
            commissionData.Type = Convert.ToString(_row["Type"]);
            commissionData.ShipState = Convert.ToString(_row["ShipState"]);
            commissionData.Date = Convert.ToDateTime(_row["Date"]);
            commissionData.Num = Convert.ToString(_row["Num"]);
            commissionData.Customer = Convert.ToString(_row["Customer"]);
            commissionData.Item = Convert.ToString(_row["Item"]);
            commissionData.ItemDescription = Convert.ToString(_row["ItemDescription"]);
            commissionData.Qty = Convert.ToInt32(_row["Qty"]);
            commissionData.SalesPrice = Convert.ToDouble(_row["SalesPrice"]);
            commissionData.Amount = Convert.ToDouble(_row["Amount"]);
            commissionData.ShipCity = Convert.ToString(_row["ShipCity"]);
            commissionData.Rate = Convert.ToInt32(_row["Rate"]);
            commissionData.Commission = Convert.ToDouble(_row["Commission"]);
            commissionData.Contractor = Convert.ToString(_row["Contractor"]);
            commissionData.ShipCity = Convert.ToString(_row["ShipCity"]);
            commissionDataList.Add(commissionData);
        }
        contractorCustomerList.Clear();
        DataTable CustomerTable = ds.Tables[1];
        foreach (DataRow _row2 in CustomerTable.Rows)
        {
            contractorCustomerList.Add(new ContractorCustomer
            {
                Customer = Convert.ToString(_row2["Customer"]),
                FirstOrderDate = Convert.ToDateTime(_row2["FirstOrderDate"])
            });
        }
    }

    private void GenerateDetailExcel()
    {
        ExcelFile oWB = ExcelFile.Load(commissionTemplate);
        ExcelWorksheet oSheet = oWB.Worksheets[0];
        int index = CommissionStartRow;
        foreach (ContractorCustomer customer in contractorCustomerList.Where((ContractorCustomer contractorCustomer) => commissionDataList.Select((CommissionData _c) => _c.Customer).Contains(contractorCustomer.Customer)))
        {
            oSheet.Cells[CommissionCustomerNameCol + index].Value = customer.Customer;
            oSheet.Cells[CommissionFirstOrderCol + index].Value = customer.FirstOrderDate.ToString("yyyy-MM-dd");
            index++;
            int qtyTotal = 0;
            double amountTotal = 0.0;
            double commissionTotal = 0.0;
            foreach (CommissionData item in commissionDataList.Where((CommissionData commissionData) => commissionData.Customer == customer.Customer))
            {
                oSheet.Cells[CommissionTypeCol + index].Value = item.Type;
                oSheet.Cells[CommissionShipStateCol + index].Value = item.ShipState;
                oSheet.Cells[CommissionDateCol + index].Value = item.Date.ToString("yyyy-MM-dd");
                oSheet.Cells[CommissionNumCol + index].Value = item.Num;
                oSheet.Cells[CommissionCustomerCol + index].Value = item.Customer;
                oSheet.Cells[CommissionItemCol + index].Value = item.Item;
                oSheet.Cells[CommissionItemDescriptionCol + index].Value = item.ItemDescription.Trim();
                qtyTotal += item.Qty;
                oSheet.Cells[CommissionQtyCol + index].Value = item.Qty;
                oSheet.Cells[CommissionSalesCol + index].Value = Math.Round(item.SalesPrice, 2);
                amountTotal += item.Amount;
                oSheet.Cells[CommissionAmountCol + index].Value = item.Amount;
                oSheet.Cells[CommissionRateCol + index].Value = item.Rate;
                commissionTotal += item.Commission;
                oSheet.Cells[CommissionCommissionCol + index].Value = item.Commission;
                oSheet.Cells[CommissionContractorCol + index].Value = item.Contractor;
                oSheet.Cells[CommissionShipCityCol + index].Value = item.ShipCity;
                index++;
            }
            oSheet.Cells[CommissionQtyCol + index].Value = qtyTotal;
            oSheet.Cells[CommissionAmountCol + index].Value = amountTotal;
            oSheet.Cells[CommissionCommissionCol + index].Value = commissionTotal;
            oSheet.Cells[CommissionTotalDenotion + index].Value = "total";
            index++;
        }
        oSheet.Cells[CommissionQtyCol + index].Value = commissionDataList.Sum((CommissionData commissionData) => commissionData.Qty);
        oSheet.Cells[CommissionAmountCol + index].Value = commissionDataList.Sum((CommissionData commissionData) => commissionData.Amount);
        oSheet.Cells[CommissionCommissionCol + index].Value = commissionDataList.Sum((CommissionData commissionData) => commissionData.Commission);
        oSheet.Cells[CommissionTotalDenotion + index].Value = "total";
        using MemoryStream memoryStream = new MemoryStream();
        oWB.Save(memoryStream, SaveOptions.XlsxDefault);
        content = memoryStream.ToArray();
    }

    public byte[] DownloadExcel(string contrator = "02", DateTime? startDate = null, DateTime? endDate = null)
    {
        var reportStartDate = startDate?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var reportEndDate = endDate?.Date ?? DateTime.Today;
        if (reportStartDate > reportEndDate)
        {
            throw new ArgumentException("The start date must be on or before the end date.");
        }

        SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
        SpreadsheetInfo.FreeLimitReached += delegate (object? sender, FreeLimitEventArgs e9)
        {
            e9.FreeLimitReachedAction = FreeLimitReachedAction.ContinueAsTrial;
        };
        FetchDetailsData(contrator, reportStartDate.ToString("MM/dd/yyyy"), reportEndDate.ToString("MM/dd/yyyy"));
        GenerateDetailExcel();
        return content;
    }
}
