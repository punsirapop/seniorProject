using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static FitnessMenu;

public class EnemySelectionManager : MonoBehaviour
{
    public static EnemySelectionManager Instance;

    [SerializeField] MechCanvasDisplay[] _MechIcons;
    [SerializeField] Image[] _WeaponIcons;
    [SerializeField] AMechDisplay[] _MechBars;
    [SerializeField] ArenaMechDisplay[] _MechLineUp;
    [SerializeField] TextMeshProUGUI _RewardMoneyText;

    List<MechChromo> _EnemyPool;
    List<WeaponChromosome> _WeaponPool;
    List<List<Tuple<MechChromo, WeaponChromosome>>> _EnemyParties;

    MechChromo[] _EnemyTeam => _MechBars.Select(x => x.MyMechSO).ToArray();
    public MechChromo[] EnemyTeam => _EnemyTeam;
    WeaponChromosome[] _EnemyWeapon => _MechBars.Select(x => x.MyWeaponSO).ToArray();
    public WeaponChromosome[] EnemyWeapon => _EnemyWeapon;

    private void Start()
    {
        if (Instance == null) Instance = this;

        CreateNewEnemies();
    }

    public void CreateNewEnemies()
    {
        // Prepare stat caps
        List<MechChromo> topAllies = new List<MechChromo>();

        foreach (var item in PlayerManager.FarmDatabase)
        {
            topAllies.AddRange(GetStatFitnessDict(item.MechChromos, 0)
                .OrderByDescending(x => x.Value[0]).Select(x => x.Key)
                .Take(Mathf.Min(item.MechChromos.Count, 10)));
        }

        Debug.Log("Top Allies Count: " + topAllies.Count);
        topAllies = GetStatFitnessDict(topAllies, 0).OrderByDescending(x => x.Value[0])
            .Select(x => x.Key).Cast<MechChromo>()
            .Take(Mathf.Min(topAllies.Count, 3)).ToList();
        int cap3 = Mathf.CeilToInt
            (topAllies.Sum(x => x.Atk.Sum() + x.Def.Sum() + x.Hp.Sum() + x.Spd.Sum()) / 3);
        /*
        int cap1 = topAllies.First().Atk.Sum() + topAllies.First().Def.Sum()
            + topAllies.First().Hp.Sum() + topAllies.First().Spd.Sum();
        */

        // Increase a cap for hard team and decrease for easy team
        int win = PlayerManager.BattleRecord.Where(x => x == ArenaManager.WinType.WinHard).Count() +
            Mathf.FloorToInt(PlayerManager.BattleRecord.Where(x => x == ArenaManager.WinType.WinEasy).Count() / 2);
        int hard = cap3 + win;    // More hard
        int easy = cap3 - 4 + win;    // More easy

        Debug.Log($"Hard: {hard} - Easy:{easy}");

        _WeaponPool = new List<WeaponChromosome>();
        foreach (var item in PlayerManager.FactoryDatabase.Where(x => x.LockStatus == LockableStatus.Unlock))
        {
            _WeaponPool.AddRange(item.GetAllWeapon().OrderByDescending(x => x.Fitness).Take(5));
        }
        foreach (var item in _WeaponPool)
        {
            item.SetIsMode1Active(UnityEngine.Random.Range(0, 2) == 0);
        }

        // Generate hard enemies
        _EnemyPool = new List<MechChromo>();
        _EnemyParties = new List<List<Tuple<MechChromo, WeaponChromosome>>>();

        for (int i = 0; i < 10; i++)
        {
            _EnemyPool.Add(new MechChromo(null));
            _EnemyPool.Last().SetRandomStat2(hard);
            PlayerManager.MechIDCounter--;
        }

        List<MechChromo> list = new List<MechChromo>();
        list.Add(GetStatFitnessDict(_EnemyPool, 1).Where(x => !list.Contains(x.Key)).
            OrderByDescending(x => x.Value[0]).ThenByDescending(x => x.Value[1]).First().Key);
        list.Insert(0, GetStatFitnessDict(_EnemyPool, 2).Where(x => !list.Contains(x.Key)).
            OrderByDescending(x => x.Value[0]).ThenByDescending(x => x.Value[1]).First().Key);
        list.Insert(1, GetStatFitnessDict(_EnemyPool, 0).Where(x => !list.Contains(x.Key)).
            OrderByDescending(x => x.Value[0]).First().Key);

        List<Tuple<MechChromo, WeaponChromosome>> a 
            = new List<Tuple<MechChromo, WeaponChromosome>>();
        foreach (var item in list)
        {
            a.Add(Tuple.Create(item, _WeaponPool[UnityEngine.Random.Range(0, _WeaponPool.Count)]));
        }

        _EnemyParties.Add(a);

        // Generate easy enemies
        _EnemyPool = new List<MechChromo>();

        for (int i = 0; i < 10; i++)
        {
            _EnemyPool.Add(new MechChromo(null));
            _EnemyPool.Last().SetRandomStat2(easy);
            PlayerManager.MechIDCounter--;
        }

        list = new List<MechChromo>();
        list.Add(GetStatFitnessDict(_EnemyPool, 1).Where(x => !list.Contains(x.Key)).
            OrderByDescending(x => x.Value[0]).ThenByDescending(x => x.Value[1]).First().Key);
        list.Insert(0, GetStatFitnessDict(_EnemyPool, 2).Where(x => !list.Contains(x.Key)).
            OrderByDescending(x => x.Value[0]).ThenByDescending(x => x.Value[1]).First().Key);
        list.Insert(1, GetStatFitnessDict(_EnemyPool, 0).Where(x => !list.Contains(x.Key)).
            OrderByDescending(x => x.Value[0]).First().Key);

        a = new List<Tuple<MechChromo, WeaponChromosome>>();
        foreach (var item in list)
        {
            a.Add(Tuple.Create(item, _WeaponPool[UnityEngine.Random.Range(0, _WeaponPool.Count)]));
        }

        _EnemyParties.Add(a);

        // Set images
        foreach (var item in _MechIcons.Zip(_EnemyParties.SelectMany(x => x), (a, b) => Tuple.Create(a, b)))
        {
            item.Item1.SetChromo(item.Item2.Item1);
        }
        foreach (var item in _WeaponIcons.Zip(_EnemyParties.SelectMany(x => x), (a, b) => Tuple.Create(a, b)))
        {
            item.Item1.sprite = item.Item2.Item2.Image;
        }

    }

