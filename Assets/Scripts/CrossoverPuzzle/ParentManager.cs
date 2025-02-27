using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ParentManager : MonoBehaviour
{
    public static ParentManager Instance;

    [SerializeField] private GameObject _ChromosomeRodTogglePrefab;
    [SerializeField] private GameObject _ChromosomeRodPrefab;
    [SerializeField] private Transform _ChromosomeRodsHolder;
    [SerializeField] private Color32[] _Colors;
    [SerializeField] private GameObject  _DemonstrateText;
    [SerializeField] private GameObject _WantedChildPanel;
    private int[] _WantedChild;

    private ChromosomeRodToggle[] _ChromosomeRodToggles;
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        //InstaniateChromosomeRodToggles(PuzzleType.CrossoverOnePointDemon);
    }

    void Update()
    {
        // Make sure that there are at most 2 buttons selected
        _ChromosomeRodToggles = _ChromosomeRodsHolder.GetComponentsInChildren<ChromosomeRodToggle>();
        // Count the number of selected button
        int selectCount = 0;
        foreach (ChromosomeRodToggle rodToggle in _ChromosomeRodToggles)
        {
            selectCount += rodToggle.isOn ? 1 : 0;
            rodToggle.SetInteractable(true);
        }
        // Disable other if there are already 2 buttons selected
        if (selectCount < 2)
        {
            return;
        }
        selectCount = 0;
        foreach (ChromosomeRodToggle rodToggle in _ChromosomeRodToggles)
        {
            if (!rodToggle.isOn)
            {
                rodToggle.SetInteractable(false);
            }
            else
            {
                if (selectCount >= 2)
                {
                    rodToggle.SetIsOn(false);
                    rodToggle.SetInteractable(false);
                }
                selectCount++;
            }
        }
    }

    #region ChromosomeRodToggles Instantiation
    // Create ChromosomeRodToggle correspond to the given puzzleType
    public void InstaniateChromosomeRodToggles(PuzzleType puzzleType)
    {
        // Destroy all previous object in the holder
        foreach (Transform child in _ChromosomeRodsHolder)
        {
            Destroy(child.gameObject);
        }
        switch (puzzleType)
        {
            default:
                _InstantiateRandomChromo();
                break;
            case PuzzleType.CrossoverOnePointDemon:
                _InstantiateRandomChromo(0);
                break;
            case PuzzleType.CrossoverTwoPointsDemon:
                _InstantiateRandomChromo(1);
                break;
            case PuzzleType.CrossoverOnePointSolve:
                _InstantiateMechChromo(0);
                break;
            case PuzzleType.CrossoverTwoPointsSolve:
                _InstantiateMechChromo(1);
                break;
        }
    }

    // Instantiate random chromosome using in the demonstration puzzle
    // and set description according to the crossoverType
    // crossoverType: 0 = singple-point, 1 = two-point
    private void _InstantiateRandomChromo(int crossovertype=0)
    {
        // Create pool of 3 binary chromosome (1), 3 integer chromosome (2)
        int[] chromosomePool = { 1, 1, 1, 2, 2, 2 };
        for (int i = 0; i < 4; i++)
        {
            // Random chromosome type from the pool
            int randomIndex = Random.Range(0, chromosomePool.Length);
            int chromosomeType = chromosomePool[randomIndex];
            // Remove such chromosome from the pool
            int[] newChromosomePool = new int[chromosomePool.Length - 1];
            for (int j = 0; j < chromosomePool.Length - 1; j++)
            {
                newChromosomePool[j] = (j >= randomIndex) ? chromosomePool[j + 1] : chromosomePool[j];
            }
            chromosomePool = newChromosomePool;
            // Generate content in chromosome
            int[] content = new int[5];
            Color32[] colors = new Color32[5];
            for (int j = 0; j < content.Length; j++)
            {
                // Using content from 0 to 1 for binary, content from 2 to 9 for integer
                content[j] = (chromosomeType == 1) ? Random.Range(0, 2) : Random.Range(2, 10);
                colors[j] = _Colors[i];
            }
            // Create the ChromosomeRodToggle
            GameObject newChromosomeRodToggle = Instantiate(_ChromosomeRodTogglePrefab, _ChromosomeRodsHolder);
            newChromosomeRodToggle.GetComponentInChildren<ChromosomeRod>().SetChromosome(content, colors);
            newChromosomeRodToggle.GetComponentInChildren<ChromosomeRod>().RenderRod();
        }
        // Set the explanation text
        _DemonstrateText.SetActive(true);
        _WantedChildPanel.SetActive(false);
        _DemonstrateText.GetComponentsInChildren<TextMeshProUGUI>()[1].text = (crossovertype == 0) ? "Single-point" : "Two-point";
    }

    // Instantiate possible parent chomosomes of mech according to crossoverType
    // crossoverType: 0 = single-point, 1 = two-point
    private void _InstantiateMechChromo(int crossoverType=0)
    {
        // Create wanted child
        _WantedChild = new MechChromo(null).GetChromosome()[0].ToArray()[..5];
        int[][] parents = new int[4][];
        for (int parentCount = 0; parentCount < parents.Length; parentCount += 2)
        {
            // Create new random child with difference head and accessory
            int[] randomChild = new MechChromo(null).GetChromosome()[0].ToArray()[..5];
            if (_WantedChild[0] == randomChild[0])
            {
                randomChild[0] += (randomChild[0] == 0) ? 1 : -1;
            }
            if (_WantedChild[4] == randomChild[4])
            {
                randomChild[4] += (randomChild[4] == 0) ? 1 : -1;
            }
            // Create possible parent using crossover
            List<List<int>> parent1 = new();
            List<List<int>> parent2 = new();
            parent1.Add(new List<int>(_WantedChild));
            parent2.Add(new List<int>(randomChild));
            GeneticFunc.Instance.Crossover(parent1, parent2, crossoverType);
            parents[0 + parentCount] = parent1[0].ToArray();
            parents[1 + parentCount] = parent2[0].ToArray();
        }
        // Shuffle parent's order
        for (int parentIndex = 0; parentIndex < parents.Length; parentIndex++)
        {
            // Swap this parent with another random parent
            int randomIndex = Random.Range(0, parents.Length);
            int[] thisParent = parents[parentIndex];
            parents[parentIndex] = parents[randomIndex];
            parents[randomIndex] = thisParent;
        }
        // Assign base color to all white
        Color32[] baseColor = new Color32[_WantedChild.Length];
        for (int i = 0; i < baseColor.Length; i++)
        {
            baseColor[i] = Color.white;
        }
        // Create the ChromosomeRodToggle of parent
        foreach (int[] parent in parents)
        {
            GameObject newChromosomeRodToggle = Instantiate(_ChromosomeRodTogglePrefab, _ChromosomeRodsHolder);
            newChromosomeRodToggle.GetComponentInChildren<ChromosomeRod>().SetChromosome(parent, baseColor, true);
            newChromosomeRodToggle.GetComponentInChildren<ChromosomeRod>().RenderRod();
        }
        // Destroy all previous children in the panel (if any)
        ChromosomeRod[] oldChildren = _WantedChildPanel.GetComponentsInChildren<ChromosomeRod>();
        foreach (ChromosomeRod child in oldChildren)
        {
            Destroy(child.gameObject);
        }
        // Show one of the wanted children
        _DemonstrateText.SetActive(false);
        _WantedChildPanel.SetActive(true);
        GameObject childChromosomeRodToggle = Instantiate(_ChromosomeRodPrefab, _WantedChildPanel.GetComponentInChildren<HorizontalLayoutGroup>().transform);
        childChromosomeRodToggle.GetComponentInChildren<ChromosomeRod>().SetChromosome(_WantedChild, baseColor, true);
        childChromosomeRodToggle.GetComponentInChildren<ChromosomeRod>().RenderRod();
    }
    #endregion

    // Return all ChromosomeRods that is selected
    public ChromosomeRod[] GetSelectedChromosomeRods()
    {
        // Count the number of selected chromosomeRodToggle
        _ChromosomeRodToggles = _ChromosomeRodsHolder.GetComponentsInChildren<ChromosomeRodToggle>();
        int selectCount = 0;
        foreach (ChromosomeRodToggle rodToggle in _ChromosomeRodToggles)
        {
            if (rodToggle.isOn)
            {
                selectCount++;
            }
        }
        // Create the array of selected chromosomeRod
        ChromosomeRod[] selectedRods = new ChromosomeRod[selectCount];
        selectCount = 0;
        foreach (ChromosomeRodToggle rodToggle in _ChromosomeRodToggles)
        {
            if (rodToggle.isOn)
            {
                selectedRods[selectCount] = rodToggle.GetComponentInChildren<ChromosomeRod>();
                selectCount++;
            }
        }
        return selectedRods;
    }

    // Unselect all ChromosomeRodToggles
    public void UnselectAllToggles()
    {
        _ChromosomeRodToggles = _ChromosomeRodsHolder.GetComponentsInChildren<ChromosomeRodToggle>();
        foreach (ChromosomeRodToggle rodToggle in _ChromosomeRodToggles)
        {
            rodToggle.SetIsOn(false);
        }
    }

    // Return the chromosome of wanted children
    public int[] GetWantedChild()
    {
        return _WantedChild;
    }

    // Return the type of selected chromosome rods
    // return type: 0 = binary, 1 = integer
    public int[] GetSelectedRodsType()
    {
        ChromosomeRod[] selectedRods = GetSelectedChromosomeRods();
        int[] selectedTypes = new int[selectedRods.Length];
        for (int i = 0; i < selectedRods.Length; i++)
        {
            selectedTypes[i] = (selectedRods[i].GetValueAtIndex(0) <= 1) ? 0 : 1;
        }
        return selectedTypes;
    }
}
