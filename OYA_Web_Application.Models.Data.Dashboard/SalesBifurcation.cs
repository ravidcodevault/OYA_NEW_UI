using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace OYA_Web_Application.Models.Data.Dashboard;

public class SalesBifurcation
{
	private readonly IConfiguration _configuration;

	private string connectionString = " ";

	public SalesBifurcation(IConfiguration configuration)
	{
		_configuration = configuration;
		connectionString = _configuration.GetConnectionString("DefaultConnection");
	}

	public DashboardSales GetOverallDashboardSales()
	{
		DashboardSales sales = new DashboardSales();
		try
		{
			using SqlConnection connection = new SqlConnection(connectionString);
			using SqlCommand command = new SqlCommand("dbo.USP_GetOverallDashboardSales_V2", connection);
			command.CommandType = CommandType.StoredProcedure;
			connection.Open();
			command.Parameters.Add("@thisYear", SqlDbType.Int).Value = DateTime.Now.Year;
			command.Parameters.Add("@thisMonth", SqlDbType.Int).Value = DateTime.Now.Month;
			using SqlDataReader reader = command.ExecuteReader();
			while (reader.Read())
			{


                sales.TotalAmountPublicDirect =
                    reader["PublicSales"] != DBNull.Value
                        ? Convert.ToDecimal(reader["PublicSales"])
                        : 0m;

                sales.TotalAmountForeign =
                    reader["InternetOrder"] != DBNull.Value
                        ? Convert.ToDecimal(reader["InternetOrder"])
                        : 0m;

                sales.TotalAmountOtherThisMonth =
                    reader["OtherThisMonth"] != DBNull.Value
                        ? Convert.ToDecimal(reader["OtherThisMonth"])
                        : 0m;

                sales.TotalAmountOverallOya =
                    reader["OverallSales"] != DBNull.Value
                        ? Convert.ToDecimal(reader["OverallSales"])
                        : 0m;

                sales.OYA_This_Month_Sales =
                    reader["OYASales"] != DBNull.Value
                        ? Convert.ToDecimal(reader["OYASales"])
                        : 0m;

                sales.Pino_This_Month_Sales =
                    reader["PinoSales"] != DBNull.Value
                        ? Convert.ToDecimal(reader["PinoSales"])
                        : 0m;

                sales.SalonSales =
                    reader["SalonSales"] != DBNull.Value
                        ? Convert.ToDecimal(reader["SalonSales"])
                        : 0m;}
            }
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
		return sales;
	}

	public decimal GetPublicDirectSales()
	{
		decimal totalAmount = default(decimal);
		string query = "\r\n            SELECT\r\n                SUM(InvoiceLine.Amount) AS TotalAmount\r\n            FROM\r\n                Invoice\r\n            JOIN\r\n                Customer ON Invoice.CustomerRefListID = Customer.ListID\r\n            JOIN\r\n                InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n            JOIN\r\n                Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n            WHERE\r\n                Invoice.TxnDate BETWEEN '2024-05-01' AND '2025-05-07'\r\n                AND Item.FullName LIKE 'OY%'\r\n                AND Invoice.SalesRepRefFullName IS NOT NULL\r\n                AND Invoice.SalesRepRefFullName IN (SELECT Contractor FROM Contractor)";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		if (reader.Read() && !reader.IsDBNull(0))
		{
			totalAmount = reader.GetDecimal(0);
		}
		reader.Close();
		return totalAmount;
	}

	public decimal GetForeignBusiness()
	{
		decimal totalAmount = default(decimal);
		string query = "\r\n            SELECT\r\n                SUM(InvoiceLine.Amount) AS TotalAmount\r\n            FROM\r\n                Invoice\r\n            JOIN\r\n                Customer ON Invoice.CustomerRefListID = Customer.ListID\r\n            JOIN\r\n                InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n            JOIN\r\n                Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n            WHERE\r\n                Invoice.TxnDate BETWEEN '2024-05-01' AND '2025-05-07'\r\n                AND Item.FullName LIKE 'OY%'\r\n                AND Invoice.ClassRefFullName = 'Internet Order'";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		if (reader.Read() && !reader.IsDBNull(0))
		{
			totalAmount = reader.GetDecimal(0);
		}
		reader.Close();
		return totalAmount;
	}