    /*
     * 0 - hard
     * 1 - easy
     */
    public void SetLineUp(int m)
    {
        // Set team
        for (int i = 0; i < 3; i++)
        {
            _MechLineUp[i].SetChromo(_EnemyParties[m][i].Item1);
            _MechLineUp[i].SetWeapon(_EnemyParties[m][i].Item2);
            _MechBars[i].SetChromo(_EnemyParties[m][i].Item1);
            _MechBars[i].SetWeapon(_EnemyParties[m][i].Item2);
        }
        ArenaManager.EnemyLevel = m == 0 ? 2 : 1;
        // Set reward text
        _RewardMoneyText.text = "+" + (ArenaManager.EnemyLevel * ArenaManager.Instance.RewardMoneyPerLevel).ToString();
    }

    public void CloseLineUp()
    {
        for (int i = 0; i < 3; i++)
        {
            _MechLineUp[i].SetChromo(_MechLineUp[i].MySO);
            _MechLineUp[i].SetWeapon(_MechLineUp[i].MyWeaponSO);
            _MechBars[i].SetWeapon(_MechBars[i].MyWeaponSO);
            _MechBars[i].SetChromo(_MechBars[i].MyMechSO);
        }
        ArenaManager.EnemyLevel = 0;
    }

    /*
     * 0 - default
     * 1 - offensive
     * 2 - defensive
     */
    public static Dictionary<dynamic, List<float>> GetStatFitnessDict(List<MechChromo> m, int mode)
    {
        List<Tuple<Properties, int>> fv = new List<Tuple<Properties, int>>();
        var dict = new Dictionary<dynamic, List<float>>();

        for (int i = 0; i < 4; i++) fv.Add(Tuple.Create(Properties.Com, i));

        foreach (MechChromo c in m)
        {
            List<float> list = new List<float>();
            list.Add(c.GetFitness(fv));
            switch (mode)
            {
                case 1:
                    list.Add(c.GetFitness(new List<Tuple<Properties, int>>()
                        { Tuple.Create(Properties.Com, 0) }) +
                        c.GetFitness(new List<Tuple<Properties, int>>()
                        { Tuple.Create(Properties.Com, 3) }));
                    break;
                case 2:
                    list.Add(c.GetFitness(new List<Tuple<Properties, int>>()
                        { Tuple.Create(Properties.Com, 1) }) +
                        c.GetFitness(new List<Tuple<Properties, int>>()
                        { Tuple.Create(Properties.Com, 2) }));
                    break;
                default:
                    break;
            }

            dict.Add(c, list);
        }

        return dict;
    }

    public void BackToSelection()
    {
        foreach (var item in _MechLineUp)
        {
            item.SetChromo(item.MySO);
        }
        CreateNewEnemies();
    }
}
