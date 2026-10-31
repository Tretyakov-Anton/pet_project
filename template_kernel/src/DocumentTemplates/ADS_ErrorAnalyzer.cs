using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace DocumentTemplates.Services
{
    /// <summary>
    /// Анализатор ошибок ADS для выявления и исправления проблем с NULL параметрами в AEL файлах
    /// </summary>
    public class ADSErrorAnalyzer
    {
        private readonly List<string> _errorLog = new List<string>();
        private readonly Dictionary<string, List<ParameterIssue>> _parameterIssues = new Dictionary<string, List<ParameterIssue>>();

        public class ParameterIssue
        {
            public int LineNumber { get; set; }
            public int ColumnNumber { get; set; }
            public string Operation { get; set; }
            public string ParameterName { get; set; }
            public string IssueType { get; set; }
            public string SuggestedFix { get; set; }
        }

        /// <summary>
        /// Анализирует AEL файл на предмет ошибок NULL параметров
        /// </summary>
        /// <param name="aelFilePath">Путь к AEL файлу</param>
        /// <returns>Список найденных проблем</returns>
        public List<ParameterIssue> AnalyzeAELFile(string aelFilePath)
        {
            var issues = new List<ParameterIssue>();
            
            if (!File.Exists(aelFilePath))
            {
                _errorLog.Add($"Файл не найден: {aelFilePath}");
                return issues;
            }

            var lines = File.ReadAllLines(aelFilePath);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;
                
                // Проверяем операции add, multiply, subtract, divide
                var operations = new[] { "add", "multiply", "subtract", "divide" };
                
                foreach (var operation in operations)
                {
                    var pattern = $@"{operation}\s*\(\s*([^,)]+)\s*,\s*([^,)]+)\s*\)";
                    var matches = Regex.Matches(line, pattern, RegexOptions.IgnoreCase);
                    
                    foreach (Match match in matches)
                    {
                        var param1 = match.Groups[1].Value.Trim();
                        var param2 = match.Groups[2].Value.Trim();
                        
                        // Проверяем, может ли параметр быть NULL
                        if (IsParameterPotentiallyNull(param1) || IsParameterPotentiallyNull(param2))
                        {
                            var issue = new ParameterIssue
                            {
                                LineNumber = lineNumber,
                                ColumnNumber = match.Index + 1,
                                Operation = operation,
                                ParameterName = $"{param1}, {param2}",
                                IssueType = "Potential NULL Parameter",
                                SuggestedFix = GenerateNullCheckFix(operation, param1, param2)
                            };
                            
                            issues.Add(issue);
                        }
                    }
                }
            }
            
            _parameterIssues[aelFilePath] = issues;
            return issues;
        }

        /// <summary>
        /// Проверяет, может ли параметр быть NULL
        /// </summary>
        private bool IsParameterPotentiallyNull(string parameter)
        {
            // Убираем скобки и пробелы
            parameter = parameter.Trim().Trim('(', ')');
            
            // Если это просто имя переменной без проверки на NULL
            if (Regex.IsMatch(parameter, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                return true;
            }
            
            // Если это выражение без проверки на NULL
            if (!parameter.Contains("NULL") && !parameter.Contains("!=") && !parameter.Contains("=="))
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Генерирует исправление с проверкой на NULL
        /// </summary>
        private string GenerateNullCheckFix(string operation, string param1, string param2)
        {
            var cleanParam1 = param1.Trim();
            var cleanParam2 = param2.Trim();
            
            return $@"
// Безопасная операция {operation} с проверкой на NULL
if ({cleanParam1} != NULL && {cleanParam2} != NULL) {{
    result = {operation}({cleanParam1}, {cleanParam2});
}} else {{
    result = 0; // или подходящее значение по умолчанию
}}";
        }

        /// <summary>
        /// Создает исправленную версию AEL файла
        /// </summary>
        /// <param name="aelFilePath">Путь к исходному файлу</param>
        /// <param name="outputPath">Путь для сохранения исправленного файла</param>
        public void CreateFixedAELFile(string aelFilePath, string outputPath)
        {
            var issues = AnalyzeAELFile(aelFilePath);
            if (issues.Count == 0)
            {
                File.Copy(aelFilePath, outputPath, true);
                return;
            }

            var lines = File.ReadAllLines(aelFilePath).ToList();
            
            // Сортируем проблемы по номеру строки в обратном порядке для правильной замены
            var sortedIssues = issues.OrderByDescending(i => i.LineNumber).ToList();
            
            foreach (var issue in sortedIssues)
            {
                var lineIndex = issue.LineNumber - 1;
                if (lineIndex < lines.Count)
                {
                    var originalLine = lines[lineIndex];
                    var fixedLine = ApplyFixToLine(originalLine, issue);
                    lines[lineIndex] = fixedLine;
                }
            }
            
            // Добавляем заголовок с проверками параметров в начало файла
            var headerLines = GenerateParameterValidationHeader(issues);
            lines.InsertRange(0, headerLines);
            
            File.WriteAllLines(outputPath, lines);
        }

        /// <summary>
        /// Применяет исправление к конкретной строке
        /// </summary>
        private string ApplyFixToLine(string originalLine, ParameterIssue issue)
        {
            var operationPattern = $@"{issue.Operation}\s*\([^)]+\)";
            var match = Regex.Match(originalLine, operationPattern, RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                var beforeOperation = originalLine.Substring(0, match.Index);
                var afterOperation = originalLine.Substring(match.Index + match.Length);
                
                // Извлекаем параметры из операции
                var paramMatch = Regex.Match(match.Value, @"\(([^,)]+),\s*([^,)]+)\)");
                if (paramMatch.Success)
                {
                    var param1 = paramMatch.Groups[1].Value.Trim();
                    var param2 = paramMatch.Groups[2].Value.Trim();
                    
                    var safeOperation = $@"
// Безопасная операция {issue.Operation}
if ({param1} != NULL && {param2} != NULL) {{
    result = {issue.Operation}({param1}, {param2});
}} else {{
    result = 0; // значение по умолчанию
}}";
                    
                    return beforeOperation + safeOperation + afterOperation;
                }
            }
            
            return originalLine;
        }

        /// <summary>
        /// Генерирует заголовок с валидацией параметров
        /// </summary>
        private List<string> GenerateParameterValidationHeader(List<ParameterIssue> issues)
        {
            var header = new List<string>
            {
                "// Автоматически сгенерированная валидация параметров",
                "// Проверка на NULL значения перед математическими операциями",
                ""
            };
            
            // Собираем уникальные параметры
            var uniqueParams = new HashSet<string>();
            foreach (var issue in issues)
            {
                var params = issue.ParameterName.Split(',');
                foreach (var param in params)
                {
                    var cleanParam = param.Trim();
                    if (Regex.IsMatch(cleanParam, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                    {
                        uniqueParams.Add(cleanParam);
                    }
                }
            }
            
            foreach (var param in uniqueParams)
            {
                header.Add($"// Установка значения по умолчанию для {param}");
                header.Add($"if ({param} == NULL) {param} = 0;");
            }
            
            header.Add("");
            return header;
        }

        /// <summary>
        /// Получает отчет об ошибках
        /// </summary>
        public string GetErrorReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== ОТЧЕТ ОБ АНАЛИЗЕ ОШИБОК ADS ===");
            report.AppendLine();
            
            foreach (var fileIssues in _parameterIssues)
            {
                report.AppendLine($"Файл: {fileIssues.Key}");
                report.AppendLine($"Найдено проблем: {fileIssues.Value.Count}");
                report.AppendLine();
                
                foreach (var issue in fileIssues.Value)
                {
                    report.AppendLine($"Строка {issue.LineNumber}, колонка {issue.ColumnNumber}:");
                    report.AppendLine($"  Операция: {issue.Operation}");
                    report.AppendLine($"  Параметры: {issue.ParameterName}");
                    report.AppendLine($"  Проблема: {issue.IssueType}");
                    report.AppendLine($"  Предлагаемое исправление:");
                    report.AppendLine($"  {issue.SuggestedFix}");
                    report.AppendLine();
                }
            }
            
            return report.ToString();
        }

        /// <summary>
        /// Создает шаблон безопасного AEL кода
        /// </summary>
        public string GenerateSafeAELTemplate()
        {
            return @"
// Шаблон безопасного AEL кода
// Автоматически сгенерированный шаблон для предотвращения ошибок NULL

// 1. Валидация параметров в начале
function validateParameters() {
    // Проверяем все критические параметры
    if (width == NULL) width = 100;        // ширина по умолчанию
    if (length == NULL) length = 200;      // длина по умолчанию
    if (spacing == NULL) spacing = 50;     // расстояние по умолчанию
    if (thickness == NULL) thickness = 10; // толщина по умолчанию
}

// 2. Безопасные математические операции
function safeAdd(a, b) {
    if (a != NULL && b != NULL) {
        return add(a, b);
    } else {
        return 0;
    }
}

function safeMultiply(a, b) {
    if (a != NULL && b != NULL) {
        return multiply(a, b);
    } else {
        return 0;
    }
}

function safeSubtract(a, b) {
    if (a != NULL && b != NULL) {
        return subtract(a, b);
    } else {
        return 0;
    }
}

function safeDivide(a, b) {
    if (a != NULL && b != NULL && b != 0) {
        return divide(a, b);
    } else {
        return 0;
    }
}

// 3. Основная функция генерации artwork
function generateArtwork() {
    // Вызываем валидацию параметров
    validateParameters();
    
    // Используем безопасные операции
    var totalWidth = safeAdd(width, spacing);
    var totalLength = safeMultiply(length, 2);
    
    // Генерация геометрии...
    // Ваш код здесь
}
";
        }
    }
}
