using BlazorWithTranslations.Resources;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Resources;

namespace BlazorWithTranslations
{
    public static class TranslationsManager
    {
        private static readonly ResourceManager _resourceManager = new(typeof(Translations).FullName!, typeof(Translations).Assembly);

        public static string GetTranslationToLower(string key, params object[] args)
        {
            return GetTranslation(key, args).ToLower(CultureInfo.CurrentUICulture);
        }

        public static string GetTranslation(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key) || !TryGetTranslationFromResource(key, out var value))
            {
                var defaultTranslation = Translations.ResourceManager.GetString(key, CultureInfo.InvariantCulture);
                return GetFormattedOrEmpty(defaultTranslation, args);
            }

            return string.Format(value, args);
        }

        public static string GetTranslationToLower(Expression<Func<string>> translationProp, params object[] args)
        {
            return GetTranslation(translationProp, args).ToLower(CultureInfo.CurrentUICulture);
        }

        public static string GetTranslation(Expression<Func<string>> translationProp, params object[] args)
        {
            if (translationProp is null || !TryExtractKeyFromExpression(translationProp, out var key))
            {
                return string.Empty;
            }

            if (!TryGetTranslationFromResource(key, out var value))
            {
                var defaultTranslation = translationProp.Compile().Invoke();
                return GetFormattedOrEmpty(defaultTranslation, args);
            }

            return string.Format(value, args);
        }

        public static bool TryExtractKeyFromExpression(Expression<Func<string>> expression, [NotNullWhen(true)] out string? name)
        {
            name = null;

            if (expression.Body is MemberExpression memberEx)
            {
                bool isFromCorrectType = memberEx.Member.DeclaringType != typeof(Translations);
                if (isFromCorrectType)
                {
#if DEBUG
                    Debug.Assert(isFromCorrectType, $"Expression must reference a member of the {nameof(Translations)} class.");
#endif
                    return false;
                }

                name = memberEx.Member.Name;
                return !string.IsNullOrEmpty(name);
            }

            return false;
        }

        private static string GetFormattedOrEmpty(string? translationVal, object[] args) =>
             string.IsNullOrEmpty(translationVal) ? string.Empty : string.Format(translationVal, args);

        private static bool TryGetTranslationFromResource(string key, [NotNullWhen(true)] out string? value)
        {
            value = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);

#if DEBUG
            if (value is null)
                Debug.WriteLine($"[Missing translation] Key: '{key}' in culture: {CultureInfo.CurrentUICulture.Name}");
#endif

            return value is not null;
        }

        public static IEnumerable<string> GetAllKeys()
        {
            var resourceSet = _resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, createIfNotExists: true, tryParents: true);
            return resourceSet?.Cast<DictionaryEntry>().Select(e => (string)e.Key) ?? [];
        }
    }
}
