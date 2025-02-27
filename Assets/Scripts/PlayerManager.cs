using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TimeManager;

public class PlayerManager : MonoBehaviour, ISerializationCallbackReceiver
{
    public static PlayerManager Instance;

    public static string Name;

    public static int CurrentDialogueIndex = 0;

    public static int MechIDCounter;
    public static int MechCap;

    public static Date CurrentDate;

    public static int ResearchLabTabIndex { get; private set; }

    // Resources
    public static int Money { get; private set; }

    // Arena
    public static List<ArenaManager.WinType> BattleRecord;

    // ChapterSO for validation purpose
    public static ContentChapterSO[] ContentChapterDatabase;
    [SerializeField] private ContentChapterSO[] ContentChapterDatabaseHelper;

    // Facilities
    public static int CurrentFactoryIndex = 0;
    public static int CurrentFarmIndex = 1;
    public static FactorySO[] FactoryDatabase;
    public static FarmSO[] FarmDatabase;
    public static DialogueSO[] DialogueDatabase;
    public static FactorySO CurrentFactoryDatabase => FactoryDatabase[CurrentFactoryIndex];
    public static FarmSO CurrentFarmDatabase => FarmDatabase[CurrentFarmIndex];
    //public static DialogueSO CurrentDialogueDatabase => DialogueDatabase[CurrentDialogueIndex];//New*************************************
    public static DialogueSO CurrentDialogueDatabase;
    [SerializeField] private FactorySO[] FactoryDatabaseHelper;
    [SerializeField] private FarmSO[] FarmDatabaseHelper;
    [SerializeField] private DialogueSO[] DialogueDatabaseHelper;

    // Facility fixing
    public static bool FixingFacility = false;
    public static FacilityType FacilityToFix;
    public static int FacilityToFixIndex;
    // Special case of fixing the last factory,
    // which is the only puzzle that involve more than one JigsawPieceSO
    public static bool IsFixingLastFactory => (FixingFacility && (FacilityToFixIndex == 3));
    public static JigsawPieceSO[] JigsawPieceForLastFactory;
    [SerializeField] private JigsawPieceSO[] JigsawPieceForLastFactoryHelper;

    // Puzzle/JigsawPiece for HallOfFame
    public static JigsawPieceSO[] JigsawPieceDatabase;
    [SerializeField] private JigsawPieceSO[] JigsawPieceDatabaseHelper;
    public static JigsawPieceSO CurrentJigsawPiece;
    public static PuzzleType PuzzleToGenerate => CurrentJigsawPiece.HowToObtain;

    // Information (tutorial), for the purpose of resetting
    public static InformationSO[] InformationDatabase;
    [SerializeField] private InformationSO[] InformationDatabaseHelper;

    // Quest
    public static MainQuestDatabaseSO MainQuestDatabase;
    [SerializeField] private MainQuestDatabaseSO _MainQuestDatabaseHelper;
    public static SideQuestDatabaseSO SideQuestDatabase;
    [SerializeField] private SideQuestDatabaseSO _SideQuestDatabaseHelper;

    // Shop
    public static ShopSO Shop;
    [SerializeField] private ShopSO _ShopHelper;
    // Capybara
    public static CapybaraDatabaseSO CapybaraDatabase;
    [SerializeField] private CapybaraDatabaseSO _CapybaraDatabaseHelper;

    public enum FacilityType
    {
        Factory,
        Farm
    }

    // Assign factories data from serialized field on editor to the static variable
    public void OnAfterDeserialize()
    {
        ContentChapterDatabase = ContentChapterDatabaseHelper;
        FactoryDatabase = FactoryDatabaseHelper;
        FarmDatabase = FarmDatabaseHelper;
        JigsawPieceDatabase = JigsawPieceDatabaseHelper;
        JigsawPieceForLastFactory = JigsawPieceForLastFactoryHelper;
        DialogueDatabase = DialogueDatabaseHelper;
        MainQuestDatabase = _MainQuestDatabaseHelper;
        SideQuestDatabase = _SideQuestDatabaseHelper;
        InformationDatabase = InformationDatabaseHelper;
        Shop = _ShopHelper;
        CapybaraDatabase = _CapybaraDatabaseHelper;
    }

