using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CandidateManager : MonoBehaviour
{
    public static CandidateManager Instance;

    // Enumeration specify how the parent chromosomes are selected
    public enum Operation { 
        // Batch operation that apply on the number of chromosomes
        Group,                  // Clicking on the Group button
        Chance,                 // Clicking on the Chance button
        Rank,                   // Clicking on the Rank button
        Wheel,                  // Clicking on the Wheel button
        ReverseFitness,         // Clicking on the ~Fitness button
        // Single operation that apply on the single individual chromosome; This generate the parent in the selected parent panel
        PickBestInGroup,        // Clicking on the best fitness chromosome in the group (preferred operation of Tournament-based selection)
        PickNotBestInGroup,     // Clicking on the chromosome which is not the best fitness (not preferred)
        PickInChance,           // Clicking on the chromosome with calculated selected chance (not preferred)
        PickInRank,             // Clicking on the chromosome with calculated rank (not preferred)
        SpinWheel,              // Clicking on the created wheel to random chromosome from the wheel (preferred for RW and RB selection)
    }
    // Preferred operation log for selecting 6 parents
    // TB: {Group, PickBestInGroup, Group, PickBestInGroup, Group, PickBestInGroup, Group, PickBestInGroup, Group, PickBestInGroup, Group, PickBestInGroup}
    // RW: {Chance, Wheel, SpinWheel, SpinWheel, SpinWheel, SpinWheel, SpinWheel, SpinWheel}
    // RB: {Rank, ReverseFitness, Chance, Wheel, SpinWheel, SpinWheel, SpinWheel, SpinWheel, SpinWheel, SpinWheel}
    [SerializeField] private List<Operation> _OperationLog;
    public List<Operation> OperationLog => _OperationLog;
    [SerializeField] private Transform _ChromosomeHolder;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Destroy all object in this panel (if any)
        foreach (Transform child in _ChromosomeHolder)
        {
            Destroy(child.gameObject);
        }
    }

    // Copy the number of chromosome from the population equal to the given candidateCount
    public void CreateGroup(int candidateCount)
    {
        // Destroy all object in this panel (if any)
        foreach (Transform child in _ChromosomeHolder)
        {
            Destroy(child.gameObject);
        }
        // If the given number of candidate exceed the number of population, do nothing
        GameObject[] populationPool = PopulationManager.Instance.Population;
        if (candidateCount > populationPool.Length)
        {
            return;
        }
        // If the given number of candidate not exceed the number of population
        // random picking chromosome from the population pool
        else
        {
            // Randompicking chromosome from pool
            for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
            {
                int randomIndex = Random.Range(0, populationPool.Length);
                // Instantiate the actual GameObject of the candidate
                GameObject newIidividual = Instantiate(populationPool[randomIndex], _ChromosomeHolder);
                newIidividual.AddComponent<Button>();
                ChromosomeRod newIndividualRod = newIidividual.GetComponentInChildren<ChromosomeRod>();
                newIidividual.GetComponent<Button>().onClick.AddListener(() => SelectedParentManager.Instance.AddSelectedChromosome(newIndividualRod));
                // Remove selected index from the pool
                GameObject[] newPool = new GameObject[populationPool.Length - 1];
                for (int i = 0; i < newPool.Length; i++)
                {
                    newPool[i] = (i >= randomIndex) ? populationPool[i + 1] : populationPool[i];
                }
                populationPool = newPool;
            }
        }
        // Record the operation
        _OperationLog.Add(Operation.Group);
    }
}
