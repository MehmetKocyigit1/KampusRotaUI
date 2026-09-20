using System.Collections.Generic;

namespace KampusRotaUI.Models
{
    public class University
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int DefaultZoom { get; set; } = 14;
        public string EmailDomain { get; set; } = string.Empty;
        public string? IletisimEmail { get; set; }
        public int LocationCount { get; set; }
        public List<CampusLocation> Locations { get; set; } = new();
    }

    public class CampusLocation
    {
        public int Id { get; set; }
        public int UniversityId { get; set; }
        public string LocationKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Name => Title;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Category { get; set; } = "Kampus";
        public string? Description { get; set; }
    }
}
