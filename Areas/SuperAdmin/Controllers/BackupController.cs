using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace project_lifecycle.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class BackupController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _backupFolder;

        public BackupController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            // Store backups in a folder named 'Backups' in the project root
            _backupFolder = Path.Combine(env.ContentRootPath, "Backups");
            
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        public IActionResult Index()
        {
            var files = Directory.GetFiles(_backupFolder)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            return View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;
                string fileName = $"{databaseName}_Export_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                string filePath = Path.Combine(_backupFolder, fileName);

                StringBuilder sqlScript = new StringBuilder();
                sqlScript.AppendLine($"-- Database Export: {databaseName}");
                sqlScript.AppendLine($"-- Generated at: {DateTime.Now}");
                sqlScript.AppendLine();

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Disable all constraints to allow clearing and re-populating tables
                    sqlScript.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';");
                    sqlScript.AppendLine("GO");
                    sqlScript.AppendLine();

                    // Get all user tables
                    var tables = new List<string>();
                    using (var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME NOT LIKE '__EFMigrationsHistory'", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }

                    foreach (var table in tables)
                    {
                        // Check if table has an identity column
                        bool hasIdentity = false;
                        using (var cmdIdentity = new SqlCommand($"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('[{table}]') AND is_identity = 1", connection))
                        {
                            hasIdentity = (int)await cmdIdentity.ExecuteScalarAsync() > 0;
                        }

                        sqlScript.AppendLine($"-- Processing table: {table}");
                        sqlScript.AppendLine($"DELETE FROM [{table}];");
                        
                        if (hasIdentity) sqlScript.AppendLine($"SET IDENTITY_INSERT [{table}] ON;");
                        
                        using (var cmd = new SqlCommand($"SELECT * FROM [{table}]", connection))
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                            var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));

                            while (await reader.ReadAsync())
                            {
                                var values = new List<string>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (reader.IsDBNull(i))
                                        values.Add("NULL");
                                    else
                                    {
                                        var val = reader.GetValue(i);
                                        if (val is string || val is DateTime || val is Guid)
                                            values.Add($"'{val.ToString().Replace("'", "''")}'");
                                        else if (val is bool b)
                                            values.Add(b ? "1" : "0");
                                        else if (val is byte[] bin)
                                            values.Add("0x" + BitConverter.ToString(bin).Replace("-", ""));
                                        else
                                            values.Add(val.ToString().Replace(",", ".")); 
                                    }
                                }
                                sqlScript.AppendLine($"INSERT INTO [{table}] ({columnList}) VALUES ({string.Join(", ", values)});");
                            }
                        }

                        if (hasIdentity) sqlScript.AppendLine($"SET IDENTITY_INSERT [{table}] OFF;");
                        sqlScript.AppendLine("GO");
                        sqlScript.AppendLine();
                    }

                    // Re-enable all constraints
                    sqlScript.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';");
                    sqlScript.AppendLine("GO");
                }

                await System.IO.File.WriteAllTextAsync(filePath, sqlScript.ToString());
                TempData["Success"] = "Database export (SQL Script) created successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating export: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            string filePath = Path.Combine(_backupFolder, fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            return PhysicalFile(filePath, "application/sql", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            string filePath = Path.Combine(_backupFolder, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                TempData["Success"] = "Export deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportBackup(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid SQL file.";
                return RedirectToAction(nameof(Index));
            }

            if (!file.FileName.EndsWith(".sql"))
            {
                TempData["Error"] = "Only .sql files are allowed.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    string script = await reader.ReadToEndAsync();
                    string connectionString = _configuration.GetConnectionString("DefaultConnection");

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        // Split script by 'GO' keyword (case-insensitive, handles batches)
                        var batches = System.Text.RegularExpressions.Regex.Split(
                            script, 
                            @"^\s*GO\s*$", 
                            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        );

                        foreach (var batch in batches)
                        {
                            if (string.IsNullOrWhiteSpace(batch)) continue;

                            using (var command = new SqlCommand(batch, connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }

                TempData["Success"] = "Database script executed/imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error importing database: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
