using BepInEx;
using EliteReworks2.Common.Buffs;
using EliteReworks2.Elites;
using EliteReworks2.Modules;
using R2API.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace EliteReworks2
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2API.SoundAPI.PluginGUID)]
    [BepInDependency(R2API.DamageAPI.PluginGUID)]
    [BepInDependency(R2API.EliteAPI.PluginGUID)]
    [BepInDependency("com.TPDespair.ZetAspects", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(EliteReworks2Plugin.pluginGUID, EliteReworks2Plugin.pluginName, "2.0.0")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    public class EliteReworks2Plugin : BaseUnityPlugin
    {
        public const string pluginGUID = "com.Moffein.EliteReworks2";
        public const string pluginName = "EliteReworks2";

        public static PluginInfo info;
        internal void Awake()
        {
            info = Info;
            new PluginContentPack().Initialize();
            using (var bankStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EliteReworks2.EliteReworks.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                R2API.SoundAPI.SoundBanks.Add(bytes);
            }
            ModCompat.Init();
            InitContent();
            AddToAssembly();
        }

        private void InitContent()
        {
            DisablePassiveEffect.Init();
        }

        private void AddToAssembly()
        {
            var fixTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(TweakBase)));

            foreach (var tweakType in fixTypes)
            {
                TweakBase tweak = (TweakBase)Activator.CreateInstance(tweakType);
                tweak.Init(Config);
            }
        }
    }
}
