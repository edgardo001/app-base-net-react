using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AppBaseNetReact.Infrastructure.Email;

public partial class EmailRenderer
{
    private static readonly Assembly Assembly = typeof(EmailRenderer).Assembly;
    private static readonly ConcurrentDictionary<string, string> TemplateCache = new();

    public string Render(string templateName, Dictionary<string, string> variables)
    {
        var template = TemplateCache.GetOrAdd(templateName, LoadTemplate);
        return ReplaceVariables(template, variables);
    }

    private static string LoadTemplate(string templateName)
    {
        var resourceName = $"AppBaseNetReact.Infrastructure.Email.Templates.{templateName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Email template '{templateName}' not found as embedded resource.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        return VariablePattern().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (!variables.TryGetValue(key, out var value))
                throw new InvalidOperationException($"Missing variable '{{{{{key}}}}}' in email template.");
            return value;
        });
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariablePattern();
}
