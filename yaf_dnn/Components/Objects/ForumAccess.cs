/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014 Ingo Herbote
 * http://www.yetanotherforum.net/
 * 
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * http://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
namespace YAF.DotNetNuke.Components.Objects
{
    using YAF.Types.Flags;

    /// <summary>
    /// Forum Access List
    /// </summary>
    public class ForumAccess
    {
        #region Constants and Fields

        /// <summary>
        /// Gets or sets the group unique identifier.
        /// </summary>
        /// <value>
        /// The group unique identifier.
        /// </value>
        public int GroupID { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        /// <value>
        /// The name of the group.
        /// </value>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets the name of the access mask.
        /// </summary>
        /// <value>
        /// The name of the access mask.
        /// </value>
        public string AccessMaskName { get; set; }

        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>
        /// The flags.
        /// </value>
        public AccessFlags Flags { get; set; }

        #endregion
    }
}