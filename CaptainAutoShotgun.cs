using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using On.EntityStates.Captain.Weapon;
using UnityEngine;
using RoR2;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CaptainAutoShotgun
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(GUID, MOD_NAME, MOD_VERSION)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class CaptainAutoShotgun : BaseUnityPlugin
    {
        public const string GUID = "com.Lunzir.CaptainAutoShotgun", MOD_NAME = "CaptainAutoShotgun", MOD_VERSION = "1.1.0";
        public static PluginInfo PluginInfo;
        List<CaptainAutoShotgunStruct> Instance;
        int CurrentIndex;

        public void Awake()
        {
            ModConfig.InitConfig(Config);

            PluginInfo = Info;
            Tokens.RegisterLanguageTokens();

            On.RoR2.Run.Start += Run_Start;
            ChargeCaptainShotgun.FixedUpdate += ChargeCaptainShotgun_FixedUpdate;
            On.RoR2.UI.SkillIcon.Update += SkillIcon_Update;
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            UpdateData();
        }

        private void UpdateData()
        {
            if (Instance != null)
            {
                Instance.Clear();
            }
            Instance = new List<CaptainAutoShotgunStruct>()
            {
                new CaptainAutoShotgunStruct("CAPTAINAUTOSHOTGUN_DEFAULT", CaptainShogunMode.Default),
                new CaptainAutoShotgunStruct("CAPTAINAUTOSHOTGUN_DIRECTSHOT", CaptainShogunMode.DirectShot),
                new CaptainAutoShotgunStruct("CAPTAINAUTOSHOTGUN_CHARGE", CaptainShogunMode.Charge),
            };
            switch (ModConfig.SelectMode.Value)
            {
                case CaptainShogunMode.Default:
                    CurrentIndex = 0;
                    break;
                case CaptainShogunMode.DirectShot:
                    CurrentIndex = 1;
                    break;
                case CaptainShogunMode.Charge:
                    CurrentIndex = 2;
                    break;
            }
        }

        private void Update()
        {
            float axis = Input.GetAxis("Mouse ScrollWheel");
            if (axis != 0f)
            {
                if (axis > 0f)
                {
                    CurrentIndex++;
                }
                if (axis < 0f)
                {
                    CurrentIndex--;
                    if (CurrentIndex < 0)
                    {
                        CurrentIndex = Instance.Count - 1;
                    }
                }
                CurrentIndex %= Instance.Count;
                ModConfig.SelectMode.Value = Instance[CurrentIndex].Mode;
            }
        }

        private void SkillIcon_Update(On.RoR2.UI.SkillIcon.orig_Update orig, RoR2.UI.SkillIcon self)
        {
            orig.Invoke(self);
            if (self.targetSkill && self.targetSkillSlot == 0)
            {
                if (self.targetSkill.characterBody.baseNameToken == "CAPTAIN_BODY_NAME")
                {
                    self.stockText.gameObject.SetActive(true);
                    self.stockText.fontSize = 12f;
                    self.stockText.SetText(Language.GetString(Instance[CurrentIndex].Name_Token));
                }
            }
        }

        private void ChargeCaptainShotgun_FixedUpdate(ChargeCaptainShotgun.orig_FixedUpdate orig, EntityStates.Captain.Weapon.ChargeCaptainShotgun self)
        {
            switch (ModConfig.SelectMode.Value)
            {
                case CaptainShogunMode.Default:
                    break;
                case CaptainShogunMode.DirectShot:
                    Reflection.SetFieldValue(self, "released", true);
                    break;
                case CaptainShogunMode.Charge:
                    if (self.fixedAge >= Reflection.GetFieldValue<float>(self, "chargeDuration"))
                    {
                        Reflection.SetFieldValue(self, "released", true);
                    }
                    break;
            }
            orig(self);
        }
        //public static void Send(string message)
        //{
        //    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
        //    {
        //        baseToken = message
        //    });
        //}
        internal class CaptainAutoShotgunStruct
        {
            public string Name_Token;
            public CaptainShogunMode Mode;

            public CaptainAutoShotgunStruct(string name_Token, CaptainShogunMode mode)
            {
                Name_Token = name_Token;
                Mode = mode;
            }
        }
    }
    enum CaptainShogunMode
    {
        Default, DirectShot, Charge
    }
    class ModConfig
    {
        public static ConfigEntry<CaptainShogunMode> SelectMode;
        public static void InitConfig(ConfigFile config)
        {
            SelectMode = config.Bind("Setting 设置", "SelectMode", CaptainShogunMode.DirectShot, "Can use the Mouse ScrollWheel to switch mode\nDefault = original, DirectShot = direct shot without charge, Charge = auto shot with charge" +
                "\n船长一技能霰弹枪模式, 可用鼠标滑轮切换\nDefault = 原版，DirectShot = 直接射击不蓄力，Charge = 自动蓄力射击");
        }
    }
    public static class Tokens
    {
        internal static string LanguageRoot
        {
            get
            {
                return System.IO.Path.Combine(AssemblyDir, "Language");
            }
        }

        internal static string AssemblyDir
        {
            get
            {
                return System.IO.Path.GetDirectoryName(CaptainAutoShotgun.PluginInfo.Location);
            }
        }
        public static void RegisterLanguageTokens()
        {
            On.RoR2.Language.SetFolders += Language_SetFolders;
        }

        private static void Language_SetFolders(On.RoR2.Language.orig_SetFolders orig, Language self, IEnumerable<string> newFolders)
        {
            if (Directory.Exists(LanguageRoot))
            {
                IEnumerable<string> second = Directory.EnumerateDirectories(System.IO.Path.Combine(new string[]
                {
                    LanguageRoot
                }), self.name);
                orig.Invoke(self, newFolders.Union(second));
            }
            else
            {
                orig.Invoke(self, newFolders);
            }
        }
    }
}
