// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;

namespace Setup
{
    /// <summary>
    /// This is the Settings static class that can be used in your Core solution or in any
    /// of your client applications. All settings are laid out the same exact way with getters
    /// and setters. 
    /// </summary>
    public static class SetupSettings
    {
        static readonly Lazy<SetupIsolatedStorage> _implementation = new Lazy<SetupIsolatedStorage>(static () => CreateSettings(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        private static SetupIsolatedStorage AppSettings
        {
            get
            {
                SetupIsolatedStorage ret = _implementation.Value;
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
        internal static string YamlFile
        {
            get => AppSettings?.GetValueOrDefault("YamlFile", "") ?? throw new InvalidOperationException("AppSettings not yet initialised");
            set => AppSettings.AddOrUpdateValue("YamlFile", value);
        }


        private static SetupIsolatedStorage CreateSettings()
        {
            return new SetupIsolatedStorage();
        }

    }

}