	public decimal GetOtherThisMonthSales()
	{
		decimal totalAmount = default(decimal);
		string query = "\r\n            SELECT\r\n               SUM(InvoiceLine.Amount) AS TotalAmount\r\n            FROM\r\n                Invoice\r\n            JOIN\r\n                Customer ON Invoice.CustomerRefListID = Customer.ListID\r\n            JOIN\r\n                InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n            JOIN\r\n                Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n            WHERE\r\n                Invoice.TxnDate BETWEEN '2024-05-01' AND '2025-05-07'\r\n                AND Customer.FullName NOT IN (\r\n                    'OYA ISCS:ISCS Derek Moser', \r\n                    'OYA ISCS:ISCS Michelle Fanning',\r\n                    'OYA M Beauty:Andrea''s', \r\n                    'OYA M Beauty:Ariel Derrick (Residence)', \r\n                    'OYA M Beauty:Breanna Taylor (Residence)',\r\n                    'OYA M Beauty:Elite Salon', \r\n                    'OYA M Beauty:Flaunt Salon', \r\n                    'OYA M Beauty:Fran Medina (Residence)', \r\n                    'OYA M Beauty:GAL Studios', \r\n                    'OYA M Beauty:Irene Fernandez', \r\n                    'OYA M Beauty:Jagged Edge', \r\n                    'OYA M Beauty:Karen Poulsen', \r\n                    'OYA M Beauty:Koolfx Salon at Mesa Royale',              \r\n                    'OYA M Beauty:Linda Reese (Residence)', \r\n                    'OYA M Beauty:Marc Reid Salon @ Steven Paul Salon',\r\n                    'OYA M Beauty:Mira Bella Hair Salon',\r\n                    'OYA M Beauty:R Salon Day Spa - Donna Raffanti',\r\n                    'OYA M Beauty:Salon Boutique - Arrowhead', \r\n                    'OYA M Beauty:Schwahn Salon',\r\n                    'OYA M Beauty:Shaggy''s', \r\n                    'OYA M Beauty:Shear Creations',\r\n                    'OYA M Beauty:Stasyss Salon',\r\n                    'OYA M Beauty:Sublime Salon',\r\n                    'OYA Unique Salon Concepts:Lisa Gagne'\r\n                )\r\n                AND Customer.FullName NOT IN (\r\n                    'OYA Uberbeauty808',\r\n                    'OYA C.K. ColorHouse',\r\n                    'OYA Store Professional',\r\n                    'OYA Store Consumer',\r\n                    'OYA M Beauty',\r\n                    'OYA Josephs College',\r\n                    'OYA Blaze Salon',\r\n                    'OYA ProSalon Distribution Group',\r\n                    'OYA Central States Supply',\r\n                    'OYA Elite Salon Distributors USA OHIO',\r\n                    'OYA Discover Salon Services',\r\n                    'OYA Mid America Beauty Supply',\r\n                    'OYA Economy',\r\n                    'OYA Elite Salon Services',\r\n                    'OYA Valley Beauty Supply',\r\n                    'OYA Unique Salon Concepts',\r\n                    'OYA Style Care',\r\n                    'OYA ISCS',\r\n                    'OYA Prime Salon Services',\r\n                    'OYA Aidan Laura Aine Ltd - ALA',\r\n                    'OYA Desbo Distribution Inc.',\r\n                    'OYA SOID Distributors, P.R.',\r\n                    'OYA De Bleu Beauty Supply',\r\n                    'OYA Binani Desbo Group',\r\n                    'OYA Braliza LLC',\r\n                    'OYA NCN Beauty LLC',\r\n                    'OYA NCN Beauty LLC Venezuela',\r\n                    'OYA NCN Beauty LLC Ecuador',\r\n                    'OYA Brothers Beauty Products'\r\n                )\r\n                 AND Item.FullName LIKE 'OY%'";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		if (reader.Read() && !reader.IsDBNull(0))
		{
			totalAmount = reader.GetDecimal(0);
		}
		reader.Close();
		return totalAmount;
	}

	public decimal GetOverallOyaSales()
	{
		decimal totalAmount = default(decimal);
		string query = "\r\n            SELECT\r\n                SUM(InvoiceLine.Amount) AS TotalAmount\r\n            FROM\r\n                Invoice\r\n            JOIN\r\n                Customer ON Invoice.CustomerRefListID = Customer.ListID\r\n            JOIN\r\n                InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n            JOIN\r\n                Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n            WHERE\r\n                Invoice.TxnDate BETWEEN '2024-05-01' AND '2024-05-07'\r\n                AND Item.FullName LIKE 'OY%'";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		if (reader.Read() && !reader.IsDBNull(0))
		{
			totalAmount = reader.GetDecimal(0);
		}
		reader.Close();
		return totalAmount;
	}

