using System;
using System.Collections.Generic;
using System.Linq;

public class Fuzzy
{
	public static List<string> Search(
	    string word,
	    List<string> wordList,
	    double fuzzyness = 0.6)
	{
	    List<string> foundWords =
	        (
	            from s in wordList
	            let levenshteinDistance = LevenshteinDistance(word, s)
	            let length = Math.Max(s.Length, word.Length)
	            let score = 1.0 - (double)levenshteinDistance / length
	            where score > fuzzyness
	            select s
	        ).ToList();
	
	    return foundWords;
	}
	
	public static int LevenshteinDistance(string src, string dest)
	{
	    int[,] d = new int[src.Length + 1, dest.Length + 1];
	    int i, j, cost;
	    char[] str1 = src.ToCharArray();
	    char[] str2 = dest.ToCharArray();
	
	    for (i = 0; i <= str1.Length; i++)
	    {
	        d[i, 0] = i;
	    }
	    for (j = 0; j <= str2.Length; j++)
	    {
	        d[0, j] = j;
	    }
	    for (i = 1; i <= str1.Length; i++)
	    {
	        for (j = 1; j <= str2.Length; j++)
	        {
	
	            if (str1[i - 1] == str2[j - 1])
	                cost = 0;
	            else
	                cost = 1;
	
	            d[i, j] =
	                Math.Min(
	                    d[i - 1, j] + 1,              // Deletion
	                    Math.Min(
	                        d[i, j - 1] + 1,          // Insertion
	                        d[i - 1, j - 1] + cost)); // Substitution
	
	            if ((i > 1) && (j > 1) && (str1[i - 1] == 
	                str2[j - 2]) && (str1[i - 2] == str2[j - 1]))
	            {
	                d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
	            }
	        }
	    }
	
	    return d[str1.Length, str2.Length];
	}
}