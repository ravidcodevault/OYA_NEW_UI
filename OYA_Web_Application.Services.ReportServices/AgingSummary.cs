using System;

namespace OYA_Web_Application.Services.ReportServices;

public class AgingSummary
{
	public string TxnType { get; set; }

	public string CustomerName { get; set; }

	public double Amount { get; set; }

	public string Category { get; set; }

	public string RefNumber { get; set; }

	public string ClassRefFullName { get; set; }

	public string AccountRefFullName { get; set; }

	public string SalesRepRefFullName { get; set; }

	public string CustomerTypeRefFullName { get; set; }

	public DateTime TxnDate { get; set; }

	public DateTime DueDate { get; set; }

	public int Year { get; set; }

	public int DiffInDays { get; set; }

	public string TermsRefFullName { get; set; }
}
