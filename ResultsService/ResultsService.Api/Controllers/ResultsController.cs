using Microsoft.AspNetCore.Mvc;
using ResultsService.Api.DTOs;
using ResultsService.Api.Services;

namespace ResultsService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController: ControllerBase
    {
        private readonly IResultsService _resultsService;

        public ResultsController(IResultsService resultsService)
        {
            _resultsService = resultsService;
        }

        /// <summary>
        /// Первый метод, принимающий CSV-файл, обрабатывающий его и сохраняющий данные в базу данных.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Результат обработки файла, содержащий имя файла, количество обработанных строк и рассчитанные интегральные показатели.</returns>
        /// <response code="200">Файл успешно обработан и сохранён.</response>
        /// <response code="400">Файл не передан, пуст или имеет некорректный формат.</response>
        [HttpPost("import")]
        [ProducesResponseType(typeof(ImportFileResponseDto),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImportFileResponseDto>> Import(
            [FromForm] IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file == null)
            {
                return BadRequest(new ErrorDto{Error = "FileRequired", Message = "Необходимо передать CSV-файл."});
            }

            if (file.Length == 0)
            {
                return BadRequest(new ErrorDto{Error = "EmptyFile", Message = "Переданный файл пуст."});
            }

            var extension = Path.GetExtension(file.FileName);

            if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ErrorDto{Error = "InvalidFileType", Message = "Допускаются только CSV-файлы."});
            }

            try
            {
                var result = await _resultsService.ImportAsync(file, cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorDto{Error = "InvalidFile", Message = ex.Message});
            }
        }

        /// <summary>
        /// Второй метод, предназначенный для получения списка интегральных результатов с применением фильтров.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Список интегральных результатов, соответствующих заданным фильтрам.</returns>
        /// <response code="200">Список результатов успешно получен.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetResults([FromQuery] ResultFilterDto filter, CancellationToken cancellationToken)
        {
            var results = await _resultsService.GetResultsAsync(filter, cancellationToken);

            return Ok(results);
        }

        /// <summary>
        /// Третий метод, предназначенный для получения списка последних 10 значений, отсортированных по начальному времени запуска Date по имени заданного файла.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Список последних 10 значений указанного файла, отсортированных по дате запуска в порядке убывания.</returns>
        /// <response code="200">Значения успешно получены.</response>
        /// <response code="400">Имя файла не указано.</response>
        /// <response code="404">Значения для указанного файла не найдены.</response>
        [HttpGet("{fileName}/values")]
        [ProducesResponseType(typeof(IEnumerable<ValueDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ValueDto>>> GetLatestValues([FromRoute] string fileName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new ErrorDto{Error = "FileNameRequired", Message = "Необходимо указать имя файла."});
            }

            var values = await _resultsService.GetLatestValuesAsync(fileName, cancellationToken);

            if (!values.Any())
            {
                return NotFound(new ErrorDto{Error = "FileNotFound", Message = $"Данные для файла '{fileName}' не найдены."});
            }

            return Ok(values);
        }
    }
}
