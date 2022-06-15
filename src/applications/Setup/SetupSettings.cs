// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using FAnsi.Discovery;
using Plugin.Settings.Abstractions;
using ReusableLibraryCode.Checks;
using static ReusableLibraryCode.Checks.CheckEventArgs;

namespace Setup
{
    /// <summary>
    /// This is the Settings static class that can be used in your Core solution or in any
    /// of your client applications. All settings are laid out the same exact way with getters
    /// and setters. 
    /// </summary>
    public static class SetupSettings
    {
        static Lazy<ISettings> implementation = new Lazy<ISettings>(() => CreateSettings(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        private static ISettings AppSettings
        {
            get
            {
                ISettings ret = implementation.Value;
                if (ret == null)
                {
                    throw new NotImplementedException("Isolated Storage does not work in this environment...");
                }
                return ret;
            }
        }

        /// <summary>
        /// Last loaded/selected .yaml file
        /// </summary>
        public static string YamlFile
        {
            get { return AppSettings.GetValueOrDefault("YamlFile", ""); }
            set { AppSettings.AddOrUpdateValue("YamlFile", value); }
        }


        static ISettings CreateSettings()
        {
            return new SetupIsolatedStorage();
        }

    }

}
