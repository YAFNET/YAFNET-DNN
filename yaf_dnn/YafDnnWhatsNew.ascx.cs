/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bj�rnar Henden
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

using DotNetNuke.Services.ClientDependency;

using YAF.DotNetNuke.Components;

namespace YAF.DotNetNuke;

using global::DotNetNuke.Abstractions.Application;
using global::DotNetNuke.Abstractions.ClientResources;
using global::DotNetNuke.Abstractions.Logging;
using global::DotNetNuke.Abstractions.Security.Permissions;
using global::DotNetNuke.Services.Url.FriendlyUrl;

using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

using YAF.Web.Controls;

/// <summary>
/// The YAF What's new Module Page.
/// </summary>
public partial class YafDnnWhatsNew : PortalModuleBase, IHaveServiceLocator
{
    /// <summary>
    ///   The YAF board id.
    /// </summary>
    private int boardId;

    /// <summary>
    ///   The max posts.
    /// </summary>
    private int maxPosts;

    /// <summary>
    ///   The YAF module id.
    /// </summary>
    private int yafModuleId;

    /// <summary>
    ///   The YAF tab id.
    /// </summary>
    private int yafTabId;

    /// <summary>
    /// The YAF TabInfo.
    /// </summary>
    private TabInfo yafTabInfo;

    /// <summary>
    /// The sort order
    /// </summary>
    private string sortOrder;

    /// <summary>
    /// The header Template
    /// </summary>
    private string headerTemplate;

    /// <summary>
    /// The item Template
    /// </summary>
    private string itemTemplate;

    /// <summary>
    /// The footer Template
    /// </summary>
    private string footerTemplate;

    /// <summary>
    /// Gets the Service Locator.
    /// </summary>
    [Inject]
    public IServiceLocator ServiceLocator => BoardContext.Current.ServiceLocator;

    /// <summary>
    /// The client resource controller
    /// </summary>
    private readonly IClientResourceController clientResourceController;

    /// <summary>
    /// The event logger.
    /// </summary>
    private readonly IEventLogger eventLogger;

    /// <summary>
    /// The permission definition service.
    /// </summary>
    private readonly IPermissionDefinitionService permissionDefinitionService;

    /// <summary>
    /// The host settings.
    /// </summary>
    private readonly IHostSettings hostSettings;

    /// <summary>
    /// The application status information.
    /// </summary>
    private readonly IApplicationStatusInfo applicationStatusInfo;

    /// <summary>
    /// The On PreRender event.
    /// </summary>
    /// <param name="e">
    /// the Event Arguments
    /// </param>
    protected override void OnPreRender(EventArgs e)
    {
        this.clientResourceController.RegisterStylesheet(this.Get<ITheme>().BuildThemePath("bootstrap-forum.min.css"));

        base.OnPreRender(e);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YafDnnWhatsNew"/> class.
    /// </summary>
    public YafDnnWhatsNew()
    {
        this.clientResourceController = this.DependencyProvider.GetRequiredService<IClientResourceController>();
        this.eventLogger = this.DependencyProvider.GetRequiredService<IEventLogger>();
        this.permissionDefinitionService = this.DependencyProvider.GetRequiredService<IPermissionDefinitionService>();
        this.hostSettings = this.DependencyProvider.GetRequiredService<IHostSettings>();
        this.applicationStatusInfo = this.DependencyProvider.GetRequiredService<IApplicationStatusInfo>();
    }

    /// <summary>
    /// Creates a new <see cref="ModuleController"/> instance using the non-obsolete constructor.
    /// </summary>
    /// <returns>Returns a new <see cref="ModuleController"/>.</returns>
    private ModuleController NewModuleController()
    {
        return new ModuleController(this.eventLogger, this.permissionDefinitionService, this.hostSettings);
    }

    /// <summary>
    /// Creates a new <see cref="TabController"/> instance using the non-obsolete constructor.
    /// </summary>
    /// <returns>Returns a new <see cref="TabController"/>.</returns>
    private TabController NewTabController()
    {
        return new TabController(
            this.eventLogger,
            DataProvider.Instance(),
            this.permissionDefinitionService,
            this.hostSettings,
            this.applicationStatusInfo);
    }

    /// <summary>
    /// The latest posts_ item data bound.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The <see cref="RepeaterItemEventArgs"/> instance containing the event data.
    /// </param>
    protected void LatestPostsItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        switch (e.Item.ItemType)
        {
            case ListItemType.Header:
            {
                var objLiteral = new Literal { Text = this.GetHeader() };
                e.Item.Controls.Add(objLiteral);
            }

                break;
            case ListItemType.AlternatingItem:
            case ListItemType.Item:
            {
                var objLiteral = new Literal { Text = this.ProcessItem(e) };
                e.Item.Controls.Add(objLiteral);
            }

                break;
            case ListItemType.Footer:
            {
                var objLiteral = new Literal { Text = this.GetFooter() };
                e.Item.Controls.Add(objLiteral);
            }

                break;
        }
    }

