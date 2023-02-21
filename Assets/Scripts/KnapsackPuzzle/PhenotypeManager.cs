using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhenotypeManager : MonoBehaviour
{
    public static PhenotypeManager Instance;

    // Reference to the holder UI and prefab
    [SerializeField] private Transform _Mask;
    [SerializeField] private Transform _ItemHolder;
    [SerializeField] private Transform _KnapsackHolder;
    [SerializeField] private GameObject _ItemPrefab;
    [SerializeField] private GameObject _KnapsackPrefab;
    // Reference to actual Item and Knapsack
    private FactorySO[] _FactoriesData;
    private int _ItemPresetIndex;
    private int _KnapsackPresetIndex;
    private Item[] _Items;
    private Knapsack[] _Knapsacks;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        _ItemPresetIndex = 0;
        _KnapsackPresetIndex = 0;
        _FactoriesData = KnapsackPuzzleManager.Instance.FactoriesData;
        ResetObject();
    }

    public void ResetObject()
    {
        _InstantiateKnapsacks();
        _InstantiateItems();
        EnableObjectSetting();
    }

    // Make the objects can be assigned to the BitBlock when it's clicked by adding listener
    public void EnableObjectSetting()
    {
        // Get reference to all available item in the panel
        _Items = GetComponentsInChildren<Item>();
        foreach (Item item in _Items)
        {
            // Add this item to enabled BitBlock(s) when click on this item
            Button button = item.GetComponent<Button>();
            button.onClick.AddListener(() => GenotypeManager.Instance.SetItemOnBits(item.Name));
        }
        // Get reference to all available knapsack in the panel
        _Knapsacks = GetComponentsInChildren<Knapsack>();
        foreach (Knapsack knapsack in _Knapsacks)
        {
            // Add this knapsack to enabled BitBlock(s) when click on this knapsack
            Button button = knapsack.GetComponent<Button>();
            button.onClick.AddListener(() => GenotypeManager.Instance.SetKnapsackOnBits(knapsack.Name));
        }
    }

    #region Knapsack Instantiation ################################################################
    // Change the Knapsack preset
    public void ChangeKnapsackPreset(int amount)
    {
        _KnapsackPresetIndex += amount;
        if (_KnapsackPresetIndex > _FactoriesData.Length - 1)
        {
            _KnapsackPresetIndex = 0;
        }
        else if (_KnapsackPresetIndex < 0)
        {
            _KnapsackPresetIndex = _FactoriesData.Length - 1;
        }
        ResetObject();
    }

    // Instantiate Knapsack according to the preset index
    private void _InstantiateKnapsacks()
    {
        // Destroy all previous object in the holder
        foreach (Transform child in _KnapsackHolder)
        {
            Destroy(child.gameObject);
        }
        // Set the preset as same as the configuration in the factory
        KnapsackSO[] knapsackPreset = _FactoriesData[_KnapsackPresetIndex].Knapsacks;
        // Create actual Knapsack object in the game
        foreach(KnapsackSO knapsack in knapsackPreset)
            {
            GameObject newKnapsack = Instantiate(_KnapsackPrefab);
            newKnapsack.transform.SetParent(_KnapsackHolder);
            newKnapsack.GetComponent<Knapsack>().SetKnapsack(knapsack);
        }
        // Keep reference to the Knapsack
        _Knapsacks = GetComponentsInChildren<Knapsack>();
    }
    #endregion

    #region Item Instantiation ####################################################################
    // Change the Item preset
    public void ChangeItemsPreset(int amount)
    {
        _ItemPresetIndex += amount;
        if (_ItemPresetIndex > _FactoriesData.Length - 1)
        {
            _ItemPresetIndex = 0;
        }
        else if (_ItemPresetIndex < 0)
        {
            _ItemPresetIndex = _FactoriesData.Length - 1;
        }
        ResetObject();
    }

    // Instantiate Item according to the preset
    private void _InstantiateItems()
    {
        // Destroy all previous object in the holder
        foreach (Transform child in _ItemHolder)
        {
            Destroy(child.gameObject);
        }
        // Set the preset as same as the configuration in the factory
        ItemSO[] itemPreset = _FactoriesData[_ItemPresetIndex].Items;
        // Create actual Item object in the game
        foreach (ItemSO item in itemPreset)
        {
            GameObject newItem = Instantiate(_ItemPrefab);
            newItem.transform.SetParent(_ItemHolder);
            newItem.GetComponent<Item>().SetMask(_Mask);
            newItem.GetComponent<Item>().SetItem(item);
        }
        // Keep reference to the Item
        _Items = GetComponentsInChildren<Item>();
    }
    #endregion
}
