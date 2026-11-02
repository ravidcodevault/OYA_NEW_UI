namespace OYA_Web_Application.Models;

public class DistributorModel
{
	public string Customer { get; set; }

	public string Name { get; set; }

	public string Code { get; set; }

	public double? Start { get; set; }

	public string Sheet { get; set; }

	public string Region { get; set; }

	public string Territory { get; set; }

	public string HQ { get; set; }

	public double? PrevYearTotal { get; set; }

	public double? Plan { get; set; }

	public int Row { get; set; }

	public int WestRow { get; set; }

	public int EastRow { get; set; }

	public int CentralRow { get; set; }

	public int InternationalRow { get; set; }

	public bool _changed { get; set; }

	public string _type { get; set; }

	public int _newRow { get; set; }
}