	public decimal GetOyaThisMonth()
	{
		decimal totalAmount = default(decimal);
		string query = "\r\n            SELECT\r\n                SUM(InvoiceLine.Amount) AS OYA_This_Month_Sales\r\n            FROM\r\n                Invoice\r\n            JOIN\r\n                Customer ON Invoice.CustomerRefListID = Customer.ListID\r\n            JOIN\r\n                InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n            JOIN\r\n                Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n            WHERE\r\n                Invoice.TxnDate BETWEEN '2024-05-01' AND '2025-05-07'\r\n                AND Customer.FullName IN (\r\n                    'OYA Uberbeauty808',\r\n                    'OYA C.K. ColorHouse',\r\n                    'OYA Store Professional',\r\n                    'OYA Store Consumer',\r\n                    'OYA M Beauty',\r\n                    'OYA Josephs College',\r\n                    'OYA Blaze Salon',\r\n                    'OYA ProSalon Distribution Group',\r\n                    'OYA Central States Supply',\r\n                    'OYA Elite Salon Distributors USA OHIO',\r\n                    'OYA Discover Salon Services',\r\n                    'OYA Mid America Beauty Supply',\r\n                    'OYA Economy',\r\n                    'OYA Elite Salon Services',\r\n                    'OYA Valley Beauty Supply',\r\n                    'OYA Unique Salon Concepts',\r\n                    'OYA Style Care',\r\n                    'OYA ISCS',\r\n                    'OYA Prime Salon Services',\r\n                    'OYA Aidan Laura Aine Ltd - ALA',\r\n                    'OYA Desbo Distribution Inc.',\r\n                    'OYA SOID Distributors, P.R.',\r\n                    'OYA De Bleu Beauty Supply',\r\n                    'OYA Binani Desbo Group',\r\n                    'OYA Braliza LLC',\r\n                    'OYA NCN Beauty LLC',\r\n                    'OYA NCN Beauty LLC Venezuela',\r\n                    'OYA NCN Beauty LLC Ecuador',\r\n                    'OYA Brothers Beauty Products'\r\n                )\r\n                 AND Item.FullName LIKE 'OY%'";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		if (reader.Read() && !reader.IsDBNull(0))
		{
			totalAmount = reader.GetDecimal(0);
		}
		reader.Close();
		return totalAmount;
	}

	public decimal GetPinoThisMonth()
	{
		decimal totalAmount = default(decimal);
		string query = "\r\n\t\t\t\tSELECT\r\n\t\t\t\tSUM(InvoiceLine.Amount) AS TotalAmount\r\n\t\t\t\tFROM\r\n\t\t\t\tInvoice\r\n\t\t\t\tJOIN\r\n\t\t\t\tCustomer ON Invoice.CustomerRefListID = Customer.ListID\r\n\t\t\t\tJOIN\r\n\t\t\t\tInvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n\t\t\t\tJOIN\r\n\t\t\t\t\tItem ON InvoiceLine.ItemRefListID = Item.ListID\r\n\t\t\t\tWHERE\r\n\t\t\t\t\tInvoice.TxnDate BETWEEN '2024-05-01' AND '2024-05-07'\r\n\t\t\t\tAND Item.FullName LIKE 'PN%'";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		if (reader.Read() && !reader.IsDBNull(0))
		{
			totalAmount = reader.GetDecimal(0);
		}
		reader.Close();
		return totalAmount;
	}

	public List<CustomerSales> GetTopCustomerSales()
	{
		List<CustomerSales> topCustomerSales = new List<CustomerSales>();
		string query = "\r\n\t\t\t\tSELECT TOP 8\r\n\t\t\t\t\tCustomer.FullName AS CustomerName,\r\n\t\t\t\t\tSUM(InvoiceLine.Amount) AS TotalAmount\r\n\t\t\t\tFROM\r\n\t\t\t\t\tInvoice\r\n\t\t\t\tJOIN\r\n\t\t\t\t\tCustomer ON Invoice.CustomerRefListID = Customer.ListID\r\n\t\t\t\tJOIN\r\n\t\t\t\t\tInvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n\t\t\t\tJOIN\r\n\t\t\t\t\tItem ON InvoiceLine.ItemRefListID = Item.ListID\r\n\t\t\t\tWHERE\r\n\t\t\t\t\tYEAR(Invoice.TxnDate) = YEAR(GETDATE())\r\n\t\t\t\t\t--AND Item.FullName LIKE 'PN%'\r\n\t\t\t\tGROUP BY Customer.FullName\r\n\t\t\t\tORDER BY TotalAmount DESC";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		while (reader.Read())
		{
			CustomerSales customerSales = new CustomerSales();
			customerSales.CustomerName = reader.GetString(0);
			customerSales.TotalAmount = reader.GetDecimal(1);
			topCustomerSales.Add(customerSales);
		}
		reader.Close();
		return topCustomerSales;
	}

