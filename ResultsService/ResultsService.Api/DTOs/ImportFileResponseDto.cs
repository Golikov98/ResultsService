namespace ResultsService.Api.DTOs
{
    public class ImportFileResponseDto
    {
        public string FileName { get; set; } = null!;
        public int RowsCount { get; set; }
        public ResultDto Result { get; set; } = null!;
    }
}
