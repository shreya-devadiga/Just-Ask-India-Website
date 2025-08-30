using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JustAskIndia.Services
{
    public static class TextExtractor
    {
        /// <summary>
        /// Extracts spelling mistakes and search queries from GPT-formatted response.
        /// Example:
        /// - Spelling Mistakes:
        ///     ["solar pannels"]
        /// - Search query:
        ///     ["solar panels", "solar energy"]
        /// </summary>
        public static void ExtractSpellingAndQuery(string input, out List<string> spellingMistakes, out List<string> searchQueries)
        {
            spellingMistakes = new List<string>();
            searchQueries = new List<string>();

            // Match sections using Regex
            var spellingMatch = Regex.Match(input, @"- Spelling Mistakes:\s*\[\s*([^\]]*)\]");
            var queryMatch = Regex.Match(input, @"- Search query:\s*\[\s*([^\]]*)\]");

            if (spellingMatch.Success)
            {
                spellingMistakes = ExtractStringsFromList(spellingMatch.Groups[1].Value);
            }

            if (queryMatch.Success)
            {
                searchQueries = ExtractStringsFromList(queryMatch.Groups[1].Value);
            }
        }

        /// <summary>
        /// Extracts all double-quoted strings from a comma-separated list.
        /// Example: "solar panels", "solar energy" → ["solar panels", "solar energy"]
        /// </summary>
        private static List<string> ExtractStringsFromList(string listContent)
        {
            var results = new List<string>();
            var matches = Regex.Matches(listContent, @"""([^""]+)""");

            foreach (Match match in matches)
            {
                results.Add(match.Groups[1].Value.Trim());
            }

            return results;
        }
    }
}