    // Reflect the value back into editor
    public void OnBeforeSerialize()
    {
        ContentChapterDatabaseHelper = ContentChapterDatabase;
        FactoryDatabaseHelper = FactoryDatabase;
        FarmDatabaseHelper = FarmDatabase;
        JigsawPieceDatabaseHelper = JigsawPieceDatabase;
        JigsawPieceForLastFactoryHelper = JigsawPieceForLastFactory;
        DialogueDatabaseHelper = DialogueDatabase;
        _MainQuestDatabaseHelper = MainQuestDatabase;
        _SideQuestDatabaseHelper = SideQuestDatabase;
        InformationDatabaseHelper = InformationDatabase;
        _CapybaraDatabaseHelper = CapybaraDatabase;
    }

    private void Awake()
    {
        TimeManager.OnChangeDate += OnChangeDate;
        SaveManager.OnReset += ResetMoney;
        SaveManager.OnReset += ValidateUnlocking;
        SaveManager.OnReset += ResetDate;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        FixingFacility = false;
        if (MechCap == 0) MechCap = 4;

        // Clear Arena Record
        BattleRecord = new List<ArenaManager.WinType>();
    }

    private void OnDestroy()
    {
        TimeManager.OnChangeDate -= OnChangeDate;
    }

    public static void OnChangeDate(Date d)
    {
        int day = d.CompareDate(CurrentDate);
        // Debug.Log("ASK TO BREED FOR " + day + " FROM PM");
        for (int i = 0; i < day; i++)
        {
            foreach (var item in FarmDatabase)
            {
                if (item.Status == Status.BREEDING) item.FillBreedGuage();
            }
            foreach (var factory in FactoryDatabase)
            {
                if (factory.Status == Status.BREEDING)
                {
                    factory.FillBreedGuage();
                }
            }
        }
        // Generate new side quest(s) by skipped time
        SideQuestDatabase.GenerateNewQuestByTime(CurrentDate, day);
        MainQuestDatabase.PassDay();
        // Generate new capybara by skipped time
        CapybaraDatabase.AddChanceByDays(day);

        // Restock shop
        Shop.CheckRestockTime(CurrentDate, day);

        // Clear Arena Record
        BattleRecord = new List<ArenaManager.WinType>();

        CurrentDate = d.DupeDate();

        // Valdiate for checking expiration
        MainQuestDatabase.ValidateAllQuestStatus();
        SideQuestDatabase.ValidateAllQuestStatus();

        // Set stat cap for mechs in case of reaching rank S
        if (FarmDatabase.Any(x => x.MechChromos.Count > 0))
        {
            // Prepare stat caps
            List<MechChromo> topAllies = new List<MechChromo>();

            foreach (var item in FarmDatabase)
            {
                if (item.MechChromos.Count > 0)
                {
                    topAllies.AddRange(EnemySelectionManager.GetStatFitnessDict(item.MechChromos, 0)
                        .OrderByDescending(x => x.Value[0]).Select(x => x.Key).Cast<MechChromo>());
                }
            }

            MechChromo m = EnemySelectionManager.GetStatFitnessDict(topAllies, 0)
                .OrderByDescending(x => x.Value[0]).Select(x => x.Key).Cast<MechChromo>().First();

            // Increase cap until it's not S
            int extraCap = 0;
            while (m.Rank == MechChromo.Ranks.S)
            {
                extraCap++;
                MechCap++;
                m.SetRank();
            }
            // Set rank for every other mechs
            if (extraCap > 0)
            {
                foreach (var item in topAllies)
                {
                    item.SetRank();
                }
            }
        }
    }

    public static void SetCurrentDate(TimeManager.Date newDate)
    {
        CurrentDate = newDate;
    }

