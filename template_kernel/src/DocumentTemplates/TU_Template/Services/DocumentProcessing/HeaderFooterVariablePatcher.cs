using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TU_Template.Services.DocumentProcessing;

/// <summary>
/// Дорабатывает переменные в колонтитулах уже сохранённого .docx.
///
/// Зачем это нужно: в колонтитулах шаблона ГОСТ-рамка со штампом лежит в текстовых рамках
/// внутри w:AlternateContent, и там две копии одного и того же содержимого — современная
/// (Choice/DrawingML) и устаревшая (Fallback/VML). DocX заменяет текст только в первой,
/// поэтому в сохранённом файле в VML-копии остаётся «${ТУ}», «${названия}» и т.п.
/// В современном Word значение видно правильное, но часть просмотрщиков и конвертеров
/// показывают именно Fallback — и пользователь видит маркер вместо номера ТУ.
/// </summary>
public static class HeaderFooterVariablePatcher
{
    private static readonly Regex TextNodeRegex =
        new(@"(<w:t(?:\s[^>]*)?>)(.*?)(</w:t>)", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex MarkerRegex =
        new(@"\$\s*\{([^}]{1,80})\}", RegexOptions.Compiled);

    /// <summary>
    /// Подставляет значения переменных в колонтитулы файла. Возвращает число выполненных замен.
    /// Неизвестные переменные оставляет как есть. Файл меняется только при успешной обработке.
    /// </summary>
    public static int Patch(string docxPath, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath)) return 0;
        if (variables == null || variables.Count == 0) return 0;

        // Работаем на копии и подменяем исходный файл только после удачной обработки,
        // чтобы при сбое не оставить пользователя с повреждённым документом.
        var tempPath = docxPath + ".hf.tmp";
        int total = 0;

        try
        {
            File.Copy(docxPath, tempPath, overwrite: true);

            using (var archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
            {
                foreach (var entry in archive.Entries.Where(IsHeaderOrFooter).ToList())
                {
                    string xml;
                    using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        xml = reader.ReadToEnd();

                    var patched = ReplaceInTextNodes(xml, variables, out int replaced);
                    if (replaced == 0) continue;

                    using (var stream = entry.Open())
                    {
                        stream.SetLength(0);
                        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                        writer.Write(patched);
                    }

                    total += replaced;
                }
            }

            if (total > 0)
                File.Copy(tempPath, docxPath, overwrite: true);
        }
        catch (Exception)
        {
            // Документ уже сохранён и корректен — доработка колонтитулов необязательна.
            return 0;
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* не мешаем основному сценарию */ }
        }

        return total;
    }

    private static bool IsHeaderOrFooter(ZipArchiveEntry entry)
    {
        var name = entry.FullName;
        return name.StartsWith("word/header", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("word/footer", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Заменяет маркеры только внутри текстовых узлов w:t, не трогая разметку.
    /// </summary>
    private static string ReplaceInTextNodes(string xml, IReadOnlyDictionary<string, string> variables, out int replaced)
    {
        int count = 0;

        var result = TextNodeRegex.Replace(xml, node =>
        {
            var open = node.Groups[1].Value;
            var text = node.Groups[2].Value;
            var close = node.Groups[3].Value;

            if (text.IndexOf('$') < 0) return node.Value;

            var newText = MarkerRegex.Replace(text, marker =>
            {
                var key = marker.Groups[1].Value.Trim();

                if (!TryResolve(variables, key, out var value))
                    return marker.Value;

                count++;
                return Escape(value);
            });

            return open + newText + close;
        });

        replaced = count;
        return result;
    }

    private static bool TryResolve(IReadOnlyDictionary<string, string> variables, string key, out string value)
    {
        if (variables.TryGetValue(key, out var exact))
        {
            value = exact ?? "";
            return true;
        }

        var alt = variables.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        if (alt != null)
        {
            value = variables[alt] ?? "";
            return true;
        }

        value = "";
        return false;
    }

    private static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
