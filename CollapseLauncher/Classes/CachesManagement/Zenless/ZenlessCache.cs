﻿using CollapseLauncher.GameSettings.Zenless;
using CollapseLauncher.Interfaces;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace CollapseLauncher
{
    internal class ZenlessCache(UIElement parentUI, IGameVersionCheck gameVersionManager, ZenlessSettings gameSettings)
        : ZenlessRepair(parentUI, gameVersionManager, gameSettings, false, null, true), ICache, ICacheBase<ZenlessCache>
    {
        public ZenlessCache AsBaseType() => this;

        public async Task StartUpdateRoutine(bool showInteractivePrompt = false)
            => await StartRepairRoutine(showInteractivePrompt);
    }
}