    /// <summary>
    /// Handles the Load event of the Page control.
    /// </summary>
    /// <param name="sender">
    /// The source of the event.
    /// </param>
    /// <param name="e">
    /// The <see cref="EventArgs"/> instance containing the event data.
    /// </param>
    protected void Page_Load(object sender, EventArgs e)
    {
        this.RunStartupServices();

        this.LoadSettings();

        this.BindData();
    }

    /// <summary>
    /// Get the YAF Board id from ModuleId and the Module Settings
    /// </summary>
    /// <param name="moduleId">
    ///     The Module id of the YAF Module Instance
    /// </param>
    /// <param name="tabId">
    /// The Tab ID of the YAF Module Instance
    /// </param>
    /// <returns>
    /// The Board Id From the Settings
    /// </returns>
    private int GetYafBoardId(int moduleId, int tabId)
    {
        var moduleInfo = this.NewModuleController().GetModule(moduleId, tabId);

        var moduleSettings = moduleInfo.ModuleSettings;

        int forumId;

        try
        {
            forumId = moduleSettings["forumboardid"].ToType<int>();
        }
        catch (Exception)
        {
            forumId = -1;
        }

        return forumId;
    }

    /// <summary>
    /// Binds the data.
    /// </summary>
    private void BindData()
    {
        try
        {
            var yafUserId = this.GetYafUserId();

            BoardContext.Current.GetRepository<ActiveAccess>().InsertPageAccess(this.boardId, yafUserId,
                HttpContext.Current.User.Identity.IsAuthenticated);

            var sortOrderTopics = this.sortOrder switch
            {
                "views" => 1,
                "replies" => 2,
                _ => 0
            };

            var activeTopics = BoardContext.Current.GetRepository<Topic>().Latest(
                this.boardId,
                0,
                this.maxPosts,
                yafUserId,
                true,
                true,
                sortOrderTopics);

            this.LatestPosts.DataSource = activeTopics;
            this.LatestPosts.DataBind();

            if (!activeTopics.Any())
            {
                this.lInfo.Text = Localization.GetString("NoMessages.Text", this.LocalResourceFile);
                this.lInfo.Style.Add("font-style", "italic");
                this.lInfo.Visible = true;
            }
            else
            {
                this.lInfo.Visible = false;
            }
        }
        catch (Exception exc)
        {
            // Module failed to load
            Exceptions.ProcessModuleLoadException(this, exc);
        }
    }

