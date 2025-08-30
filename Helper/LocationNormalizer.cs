using System;
using System.Collections.Generic;

namespace JustAskIndia.Helpers
{
    public static class LocationHelper
    {
        private static readonly Dictionary<string, string> LocationMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Bengaluru
            { "bangalore", "Bengaluru" },
            { "bengalore", "Bengaluru" },
            { "blr", "Bengaluru" },

            // Mysore
            { "mysore", "Mysore" },
            { "mysuru", "Mysore" },

            // Kanakapura
            { "kanakpura", "Kanakapura" },

            // Chickballapur
            { "chickballapur", "Chickballapur" },
            { "chikkaballapur", "Chickballapur" },
            { "chikkaballapura", "Chickballapur" },

            // Bellary
            { "bellary", "Bellary" },
            { "ballari", "Bellary" },

            // Shimoga
            { "shimoga", "Shimoga" },
            { "shivamogga", "Shimoga" },

            // Belgaum
            { "belgaum", "Belgaum" },
            { "belagavi", "Belgaum" },

            // Hospet
            { "hospet", "Hospet" },
            { "hosapete", "Hospet" },

            // Raichur
            { "raichur", "Raichur" },
            { "rayachuru", "Raichur" },
            { "raichore", "Raichur" },

            // Maddur
            { "maddur", "Maddur" },
            { "madduru", "Maddur" },

            // Channapatna
            { "channapatna", "Channapatna" },
            { "channapattana", "Channapatna" },
            { "chennapattana", "Channapatna" },

            // Krishnarajanagar
            { "krishnarajanagar", "Krishnarajanagar" },
            { "krishnarajanagara", "Krishnarajanagar" },

            // Chikodi
            { "chikodi", "Chikodi" },
            { "chikkodi", "Chikodi" },

            // Dod Ballapur
            { "dod ballapur", "Dod Ballapur" },
            { "doddaballapura", "Dod Ballapur" },
            { "doddaballapur", "Dod Ballapur" },

            // Kolar
            { "kolar", "Kolar" },

            // Bangarapet
            { "bangarapet", "Bangarapet" },
            { "bangarapete", "Bangarapet" },

            // Vijayapura
            { "vijayapura", "Vijayapura" },
            { "bijapur", "Vijayapura" },

            // Harihar
            { "harihar", "Harihar" },
            { "harihara", "Harihar" },

            // Krishnarajpet
            { "krishnarajpet", "Krishnarajpet" },
            { "krishnarajpete", "Krishnarajpet" },

            // Gadag
            { "gadag", "Gadag" },
            { "gadaga", "Gadag" },

            // Chikmagalur
            { "chikmagalur", "Chikmagalur" },
            { "chikkamagaluru", "Chikmagalur" },
            { "chikmangluru", "Chikmagalur" },

            // Tumkur
            { "tumkur", "Tumkur" },
            { "tumakuru", "Tumkur" },

            // Nanjangud
            { "nanjangud", "Nanjangud" },
            { "nanjanagudu", "Nanjangud" },

            // Dharwad
            { "dharwad", "Dharwad" },
            { "dharwar", "Dharwad" },

            // Gulbarga
            { "gulbarga", "Gulbarga" },
            { "kalaburagi", "Gulbarga" },

            // Koppal
            { "koppal", "Koppal" },
            { "koppala", "Koppal" },

            // Malur
            { "malur", "Malur" },
            { "maluru", "Malur" },

            // Davanagere
            { "davanagere", "Davanagere" },
            { "davangere", "Davanagere" },

            // Jamkhandi
            { "jamkhandi", "Jamkhandi" },
            { "jamakhandi", "Jamkhandi" },

            // Hosakote
            { "hosakote", "Hosakote" },
            { "hoskote", "Hosakote" },

            // Hubli
            { "hubli", "Hubli" },
            { "hubballi", "Hubli" },
            // Gangawati
            { "gangawati", "Gangavathi" },
            { "gangavathi", "Gangavathi" },

            // Honavar
            { "honavar", "Honnavar" },
            { "honnavar", "Honnavar" },

            // Byadagi
            { "byadagi", "Byadgi" },
            { "byadgi", "Byadgi" },

            // Kushalnagar
            { "kushalnagar", "Kushalanagar" },
            { "kushalanagar", "Kushalanagar" },

            // Virarajendrapet
            { "virarajendrapet", "Virajpet" },
            { "virajpet", "Virajpet" },

            // Mangaluru
            { "mangaluru", "Mangalore" },
            { "mangalore", "Mangalore" }

        };

        public static string? NormalizeLocation(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return null;

            var parts = location.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var normalizedParts = new List<string>();

            foreach (var part in parts)
            {
                var trimmed = part.Trim().ToLowerInvariant();
                normalizedParts.Add(LocationMap.TryGetValue(trimmed, out var norm) ? norm : part.Trim());
            }

            return string.Join(", ", normalizedParts);
        }

    }
}
