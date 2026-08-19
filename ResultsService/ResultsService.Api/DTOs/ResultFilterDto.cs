namespace ResultsService.Api.DTOs
{
    public class ResultFilterDto
    {
        public string? FileName { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public decimal? AverageValueFrom { get; set; }
        public decimal? AverageValueTo { get; set; }
        public double? AverageExecutionTimeFrom { get; set; }
        public double? AverageExecutionTimeTo { get; set; }
    }
}
