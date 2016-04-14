/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2016 Ingo Herbote
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

namespace YAF.DotNetNuke.Components.Controllers
{
    #region Using

    using global::DotNetNuke.Entities.Modules;
    using global::DotNetNuke.Services.Search;

    #endregion

    /// <summary>
    /// Search Controller
    /// </summary>
    public class Search : ModuleSettingsBase, ISearchable
    {
        #region Implemented Interfaces

        #region ISearchable

        /// <summary>
        /// TODO : Get YAF Search Items
        /// </summary>
        /// <param name="moduleInfo">The module info.</param>
        /// <returns>
        /// Search Items
        /// </returns>
        public SearchItemInfoCollection GetSearchItems(ModuleInfo moduleInfo)
        {
            /*
            // Get Used Board Id for the Module Instance
            try
            {
                if (this.Settings["forumboardid"].ToString().IsSet())
                {
                    this.boardId = this.Settings["forumboardid"].ToType<int>();
                }
            }
            catch (Exception)
            {
                this.boardId = 1;
            }

            var searchItemCollection = new SearchItemInfoCollection();

            // Get all Messages
            var yafMessages = Controller.Data.YafDnnGetMessages();

            // Get all Topics
            var yafTopics = Controller.Data.YafDnnGetTopics();

            // Get the forum name
            var forumName = moduleInfo.ModuleTitle.IsSet() ? moduleInfo.ModuleTitle : "YAF-Forum";

            foreach (Messages message in yafMessages)
            {
                // find the Topic of the message
                var curMessage = message;

                var curTopic =
                    yafTopics.Find(
                        topics => topics.TopicId.Equals(curMessage.TopicId) && topics.ForumId.Equals(this.boardId));

                if (curTopic == null)
                {
                    continue;
                }

                // Format message
                string sMessage = message.Message;

                if (sMessage.Contains(" "))
                {
                    string[] messageWords = sMessage.Split(' ');

                    var message1 = message;

                    foreach (var searchItem in from word in messageWords
                                               where !string.IsNullOrEmpty(word)
                                               select
                                                   new SearchItemInfo(
                                                   "{0} - {1}".FormatWith(forumName, curTopic.TopicName),
                                                   message1.Message,
                                                   Null.NullInteger,
                                                   message1.Posted,
                                                   moduleInfo.ModuleID,
                                                   "m{0}".FormatWith(message1.MessageId),
                                                   word,
                                                   "&g=posts&t={0}&m={1}#post{1}".FormatWith(
                                                       message1.TopicId, message1.MessageId),
                                                   Null.NullInteger))
                    {
                        searchItemCollection.Add(searchItem);
                    }
                }
                else
                {
                    var searchItem = new SearchItemInfo(
                        "{0} - {1}".FormatWith(forumName, curTopic.TopicName),
                        message.Message,
                        Null.NullInteger,
                        message.Posted,
                        moduleInfo.ModuleID,
                        "m{0}".FormatWith(message.MessageId),
                        sMessage,
                        "&g=posts&t={0}&m={1}#post{1}".FormatWith(message.TopicId, message.MessageId),
                        Null.NullInteger);

                    searchItemCollection.Add(searchItem);
                }
            }

            foreach (Topics topic in yafTopics)
            {
                if (!topic.ForumId.Equals(this.boardId))
                {
                    continue;
                }

                // Format Topic
                string sTopic = topic.TopicName;

                if (sTopic.Contains(" "))
                {
                    string[] topicWords = sTopic.Split(' ');

                    Topics topic1 = topic;

                    foreach (var searchItem in from topicWord in topicWords
                                               where !string.IsNullOrEmpty(topicWord)
                                               select
                                                   new SearchItemInfo(
                                                   "{0} - {1}".FormatWith(forumName, topic1.TopicName),
                                                   topic1.TopicName,
                                                   Null.NullInteger,
                                                   topic1.Posted,
                                                   moduleInfo.ModuleID,
                                                   "t{0}".FormatWith(topic1.TopicId),
                                                   topicWord,
                                                   "&g=posts&t={0}".FormatWith(topic1.TopicId),
                                                   Null.NullInteger))
                    {
                        searchItemCollection.Add(searchItem);
                    }
                }
                else
                {
                    var searchItem = new SearchItemInfo(
                        "{0} - {1}".FormatWith(forumName, topic.TopicName),
                        topic.TopicName,
                        Null.NullInteger,
                        topic.Posted,
                        moduleInfo.ModuleID,
                        "t{0}".FormatWith(topic.TopicId),
                        sTopic,
                        "&g=posts&t={0}".FormatWith(topic.TopicId),
                        Null.NullInteger);

                    searchItemCollection.Add(searchItem);
                }
            }

            return searchItemCollection;*/
            
            return null;
        }

        #endregion

        #endregion
    }
}