    /// <summary>
    /// Get YAF User Id from the Current DNN User
    /// </summary>
    /// <returns>
    /// The YAF User ID
    /// </returns>
    private int GetYafUserId()
    {
        // Check for user
        if (!HttpContext.Current.User.Identity.IsAuthenticated)
        {
            return BoardContext.Current.GuestUserID;
        }

        // Check if the user exists in yaf
        var yafUser = BoardContext.Current.GetRepository<User>()
            .GetUserByProviderKey(this.boardId, this.UserInfo.UserID.ToString());

        if (yafUser != null)
        {
            return yafUser.ID;
        }

        yafUser = BoardContext.Current.GetRepository<User>()
            .GetSingle(u => u.Name == this.UserInfo.Username && u.Email == this.UserInfo.Email);

        if (yafUser is null)
        {
            return UserImporter.CreateYafUser(
                this.UserInfo,
                this.boardId,
                this.PortalSettings.PortalId,
                BoardContext.Current.Get<BoardSettings>());
        }

        // update provider Key
        BoardContext.Current.GetRepository<User>().UpdateOnly(
            () => new User { ProviderUserKey = this.UserInfo.UserID.ToString() },
            u => u.ID == yafUser.ID);

        return yafUser.ID;
    }

    /// <summary>
    /// Load Module Settings
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            var moduleSettings = this.Settings;

