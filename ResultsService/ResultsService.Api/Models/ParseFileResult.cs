namespace ResultsService.Api.Models
{
    public class ParseFileResult
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public DateTime Date { get; set; }
        public double ExecutionTime { get; set; }
        public decimal Value { get; set; }
    }
}