    public void ResetDate()
    {
        CurrentDate = new TimeManager.Date();
    }

    public static void SetResearchLabTabIndex(int newTabIndex)
    {
        ResearchLabTabIndex = newTabIndex;
    }

    #region Money
    public static void SetMoney(int amount)
    {
        if (amount < 0)
        {
            return;
        }
        Money = amount;
    }

    public void ResetMoney()
    {
        Money = 3000;   // Hard-code initial amount of Money
    }

    // Deduct Money and return true if Money is enough. Otherwise, do nothing and return false
    public static bool SpendMoneyIfEnought(int deductAmount)
    {
        if (deductAmount <= Money)
        {
            Money -= deductAmount;
            if (deductAmount > 0)
            {
                SoundEffectManager.Instance.PlaySoundEffect("SpendMoney");
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    // Deduct Money, just that
    // DANGER, USE WITH CAUTION
    public static void ForceSpendMoney(int deductAmount)
    {
        Money -= deductAmount;
        if (deductAmount > 0)
        {
            SoundEffectManager.Instance.PlaySoundEffect("SpendMoney");
        }
    }

    // Gain money and return true if success, Otherwise, do nothing and return false
    public static bool GainMoneyIfValid(int gainAmount)
    {
        if (gainAmount >= 0)
        {
            Debug.Log($"Giving Money {gainAmount}G");
            Money += gainAmount;
            if (gainAmount > 0)
            {
                SoundEffectManager.Instance.PlaySoundEffect("GainMoney");
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion

    #region Facility Navigation and fixing
    // Change current factory
    public void SetCurrentFactoryIndex(int newFactoryIndex)
    {
        CurrentFactoryIndex = newFactoryIndex;
    }

    public void SetCurrentFarmIndex(int index)
    {
        CurrentFarmIndex = index;
    }

    public void SetCurrentDialogueIndex(int newIndex)
    {
        CurrentDialogueIndex = newIndex;
    }

    // Set facility to be fixed after the puzzle is done
    public void SetFacilityToFix(FacilityType facilityType, int facilityIndex)
    {
        FixingFacility = true;
        FacilityToFix = facilityType;
        FacilityToFixIndex = facilityIndex;
    }

    // Get the facility to fix name
    public static string GetFacilityToFixName()
    {
        if (FacilityToFix == FacilityType.Factory)
        {
            return FactoryDatabase[FacilityToFixIndex].Name;
        }
        else if (FacilityToFix == FacilityType.Farm)
        {
            return FarmDatabase[FacilityToFixIndex].Name;
        }
        return "Invalid Facility";
    }

    // Fix the facility and return the name of fixed facility
    public static void FixFacility()
    {
        if (FacilityToFix == FacilityType.Factory)
        {
            FactoryDatabase[FacilityToFixIndex].Fixed();
        }
        else if (FacilityToFix == FacilityType.Farm)
        {
            FarmDatabase[FacilityToFixIndex].Fixed();
        }
    }
    #endregion

    // Validate locking status of all SO
    public static void ValidateUnlocking()
    {
        Debug.Log("PlayerManager validating all unlockable object");
        foreach (ContentChapterSO chapter in ContentChapterDatabase)
        {
            chapter.ValidateUnlockRequirement();
        }
        foreach (FactorySO factory in FactoryDatabase)
        {
            factory.ValidateUnlockRequirement();
        }
        foreach (FarmSO farm in FarmDatabase)
        {
            farm.ValidateUnlockRequirement();
        }
        foreach (JigsawPieceSO piece in JigsawPieceDatabase)
        {
            piece.ValidateUnlockRequirement();
        }
        MainQuestDatabase.ValidateAllQuestStatus();
        SideQuestDatabase.ValidateAllQuestStatus();
    }

    // TEMP function for start the game with the least restriction
    public static void GMStart()
    {
        Money = 3000;
        foreach (ContentChapterSO chapter in ContentChapterDatabase)
        {
            chapter.ForceUnlock();
        }
        foreach (FactorySO factory in FactoryDatabase)
        {
            factory.ForceUnlock();
        }
        foreach (FarmSO farm in FarmDatabase)
        {
            farm.ForceUnlock();
        }
        foreach (JigsawPieceSO piece in JigsawPieceDatabase)
        {
            piece.ForceUnlock();
        }
        MainQuestDatabase.ForceCompleteQuest();
        SideQuestDatabase.ForceCompleteQuest();
        ValidateUnlocking();
    }

    #region Puzzle
    // Return a description string of the given PuzzleType
    public static string DescribePuzzleType(PuzzleType puzzleType)
    {
        switch (puzzleType)
        {
            default:
                return "Unknown puzzle type";
            case PuzzleType.Dialogue:
                return "Answer the questions";
            // Crossover
            case PuzzleType.CrossoverOnePointDemon:
                return "Demonstrate Single-point Crossover";
            case PuzzleType.CrossoverOnePointSolve:
                return "Solve Crossover problem";
            case PuzzleType.CrossoverTwoPointsDemon:
                return "Demonstrate Two-point Crossover";
            case PuzzleType.CrossoverTwoPointsSolve:
                return "Solve Crossover problem";
            // Selection
            case PuzzleType.SelectionTournamentDemon:
                return "Demonstrate Tournament-based Selection";
            case PuzzleType.SelectionTournamentSolve:
                return "Solve Selection problem";
            case PuzzleType.SelectionRouletteDemon:
                return "Demonstrate Roulette Wheel Selection";
            case PuzzleType.SelectionRouletteSolve:
                return "Solve Selection problem";
            case PuzzleType.SelectionRankDemon:
                return "Demonstrate Rank-based Selection";
            case PuzzleType.SelectionRankSolve:
                return "Solve Selection problem";
            // Knapsack
            case PuzzleType.KnapsackStandardDemon:
                return "Demonstrate Standard Knapsack encoding/decoding";
            case PuzzleType.KnapsackStandardSolve:
                return "Solve Knapsack encoding/decoding problem";
            case PuzzleType.KnapsackMultiDimenDemon:
                return "Demonstrate Multidimensional Knapsack encoding/decoding";
            case PuzzleType.KnapsackMultiDimenSolve:
                return "Solve Knapsack encoding/decoding problem";
            case PuzzleType.KnapsackMultipleDemon:
                return "Demonstrate Multiple Knapsack encoding/decoding";
            case PuzzleType.KnapsackMultipleSolve:
                return "Solve Knapsack encoding/decoding problem";
        }
    }

    public static void SetCurrentJigsawPiece(JigsawPieceSO jigsawPiece)
    {
        CurrentJigsawPiece = jigsawPiece;
    }

    // Record and return result after completing puzzle
    public static List<string> RecordPuzzleResult(bool isSuccess)
    {
        // Record progress in JigsawPieceSO
        int[] amountAndMoney = CurrentJigsawPiece.AddProgressCount(isSuccess, 1);
        // Generate feedback string from JigsawPieceSO.AddProgressCount()
        List<string> feedbackTexts = new List<string>();
        int amount = amountAndMoney[0];
        int money = amountAndMoney[1];
        string obtainSuffix = " x " + CurrentJigsawPiece.GetLockableObjectName();
        // Feedback on piece count
        if (amount > 0)
        {
            feedbackTexts.Add("- Obtain: " + amount.ToString() + obtainSuffix);
        }
        else if (amount < 0)
        {
            feedbackTexts.Add("- Fail to obtain: " + (-amount).ToString() + obtainSuffix);
        }
        // Feedback on money
        if (money > 0)
        {
            feedbackTexts.Add("- Gain money: " + money.ToString());
        }
        else if (money < 0)
        {
            feedbackTexts.Add("- Spend money: " + (-money).ToString());
        }
        // Fix facility if it's in fixing mode
        if (!FixingFacility)
        {
            return feedbackTexts;
        }
        FixingFacility = false;
        string fixingFeedback = "";
        if (isSuccess)
        {
            FixFacility();
            fixingFeedback += "- Successfully fix: ";
        }
        else
        {
            fixingFeedback += "- Fail to fix: ";
        }
        fixingFeedback += GetFacilityToFixName();
        feedbackTexts.Add(fixingFeedback);
        return feedbackTexts;
    }

    // Special case of RecordPuzzleResult for fixing the last factory
    public static List<string> RecordPuzzleResultForLastFactory(bool isSuccess)
    {
        // Calculate the pieces to obtain
        bool isSolve = (
                (PlayerManager.PuzzleToGenerate == PuzzleType.KnapsackMultiDimenSolve) ||
                (PlayerManager.PuzzleToGenerate == PuzzleType.KnapsackMultipleSolve)
                );
        List<JigsawPieceSO> jigsawToObtain = new List<JigsawPieceSO>();
        foreach (JigsawPieceSO piece in JigsawPieceForLastFactory)
        {
            if (PuzzleToGenerate == PuzzleType.Dialogue)
            {
                jigsawToObtain.Add(CurrentJigsawPiece);
                break;
            }
            else if(isSolve)
            {
                if ((piece.HowToObtain == PuzzleType.KnapsackMultiDimenSolve) ||
                    (piece.HowToObtain == PuzzleType.KnapsackMultipleSolve))
                {
                    jigsawToObtain.Add(piece);
                }
            }
            else
            {
                if ((piece.HowToObtain == PuzzleType.KnapsackMultiDimenDemon) ||
                    (piece.HowToObtain == PuzzleType.KnapsackMultipleDemon))
                {
                    jigsawToObtain.Add(piece);
                }
            }
        }
        // Record progress and generate feedback from JigsawPieceSO
        List<string> feedbackTexts = new List<string>();
        int obtainMoney = 0;
        foreach (JigsawPieceSO piece in jigsawToObtain)
        {
            int[] amountAndMoney = piece.AddProgressCount(isSuccess, 1);
            int amount = amountAndMoney[0];
            obtainMoney += amountAndMoney[1];
            string obtainSuffix = " x " + piece.GetLockableObjectName();
            if (amount > 0)
            {
                feedbackTexts.Add("- Obtain: " + amount.ToString() + obtainSuffix);
            }
            else if (amount < 0)
            {
                feedbackTexts.Add("- Fail to obtain: " + (-amount).ToString() + obtainSuffix);
            }
        }
        // Feedback on money
        if (obtainMoney > 0)
        {
            feedbackTexts.Add("- Gain money: " + obtainMoney.ToString());
        }
        else if (obtainMoney < 0)
        {
            feedbackTexts.Add("- Spend money: " + (-obtainMoney).ToString());
        }
        // Fix facility if it's in fixing mode
        if (!FixingFacility)
        {
            return feedbackTexts;
        }
        FixingFacility = false;
        string fixingFeedback = "";
        if (isSuccess)
        {
            FixFacility();
            fixingFeedback += "- Successfully fix: ";
        }
        else
        {
            fixingFeedback += "- Fail to fix: ";
        }
        fixingFeedback += GetFacilityToFixName();
        feedbackTexts.Add(fixingFeedback);
        return feedbackTexts;
    }
    #endregion

    public static void SetCurrentDialogue(DialogueSO newDialogue)
    {
        CurrentDialogueDatabase = newDialogue;
    }
}

public enum PuzzleType
{
    Dialogue,
    CrossoverOnePointDemon,
    CrossoverOnePointSolve,
    CrossoverTwoPointsDemon,
    CrossoverTwoPointsSolve,
    SelectionTournamentDemon,
    SelectionTournamentSolve,
    SelectionRouletteDemon,
    SelectionRouletteSolve,
    SelectionRankDemon,
    SelectionRankSolve,
    KnapsackStandardDemon,
    KnapsackStandardSolve,
    KnapsackMultiDimenDemon,
    KnapsackMultiDimenSolve,
    KnapsackMultipleDemon,
    KnapsackMultipleSolve
}