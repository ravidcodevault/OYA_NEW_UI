namespace OYA_Web_Application.Models
{
    public class DatabaseEntryModel
    {
        public string? Customer { get; set; }
        public string? Sheet { get; set; }
        public double? Row { get; set; }

        public string? Region { get; set; }
        public string? Territory { get; set; }
        public string? HQ { get; set; }
        public string? Code { get; set; }

        public double? Start { get; set; }
        public string? Name { get; set; }

        public double? Terr { get; set; }
        public double? Dsc { get; set; }
        public double? Store { get; set; }
        public double? De { get; set; }

        public double? PrevYearTotal { get; set; }
        public double? Plan { get; set; }

        public int? InternationalRow { get; set; }
        public int? WestRow { get; set; }
        public int? EastRow { get; set; }
        public int? CentralRow { get; set; }

        public bool IsActive { get; set; } = true;
    }
}