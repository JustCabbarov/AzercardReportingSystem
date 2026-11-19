namespace RMS.Contract.DTOs
{
    public class AzMapFilterOptionsDto
    {
        public List<string> AcquiringDeviceTypes { get; set; } = new();
        public List<string> TransGroups { get; set; } = new();
        public List<string> OperationTypes { get; set; } = new();
        public List<string> TokenStatuses { get; set; } = new();
        public List<string> Mccs { get; set; } = new();
        public List<string> IsAcquiringCategories { get; set; } = new();
        public List<string> TargetBankNames { get; set; } = new();
        public List<string> SourceBankNames { get; set; } = new();
        public List<string> TargetCities { get; set; } = new();
        public List<string> PaymentSystems { get; set; } = new();
    }

    public class WorldMapFilterOptionsDto
    {
        public List<string> IsIssuingCategories { get; set; } = new();
        public List<string> CardProductTypeCategories { get; set; } = new();
        public List<string> PaymentSystems { get; set; } = new();
        public List<string> CardAccountCurrencies { get; set; } = new();
        public List<string> SourceBankNames { get; set; } = new();
        public List<string> AcquiringDeviceTypes { get; set; } = new();
        public List<string> TransGroups { get; set; } = new();
        public List<string> OperationTypes { get; set; } = new();
        public List<string> TokenStatuses { get; set; } = new();
    }
}
