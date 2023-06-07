using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    public class StringAnalyzer
    {
        public static double JaroWinklerDistance(string s1, string s2)
        {
            int m = 0;
            int n1 = s1.Length;
            int n2 = s2.Length;

            if (n1 == 0 || n2 == 0) return 0.0;

            int matchDistance = Math.Max(n1, n2) / 2 - 1;
            bool[] matched1 = new bool[n1];
            bool[] matched2 = new bool[n2];

            for (int i = 0; i < n1; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, n2);

                for (int j = start; j < end; j++)
                {
                    if (matched2[j]) continue;
                    if (s1[i] != s2[j]) continue;

                    matched1[i] = true;
                    matched2[j] = true;
                    m++;
                    break;
                }
            }

            if (m == 0) return 0.0;

            int k = 0;
            int numTranspositions = 0;

            for (int i = 0; i < n1; i++)
            {
                if (!matched1[i]) continue;

                while (!matched2[k]) k++;

                if (s1[i] != s2[k]) numTranspositions++;

                k++;
            }

            double jaro = ((double)m / n1 + (double)m / n2 + (double)(m - numTranspositions / 2) / m) / 3;
            int numCommonChars = 0;

            for (int i = 0; i < Math.Min(4, Math.Min(n1, n2)); i++)
            {
                if (s1[i] == s2[i]) numCommonChars++;
            }

            return jaro + 0.1 * numCommonChars * (1 - jaro);
        }

        public static bool AreStringsRepeating(List<string> strings, double threshold)
        {
            for (int i = 0; i < strings.Count - 1; i++)
            {
                for (int j = i + 1; j < strings.Count; j++)
                {
                    double similarity = JaroWinklerDistance(strings[i], strings[j]);
                    if (similarity < threshold) return false;
                }
            }

            return true;
        }
    }
}
