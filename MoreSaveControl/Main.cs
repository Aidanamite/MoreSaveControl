using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MoreSaveControl
{
    [BepInPlugin("Aidanamite.MoreSaveControl", "MoreSaveControl", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{Environment.CurrentDirectory}\\BepInEx\\{modName}";
        static Main instance;

        void Awake()
        {
            instance = this;
            new Harmony($"com.Aidanamite.{modName}").PatchAll(modAssembly);
            Logger.LogInfo($"{modName} has loaded");
        }

        public void Update()
        {
            if (GUIManager.Instance && GUIManager.Instance.input.ButtonStart.WasPressed && GUIManager.Instance.resultsPanel && GUIManager.Instance.resultsPanel._isActive && !InfoPopUp._instance._isActive)
            {
                string text = DigitalSunGames.Languages.I2.Localization.Get("GUI/CONFIRM_EXIT");
                text = string.Join("\n", text.Replace(".", ".\n").SplitLines(35));
                GameManager.Instance.CanSave = false;
                Patch_ResultsKeypress.Override = true;
                InfoPopUp.Show(text, (b) => 
                {
                    if (b)
                    {
                        GameManager.Instance.UnpauseGame();
                        GameManager.Instance.ReturnToMainMenu();
                    }
                    GameManager.Instance.CanSave = true;
                    Patch_ResultsKeypress.Override = false;
                });
            }
            try
            {

            }
            catch (Exception err)
            {
                Logger.LogInfo(err);
            }
        }

        static bool firstCall = true;
        public static bool AskSave(Action y, Action n)
        {
            if (!firstCall || GameManager.Instance.IsWillInDungeon() || ShopManager.Instance.IsShopOpen())
            {
                firstCall = true;
                return true;
            }
            string text = "Do you want to save before you exit?";
            text = string.Join("\n", text.Replace(".", ".\n").SplitLines(35));
            InfoPopUp.Show(text, delegate (bool b)
            {
                if (b)
                {
                    GameManager.Instance.SaveSlotOnGoToDungeon(true);
                    y();
                }
                else
                {
                    firstCall = false;
                    n();
                }
            });
            return false;
        }

        public static void Log(object message) => instance.Logger.LogInfo(message);
    }

    [HarmonyPatch(typeof(DungeonResultsPanel))]
    public class Patch_ResultsKeypress
    {
        public static bool Override = false;
        [HarmonyPatch("OnAButtonReleased")]
        [HarmonyPrefix]
        static bool OnAButtonReleased(ref bool __result)
        {
            __result = false;
            return !Override;
        }
        [HarmonyPatch("OnBButtonPressed")]
        [HarmonyPrefix]
        static bool OnBButtonPressed(ref bool __result) => OnAButtonReleased(ref __result);
    }

    [HarmonyPatch(typeof(PausePanel))]
    public class Patch_PauseMenu
    {
        [HarmonyPatch("ExitGame")]
        [HarmonyPrefix]
        static bool ExitGame(PausePanel __instance) => Main.AskSave(GameManager.Instance.ExitGame, __instance.ExitGame);

        [HarmonyPatch("GoToMenu")]
        [HarmonyPrefix]
        static bool GoToMenu(PausePanel __instance) => Main.AskSave(delegate { GameManager.Instance.UnpauseGame(); GameManager.Instance.ReturnToMainMenu(); }, __instance.GoToMenu);
    }
}