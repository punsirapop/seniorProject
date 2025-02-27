using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/*
 * Store functions related to genetic algorithm
 * - SelectParent *Under development*
 * - Crossover
 * - Mutate
 */
public class GeneticFunc : MonoBehaviour
{
    public static GeneticFunc Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // -------------- General --------------

    /*
     * Create list of parents
     * 
     * Input
     *      fv: population fitness val
     *      eliteCount: amount of elites
     *      mode: selection mode
     *          0 - Random
     *          1 - Tournament-based
     *          2 - Roulette Wheel
     *          3 - Rank-based
     *      
     * Output
     *      list of parents' indexes
     */
    public List<dynamic> SelectParent(Dictionary<dynamic, float> fv, int eliteCount, int mode, int k)
    {
        List<dynamic> result = new List<dynamic>();
        while (result.Count < fv.Count - eliteCount - (fv.Count - eliteCount) % 2)
        {
            switch (mode)
            {
                // random
                case 0:
                    int r1 = Random.Range(0, fv.Count);
                    result.Add(fv.ElementAt(r1).Key);
                    break;
                // tournament-based
                case 1:
                    Dictionary<dynamic, float> tmp1 = new Dictionary<dynamic, float>();
                    for (int i = 0; i < k; i++)
                    {
                        int r2 = 0;
                        do r2 = Random.Range(0, fv.Count);
                        while (tmp1.ContainsKey(fv.ElementAt(r2).Key));
                        tmp1.Add(fv.ElementAt(r2).Key, fv.ElementAt(r2).Value);
                    }
                    result.Add(tmp1.OrderBy(x => x.Value).First().Key);
                    break;
                // roulette
                case 2:
                    Dictionary<float, List<dynamic>> fvNew = fv.GroupBy(kv => kv.Value)
                        .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());
                    List<float> f = new List<float>();
                    foreach (var item in fvNew)
                    {
                        f.Add(item.Key * item.Value.Count);
                    }
                    float r3 = Random.Range(0, f.Sum());
                    int index = 0;
                    float u = f[index];
                    while (u < r3)
                    {
                        index++;
                        u += f[index];
                    }
                    int r4 = Random.Range(0, fvNew.ElementAt(index).Value.Count);
                    result.Add(fvNew.ElementAt(index).Value[r4]);
                    /*
                    float r3 = Random.Range(0, fv.Values.Sum());
                    int index = 0;
                    float u = fv.First().Value;
                    while (u < r3)
                    {
                        index++;
                        u += fv.ElementAt(index).Value;
                    }
                    result.Add(fv.ElementAt(index).Key);
                    */
                    break;
                // rank-based
                case 3:
                    Dictionary<dynamic, float> tmp2 = new Dictionary<dynamic, float>
                        (fv.OrderBy(x => x.Value));
                    for (int i = 0; i < tmp2.Count; i++)
                    {
                        tmp2[tmp2.ElementAt(i).Key] = i;
                    }
                    float r5 = Random.Range(0, tmp2.Values.Sum());
                    int index2 = 0;
                    float u2 = 0;
                    while (u2 < r5)
                    {
                        index2++;
                        u2 += tmp2.ElementAt(index2).Value;
                    }
                    result.Add(tmp2.ElementAt(index2).Key);
                    break;
            }
        }
        return result;
    }

    /* 
     * Crossover 2 lists
     * 
     * Input
     *      a & b: lists to be crossed over
     *      type: crossover type
     *          0 - one-point
     *          1 - two-point
     *          2 - uniform
     */
    public void Crossover(List<List<int>> a, List<List<int>> b, int type)
    {
        List<List<int>> temp = new List<List<int>>(b);
        for (int i = 0; i < b.Count; i++)
        {
            temp[i] = new List<int>(b[i]);
        }

        if (type < 2)
        {   // not uniform
            // set start & end points
            int start = Random.Range(1, a[0].Count - type);
            int end = (type == 1) ? Random.Range(start, a[0].Count - 1) : a[0].Count - 1;
            // swap intervals
            for (int i = start; i <= end; i++)
            {
                for (int j = 0; j < a.Count; j++)
                {
                    //Debug.Log("BEFORE: a bit = " + a[j][i].ToString() + ", b bit = " + b[j][i]);
                    b[j][i] = a[j][i];
                    a[j][i] = temp[j][i];
                    //Debug.Log("AFTER: a bit = " + a[j][i].ToString() + ", b bit = " + b[j][i]);
                }
            }
        }
        else
        {   // uniform
            for (int i = 0; i < a.Count; i++)
            {
                if (Random.Range(0, 100) >= 50)
                {
                    for (int j = 0; j < a.Count; j++)
                    {
                        b[j][i] = a[j][i];
                        a[j][i] = temp[j][i];
                    }
                }
            }
        }
    }

    /* 
     * Randomly mutate genes
     * 
     * Input
     *      c: encoded chromosome to be mutated
     *      statCap: list of maximum number for each gene
     */
    public void Mutate(List<List<int>> c, List<int> statCap)
    {
        // Debug.Log("IM MUTATING");

        for (int i = 0; i < c.Count; i++)
        {
            if (Random.Range(0, 100) < 100 / c.Count)
            {
                int r = Random.Range(0, statCap[i]);
                for (int j = 0; j < c.Count; j++)
                {
                    c[j][i] = r;
                }
                // Debug.Log("I MUTATED AT " + i);
            }
        }
    }

    // -------------- Under Construction --------------
}
