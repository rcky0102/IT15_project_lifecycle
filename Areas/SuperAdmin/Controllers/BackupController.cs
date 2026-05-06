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
                        // Use parameters for the metadata check
                        bool hasIdentity = false;
                        using (var cmdIdentity = new SqlCommand("SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(@tableName) AND is_identity = 1", connection))
                        {
                            cmdIdentity.Parameters.AddWithValue("@tableName", $"[{table}]");
                            hasIdentity = (int)await cmdIdentity.ExecuteScalarAsync() > 0;
                        }

                        // Safely escape table name for the script
                        string safeTableName = $"[{table.Replace("]", "]]")}]";

                        sqlScript.AppendLine($"-- Processing table: {safeTableName}");
                        sqlScript.AppendLine($"DELETE FROM {safeTableName};");
                        
                        if (hasIdentity) sqlScript.AppendLine($"SET IDENTITY_INSERT {safeTableName} ON;");
                        
                        using (var cmd = new SqlCommand($"SELECT * FROM {safeTableName}", connection))
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                            var columnList = string.Join(", ", columns.Select(c => $"[{c.Replace("]", "]]")}]"));

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
                                            values.Add($"N'{val.ToString().Replace("'", "''")}'"); // Using N'' for unicode safety
                                        else if (val is bool b)
                                            values.Add(b ? "1" : "0");
                                        else if (val is byte[] bin)
                                            values.Add("0x" + BitConverter.ToString(bin).Replace("-", ""));
                                        else
                                            values.Add(val.ToString().Replace(",", ".")); 
                                    }
                                }
                                sqlScript.AppendLine($"INSERT INTO {safeTableName} ({columnList}) VALUES ({string.Join(", ", values)});");
                            }
                        }

                        if (hasIdentity) sqlScript.AppendLine($"SET IDENTITY_INSERT {safeTableName} OFF;");
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
            if (!TryGetSafeBackupFilePath(fileName, out string filePath))
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(filePath)) return NotFound();

            return PhysicalFile(filePath, "application/sql", Path.GetFileName(filePath));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (!TryGetSafeBackupFilePath(fileName, out string filePath))
            {
                return NotFound();
            }

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                TempData["Success"] = "Export deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Validates fileName and returns a safe full path within the backup folder.
        /// Prevents path traversal attacks by ensuring the file is a simple filename
        /// and the resolved path stays within the backup directory.
        /// </summary>
        private bool TryGetSafeBackupFilePath(string fileName, out string safePath)
        {
            safePath = null;

            // Reject null, empty, or whitespace-only names
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // Reject rooted paths (e.g., C:\, /etc/)
            if (Path.IsPathRooted(fileName))
                return false;

            // Reject any path separators or parent directory traversal
            if (fileName.Contains(Path.DirectorySeparatorChar) ||
                fileName.Contains(Path.AltDirectorySeparatorChar) ||
                fileName.Contains(".."))
                return false;

            // Ensure fileName is just a filename (no directory components)
            if (fileName != Path.GetFileName(fileName))
                return false;

            // Build the full path
            string candidatePath = Path.Combine(_backupFolder, fileName);

            // Canonicalize both paths to resolve any remaining tricks
            string canonicalBackupFolder = Path.GetFullPath(_backupFolder);
            string canonicalFilePath = Path.GetFullPath(candidatePath);

            // Ensure the canonical file path is still under the backup folder
            if (!canonicalFilePath.StartsWith(canonicalBackupFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !canonicalFilePath.Equals(canonicalBackupFolder, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            safePath = canonicalFilePath;
            return true;
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
