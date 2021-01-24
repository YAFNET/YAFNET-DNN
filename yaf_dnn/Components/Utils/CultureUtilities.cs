/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2021 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.Utils
{
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;

    using YAF.Core.Helpers;
    using YAF.DotNetNuke.Components.Objects;

    /// <summary>
    /// Culture Helper Class
    /// </summary>
    public class CultureUtilities
    {
        /// <summary>
        /// Gets the YAF cultures.
        /// </summary>
        /// <returns>
        /// Dictionary with YAF Cultures
        /// </returns>
        public static List<YafCultureInfo> GetYafCultures()
        {
            var cult = StaticDataHelper.Cultures();

            var yafCultures = (from DataRow row in cult.Rows
                                                select
                                                    new YafCultureInfo
                                                        {
                                                            Culture = row["CultureTag"].ToString(),
                                                            LanguageFile = row["CultureFile"].ToString()
                                                        })
                .ToList();

            if (yafCultures.Count == 0)
            {
                yafCultures.Add(new YafCultureInfo { Culture = "en", LanguageFile = "english.xml" });
            }

            return yafCultures;
        }

        /// <summary>
        /// Gets the YAF culture info.
        /// </summary>
        /// <param name="yafCultures">The YAF cultures.</param>
        /// <param name="cultureInfo">The culture info.</param>
        /// <returns>
        /// The YAF Culture
        /// </returns>
        public static YafCultureInfo GetYafCultureInfo(List<YafCultureInfo> yafCultures, CultureInfo cultureInfo)
        {
            var culture = "en";
            var lngFile = "english.xml";

            var yafCultureInfo = new YafCultureInfo();

            if (cultureInfo != null)
            {
                if (yafCultures.Find(yafCult => yafCult.Culture.Equals(cultureInfo.TwoLetterISOLanguageName)) != null)
                {
                    culture = cultureInfo.TwoLetterISOLanguageName;
                    lngFile =
                        yafCultures.Find(yafCult => yafCult.Culture.Equals(cultureInfo.TwoLetterISOLanguageName)).LanguageFile;
                }
                else if (yafCultures.Find(yafCult => yafCult.Culture.Equals(cultureInfo.Name)) != null)
                {
                    culture = cultureInfo.Name;
                    lngFile = yafCultures.Find(yafCult => yafCult.Culture.Equals(cultureInfo.Name)).LanguageFile;
                }
            }

            yafCultureInfo.Culture = culture;
            yafCultureInfo.LanguageFile = lngFile;

            return yafCultureInfo;
        } 
    }
}