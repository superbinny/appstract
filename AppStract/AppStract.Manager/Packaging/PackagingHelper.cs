﻿#region Copyright (C) 2009-2010 Simon Allaeys

/*
    Copyright (C) 2009-2010 Simon Allaeys
 
    This file is part of AppStract

    AppStract is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AppStract is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with AppStract.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Windows.Forms;
using AppStract.Core;
using AppStract.Core.Data.Application;
using AppStract.Core.Virtualization.Process.Packaging;

namespace AppStract.Manager.Packaging
{
  public static class PackagingHelper
  {

    #region Public Methods

    public static void Start()
    {
      CoreBus.Log.Message("Starting wizard to create a new application package.");
      PreConfigurationState preConfigurationState;
      if (!RunPreWizard(out preConfigurationState))
        return;
      PackagedApplication packagedApplication;
      if (!RunPackagingSequence(preConfigurationState, out packagedApplication))
        return;
      CoreBus.Log.Message("Successfully constructed a package for: " + preConfigurationState.InstallerExecutable);
      var applicationDataFile = System.IO.Path.Combine(preConfigurationState.InstallerOutputDestination,
                                                       CoreBus.Configuration.Application.DefaultApplicationDataFile);
      if (!RunPostWizard(packagedApplication, applicationDataFile))
      {
        MessageBox.Show("Failed to create an application data file for the packaged application.",
                        "Unable to create application data file", MessageBoxButtons.OK, MessageBoxIcon.Error);
        // ToDo:
        //  Ask user whether or not he wants to start the ApplicationConfigurationUtility
        //  or create a recovery utility based on ApplicationConfigurationUtility
        return;
      }
      // Start the application, if requested.
      if (preConfigurationState.Autostart)
        CoreManager.StartProcess(applicationDataFile);
    }

    #endregion

    #region Private Methods

    private static bool RunPreWizard(out PreConfigurationState preConfigurationState)
    {
      preConfigurationState = null;
      var preWizard = new PrePackagingWizard();
      if (preWizard.ShowDialog() != DialogResult.OK)
        return false;
      preConfigurationState = preWizard.Result;
      return true;
    }

    private static bool RunPackagingSequence(PreConfigurationState preConfigurationState, out PackagedApplication packagedApplication)
    {
      try
      {
        var packager = new Packager(preConfigurationState.InstallerExecutable,
                                    preConfigurationState.InstallerOutputDestination);
        packagedApplication = packager.CreatePackage();
        return true;
      }
      catch (PackageException ex)
      {
        CoreBus.Log.Error("Packaging failed", ex);
        MessageBox.Show("Failed to package the application.\r\nPlease check the log files for more information.",
                        "Packaging failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        // ToDo: Clean up first!
        packagedApplication = null;
        return false;
      }
    }

    private static bool RunPostWizard(PackagedApplication packagedApplication, string applicationDataFile)
    {
      // Packaging succeeded, now gather the required configuration data from the user.
      var postWizard = new PostPackagingWizard(packagedApplication);
      if (postWizard.ShowDialog() != DialogResult.OK)
        return false; // ToDo: Clean up first?
      // Save the resulting data.
      return ApplicationData.Save(postWizard.Result, applicationDataFile);
    }

    #endregion

  }
}