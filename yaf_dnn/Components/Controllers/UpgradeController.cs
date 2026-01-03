/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2026 Ingo Herbote
 * https://www.yetanotherforum.net/
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * https://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

namespace YAF.DotNetNuke.Components.Controllers;

using YAF.Core.Services.Import;
using YAF.Types.EventProxies;
using YAF.Types.Interfaces.Events;

/// <summary>
/// The upgrade controller.
/// </summary>
public class UpgradeController : ModuleSettingsBase, IUpgradeable, IHaveServiceLocator
{
    /// <summary>
    ///     Gets or sets the service locator.
    /// </summary>
    public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

    /// <summary>
    ///     The BBCode extensions import xml file.
    /// </summary>
    private const string BbcodeImport = "~/DesktopModules/YetAnotherForumDotNet/Install/BBCodeExtensions.xml";

    /// <summary>
    ///     The Spam Words list import xml file.
    /// </summary>
    private const string SpamWordsImport = "~/DesktopModules/YetAnotherForumDotNet/Install/SpamWords.xml";

    /// <summary>
    /// Upgrades the module.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>Returns nothing</returns>
    public string UpgradeModule(string version)
    {
        var versionType = this.GetRepository<Registry>().ValidateVersion(BoardInfo.AppVersion);

        switch (versionType)
        {
            case DbVersionType.Upgrade:
                // Run Auto Upgrade
                this.Get<UpgradeService>().Upgrade();
                this.AddOrUpdateExtensions();
                this.UpdateProviderKeys();
                return string.Empty;
            case DbVersionType.NewInstall:
                this.Get<InstallService>().InitializeDatabase();
                this.AddOrUpdateExtensions();
                return string.Empty;
            case DbVersionType.Current:
                return string.Empty;
            default:
                return string.Empty;
        }
    }

    private void UpdateProviderKeys()
    {
        var prevVersion = this.GetRepository<Registry>().GetDbVersion();

        if (prevVersion > 87)
        {
            return;
        }

        var dnnUsers = UserController.GetUsers(this.PortalId);

        if (dnnUsers == null)
        {
            return;
        }

        foreach (UserInfo user in dnnUsers)
        {
            // Migrate from Yaf < 3
            var yafUser = this.GetRepository<User>()
                .GetSingle(u => u.Name == user.Username && u.Email == user.Email);

            if (yafUser != null)
            {
                // update provider Key
                this.GetRepository<User>().UpdateOnly(
                    () => new User { ProviderUserKey = user.UserID.ToString() },
                    u => u.ID == yafUser.ID);
            }
        }
    }

    /// <summary>
    ///    Add or Update BBCode Extensions and Spam Words
    /// </summary>
    private void AddOrUpdateExtensions()
    {
        var loadWrapper = new Action<string, Action<Stream>>(
            (file, streamAction) =>
                {
                    var fullFile = this.Get<HttpRequestBase>().MapPath(file);

                    if (!File.Exists(fullFile))
                    {
                        return;
                    }

                    // import into board...
                    using var stream = new StreamReader(fullFile);
                    streamAction(stream.BaseStream);
                    stream.Close();
                });

        // get all boards...
        var boardIds = this.GetRepository<Board>().GetAll().Select(x => x.ID);

        // Upgrade all Boards
        boardIds.ForEach(
            boardId =>
                {
                    this.Get<IRaiseEvent>().Raise(new ImportStaticDataEvent(boardId));

                    // load default bbcode if available...
                    loadWrapper(BbcodeImport, s => DataImport.BBCodeExtensionImport(boardId, s));

                    // load default spam word if available...
                    loadWrapper(SpamWordsImport, s => DataImport.SpamWordsImport(boardId, s));
                });
    }
}