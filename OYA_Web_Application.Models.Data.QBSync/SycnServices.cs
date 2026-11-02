using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace OYA_Web_Application.Models.Data.QBSync;

public class SycnServices
{
	public class SyncDetails
	{
		public int Id { get; set; }

		public string SyncType { get; set; }

		public DateTime LastSyncedOn { get; set; }

		public string Status { get; set; }

		public string Description { get; set; }

		public bool IsActive { get; set; }
	}

	private readonly IConfiguration _configuration;

	private string connectionString = " ";

	public SycnServices(IConfiguration configuration)
	{
		_configuration = configuration;
		connectionString = _configuration.GetConnectionString("DefaultConnection");
	}

	public List<SyncDetails> GetAllSyncDetails()
	{
		List<SyncDetails> syncDetails = new List<SyncDetails>();
		try
		{
			using SqlConnection connection = new SqlConnection(connectionString);
			using SqlCommand command = new SqlCommand("SELECT * FROM [dbo].[SyncStatus]", connection);
			connection.Open();
			using SqlDataReader reader = command.ExecuteReader();
			while (reader.Read())
			{
				syncDetails.Add(new SyncDetails
				{
					Id = Convert.ToInt32(reader["Id"]),
					SyncType = Convert.ToString(reader["SyncType"]),
					LastSyncedOn = Convert.ToDateTime(reader["LastSyncedOn"]),
					IsActive = Convert.ToBoolean(reader["IsActive"]),
					Status = Convert.ToString(reader["Status"]),
					Description = Convert.ToString(reader["Description"])
				});
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
		return syncDetails;
	}

	public List<SyncDetails> UpdateStatusForAll(SyncDetails sync)
	{
		List<SyncDetails> syncDetails = new List<SyncDetails>();
		try
		{
			using SqlConnection connection = new SqlConnection(connectionString);
			try
			{
				using SqlCommand command = new SqlCommand("UPDATE [dbo].[SyncStatus] SET [Status] = @Status", connection);
				command.Parameters.AddWithValue("@Status", sync.Status);
				connection.Open();
				command.ExecuteNonQuery();
			}
			catch (Exception)
			{
			}
			finally
			{
				if (connection.State != ConnectionState.Closed)
				{
					connection.Close();
					connection.Dispose();
				}
			}
		}
		catch (Exception ex2)
		{
			Console.WriteLine("Error: " + ex2.Message);
		}
		return syncDetails;
	}
}
