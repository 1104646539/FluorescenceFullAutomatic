﻿using FluorescenceFullAutomatic.HomeModule.Services;
using FluorescenceFullAutomatic.HomeModule.ViewModels;
using FluorescenceFullAutomatic.HomeModule.Views;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeModule
{
    public class HomeModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //containerRegistry.RegisterSingleton<IHomeService, HomeService>();

            //containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
        }
    }
}
