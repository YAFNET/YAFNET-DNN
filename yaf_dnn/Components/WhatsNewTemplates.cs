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

using System.Text.RegularExpressions;

namespace YAF.DotNetNuke.Components;

/// <summary>
/// The default templates of the What's New module (the same look as the latest topics of the YAF.NET sample application).
/// </summary>
public static class WhatsNewTemplates
{
    /// <summary>
    /// The default header template.
    /// </summary>
    public const string Header = """<div class="card m-3"><ul class="list-group list-group-flush">""";

    /// <summary>
    /// The default item template.
    /// </summary>
    public const string Item = """
                               <li class="list-group-item d-flex align-items-start gap-3 py-3">
                               	[TOPICICON]
                               	<div class="flex-grow-1" style="min-width: 0">
                               		<div class="text-truncate">[TOPICLINK]</div>
                               		<div class="small text-body-secondary">[INFORUMLINK] &middot; [BYTEXT] [LASTUSERLINK], [LASTPOSTEDDATETIME]</div>
                               	</div>
                               	[POSTS]
                               </li>
                               """;

    /// <summary>
    /// The default footer template.
    /// </summary>
    public const string Footer = "</ul></div>";

    /// <summary>
    /// The header template of older versions.
    /// </summary>
    private const string OldHeader = """<div class="container my-3 p-3">""";

    /// <summary>
    /// The item template of older versions.
    /// </summary>
    private const string OldItem = """
                                   <div class="d-flex text-secondary pt-3">
                                   	[LASTPOSTICON]
                                   	<p class="pb-3 mb-0 small lh-sm border-bottom">
                                   		<span class="d-block text-secondary"><strong>[TOPICLINK]</strong>&nbsp;([FORUMLINK])</strong>
                                   		[LASTMESSAGE:150]</span>
                                           [BYTEXT]&nbsp;[LASTUSERLINK]&nbsp;[LASTPOSTEDDATETIME]
                                   </p> </div>
                                   """;

    /// <summary>
    /// The footer template of older versions.
    /// </summary>
    private const string OldFooter = "</div>";

    /// <summary>
    /// The header template to use: the saved one, the default when nothing is saved or the default of an older version is saved.
    /// </summary>
    /// <param name="saved">The saved template.</param>
    /// <returns>The template.</returns>
    public static string HeaderOrDefault(string saved) => OrDefault(saved, OldHeader, Header);

    /// <summary>
    /// The item template to use: the saved one, the default when nothing is saved or the default of an older version is saved.
    /// </summary>
    /// <param name="saved">The saved template.</param>
    /// <returns>The template.</returns>
    public static string ItemOrDefault(string saved) => OrDefault(saved, OldItem, Item);

    /// <summary>
    /// The footer template to use: the saved one, the default when nothing is saved or the default of an older version is saved.
    /// </summary>
    /// <param name="saved">The saved template.</param>
    /// <returns>The template.</returns>
    public static string FooterOrDefault(string saved) => OrDefault(saved, OldFooter, Footer);

    /// <summary>
    /// The saved template, or the new default when nothing is saved or the (unchanged) old default is saved:
    /// the settings page saves the default templates, so these modules get the new look too.
    /// </summary>
    /// <param name="saved">The saved template.</param>
    /// <param name="oldDefault">The default of older versions.</param>
    /// <param name="newDefault">The default.</param>
    /// <returns>The template.</returns>
    private static string OrDefault(string saved, string oldDefault, string newDefault)
    {
        if (saved.IsNotSet())
        {
            return newDefault;
        }

        return Normalize(saved) == Normalize(oldDefault) ? newDefault : saved;
    }

    /// <summary>
    /// The template without whitespace (the saved templates differ in indentation and line breaks).
    /// </summary>
    /// <param name="template">The template.</param>
    /// <returns>The template without whitespace.</returns>
    private static string Normalize(string template) => Regex.Replace(template, @"\s+", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));
}