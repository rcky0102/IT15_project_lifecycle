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

        public async Task<IActionResult> Index()
        {
            var apiKey = _config["NewsApi:ApiKey"];
            var country = _config["NewsApi:Country"] ?? "us";
            var pageSize = _config["NewsApi:PageSize"] ?? "10";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ViewBag.Error = "News API key not configured. Please set NewsApi:ApiKey in appsettings.json or environment variables.";

                // Provide a placeholder article so the UI shows meaningful content
                var placeholder = new List<NewsArticle>
                {
                    new NewsArticle
                    {
                        Title = "News API key not configured",
                        Description = "To fetch live news, add your NewsAPI.org key to the NewsApi:ApiKey setting in appsettings.json or set the environment variable 'NewsApi__ApiKey'. This placeholder article is shown while no API key is configured.",
                        Url = null,
                        UrlToImage = null,
                        Source = "Local",
                        PublishedAt = DateTime.UtcNow
                    }
                };

                return View(placeholder);
            }

            // Use "everything" endpoint – works on the free Developer plan
            var requestUrl = $"https://newsapi.org/v2/everything?q=latest&language=en&sortBy=publishedAt&pageSize={pageSize}&apiKey={apiKey}";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "ProjectLifecycle/1.0");
                var resp = await client.GetAsync(requestUrl);
                if (!resp.IsSuccessStatusCode)
                {
                    var errorBody = await resp.Content.ReadAsStringAsync();
                    ViewBag.Error = $"News provider returned {(int)resp.StatusCode} – {resp.ReasonPhrase}. Details: {errorBody}";
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
                        var article = new NewsArticle
                        {
                            Title = a.TryGetProperty("title", out var t) ? t.GetString() : null,
                            Description = a.TryGetProperty("description", out var d) ? d.GetString() : null,
                            Url = a.TryGetProperty("url", out var u) ? u.GetString() : null,
                            UrlToImage = a.TryGetProperty("urlToImage", out var i) ? i.GetString() : null,
                            Source = a.TryGetProperty("source", out var s) && s.TryGetProperty("name", out var n) ? n.GetString() : null,
                            PublishedAt = a.TryGetProperty("publishedAt", out var p) && p.ValueKind == JsonValueKind.String && DateTime.TryParse(p.GetString(), out var dt) ? dt : (DateTime?)null
                        };
                        list.Add(article);
                    }
                }

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
