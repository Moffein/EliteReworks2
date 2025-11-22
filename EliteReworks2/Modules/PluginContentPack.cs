using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EliteReworks2.Modules
{
    public class PluginContentPack : IContentPackProvider
    {
        internal ContentPack contentPack = new ContentPack();
        public string identifier => EliteReworks2Plugin.pluginGUID;

        public static List<BuffDef> buffDefs = new List<BuffDef>();
        public static List<EffectDef> effectDefs = new List<EffectDef>();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<NetworkSoundEventDef> networkSoundEventDefs = new List<NetworkSoundEventDef>();

        public void Initialize()
        {
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(this);
        }

        public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            this.contentPack.identifier = this.identifier;
            contentPack.networkSoundEventDefs.Add(networkSoundEventDefs.ToArray());
            contentPack.projectilePrefabs.Add(projectilePrefabs.ToArray());
            contentPack.buffDefs.Add(buffDefs.ToArray());
            contentPack.effectDefs.Add(effectDefs.ToArray());

            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(this.contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
