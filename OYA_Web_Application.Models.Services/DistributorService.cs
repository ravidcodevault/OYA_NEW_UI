using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace OYA_Web_Application.Models.Services;

public class DistributorService
{
	private readonly string _connectionString;

	private readonly IConfiguration _configuration;

	public DistributorService(IConfiguration configuration)
	{
		_configuration = configuration;
		_connectionString = _configuration.GetConnectionString("DefaultConnection");
	}

	public List<DistributorModel> GetAll()
	{
		List<DistributorModel> list = new List<DistributorModel>();
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		using SqlCommand cmd = new SqlCommand("SELECT * FROM Distributor", conn);
		using SqlDataReader reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			list.Add(new DistributorModel
			{
				Customer = reader["Customer"]?.ToString(),
				Name = reader["name"]?.ToString(),
				Code = reader["code"]?.ToString(),
				Sheet = reader["Sheet"]?.ToString(),
				Region = reader["region"]?.ToString(),
				Territory = reader["territory"]?.ToString(),
				HQ = reader["HQ"]?.ToString(),
				Row = ((reader["Row"] != DBNull.Value) ? Convert.ToInt32(reader["Row"]) : 0),
				WestRow = ((reader["WestRow"] != DBNull.Value) ? Convert.ToInt32(reader["WestRow"]) : 0),
				EastRow = ((reader["EastRow"] != DBNull.Value) ? Convert.ToInt32(reader["EastRow"]) : 0),
				CentralRow = ((reader["CentralRow"] != DBNull.Value) ? Convert.ToInt32(reader["CentralRow"]) : 0),
				InternationalRow = ((reader["InternationalRow"] != DBNull.Value) ? Convert.ToInt32(reader["InternationalRow"]) : 0)
			});
		}
		return list;
	}

	public void Insert(DistributorModel model)
	{


        Console.WriteLine("===== INSERT CALLED =====");
        Console.WriteLine($"Customer : {model.Customer}");
        Console.WriteLine($"Name     : {model.Name}");

        using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		string query = "\r\n        INSERT INTO Distributor\r\n        (Customer, name, code, Sheet, region, territory, HQ, Row, WestRow, EastRow, CentralRow, InternationalRow, PrevYearTotal, Plan)\r\n        VALUES\r\n        (@Customer, @Name, @Code, @Sheet, @Region, @Territory, @HQ, @Row, @WestRow, @EastRow, @CentralRow, @InternationalRow, @PrevYearTotal, @Plan)";
		using SqlCommand cmd = new SqlCommand(query, conn);
		cmd.Parameters.AddWithValue("@Customer", model.Customer ?? "");
		cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
		cmd.Parameters.AddWithValue("@Code", model.Code ?? "");
		cmd.Parameters.AddWithValue("@Sheet", model.Sheet ?? "");
		cmd.Parameters.AddWithValue("@Region", model.Region ?? "");
		cmd.Parameters.AddWithValue("@Territory", model.Territory ?? "");
		cmd.Parameters.AddWithValue("@HQ", model.HQ ?? "");
		cmd.Parameters.AddWithValue("@Row", model.Row);
		cmd.Parameters.AddWithValue("@WestRow", model.WestRow);
		cmd.Parameters.AddWithValue("@EastRow", model.EastRow);
		cmd.Parameters.AddWithValue("@CentralRow", model.CentralRow);
		cmd.Parameters.AddWithValue("@InternationalRow", model.InternationalRow);
		SqlParameterCollection parameters = cmd.Parameters;
		double? prevYearTotal = model.PrevYearTotal;
		parameters.AddWithValue("@PrevYearTotal", prevYearTotal.HasValue ? ((object)prevYearTotal.GetValueOrDefault()) : DBNull.Value);
		SqlParameterCollection parameters2 = cmd.Parameters;
		prevYearTotal = model.Plan;
		parameters2.AddWithValue("@Plan", prevYearTotal.HasValue ? ((object)prevYearTotal.GetValueOrDefault()) : DBNull.Value);
		cmd.ExecuteNonQuery();

        Console.WriteLine("INSERT SUCCESS");
    }

	public void Update(DistributorModel model)
	{
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		string query = "\r\n        UPDATE Distributor SET\r\n            name = @Name,\r\n            code = @Code,\r\n            Sheet = @Sheet,\r\n            region = @Region,\r\n            territory = @Territory,\r\n            HQ = @HQ,\r\n            Row = @Row,\r\n            WestRow = @WestRow,\r\n            EastRow = @EastRow,\r\n            CentralRow = @CentralRow,\r\n            InternationalRow = @InternationalRow,\r\n            PrevYearTotal = @PrevYearTotal,\r\n            Plan = @Plan\r\n        WHERE Customer = @Customer";
		using SqlCommand cmd = new SqlCommand(query, conn);
		cmd.Parameters.AddWithValue("@Customer", model.Customer);
		cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
		cmd.Parameters.AddWithValue("@Code", model.Code ?? "");
		cmd.Parameters.AddWithValue("@Sheet", model.Sheet ?? "");
		cmd.Parameters.AddWithValue("@Region", model.Region ?? "");
		cmd.Parameters.AddWithValue("@Territory", model.Territory ?? "");
		cmd.Parameters.AddWithValue("@HQ", model.HQ ?? "");
		cmd.Parameters.AddWithValue("@Row", model.Row);
		cmd.Parameters.AddWithValue("@WestRow", model.WestRow);
		cmd.Parameters.AddWithValue("@EastRow", model.EastRow);
		cmd.Parameters.AddWithValue("@CentralRow", model.CentralRow);
		cmd.Parameters.AddWithValue("@InternationalRow", model.InternationalRow);
		SqlParameterCollection parameters = cmd.Parameters;
		double? prevYearTotal = model.PrevYearTotal;
		parameters.AddWithValue("@PrevYearTotal", prevYearTotal.HasValue ? ((object)prevYearTotal.GetValueOrDefault()) : DBNull.Value);
		SqlParameterCollection parameters2 = cmd.Parameters;
		prevYearTotal = model.Plan;
		parameters2.AddWithValue("@Plan", prevYearTotal.HasValue ? ((object)prevYearTotal.GetValueOrDefault()) : DBNull.Value);
		cmd.ExecuteNonQuery();
	}

	public List<DistributorModel> GetAllMaster()
	{
		List<DistributorModel> list = new List<DistributorModel>();
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		string query = "\r\n            SELECT Customer, name, code, region, territory\r\n            FROM Distributor";
		using SqlCommand cmd = new SqlCommand(query, conn);
		using SqlDataReader reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			list.Add(new DistributorModel
			{
				Customer = reader["Customer"]?.ToString(),
				Name = reader["name"]?.ToString(),
				Code = reader["code"]?.ToString(),
				Region = reader["region"]?.ToString(),
				Territory = reader["territory"]?.ToString()
			});
		}
		return list;
	}

	public void InsertMaster(DistributorModel model)
	{
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		string query = "\r\n            INSERT INTO Distributor\r\n            (Customer, name, code, region, territory)\r\n            VALUES\r\n            (@Customer, @Name, @Code, @Region, @Territory)";
		using SqlCommand cmd = new SqlCommand(query, conn);
		cmd.Parameters.AddWithValue("@Customer", model.Customer ?? "");
		cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
		cmd.Parameters.AddWithValue("@Code", model.Code ?? "");
		cmd.Parameters.AddWithValue("@Region", model.Region ?? "");
		cmd.Parameters.AddWithValue("@Territory", model.Territory ?? "");
		cmd.ExecuteNonQuery();
	}

	public void UpdateMaster(DistributorModel model)
	{
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		string query = "\r\n            UPDATE Distributor SET\r\n                name = @Name,\r\n                code = @Code,\r\n                region = @Region,\r\n                territory = @Territory\r\n            WHERE Customer = @Customer";
		using SqlCommand cmd = new SqlCommand(query, conn);
		cmd.Parameters.AddWithValue("@Customer", model.Customer);
		cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
		cmd.Parameters.AddWithValue("@Code", model.Code ?? "");
		cmd.Parameters.AddWithValue("@Region", model.Region ?? "");
		cmd.Parameters.AddWithValue("@Territory", model.Territory ?? "");
		cmd.ExecuteNonQuery();
	}

	public void ApplyAutoShift(string type, int newRow)
	{
		if (newRow <= 0)
		{
			return;
		}
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		string column = type switch
		{
			"main" => "Row", 
			"west" => "WestRow", 
			"east" => "EastRow", 
			"central" => "CentralRow", 
			"intl" => "InternationalRow", 
			_ => "", 
		};
		if (string.IsNullOrEmpty(column))
		{
			return;
		}
		string query = $"\r\n            UPDATE Distributor\r\n            SET {column} = {column} + 1\r\n            WHERE {column} >= @Row";
		using SqlCommand cmd = new SqlCommand(query, conn);
		cmd.Parameters.AddWithValue("@Row", newRow);
		cmd.ExecuteNonQuery();
	}

	public void UpdateMappingWithShift(List<DistributorModel> list)
	{
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		foreach (DistributorModel d in list)
		{
			if (d._changed && d._newRow > 0)
			{
				string column = d._type switch
				{
					"main" => "Row", 
					"west" => "WestRow", 
					"east" => "EastRow", 
					"central" => "CentralRow", 
					"intl" => "InternationalRow", 
					_ => "", 
				};
				if (!string.IsNullOrEmpty(column))
				{
					string shiftQuery = $"\r\n                        UPDATE Distributor\r\n                        SET {column} = {column} + 1\r\n                        WHERE {column} >= @Row";
					using SqlCommand shiftCmd = new SqlCommand(shiftQuery, conn);
					shiftCmd.Parameters.AddWithValue("@Row", d._newRow);
					shiftCmd.ExecuteNonQuery();
				}
			}
			string updateQuery = "\r\n                UPDATE Distributor SET\r\n                    Row = @Row,\r\n                    WestRow = @WestRow,\r\n                    EastRow = @EastRow,\r\n                    CentralRow = @CentralRow,\r\n                    InternationalRow = @InternationalRow\r\n                WHERE Customer = @Customer";
			using SqlCommand cmd = new SqlCommand(updateQuery, conn);
			cmd.Parameters.AddWithValue("@Customer", d.Customer ?? "");
			cmd.Parameters.AddWithValue("@Row", d.Row);
			cmd.Parameters.AddWithValue("@WestRow", d.WestRow);
			cmd.Parameters.AddWithValue("@EastRow", d.EastRow);
			cmd.Parameters.AddWithValue("@CentralRow", d.CentralRow);
			cmd.Parameters.AddWithValue("@InternationalRow", d.InternationalRow);
			cmd.ExecuteNonQuery();
		}
	}

	public void UpdateMapping(List<DistributorModel> list)
	{
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		foreach (DistributorModel d in list)
		{
			string query = "\r\n                UPDATE Distributor SET\r\n                    Row = @Row,\r\n                    WestRow = @WestRow,\r\n                    EastRow = @EastRow,\r\n                    CentralRow = @CentralRow,\r\n                    InternationalRow = @InternationalRow\r\n                WHERE Customer = @Customer";
			using SqlCommand cmd = new SqlCommand(query, conn);
			cmd.Parameters.AddWithValue("@Customer", d.Customer ?? "");
			cmd.Parameters.AddWithValue("@Row", d.Row);
			cmd.Parameters.AddWithValue("@WestRow", d.WestRow);
			cmd.Parameters.AddWithValue("@EastRow", d.EastRow);
			cmd.Parameters.AddWithValue("@CentralRow", d.CentralRow);
			cmd.Parameters.AddWithValue("@InternationalRow", d.InternationalRow);
			cmd.ExecuteNonQuery();
		}
	}

	public void AddMappingWithShift(MappingRequest req)
	{
		using SqlConnection conn = new SqlConnection(_connectionString);
		conn.Open();
		string column = req.Type switch
		{
			"main" => "Row", 
			"west" => "WestRow", 
			"east" => "EastRow", 
			"central" => "CentralRow", 
			"intl" => "InternationalRow", 
			_ => "", 
		};
		if (string.IsNullOrEmpty(column))
		{
			return;
		}
		string shift = $"UPDATE Distributor SET {column} = {column} + 1 WHERE {column} >= @Row";
		using (SqlCommand cmd = new SqlCommand(shift, conn))
		{
			cmd.Parameters.AddWithValue("@Row", req.Row);
			cmd.ExecuteNonQuery();
		}
		string update = "UPDATE Distributor SET " + column + " = @Row WHERE Customer = @Customer";
		using SqlCommand cmd2 = new SqlCommand(update, conn);
		cmd2.Parameters.AddWithValue("@Customer", req.Customer);
		cmd2.Parameters.AddWithValue("@Row", req.Row);
		cmd2.ExecuteNonQuery();
	}
}
