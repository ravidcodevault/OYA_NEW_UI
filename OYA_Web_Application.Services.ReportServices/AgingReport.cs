using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using GemBox.Spreadsheet;
using Microsoft.Extensions.Configuration;

namespace OYA_Web_Application.Services.ReportServices;

public class AgingReport
{
	private readonly IConfiguration _configuration;

	private readonly string path = "";

	private readonly string summaryTemplate = "";

	private readonly string detailTemplate = "";

	private readonly string connectionString = " ";

	private static string SummaryCutomerCell = "A";

	private static string SummaryCurrentCell = "C";

	private static string Summary1_30Cell = "E";

	private static string Summary31_60Cell = "G";

	private static string Summary61_90Cell = "I";

	private static string Summary90PlusCell = "K";

	private static string SummaryTotalCell = "M";

	private static string SummaryTotalRowMarking = "N";

	private static int SummaryStartRow = 2;

	private static string DetailCategoryCell = "A";

	private static string DetailTypeCell = "C";

	private static string DetailDateCell = "E";

	private static string DetailRefNumberCell = "G";

	private static string DetailCustomerCell = "K";

	private static string DetailTermsCell = "M";

	private static string DetailDueDateCell = "O";

	private static string DetailAgingCell = "R";

	private static string DetailOpenBalanceCell = "T";

	private static int DetailtartRow = 2;

	private byte[] content;

	private List<AgingSummary> agingSummaryList = new List<AgingSummary>();

	public AgingReport(IConfiguration configuration)
	{
		_configuration = configuration;
		if (configuration != null)
		{
		
			summaryTemplate = _configuration["FilePaths:AR_Summary_OYA_Report_Path"];
			detailTemplate = _configuration["FilePaths:AR_Detail_OYA_Report_Path"];
			connectionString = _configuration.GetConnectionString("DefaultConnection");
		}
	}

	private void FetchSummaryData(string type)
	{
		using SqlConnection connection = new SqlConnection(connectionString);
		using SqlCommand command = new SqlCommand("GetAgingReportSummary", connection);
		command.CommandType = CommandType.StoredProcedure;
		connection.Open();
		command.Parameters.Add("@reportType", SqlDbType.VarChar).Value = "Summary";
		command.Parameters.Add("@reportFor", SqlDbType.VarChar).Value = type;
		SqlDataAdapter adapt = new SqlDataAdapter(command);
		DataSet ds = new DataSet();
		adapt.Fill(ds);
		DataTable thisMonthDataOya = ds.Tables[0];
		agingSummaryList.Clear();
		foreach (DataRow _row in thisMonthDataOya.Rows)
		{
			AgingSummary agingSummary = new AgingSummary();
			agingSummary.CustomerName = Convert.ToString(_row["Customer"]);
			agingSummary.Amount = Convert.ToDouble(_row["Amount"]);
			agingSummary.Category = Convert.ToString(_row["Category"]);
			agingSummary.AccountRefFullName = Convert.ToString(_row["AccountRefFullName"]);
			agingSummaryList.Add(agingSummary);
		}
	}

