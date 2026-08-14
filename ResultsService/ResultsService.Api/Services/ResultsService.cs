using Microsoft.EntityFrameworkCore;
using ResultsService.Api.Data;
using ResultsService.Api.DTOs;
using ResultsService.Api.Models;
using System.Globalization;

namespace ResultsService.Api.Services
{
    public class ResultsService: IResultsService
    {
        private readonly ResultsDbContext _dbContext;
        public ResultsService(ResultsDbContext dbContext) 
        { 
            _dbContext = dbContext;
        }

        /// <summary>
        /// Первый метод, принимающий на вход csv, после чего происходит обработка и сохранение данных файла в БД.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Результат обработки файла, содержащий имя файла, количество обработанных строк и рассчитанные интегральные показатели.</returns>
        public async Task<ImportFileResponseDto> ImportAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var values = await ParseCsvAsync(file, cancellationToken);
            ValidateValues(values);
            var result = CalculateResult(values);
            await SaveAsync(file.FileName, values, result, cancellationToken);
            return MapToDto(result, values.Count);
        }

        /// <summary>
        /// Читает и разбирает CSV-файл, преобразуя строки файла в список объектов.
        /// Выполняет проверку структуры CSV-файла и корректности типов данных.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Список распарсенных записей файла в виде объектов</returns>
        private async Task<List<ParseFileResult>> ParseCsvAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var fileName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Имя файла не может быть пустым.");
            }

            var values = new List<ParseFileResult>();

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            // Заголовок
            var header = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(header))
            {
                throw new ArgumentException("CSV-файл пуст.");
            }

            const string expectedHeader = "Date;ExecutionTime;Value";

            if (!string.Equals(
                    header.Trim(),
                    expectedHeader,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Некорректный заголовок CSV. " + "Ожидается: Date;ExecutionTime;Value");
            }

            var lineNumber = 1;

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken);

                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    throw new ArgumentException($"Строка {lineNumber} пуста.");
                }

                var columns = line.Split(';');

                if (columns.Length != 3)
                {
                    throw new ArgumentException($"Строка {lineNumber} должна содержать " + "ровно 3 значения.");
                }

                // Date
                if (string.IsNullOrWhiteSpace(columns[0]))
                {
                    throw new ArgumentException($"Строка {lineNumber}: " + "отсутствует Date.");
                }

                if (!DateTime.TryParse(
                        columns[0],
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var date))
                {
                    throw new ArgumentException($"Строка {lineNumber}: " + "некорректное значение Date.");
                }

                // ExecutionTime
                if (string.IsNullOrWhiteSpace(columns[1]))
                {
                    throw new ArgumentException($"Строка {lineNumber}: " + "отсутствует ExecutionTime.");
                }

                if (!double.TryParse(
                        columns[1],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var executionTime))
                {
                    throw new ArgumentException($"Строка {lineNumber}: " + "некорректное значение ExecutionTime.");
                }

                // Value
                if (string.IsNullOrWhiteSpace(columns[2]))
                {
                    throw new ArgumentException($"Строка {lineNumber}: " + "отсутствует Value.");
                }

                if (!decimal.TryParse(
                        columns[2],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var value))
                {
                    throw new ArgumentException($"Строка {lineNumber}: " + "некорректное значение Value.");
                }

                values.Add(new ParseFileResult
                {
                    FileName = fileName,
                    Date = date,
                    ExecutionTime = executionTime,
                    Value = value
                });

                // Больше 10000 строк
                if (values.Count > 10_000)
                {
                    throw new ArgumentException("Количество строк не может " + "превышать 10000.");
                }
            }

            return values;
        }

        /// <summary>
        /// Выполняет проверку распарсенных данных CSV-файла на соответствие ограничениям предметной области.
        /// Проверяет количество записей, допустимый диапазон дат, время выполнения и значение показателя.
        /// </summary>
        /// <param name="values"></param>
        private void ValidateValues(List<ParseFileResult> values)
        {
            // От 1 до 10000 строк
            if (values.Count < 1)
            {
                throw new ArgumentException("CSV-файл должен содержать " + "хотя бы одну строку.");
            }

            if (values.Count > 10_000)
            {
                throw new ArgumentException("Количество строк не может " + "превышать 10000.");
            }

            var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var currentDate = DateTime.UtcNow;

            foreach (var value in values)
            {
                // Date >= 01.01.2000
                if (value.Date < minDate)
                {
                    throw new ArgumentException($"Дата {value.Date:O} " + "не может быть раньше 01.01.2000.");
                }

                // Date <= текущего времени
                if (value.Date > currentDate)
                {
                    throw new ArgumentException($"Дата {value.Date:O} " + "не может быть позже текущего времени.");
                }

                // ExecutionTime >= 0
                if (value.ExecutionTime < 0)
                {
                    throw new ArgumentException($"ExecutionTime не может быть " + $"меньше 0. Значение: " + $"{value.ExecutionTime}.");
                }

                // Value >= 0
                if (value.Value < 0)
                {
                    throw new ArgumentException($"Value не может быть меньше 0. " + $"Значение: {value.Value}.");
                }
            }
        }

        /// <summary>
        /// Рассчитывает интегральные результаты на основе распарсенных значений CSV-файла: дельту времени, время запуска первой операции,
        /// среднее время выполнения, среднее, медиану, максимальное и минимальное значения показателя.
        /// </summary>
        /// <param name="values"></param>
        /// <returns>Объект, содержащий рассчитанныеинтегральные показатели файла.</returns>
        private Models.Result CalculateResult(List<ParseFileResult> values)
        {
            var minDate = values.Min(x => x.Date);
            var maxDate = values.Max(x => x.Date);

            var timeDelta =
                (maxDate - minDate).TotalSeconds;

            var averageExecutionTime =
                values.Average(x => x.ExecutionTime);

            var averageValue =
                values.Average(x => x.Value);

            var sortedValues = values
                .Select(x => x.Value)
                .OrderBy(x => x)
                .ToList();

            decimal medianValue;

            if (sortedValues.Count % 2 == 1)
            {
                // Нечётное количество
                medianValue = sortedValues[sortedValues.Count / 2];
            }
            else
            {
                // Чётное количество
                var middleIndex = sortedValues.Count / 2;

                medianValue = (sortedValues[middleIndex - 1] + sortedValues[middleIndex]) / 2;
            }

            return new Models.Result
            {
                FileName = values[0].FileName,
                TimeDelta = timeDelta,
                StartDate = minDate,
                AverageExecutionTime = averageExecutionTime,
                AverageValue = averageValue,
                MedianValue = medianValue,
                MaxValue = values.Max(x => x.Value),
                MinValue = values.Min(x => x.Value)
            };
        }

        /// <summary>
        /// Сохраняет значения и интегральный результат файла в базе данных в рамках транзакции. Перед сохранением удаляет ранее сохранённые значения и результат для файла с указанным именем.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="values"></param>
        /// <param name="result"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Асинхронная операция сохранения данных в базе данных.</returns>
        private async Task SaveAsync(string fileName, List<ParseFileResult> values, Models.Result result, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Удаляем старые Values
                var oldValues = await _dbContext.Values
                    .Where(x => x.FileName == fileName)
                    .ToListAsync(cancellationToken);

                _dbContext.Values.RemoveRange(oldValues);

                // Удаляем старый Result
                var oldResult = await _dbContext.Results
                    .FirstOrDefaultAsync(
                        x => x.FileName == fileName,
                        cancellationToken);

                if (oldResult != null)
                {
                    _dbContext.Results.Remove(oldResult);
                }

                // Добавляем новые Values
                await _dbContext.Values.AddRangeAsync(values, cancellationToken);

                // Добавляем новый Result
                await _dbContext.Results.AddAsync(result, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        }

        /// <summary>Преобразует интегральный результат в DTO для возврата клиенту.</summary>
        /// <param name="result"></param>
        /// <param name="rowsCount"></param>
        /// <returns>Объект, содержащий имя файла, количество обработанных строк и рассчитанные интегральные показатели.</returns>
        private ImportFileResponseDto MapToDto(Models.Result result, int rowsCount)
        {
            return new ImportFileResponseDto
            {
                FileName = result.FileName,
                RowsCount = rowsCount,

                Result = new ResultDto
                {
                    FileName = result.FileName,
                    TimeDelta = result.TimeDelta,
                    StartDate = result.StartDate,
                    AverageExecutionTime = result.AverageExecutionTime,
                    AverageValue = result.AverageValue,
                    MedianValue = result.MedianValue,
                    MaxValue = result.MaxValue,
                    MinValue = result.MinValue
                }
            };
        }

        /// <summary>
        /// Второй метод, предназначенный для получения списка записей из таблицы Results, подходящих под фильтры.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Список записей из таблицы Results, подходящих под фильтры.</returns>
        public async Task<IEnumerable<ResultDto>> GetResultsAsync(ResultFilterDto filter, CancellationToken cancellationToken)
        {
            var query = _dbContext.Results
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.FileName))
            {
                query = query.Where(x => x.FileName == filter.FileName);
            }

            if (filter.StartDateFrom.HasValue)
            {
                query = query.Where(x => x.StartDate >= filter.StartDateFrom.Value);
            }

            if (filter.StartDateTo.HasValue)
            {
                query = query.Where(x => x.StartDate <= filter.StartDateTo.Value);
            }

            if (filter.AverageValueFrom.HasValue)
            {
                query = query.Where(x => x.AverageValue >= filter.AverageValueFrom.Value);
            }

            if (filter.AverageValueTo.HasValue)
            {
                query = query.Where(x => x.AverageValue <= filter.AverageValueTo.Value);
            }

            if (filter.AverageExecutionTimeFrom.HasValue)
            {
                query = query.Where(x => x.AverageExecutionTime >= filter.AverageExecutionTimeFrom.Value);
            }

            if (filter.AverageExecutionTimeTo.HasValue)
            {
                query = query.Where(x => x.AverageExecutionTime <= filter.AverageExecutionTimeTo.Value);
            }

            return await query
                .Select(x => new ResultDto
                {
                    FileName = x.FileName,
                    TimeDelta = x.TimeDelta,
                    StartDate = x.StartDate,
                    AverageExecutionTime = x.AverageExecutionTime,
                    AverageValue = x.AverageValue,
                    MedianValue = x.MedianValue,
                    MaxValue = x.MaxValue,
                    MinValue = x.MinValue
                })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Третий метод, предназначенный для получения списка последних 10 значений, отсортированных по начальному времени запуска Date по имени заданного файла.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Список последних 10 значений файла, отсортированных по дате запуска в порядке убывания.</returns>
        public async Task<IEnumerable<ValueDto>> GetLatestValuesAsync(string fileName, CancellationToken cancellationToken)
        {
            return await _dbContext.Values
                .AsNoTracking()
                .Where(x => x.FileName == fileName)
                .OrderByDescending(x => x.Date)
                .Take(10)
                .Select(x => new ValueDto
                {
                    Date = x.Date,
                    ExecutionTime = x.ExecutionTime,
                    Value = x.Value
                })
                .ToListAsync(cancellationToken);
        }
    }
}
