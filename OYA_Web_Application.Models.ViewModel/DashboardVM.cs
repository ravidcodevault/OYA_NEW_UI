using System.Collections.Generic;
using OYA_Web_Application.Models.Data.Dashboard;

namespace OYA_Web_Application.Models.ViewModel;

public class DashboardVM
{
	public DashboardSales dashboardSales { get; set; }

	public List<CustomerSales> customerSales { get; set; }

	public List<MonthlySales> oyaMonthlySales { get; set; }

	public List<YearlySales> oyaYearlySales { get; set; }

	public List<MonthlySales> pinoMonthlySales { get; set; }

	public List<YearlySales> pinoYearlySales { get; set; }
}
