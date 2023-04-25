using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FactoryMain : MonoBehaviour
{
    // Game object of sprite group of each state
    [SerializeField] private GameObject _Locked;
    [SerializeField] private GameObject _Unlockable;
    [SerializeField] private GameObject _Unlocked;

    // Sprite of this factory in main game page
    [SerializeField] private Sprite _MainLocked;
    private Sprite _MainNormal;
    private Sprite _MainBroken;
    private Sprite _Locker;

    // Actual UI element object
    [SerializeField] private GameObject _MainBackground;
    [SerializeField] private GameObject _MainLightBoard;
    [SerializeField] private GameObject _FixButton;
    [SerializeField] private GameObject _UnlockableLockerUI;
    [SerializeField] private GameObject _LockedLockerUI;

    // Factory information, this should be managed by PlayerManager and/or FactorySO later
    private int _FactoryIndex;

    void Start()
    {
        _MainBackground.GetComponent<Image>().sprite = _MainNormal;
        _UnlockableLockerUI.GetComponent<Image>().sprite = _Locker;
        _LockedLockerUI.GetComponent<Image>().sprite = _Locker;
        // _MainLightBoard.GetComponent<Button>().onClick.AddListener(() => EnterKnapsackPuzzle());
    }

    void Update()
    {
        int lights = 0;
        bool fixable = false;
        foreach (var item in _MainLightBoard.GetComponentsInChildren<Image>())
        {
            if (lights >= PlayerManager.FactoryDatabase[_FactoryIndex].Condition)
            {
                fixable = true;
            }
            item.color = (lights < PlayerManager.FactoryDatabase[_FactoryIndex].Condition) ? Color.green : Color.red;
            lights++;
        }
        _FixButton.SetActive(fixable);
    }

    public void SetFactory(FactorySO newFactorySO)
    {
        _FactoryIndex = newFactorySO.FactoryIndex;
        _MainNormal = newFactorySO.MainNormal;
        _MainBroken = newFactorySO.MainBroken;
        _Locker = newFactorySO.Locker;
        RenderSprites();
    }

    public void RenderSprites()
    {
        _Locked.SetActive(PlayerManager.FactoryDatabase[_FactoryIndex].LockStatus == LockableStatus.Lock);
        _Unlockable.SetActive(PlayerManager.FactoryDatabase[_FactoryIndex].LockStatus == LockableStatus.Unlockable);
        _Unlocked.SetActive(PlayerManager.FactoryDatabase[_FactoryIndex].LockStatus == LockableStatus.Unlock);
    }

    public void UnlockFactory()
    {
        PlayerManager.FactoryDatabase[_FactoryIndex].SetLockStatus(LockableStatus.Unlock);
        GetComponent<Animator>().Play("UnlockFactory");
    }

    // Wrap function for validate unlocking condition, triggered at the end of UnlockFactory animation
    public void ValidateUnlocking()
    {
        PlayerManager.Instance.ValidateUnlocking();
    }

    public void EnterFactory()
    {
        PlayerManager.CurrentFactoryIndex = _FactoryIndex;
        this.GetComponent<SceneMng>().ChangeScene("Factory");
    }

    public void OnFixButtonClick()
    {
        PlayerManager.Instance.SetFacilityToFix(PlayerManager.FacilityType.Factory, _FactoryIndex);
        MainPageManager.Instance.DisplayFixChoice();
    }
}
