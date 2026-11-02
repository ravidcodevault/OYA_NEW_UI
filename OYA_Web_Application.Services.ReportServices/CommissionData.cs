using System;

namespace OYA_Web_Application.Services.ReportServices;

public class CommissionData
{
	public string Type { get; set; }

	public string ShipState { get; set; }

	public DateTime Date { get; set; }

	public string Num { get; set; }

	public string Customer { get; set; }

	public string Item { get; set; }

	public string ItemDescription { get; set; }

	public int Qty { get; set; }

	public double SalesPrice { get; set; }

	public double Amount { get; set; }

	public DateTime FirstOrderDate { get; set; }

	public int Rate { get; set; }

	public double Commission { get; set; }

	public string Contractor { get; set; }

	public string ShipCity { get; set; }
}