	private void GenerateSummaryExcel(string type = "OYA")
	{
		ExcelFile oWB = ExcelFile.Load(summaryTemplate);
		ExcelWorksheet oSheet = oWB.Worksheets[0];
		int index = SummaryStartRow;
		List<string> customerLists = (from agingSummary in agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.AccountRefFullName == type).DistinctBy((AgingSummary agingSummary) => agingSummary.CustomerName)
			select agingSummary.CustomerName).ToList();
		double totalCurrent = 0.0;
		double total1_30 = 0.0;
		double total31_60 = 0.0;
		double total61_90 = 0.0;
		double total90Plus = 0.0;
		foreach (string customer in customerLists)
		{
			oSheet.Cells[SummaryCutomerCell + index].Value = customer;
			double current = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.CustomerName == customer && agingSummary.Category == "0").Sum((AgingSummary agingSummary) => agingSummary.Amount);
			double current1_30 = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.CustomerName == customer && agingSummary.Category == "1-30").Sum((AgingSummary agingSummary) => agingSummary.Amount);
			double current1_60 = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.CustomerName == customer && agingSummary.Category == "31-60").Sum((AgingSummary agingSummary) => agingSummary.Amount);
			double current1_90 = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.CustomerName == customer && agingSummary.Category == "61-90").Sum((AgingSummary agingSummary) => agingSummary.Amount);
			double current90Plus = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.CustomerName == customer && agingSummary.Category == "90+").Sum((AgingSummary agingSummary) => agingSummary.Amount);
			oSheet.Cells[SummaryCurrentCell + index].Value = current;
			oSheet.Cells[Summary1_30Cell + index].Value = current1_30;
			oSheet.Cells[Summary31_60Cell + index].Value = current1_60;
			oSheet.Cells[Summary61_90Cell + index].Value = current1_90;
			oSheet.Cells[Summary90PlusCell + index].Value = current90Plus;
			totalCurrent += current;
			total1_30 += current1_30;
			total31_60 += current1_60;
			total61_90 += current1_90;
			total90Plus += current90Plus;
			index++;
		}
		oSheet.Cells[SummaryCurrentCell + index].Value = totalCurrent;
		oSheet.Cells[Summary1_30Cell + index].Value = total1_30;
		oSheet.Cells[Summary31_60Cell + index].Value = total31_60;
		oSheet.Cells[Summary61_90Cell + index].Value = total61_90;
		oSheet.Cells[Summary90PlusCell + index].Value = total90Plus;
		oSheet.Cells[SummaryTotalRowMarking + index].Value = "total";
		using MemoryStream memoryStream = new MemoryStream();
		oWB.Save(memoryStream, SaveOptions.XlsxDefault);
		content = memoryStream.ToArray();
	}

	private void FetchDetailsData(string type)
	{
		using SqlConnection connection = new SqlConnection(connectionString);
		using SqlCommand command = new SqlCommand("GetAgingReportSummary", connection);
		command.CommandType = CommandType.StoredProcedure;
		connection.Open();
		command.Parameters.Add("@reportType", SqlDbType.VarChar).Value = "Detail";
		command.Parameters.Add("@reportFor", SqlDbType.VarChar).Value = type;
		SqlDataAdapter adapt = new SqlDataAdapter(command);
		DataSet ds = new DataSet();
		adapt.Fill(ds);
		DataTable thisMonthDataOya = ds.Tables[0];
		agingSummaryList.Clear();
		foreach (DataRow _row in thisMonthDataOya.Rows)
		{
			AgingSummary agingSummary = new AgingSummary();
			agingSummary.TxnType = Convert.ToString(_row["TxnType"]);
			agingSummary.CustomerName = Convert.ToString(_row["Customer"]);
			agingSummary.Amount = Convert.ToDouble(_row["Amount"]);
			agingSummary.Category = Convert.ToString(_row["Category"]);
			agingSummary.RefNumber = Convert.ToString(_row["RefNumber"]);
			agingSummary.ClassRefFullName = Convert.ToString(_row["ClassRefFullName"]);
			agingSummary.SalesRepRefFullName = Convert.ToString(_row["SalesRepRefFullName"]);
			agingSummary.CustomerTypeRefFullName = Convert.ToString(_row["CustomerTypeRefFullName"]);
			agingSummary.TxnDate = Convert.ToDateTime(_row["TxnDate"]);
			agingSummary.DueDate = Convert.ToDateTime(_row["DueDate"]);
			agingSummary.Year = Convert.ToInt32(_row["Year"]);
			agingSummary.DiffInDays = Convert.ToInt32(_row["DiffInDays"]);
			agingSummary.TermsRefFullName = Convert.ToString(_row["TermsRefFullName"]);
			agingSummary.AccountRefFullName = Convert.ToString(_row["AccountRefFullName"]);
			agingSummaryList.Add(agingSummary);
		}
	}

	private void GenerateDetailExcel(string type = "OYA")
	{
		ExcelFile oWB = ExcelFile.Load(detailTemplate);
		ExcelWorksheet oSheet = oWB.Worksheets[0];
		int index = DetailtartRow;
		oSheet.Cells[DetailCategoryCell + index].Value = "Current";
		index++;
		foreach (AgingSummary item in agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "0" && agingSummary.AccountRefFullName == type))
		{
			oSheet.Cells[DetailTypeCell + index].Value = item.TxnType;
			oSheet.Cells[DetailDateCell + index].Value = item.TxnDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailRefNumberCell + index].Value = item.RefNumber;
			oSheet.Cells[DetailCustomerCell + index].Value = item.CustomerName;
			oSheet.Cells[DetailTermsCell + index].Value = item.TermsRefFullName;
			oSheet.Cells[DetailDueDateCell + index].Value = item.DueDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailAgingCell + index].Value = "0";
			oSheet.Cells[DetailOpenBalanceCell + index].Value = item.Amount;
			index++;
		}
		oSheet.Cells[DetailCategoryCell + index].Value = "Total Current";
		oSheet.Cells[DetailOpenBalanceCell + index].Value = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "0").Sum((AgingSummary agingSummary) => agingSummary.Amount);
		index++;
		oSheet.Cells[DetailCategoryCell + index].Value = "1 - 30";
		index++;
		foreach (AgingSummary item2 in agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "1-30"))
		{
			oSheet.Cells[DetailTypeCell + index].Value = item2.TxnType;
			oSheet.Cells[DetailDateCell + index].Value = item2.TxnDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailRefNumberCell + index].Value = item2.RefNumber;
			oSheet.Cells[DetailCustomerCell + index].Value = item2.CustomerName;
			oSheet.Cells[DetailTermsCell + index].Value = item2.TermsRefFullName;
			oSheet.Cells[DetailDueDateCell + index].Value = item2.DueDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailAgingCell + index].Value = "1";
			oSheet.Cells[DetailOpenBalanceCell + index].Value = item2.Amount;
			index++;
		}
		oSheet.Cells[DetailCategoryCell + index].Value = "Total 1 - 30";
		oSheet.Cells[DetailOpenBalanceCell + index].Value = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "1-30").Sum((AgingSummary agingSummary) => agingSummary.Amount);
		index++;
		oSheet.Cells[DetailCategoryCell + index].Value = "31 - 60";
		index++;
		foreach (AgingSummary item3 in agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "31-60"))
		{
			oSheet.Cells[DetailTypeCell + index].Value = item3.TxnType;
			oSheet.Cells[DetailDateCell + index].Value = item3.TxnDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailRefNumberCell + index].Value = item3.RefNumber;
			oSheet.Cells[DetailCustomerCell + index].Value = item3.CustomerName;
			oSheet.Cells[DetailTermsCell + index].Value = item3.TermsRefFullName;
			oSheet.Cells[DetailDueDateCell + index].Value = item3.DueDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailAgingCell + index].Value = "2";
			oSheet.Cells[DetailOpenBalanceCell + index].Value = item3.Amount;
			index++;
		}
		oSheet.Cells[DetailCategoryCell + index].Value = "Total 31-60";
		oSheet.Cells[DetailOpenBalanceCell + index].Value = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "31-60").Sum((AgingSummary agingSummary) => agingSummary.Amount);
		index++;
		oSheet.Cells[DetailCategoryCell + index].Value = "61 - 90";
		index++;
		foreach (AgingSummary item4 in agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "61-90"))
		{
			oSheet.Cells[DetailTypeCell + index].Value = item4.TxnType;
			oSheet.Cells[DetailDateCell + index].Value = item4.TxnDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailRefNumberCell + index].Value = item4.RefNumber;
			oSheet.Cells[DetailCustomerCell + index].Value = item4.CustomerName;
			oSheet.Cells[DetailTermsCell + index].Value = item4.TermsRefFullName;
			oSheet.Cells[DetailDueDateCell + index].Value = item4.DueDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailAgingCell + index].Value = "3";
			oSheet.Cells[DetailOpenBalanceCell + index].Value = item4.Amount;
			index++;
		}
		oSheet.Cells[DetailCategoryCell + index].Value = "Total 61 - 90";
		oSheet.Cells[DetailOpenBalanceCell + index].Value = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "61-90").Sum((AgingSummary agingSummary) => agingSummary.Amount);
		index++;
		oSheet.Cells[DetailCategoryCell + index].Value = "> 90";
		index++;
		foreach (AgingSummary item5 in agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "90+"))
		{
			oSheet.Cells[DetailTypeCell + index].Value = item5.TxnType;
			oSheet.Cells[DetailDateCell + index].Value = item5.TxnDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailRefNumberCell + index].Value = item5.RefNumber;
			oSheet.Cells[DetailCustomerCell + index].Value = item5.CustomerName;
			oSheet.Cells[DetailTermsCell + index].Value = item5.TermsRefFullName;
			oSheet.Cells[DetailDueDateCell + index].Value = item5.DueDate.ToString("MM/dd/yyyy");
			oSheet.Cells[DetailAgingCell + index].Value = "4";
			oSheet.Cells[DetailOpenBalanceCell + index].Value = item5.Amount;
			index++;
		}
		oSheet.Cells[DetailCategoryCell + index].Value = "Total > 90";
		oSheet.Cells[DetailOpenBalanceCell + index].Value = agingSummaryList.Where((AgingSummary agingSummary) => agingSummary.Category == "90+").Sum((AgingSummary agingSummary) => agingSummary.Amount);
		index++;
		oSheet.Cells[DetailCategoryCell + index].Value = "TOTAL";
		oSheet.Cells[DetailOpenBalanceCell + index].Value = agingSummaryList.Sum((AgingSummary agingSummary) => agingSummary.Amount);
		using MemoryStream memoryStream = new MemoryStream();
		oWB.Save(memoryStream, SaveOptions.XlsxDefault);
		content = memoryStream.ToArray();
	}

	public byte[] DownloadExcel(string type = "OYA", string report = "summary")
	{
		SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
		string text = report.ToLower();
		if (!(text == "details"))
		{
			if (!(text == "summary"))
			{
			}
			FetchSummaryData(type);
			GenerateSummaryExcel(type);
		}
		else
		{
			FetchDetailsData(type);
			GenerateDetailExcel(type);
		}
		return content;
	}
}
