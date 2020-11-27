﻿// Stationeers.Addons (c) 2018-2020 Damian 'Erdroy' Korczowski & Contributors

using System.Collections;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Stationeers.Addons.Loader.Plugins;
using Steamworks;
using UnityEngine;

namespace Stationeers.Addons.Loader.Core
{
    /// <summary>
    ///     ModManager class. Implements loader base component.
    /// </summary>
    internal class LoaderManager : MonoBehaviour
    {
        private static LoaderManager _instance;

        /// <summary>
        ///     LoaderManager's instance.
        /// </summary>
        public static LoaderManager Instance
        {
            get
            {
                if (_instance)
                    return _instance;

                var gameObject = new GameObject("ModLoader");
                _instance = gameObject.AddComponent<LoaderManager>();

                return _instance;
            }
        }

        /// <summary>
        ///     PluginManager instance.
        /// </summary>
        public PluginManager PluginManager { get; private set; }

        public void Activate()
        {
            Debug.Log("ModLoader activated!");
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            PluginManager = new PluginManager();
            // TODO: BundleManager so we will be able to add new content to the game
        }

        private IEnumerator Start()
        {
            ProgressPanel.Instance.ShowProgressBar("<b>Stationeers.Addons</b>");
            yield return LoadWorkshop();
            yield return CompilePlugins();
            yield return LoadPlugins();
            yield return LoadBundles();

            ProgressPanel.Instance.HideProgressBar();
        }

        private void OnDestroy()
        {
            // Cleanup
            PluginManager.UnloadAllPlugins();
        }

        private void OnGUI()
        {
            GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.15f);
            GUI.Label(new Rect(5.0f, 5.0f, Screen.width, 25.0f), $"Stationeers.Addons - v0.1.0 - Loaded {PluginManager.Instance.NumLoadedPlugins} plugins");
        }

        private static IEnumerator LoadWorkshop()
        {
            ProgressPanel.Instance.UpdateProgressBarCaption("Loading Workshop...");
            ProgressPanel.Instance.UpdateProgressBar(0.1f);

            // Start loading workshop
            WorkshopManager.Instance.LoadWorkshopItems();
            WorkshopManager.Instance.GetSubscribedItems();

            // Hack to detect when items are loaded.
            // Will be created inside steam callback
            WorkshopManager.Instance.SubscribedItems = null;

            // Wait until we get workshop loaded
            while (WorkshopManager.Instance.SubscribedItems == null)
                yield return null;

            Debug.Log("Workshop loaded!");
        }

        private static IEnumerator CompilePlugins()
        {
            ProgressPanel.Instance.UpdateProgressBar(0.35f);
            ProgressPanel.Instance.UpdateProgressBarCaption("Compiling plugins..."); 

            Debug.Log("Starting to compile the plugins...");

            var pluginCompiler = new PluginCompiler();

            if (!Directory.Exists("AddonManager/AddonsCache"))
                Directory.CreateDirectory("AddonManager/AddonsCache");

            var numPlugins = WorkshopManager.Instance.SubscribedItems.Count;
            var pluginIndex = 0;
            foreach (var workshopItemID in WorkshopManager.Instance.SubscribedItems)
            {
                // TODO: Read XML file and get the real addon name to show

                var progress = Mathf.Clamp01(pluginIndex / (float)numPlugins);
                ProgressPanel.Instance.UpdateProgressBar(Mathf.Lerp(0.35f, 0.70f, progress));
                ProgressPanel.Instance.UpdateProgressBarCaption("Compiling " + workshopItemID.m_PublishedFileId);

                if (SteamUGC.GetItemInstallInfo(workshopItemID, out _, out var pchFolder, 1024U, out _))
                {
                    var addonScripts = Directory.GetFiles(pchFolder, "*.cs", SearchOption.AllDirectories);

                    if (addonScripts.Length == 0) continue;

                    var addonDirectory = pchFolder;
                    var addonName = "workshop-" + workshopItemID.m_PublishedFileId;
                    pluginCompiler.CompileScripts(addonName, addonDirectory, addonScripts);

                    // TODO: Add dir+name to some list or something
                }

                pluginIndex++;
                yield return new WaitForEndOfFrame();
            }

            // TODO: Detect when plugin doesn't need to be compiled (not changed etc.)

            // Dispose the compiler
            pluginCompiler.Dispose();
        }

        private static IEnumerator LoadPlugins()
        {
            ProgressPanel.Instance.UpdateProgressBar(0.7f);
            ProgressPanel.Instance.UpdateProgressBarCaption("Loading plugins...");
            yield return new WaitForSeconds(0.6f);
        }

        private static IEnumerator LoadBundles()
        {
            ProgressPanel.Instance.UpdateProgressBar(0.8f);
            ProgressPanel.Instance.UpdateProgressBarCaption("Loading content...");
            yield return new WaitForSeconds(0.1f);
        }
    }
}