	public List<MonthlySales> GetOYAMonthlySales()
	{
		List<MonthlySales> monthlySales = new List<MonthlySales>();
		string query = "\r\n                SELECT\r\n                    MONTH(Invoice.TxnDate) AS MonthNumber,\r\n                    DATENAME(MONTH, Invoice.TxnDate) AS Month,\r\n                    SUM(InvoiceLine.Amount) AS TotalAmount\r\n                FROM\r\n                    Invoice\r\n                JOIN\r\n                    InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n                JOIN\r\n                    Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n                WHERE\r\n                    YEAR(Invoice.TxnDate) = YEAR(GETDATE())\r\n                    AND Item.FullName LIKE 'OY%'\r\n                GROUP BY \r\n                    MONTH(Invoice.TxnDate),\r\n                    DATENAME(MONTH, Invoice.TxnDate)\r\n            ORDER BY \r\n                MonthNumber";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		while (reader.Read())
		{
			MonthlySales monthlySale = new MonthlySales();
			monthlySale.MonthNumber = reader.GetInt32(0);
			monthlySale.Month = reader.GetString(1);
			monthlySale.TotalAmount = reader.GetDecimal(2);
			monthlySales.Add(monthlySale);
		}
		reader.Close();
		return monthlySales;
	}

	public List<MonthlySales> GetPinoMonthlySales()
	{
		List<MonthlySales> monthlySales = new List<MonthlySales>();
		string query = "\r\n                SELECT\r\n                    MONTH(Invoice.TxnDate) AS MonthNumber,\r\n                    DATENAME(MONTH, Invoice.TxnDate) AS Month,\r\n                    SUM(InvoiceLine.Amount) AS TotalAmount\r\n                FROM\r\n                    Invoice\r\n                JOIN\r\n                    InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n                JOIN\r\n                    Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n                WHERE\r\n                    YEAR(Invoice.TxnDate) = YEAR(GETDATE())\r\n                    AND Item.FullName LIKE 'PN%'\r\n                GROUP BY \r\n                    MONTH(Invoice.TxnDate),\r\n                    DATENAME(MONTH, Invoice.TxnDate)\r\n\t\t\t\tORDER BY MonthNumber\r\n            ";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		while (reader.Read())
		{
			MonthlySales monthlySale = new MonthlySales();
			monthlySale.MonthNumber = reader.GetInt32(0);
			monthlySale.Month = reader.GetString(1);
			monthlySale.TotalAmount = reader.GetDecimal(2);
			monthlySales.Add(monthlySale);
		}
		reader.Close();
		return monthlySales;
	}

	public List<YearlySales> GetOYAYearlySales()
	{
		List<YearlySales> yearlySales = new List<YearlySales>();
		string query = "\r\n            SELECT\r\n                YEAR(Invoice.TxnDate) AS Year,\r\n                SUM(InvoiceLine.Amount) AS TotalAmount\r\n            FROM\r\n                Invoice\r\n            JOIN\r\n                InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n            JOIN\r\n                Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n            WHERE\r\n                Item.FullName LIKE 'OY%' AND YEAR(Invoice.TxnDate) > 2015\r\n            GROUP BY \r\n                YEAR(Invoice.TxnDate)\r\n            ORDER BY \r\n                Year";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		while (reader.Read())
		{
			YearlySales yearlySale = new YearlySales();
			yearlySale.Year = reader.GetInt32(0);
			yearlySale.TotalAmount = reader.GetDecimal(1);
			yearlySales.Add(yearlySale);
		}
		reader.Close();
		return yearlySales;
	}

	public List<YearlySales> GetPinoYearlySales()
	{
		List<YearlySales> yearlySales = new List<YearlySales>();
		string query = "\r\n            SELECT\r\n                YEAR(Invoice.TxnDate) AS Year,\r\n                SUM(InvoiceLine.Amount) AS TotalAmount\r\n            FROM\r\n                Invoice\r\n            JOIN\r\n                InvoiceLine ON Invoice.TxnID = InvoiceLine.TxnID\r\n            JOIN\r\n                Item ON InvoiceLine.ItemRefListID = Item.ListID\r\n            WHERE\r\n                Item.FullName LIKE 'PN%' AND YEAR(Invoice.TxnDate) > 2015\r\n            GROUP BY \r\n                YEAR(Invoice.TxnDate)\r\n            ORDER BY \r\n                Year";
		using SqlConnection connection = new SqlConnection(connectionString);
		SqlCommand command = new SqlCommand(query, connection);
		connection.Open();
		SqlDataReader reader = command.ExecuteReader();
		while (reader.Read())
		{
			YearlySales yearlySale = new YearlySales();
			yearlySale.Year = reader.GetInt32(0);
			yearlySale.TotalAmount = reader.GetDecimal(1);
			yearlySales.Add(yearlySale);
		}
		reader.Close();
		return yearlySales;
	}
}
