/* Yet Another Forum.NET
 * Copyright (C) 2006-2013 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace YAF.DotNetNuke.Utils
{
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;

    using YAF.Core;
    using YAF.DotNetNuke.Objects;

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