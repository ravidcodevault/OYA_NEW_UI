using System;

namespace OYA_Web_Application.Services.ReportServices;

public class Sales
{
	public string Customer { get; set; }

	public decimal? Amount { get; set; }

	public string RowType { get; set; }

	public string Class { get; set; }

	public string Rep { get; set; }

	public int Month { get; set; }

	public int Year { get; set; }

	public DateTime TxnDate { get; set; }

	public string CustomerType { get; set; }

	public string AccountRefFullName { get; set; }
}