            if (moduleSettings["YafPage"].ToType<string>().IsSet())
            {
                this.yafTabId = moduleSettings["YafPage"].ToType<int>();

                this.yafTabInfo = this.NewTabController()
                    .GetTab(this.yafTabId, this.PortalSettings.PortalId, true);
            }
            else
            {
                // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                if (this.IsEditable)
                {
                    this.Response.Redirect(
                        this.ResolveUrl(
                            $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Module/ModuleId/{this.ModuleId}/Default.aspx"));
                }
                else
                {
                    this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                    this.lInfo.Style.Add("font-style", "italic");
                }
            }

            if (moduleSettings["YafModuleId"].ToType<string>().IsSet())
            {
                this.yafModuleId = moduleSettings["YafModuleId"].ToType<int>();
            }
            else
            {
                // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                if (this.IsEditable)
                {
                    this.Response.Redirect(
                        this.ResolveUrl(
                            $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Module/ModuleId/{this.ModuleId}/Default.aspx"));
                }
                else
                {
                    this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                    this.lInfo.Style.Add("font-style", "italic");
                }
            }

            // Get and Set Board Id
            this.boardId = this.GetYafBoardId(this.yafModuleId, this.yafTabId);

            if (this.boardId.Equals(-1))
            {
                // If Module is not Configured show Message or Redirect to Settings Page if Current User has Edit Rights
                if (this.IsEditable)
                {
                    this.Response.Redirect(
                        this.ResolveUrl(
                            $"~/tabid/{this.PortalSettings.ActiveTab.TabID}/ctl/Module/ModuleId/{this.ModuleId}/Default.aspx"));
                }
                else
                {
                    this.lInfo.Text = Localization.GetString("NotConfigured.Text", this.LocalResourceFile);
                    this.lInfo.Style.Add("font-style", "italic");
                }
            }
            else
            {
                this.sortOrder = moduleSettings["YafSortOrder"].ToType<string>().IsSet()
                    ? moduleSettings["YafSortOrder"].ToType<string>()
                    : "lastpost";

                this.maxPosts = moduleSettings["YafMaxPosts"].ToType<string>().IsSet()
                    ? moduleSettings["YafMaxPosts"].ToType<int>()
                    : 10;

                this.headerTemplate = WhatsNewTemplates.HeaderOrDefault(moduleSettings["YafWhatsNewHeader"].ToType<string>());

                this.itemTemplate = WhatsNewTemplates.ItemOrDefault(moduleSettings["YafWhatsNewItemTemplate"].ToType<string>());

                this.footerTemplate = WhatsNewTemplates.FooterOrDefault(moduleSettings["YafWhatsNewFooter"].ToType<string>());
            }
        }
        catch (Exception exc)
        {
            Exceptions.ProcessModuleLoadException(this, exc);
        }
    }

    /// <summary>
    /// Gets the List header.
    /// </summary>
    /// <returns>Returns the html header.</returns>
    private string GetHeader()
    {
        return this.headerTemplate;
    }

    /// <summary>
    /// Processes the item.
    /// </summary>
    /// <param name="e">
    /// The <see cref="RepeaterItemEventArgs"/> instance containing the event data.
    /// </param>
    /// <returns>
    /// Returns the Item as string
    /// </returns>
    private string ProcessItem(RepeaterItemEventArgs e)
    {
        var dataItem = (LatestTopic)e.Item.DataItem;

        var currentItem = this.itemTemplate;

        var messageUrl = FriendlyUrlProvider.Instance().FriendlyUrl(
            this.yafTabInfo,
            $"{Globals.ApplicationURL(this.yafTabInfo.TabID)}&g=posts&m={dataItem.LastMessageID}",
            UrlRewriteHelper.CleanStringForURL(
                BoardContext.Current.Get<IBadWordReplace>().Replace(dataItem.Topic)));

        currentItem = currentItem.Replace("[LASTPOSTICON]",
            """<svg class="topic-icon me-2" height="32" width="32" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><!--!Font Awesome Free v7.1.0 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2026 Fonticons, Inc.--><path d="M51.9 384.9C19.3 344.6 0 294.4 0 240 0 107.5 114.6 0 256 0S512 107.5 512 240 397.4 480 256 480c-36.5 0-71.2-7.2-102.6-20L37 509.9c-3.7 1.6-7.5 2.1-11.5 2.1-14.1 0-25.5-11.4-25.5-25.5 0-4.3 1.1-8.5 3.1-12.2l48.8-89.4zm37.3-30.2c12.2 15.1 14.1 36.1 4.8 53.2l-18 33.1 58.5-25.1c11.8-5.1 25.2-5.2 37.1-.3 25.7 10.5 54.2 16.4 84.3 16.4 117.8 0 208-88.8 208-192S373.8 48 256 48 48 136.8 48 240c0 42.8 15.1 82.4 41.2 114.7z"/></svg>""");

        // Render TOPICICON (the comment icon in a box, like the latest topics of the sample application)
        currentItem = currentItem.Replace(
            "[TOPICICON]",
            """<span class="d-inline-flex align-items-center justify-content-center flex-shrink-0 rounded-3 bg-primary-subtle text-primary-emphasis" style="width: 2.75rem; height: 2.75rem"><svg height="20" width="20" fill="currentColor" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><!--!Font Awesome Free by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free--><path d="M512 240c0 114.9-114.6 208-256 208c-37.1 0-72.3-6.4-104.1-17.9c-11.9 8.7-31.3 20.6-54.3 30.6C73.6 471.1 44.7 480 16 480c-6.5 0-12.3-3.9-14.8-9.9c-2.5-6-1.1-12.8 3.4-17.4l.3-.3c.3-.3 .7-.7 1.3-1.4c1.1-1.2 2.8-3.1 4.9-5.7c4.1-5 9.6-12.4 15.2-21.6c10-16.6 19.5-38.4 21.4-62.9C17.7 326.8 0 285.1 0 240C0 125.1 114.6 32 256 32s256 93.1 256 208z"/></svg></span>""");

        // Render POSTS (the number of posts of the topic as a badge)
        currentItem = currentItem.Replace(
            "[POSTS]",
            $"""<span class="badge bg-secondary-subtle text-secondary-emphasis border border-secondary-subtle" title="{HttpUtility.HtmlAttributeEncode(BoardContext.Current.Get<IHaveLocalization>().GetText("DEFAULT", "POSTS"))}"><svg class="me-1" height="11" width="11" fill="currentColor" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path d="M205 34.8c11.5 5.1 19 16.6 19 29.2v64H336c97.2 0 176 78.8 176 176c0 113.3-81.5 163.9-100.2 174.1c-2.5 1.4-5.3 1.9-8.1 1.9c-10.9 0-19.7-8.9-19.7-19.7c0-7.5 4.3-14.4 9.8-19.5c9.4-8.8 22.2-26.4 22.2-56.7c0-53-43-96-96-96H224v64c0 12.6-7.4 24.1-19 29.2s-25 3-34.4-5.4l-160-144C3.9 225.7 0 217.1 0 208s3.9-17.7 10.6-23.8l160-144c9.4-8.5 22.9-10.6 34.4-5.4z"/></svg>{dataItem.NumPosts:N0}</span>""");

        // Render TOPICLINK
        var textMessageLink = new HyperLink
        {
            Text =
                BoardContext.Current.Get<IBadWordReplace>()
                    .Replace(dataItem.Topic),
            NavigateUrl = messageUrl,
            CssClass = "fw-semibold text-decoration-none"
        };

        currentItem = currentItem.Replace("[TOPICLINK]", textMessageLink.RenderToString());

        // Render FORUMLINK
        var forumLink = new HyperLink
        {
            Text = dataItem.Forum,
            NavigateUrl = FriendlyUrlProvider.Instance().FriendlyUrl(
                this.yafTabInfo,
                $"{Globals.ApplicationURL(this.yafTabInfo.TabID)}&g=topics&f={dataItem.ForumID}",
                UrlRewriteHelper.CleanStringForURL(
                    BoardContext.Current.Get<IBadWordReplace>()
                        .Replace(dataItem.Forum)))
        };

        forumLink.CssClass = "link-secondary";

        var forumLinkHtml = forumLink.RenderToString();

        currentItem = currentItem.Replace("[FORUMLINK]", forumLinkHtml);

        // Render INFORUMLINK ("in" and the forum link)
        currentItem = currentItem.Replace(
            "[INFORUMLINK]",
            string.Format(BoardContext.Current.Get<IHaveLocalization>().GetText("DEFAULT", "IN"), forumLinkHtml));

        // Render BYTEXT
        currentItem = currentItem.Replace(
            "[BYTEXT]",
            BoardContext.Current.Get<IHaveLocalization>().GetText("SEARCH", "BY"));

        // Render LASTUSERLINK
        // Just in case...
        if (dataItem.LastUserID.HasValue)
        {
            var userName = BoardContext.Current.Get<BoardSettings>().EnableDisplayName
                ? dataItem.LastUserDisplayName
                : dataItem.LastUserName;

            userName = new UnicodeEncoder().XSSEncode(userName);

            var lastUserLink = new HyperLink
            {
                Text = userName,
                ToolTip = userName,
                NavigateUrl = FriendlyUrlProvider.Instance().FriendlyUrl(
                    this.yafTabInfo,
                    $"{Globals.ApplicationURL(this.yafTabInfo.TabID)}&g=profile&u={dataItem.LastUserID}",
                    userName),
                CssClass = "link-secondary"
            };

            currentItem = currentItem.Replace("[LASTUSERLINK]", lastUserLink.RenderToString());
        }

        // Render LASTMESSAGE
        var lastMessage =
            BBCodeHelper.StripBBCode(
                    HtmlTagHelper.StripHtml(HtmlTagHelper.CleanHtmlString(dataItem.LastMessage)))
                .RemoveMultipleWhitespace();

        try
        {
            var match = Regex.Match(currentItem, @"\[LASTMESSAGE\:(?<count>[0-9]*)\]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

            if (match.Success)
            {
                var messageLimit = match.Groups["count"].Value.ToType<int>();

                currentItem = currentItem.Replace(
                    $"[LASTMESSAGE:{match.Groups["count"].Value}]",
                    lastMessage.Truncate(messageLimit));
            }
            else
            {
                currentItem = currentItem.Replace("[LASTMESSAGE]", lastMessage);
            }
        }
        catch (Exception)
        {
            currentItem = currentItem.Replace("[LASTMESSAGE]", lastMessage);
        }

        // Render LASTPOSTEDDATETIME
        var displayDateTime = new DisplayDateTime { DateTime = (DateTime)dataItem.LastPosted };

        currentItem = currentItem.Replace("[LASTPOSTEDDATETIME]", displayDateTime.RenderToString());

        return currentItem;
    }

    /// <summary>
    /// Gets the List footer.
    /// </summary>
    /// <returns>Returns the html footer.</returns>
    private string GetFooter()
    {
        return this.footerTemplate;
    }
}