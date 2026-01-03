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

using DotNetNuke.Services.ClientDependency;

namespace YAF.DotNetNuke;

using global::DotNetNuke.Abstractions.ClientResources;
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
    /// The java script
    /// </summary>
    private readonly IJavaScriptLibraryHelper javaScript;

    /// <summary>
    /// The client resource controller
    /// </summary>
    private readonly IClientResourceController clientResourceController;

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
    public YafDnnWhatsNew(IJavaScriptLibraryHelper javaScript)
    {
        this.javaScript = javaScript ?? this.DependencyProvider.GetRequiredService<IJavaScriptLibraryHelper>();
        this.clientResourceController = this.DependencyProvider.GetRequiredService<IClientResourceController>();
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
    private static int GetYafBoardId(int moduleId, int tabId)
    {
        var moduleInfo = new ModuleController().GetModule(moduleId, tabId);

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

                this.yafTabInfo = new TabController().GetTab(this.yafTabId, this.PortalSettings.PortalId, true);
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
            this.boardId = GetYafBoardId(this.yafModuleId, this.yafTabId);

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

                this.headerTemplate = moduleSettings["YafWhatsNewHeader"].ToType<string>().IsSet()
                    ? moduleSettings["YafWhatsNewHeader"].ToType<string>()
                    : """<div class="container my-3 p-3">""";

                this.itemTemplate = moduleSettings["YafWhatsNewItemTemplate"].ToType<string>().IsSet()
                    ? moduleSettings["YafWhatsNewItemTemplate"].ToType<string>()
                    : """
                      <div class="d-flex text-secondary pt-3"> 
                      	[LASTPOSTICON]
                      	<p class="pb-3 mb-0 small lh-sm border-bottom"> 
                      		<span class="d-block text-secondary"><strong>[TOPICLINK]</strong>&nbsp;([FORUMLINK])</strong>
                      		[LASTMESSAGE:150]</span>
                              [BYTEXT]&nbsp;[LASTUSERLINK]&nbsp;[LASTPOSTEDDATETIME]
                      </p> </div>
                      """;

                this.footerTemplate = moduleSettings["YafWhatsNewFooter"].ToType<string>().IsSet()
                    ? moduleSettings["YafWhatsNewFooter"].ToType<string>()
                    : "</div>";
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

        // Render TOPICLINK
        var textMessageLink = new HyperLink
        {
            Text =
                BoardContext.Current.Get<IBadWordReplace>()
                    .Replace(dataItem.Topic),
            NavigateUrl = messageUrl
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

        currentItem = currentItem.Replace("[FORUMLINK]", forumLink.RenderToString());

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
                    userName)
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
            var match = Regex.Match(currentItem, @"\[LASTMESSAGE\:(?<count>[0-9]*)\]", RegexOptions.Compiled);

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