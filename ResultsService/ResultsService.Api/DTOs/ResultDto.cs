namespace ResultsService.Api.DTOs
{
    public class ResultDto
    {
        public string FileName { get; set; } = null!;
        public double TimeDelta { get; set; }
        public DateTime StartDate { get; set; }
        public double AverageExecutionTime { get; set; }
        public decimal AverageValue { get; set; }
        public decimal MedianValue { get; set; }
        public decimal MaxValue { get; set; }
        public decimal MinValue { get; set; }
    }
}
