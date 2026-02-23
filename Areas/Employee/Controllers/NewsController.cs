using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using project_lifecycle.Models;

namespace project_lifecycle.Areas.Employee.Controllers
{
    [Area("Employee")]
    public class NewsController : Controller
    {
        private readonly IConfiguration _config;

        public NewsController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// GET: /Employee/News?q=technology&sortBy=publishedAt&language=en&pageSize=10
        /// </summary>
        public async Task<IActionResult> Index(
            string? q,
            string? sortBy,
            string? language,
            int? pageSize)
        {
            var apiKey = _config["NewsApi:ApiKey"];

            // Defaults
            var query    = string.IsNullOrWhiteSpace(q) ? "latest" : q.Trim();
            var sort     = string.IsNullOrWhiteSpace(sortBy) ? "publishedAt" : sortBy;
            var lang     = string.IsNullOrWhiteSpace(language) ? "en" : language;
            var size     = pageSize is > 0 and <= 100 ? pageSize.Value
                         : int.TryParse(_config["NewsApi:PageSize"], out var cfgSize) ? cfgSize : 10;

            // Preserve filter values for the view
            ViewBag.Query    = query == "latest" ? "" : query;
            ViewBag.SortBy   = sort;
            ViewBag.Language = lang;
            ViewBag.PageSize = size;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error = "News API key not configured. Please set NewsApi:ApiKey in appsettings.json or environment variables.";
                var placeholder = new List<NewsArticle>
                {
                    new NewsArticle
                    {
                        Title       = "News API key not configured",
                        Description = "Add your NewsAPI.org key to appsettings.json → NewsApi:ApiKey, then reload.",
                        Url         = null,
                        UrlToImage  = null,
                        Source      = "Local",
                        PublishedAt = DateTime.UtcNow
                    }
                };
                return View(placeholder);
            }

            var requestUrl = $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(query)}&language={lang}&sortBy={sort}&pageSize={size}&apiKey={apiKey}";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ProjectLifecycle/1.0");
                var resp = await client.GetAsync(requestUrl);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorBody = await resp.Content.ReadAsStringAsync();
                    ViewBag.Error = $"News provider returned {(int)resp.StatusCode} – {resp.ReasonPhrase}. {errorBody}";
                    return View(new List<NewsArticle>());
                }

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var list = new List<NewsArticle>();

                if (root.TryGetProperty("articles", out var articles))
                {
                    foreach (var a in articles.EnumerateArray())
                    {
                        list.Add(new NewsArticle
                        {
                            Title       = a.TryGetProperty("title",       out var t) ? t.GetString() : null,
                            Description = a.TryGetProperty("description", out var d) ? d.GetString() : null,
                            Url         = a.TryGetProperty("url",         out var u) ? u.GetString() : null,
                            UrlToImage  = a.TryGetProperty("urlToImage",  out var i) ? i.GetString() : null,
                            Source      = a.TryGetProperty("source", out var s) && s.TryGetProperty("name", out var n) ? n.GetString() : null,
                            PublishedAt = a.TryGetProperty("publishedAt", out var p) && p.ValueKind == JsonValueKind.String && DateTime.TryParse(p.GetString(), out var dt) ? dt : (DateTime?)null
                        });
                    }
                }

                ViewBag.TotalResults = root.TryGetProperty("totalResults", out var tr) ? tr.GetInt32() : list.Count;
                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error fetching news: " + ex.Message;
                return View(new List<NewsArticle>());
            }
        }
    }
}
