using Microsoft.AspNetCore.Http;
using ResultsService.Api.DTOs;

namespace ResultsService.Api.Services
{
    public interface IResultsService
    {
        Task<ImportFileResponseDto> ImportAsync(
           IFormFile file,
           CancellationToken cancellationToken);

        Task<IEnumerable<ResultDto>> GetResultsAsync(
            ResultFilterDto filter,
            CancellationToken cancellationToken);

        Task<IEnumerable<ValueDto>> GetLatestValuesAsync(
            string fileName,
            CancellationToken cancellationToken);
    }
}
