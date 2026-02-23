using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace project_lifecycle.Services
{
    public class NagerHolidayDto
    {
        public DateTime Date { get; set; }
        public string LocalName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public bool Fixed { get; set; }
        public bool Global { get; set; }
        public string? Type { get; set; }
    }

    public interface INagerHolidayService
    {
        Task<List<NagerHolidayDto>> GetHolidaysAsync(DateTime startDate, DateTime endDate, string countryCode = "PH");
    }

    public class NagerHolidayService : INagerHolidayService
    {
        private readonly HttpClient _httpClient;

        public NagerHolidayService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<NagerHolidayDto>> GetHolidaysAsync(DateTime startDate, DateTime endDate, string countryCode = "PH")
        {
            var holidays = new List<NagerHolidayDto>();
            var yearsToFetch = new HashSet<int>();

            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                yearsToFetch.Add(year);
            }

            foreach (var year in yearsToFetch)
            {
                try
                {
                    var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}";
                    var yearHolidays = await _httpClient.GetFromJsonAsync<List<NagerHolidayDto>>(url);

                    if (yearHolidays != null)
                    {
                        holidays.AddRange(yearHolidays);
                    }
                }
                catch (HttpRequestException)
                {
                    // API unavailable – silently skip
                }
            }

            // Filter to only holidays within the date range
            holidays = holidays
                .FindAll(h => h.Date.Date >= startDate.Date && h.Date.Date <= endDate.Date);

            return holidays;
        }
    }
}
