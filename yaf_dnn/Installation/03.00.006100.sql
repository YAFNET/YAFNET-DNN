/*
   *******************************************************************
   *  SQL Script to migrate DNN active Forum to YAF.Net DNN module   *
   *  ============================================================   *
   *                                                                 *
   *  (c) Sebastian Leupold, dnnWerk, 2016                           *
   *                                                                 *
   *******************************************************************
*/

IF NOT Exists (SELECT * FROM sys.columns where object_id = OBJECT_ID(N'[{databaseOwner}].[{objectQualifier}Board]')    AND name = N'oModuleID')
   ALTER TABLE [{databaseOwner}].[{objectQualifier}Board] ADD oModuleID     Int Null

IF NOT Exists (SELECT * FROM sys.columns where object_id = OBJECT_ID(N'[{databaseOwner}].[{objectQualifier}Category]') AND name = N'oGroupID')
   ALTER TABLE [{databaseOwner}].[{objectQualifier}Category] ADD oGroupID   Int Null

IF NOT Exists (SELECT * FROM sys.columns where object_id = OBJECT_ID(N'[{databaseOwner}].[{objectQualifier}Forum]')    AND name = N'oForumID')
   ALTER TABLE [{databaseOwner}].[{objectQualifier}Forum]    ADD oForumID   Int Null

IF NOT Exists (SELECT * FROM sys.columns where object_id = OBJECT_ID(N'[{databaseOwner}].[{objectQualifier}Topic]')    AND name = N'oTopicID')
   ALTER TABLE [{databaseOwner}].[{objectQualifier}Topic]    ADD oTopicID   Int Null

IF NOT Exists (SELECT * FROM sys.columns where object_id = OBJECT_ID(N'[{databaseOwner}].[{objectQualifier}Message]')  AND name = N'oContentID')
   ALTER TABLE [{databaseOwner}].[{objectQualifier}Message]  ADD oContentID Int Null

GO

IF  exists (select top 1 1 from sys.objects WHERE object_id = OBJECT_ID(N'[{databaseOwner}].[{objectQualifier}ImportActiveForums]') AND type in (N'P', N'PC'))
DROP PROCEDURE [{databaseOwner}].[{objectQualifier}ImportActiveForums]
GO

CREATE PROCEDURE [{databaseOwner}].[{objectQualifier}ImportActiveForums] (@oModuleID int, @boardID int = 1, @oPortalID int) as
begin
BEGIN TRY
    -- PRINT N'*** start migration (this may take some time) ***';
    DECLARE @TZOffsetMin int = - DATEPART(TZOFFSET, SYSDATETIMEOFFSET());
    DECLARE @newRank smallint = (SELECT RankId FROM  [{databaseOwner}].[{objectQualifier}Rank] WHERE BoardID = @boardID AND (Flags & 1) = 1);

    -- Populate Users:
    /* YAF User Flags: None = 0, IsHostAdmin = 1, IsApproved = 2, IsGuest = 4, IsCaptchaExcluded = 8, IsActiveExcluded = 16, IsDST = 32, IsDirty = 64 */
    DECLARE @DefaultTimeZoneOffset SmallInt = (SELECT TimezoneOffset FROM [{databaseOwner}].[Portals] WHERE PortalID = @oPortalID);

    -- PRINT N'create all other users, who ever created a post:';
    WITH xUsers AS
        (SELECT U.*,
                U.UserID AS UserKey,
                P.Signature,
                P.TopicCount + P.ReplyCount AS NumPosts
        FROM      [{databaseOwner}].[Users]                     U
             JOIN [{databaseOwner}].[activeforums_UserProfiles] P ON U.UserID   = P.UserId AND P.PortalId = @oPortalID
      )
        MERGE INTO  [{databaseOwner}].[{objectQualifier}user] T
        USING xUsers S ON T.BoardID = @BoardID and T.Name = S.UserName
        WHEN NOT MATCHED THEN INSERT ( BoardID, ProviderUserKey,       Name,   DisplayName, Password,   Email,       Joined,    LastVisit,   IP,   NumPosts,   TimeZone,   Avatar,   Signature, AvatarImage, AvatarImageType,   RankID, Suspended, SuspendedReason, SuspendedBy, LanguageFile, ThemeFile, PMNotification, AutoWatchTopics, DailyDigest, NotificationType, Flags, Points, Culture, UserStyle)
                              VALUES (@BoardID,       S.UserKey, S.UserName, S.DisplayName,    N'na', S.Email, GetUTCDate(), GetUTCDate(), Null, S.NumPosts,       NULL,     NULL, S.Signature,        Null,            Null, @newRank,         0,            Null,           0,         Null,       Null,             1,               0,           0,                0,     2,      0,    Null,      Null);


    -- PRINT N'Add Guests Membership for Guest User;';
    With S AS
        (SELECT Y.UserID,
                G.GroupID
          FROM   [{databaseOwner}].[{objectQualifier}Group] G
          JOIN   [{databaseOwner}].[{objectQualifier}User]  Y ON Y.Name = N'Guest' AND Y.BoardID  = G.BoardID AND G.Flags = 2
          WHERE G.BoardID  = @BoardID
        )
        MERGE INTO  [{databaseOwner}].[{objectQualifier}UserGroup] T
        USING S ON T.UserID = S.UserID and T.GroupID = S.GroupID
        WHEN NOT MATCHED THEN INSERT (UserID, GroupID) VALUES (S.UserID, S.GroupID);

    -- PRINT N'Add All users to Registered Group;';
    With S AS
        (SELECT Y.UserID,
                G.GroupID
          FROM   [{databaseOwner}].[{objectQualifier}Group] G
          JOIN   [{databaseOwner}].[{objectQualifier}User]  Y ON Y.Name != N'Guest' AND Y.BoardID  = G.BoardID AND G.Flags = 4
          WHERE G.BoardID  = @BoardID
        )
        MERGE INTO  [{databaseOwner}].[{objectQualifier}UserGroup] T
        USING S ON T.UserID = S.UserID and T.GroupID = S.GroupID
        WHEN NOT MATCHED THEN INSERT (UserID, GroupID) VALUES (S.UserID, S.GroupID);

    -- PRINT N'Add Superusers to Administrators Group;';
    With S AS
        (SELECT Y.UserID,
                G.GroupID
          FROM   [{databaseOwner}].[{objectQualifier}Group] G
          JOIN   [{databaseOwner}].[{objectQualifier}User]  Y ON  Y.BoardID  = G.BoardID AND G.Flags = 1
          JOIN  [{databaseOwner}].[Users]     U ON  U.UserName = Y.Name AND U.IsSuperUser = 1
          WHERE G.BoardID  = @BoardID
        )
        MERGE INTO  [{databaseOwner}].[{objectQualifier}UserGroup] T
        USING S ON T.UserID = S.UserID and T.GroupID = S.GroupID
        WHEN NOT MATCHED THEN INSERT (UserID, GroupID) VALUES (S.UserID, S.GroupID);

    -- PRINT N'Populate UserGroups:';
    With S AS
        (SELECT Y.UserId,
                G.GroupID
          FROM [{databaseOwner}].[UserRoles] X
          JOIN [{databaseOwner}].[Roles]     R ON X.RoleID = R.RoleID AND R.PortalID = @oPortalID
          JOIN [{databaseOwner}].[{objectQualifier}Group] G ON R.RoleName = G.Name AND G.BoardID  = @BoardID
          JOIN [{databaseOwner}].[Users]     U ON X.UserID   = U.UserID
          JOIN [{databaseOwner}].[{objectQualifier}User]  Y ON U.Username = Y.Name AND Y.BoardID  = @BoardID
        )
        MERGE INTO  [{databaseOwner}].[{objectQualifier}UserGroup] T
        USING S ON T.UserID = S.UserID and T.GroupID = S.GroupID
        WHEN NOT MATCHED THEN INSERT (UserID, GroupID) VALUES (S.UserID, S.GroupID);

    -- PRINT N'Raise Access Mask for Administrators:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}User] T
    USING (SELECT UserID
            FROM   [{databaseOwner}].[{objectQualifier}UserGroup] R
            JOIN   [{databaseOwner}].[{objectQualifier}Group]     G ON R.GroupID = G.GroupID
            WHERE G.Flags = 1 AND G.BoardID = @BoardID
           ) S ON T.UserID = S.UserID
    WHEN MATCHED AND T.Flags != 98 THEN UPDATE SET FLAGS = 98;

    -- PRINT N'Raise Access Mask for Superusers:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}User] T
    USING (SELECT R.UserID
            FROM   [{databaseOwner}].[{objectQualifier}UserGroup] R
            JOIN   [{databaseOwner}].[{objectQualifier}Group]     G ON R.GroupID = G.GroupID
            JOIN   [{databaseOwner}].[{objectQualifier}user]      Y ON R.UserID = Y.UserID
            JOIN   [{databaseOwner}].[Users]         U ON U.UserName = Y.Name AND U.isSuperuser = 1
            WHERE G.Flags = 1 AND G.BoardID = @BoardID
           ) S ON T.UserID = S.UserID
    WHEN MATCHED AND T.Flags != 99 THEN UPDATE SET FLAGS = 99;

    -- PRINT N'Copy AF Forum Groups to YAF.Net Categories:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}category] T
    USING (SELECT * FROM [{databaseOwner}].[activeforums_Groups] WHERE ModuleID = @oModuleID) S ON T.BoardID = @BoardID AND T.Name = S.GroupName
    WHEN NOT MATCHED THEN INSERT ( BoardID,     [Name],              CategoryImage,   SortOrder, oGroupID)
                          VALUES (@BoardID,  GroupName,                       NULL, S.SortOrder, S.ForumGroupID);

    -- PRINT N'Copy AF Forums to YAF.Net Forums (parent forums):';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}forum] T
    USING (SELECT F.*,
                  Left(F.ForumName,  50) AS FName,
                  Left(F.ForumDesc, 255) AS FDesc,
                  DateAdd(n, @TZOffsetMin, F.LastPostDate) AS LPDate,
                  F.TotalTopics + F.TotalReplies AS TPosts,
                  C.CategoryID
            FROM  [{databaseOwner}].[activeforums_Forums] F
            JOIN   [{databaseOwner}].[{objectQualifier}category]        C ON F.ForumGroupID = C.oGroupID
            WHERE F.ParentForumID = 0
          ) S ON S.CategoryID = T.CategoryID AND T.Name = S.ForumName
    WHEN NOT MATCHED THEN INSERT (  CategoryID,   ParentID,  [Name], Description, SortOrder, LastPosted, LastTopicID, LastMessageID, LastUserID, LastUserName, LastUserDisplayName,     NumTopics, NumPosts, RemoteURL, Flags, ThemeURL, ImageURL, Styles, IsModeratedNewTopicOnly, oForumID)
                          VALUES (S.CategoryID,       Null, S.FName,     S.FDesc, S.SortOrder, S.LPDate,        Null,          Null,       Null,         Null,                Null, S.TotalTopics, S.TPosts,      Null,     4,     Null,     Null,   Null,                      0, S.ForumID);

    -- PRINT N'Copy AF Forums to YAF.Net Forums (child forums):';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}forum] T
    USING (SELECT F.*,
                  Left(F.ForumName,  50) AS FName,
                  Left(F.ForumDesc, 255) AS FDesc,
                  DateAdd(n, @TZOffsetMin, F.LastPostDate) AS LPDate,
                  F.TotalTopics + F.TotalReplies AS TPosts,
                  C.CategoryID,
                  Y.ForumID As ParentID
            FROM  [{databaseOwner}].[activeforums_Forums] F
            JOIN   [{databaseOwner}].[{objectQualifier}Category]        C ON F.ForumGroupID  = C.oGroupID
            JOIN   [{databaseOwner}].[{objectQualifier}Forum]           Y ON F.ParentForumID = Y.oForumID
          ) S ON S.CategoryID = T.CategoryID AND T.Name = S.ForumName
    WHEN NOT MATCHED THEN INSERT (  CategoryID,   ParentID,  [Name], Description, SortOrder, LastPosted, LastTopicID, LastMessageID, LastUserID, LastUserName, LastUserDisplayName,     NumTopics, NumPosts, RemoteURL, Flags, ThemeURL, ImageURL, Styles, IsModeratedNewTopicOnly, oForumID)
                          VALUES (S.CategoryID, S.ParentID, S.FName,     S.FDesc, S.SortOrder, S.LPDate,        Null,          Null,       Null,         Null,                Null, S.TotalTopics, S.TPosts,      Null,     4,     Null,     Null,   Null,                      0, S.ForumID);

    /* YAF Topic Flags: None = 0, IsLocked = 1, IsDeleted = 8, IsPersistent = 512, IsQuestion = 1024 */
    -- PRINT N'Create Threads:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}topic] T
    USING (SELECT T.*,
                  CASE T.StatusID WHEN 0 THEN N'INFORMATIC'
                                  WHEN 1 THEN N'QUESTION'
                                  WHEN 3 THEN N'SOLVED'
                                  ELSE N''
                  END AS YState,
                    CASE WHEN T.isDeleted = 1
                           OR C.IsDeleted = 1 THEN    8 ELSE 0 END
                  + CASE WHEN T.IsLocked  = 1 THEN    1 ELSE 0 END
                  + CASE WHEN T.StatusID  = 1 THEN 1024 ELSE 0 END AS YFlags,
                    CASE WHEN T.IsAnnounce= 1 THEN    2
                         WHEN T.IsPinned  = 1 THEN    1
                                              ELSE    0 END AS YPrio,
                  DateAdd(n, @TZOffsetMin, DateAdd(ss, X.LastTopicDate, '01/01/1970 00:00:00 AM')) AS LastTopicDate, -- TZ shifted
                  DateAdd(n, @TZOffsetMin, DateAdd(ss, X.LastReplyDate, '01/01/1970 00:00:00 AM')) AS LastReplyDate, -- TZ shifted
                  X.LastReplyID,
                  Y1.UserID      AS AuthorID,
                  Y1.DisplayName AS AuthorName,
                  Left(C.Subject, 100) AS Subject,
                  DateAdd(n, @TZOffsetMin, C.DateCreated)   AS DateCreated,  -- TZ shifted
                  Left(C.Summary, 255) as Summary,
                  C.Body,
                  IsNull(Y2.UserID, Y1.UserID)      AS RAuthorID,
                  IsNull(Y2.DisplayName, Y1.DisplayName) AS RAuthorName,
                  0 AS NumPosts,
                  F.ForumID AS YForumID,
                  F.oForumID
            FROM      [{databaseOwner}].[ActiveForums_Content]     C
            JOIN      [{databaseOwner}].[ActiveForums_Topics]      T ON T.ContentID = C.ContentID
            JOIN      [{databaseOwner}].[ActiveForums_ForumTopics] X ON T.TopicID   = X.TopicID
            JOIN      [{databaseOwner}].[{objectQualifier}forum]                F ON X.Forumid   = F.oForumID
            JOIN      [{databaseOwner}].[Users]                   U1 ON C.AuthorID  = U1.UserID
            JOIN      [{databaseOwner}].[{objectQualifier}User]                Y1 ON U1.UserID = Y1.ProviderUserKey AND Y1.BoardID = @BoardID
            LEFT JOIN [{databaseOwner}].[ActiveForums_Replies]     R ON R.ReplyID   = X.LastReplyID
            LEFT JOIN [{databaseOwner}].[ActiveForums_Content]     A ON R.ContentID = A.ContentID
            LEFT JOIN [{databaseOwner}].[Users]                   U2 ON A.AuthorID  = U2.UserID
            LEFT JOIN [{databaseOwner}].[{objectQualifier}User]                Y2 ON U2.UserID = Y2.ProviderUserKey AND Y2.BoardID = @BoardID
            -- WHERE C.isDeleted = 0 AND T.IsDeleted = 0
          ) S ON T.oTopicID = S.TopicID
    WHEN NOT MATCHED THEN INSERT (   ForumID,     UserID, UserName, UserDisplayName,        Posted,     Topic, Description, Status,   Styles, LinkDate,       Views, Priority, PollID, TopicMovedID,      LastPosted, LastMessageID,  LastUserID, LastUserName, LastUserDisplayName,  NumPosts,  Flags, AnswerMessageId, LastMessageFlags, TopicImage, oTopicID)
                          VALUES (S.YForumID, S.AuthorID,     Null,    S.AuthorName, S.DateCreated, S.Subject,   S.Summary, YState,      N'',     Null, S.ViewCount,    YPrio,   Null,         Null, S.LastReplyDate,          Null, S.RAuthorID,         Null,       S.RAuthorName,         0, YFlags,            Null,              0,       Null,  TopicID);

    /* YAF MessageFlags: IsHtml = 1, IsBBCode = 2, IsSmilies = 4, IsDeleted = 8, IsApproved = 16, IsLocked = 32, NotFormatted = 64, IsReported = 128, IsPersistant = 512 */
    -- PRINT N'Copy Initial Posts:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}Message] T
    USING (SELECT C.ContentID,
                  512 + 4 -- persistant and smilies allowed
                  + 1     -- containes HTML, else + 2
                  + CASE WHEN R.isDeleted  = 1
                           OR C.IsDeleted  = 1 THEN    8 ELSE 0 END
                  + CASE WHEN R.IsApproved = 1 THEN   16 ELSE 0 END
                  + CASE WHEN R.IsLocked   = 1 THEN   32 ELSE 0 END AS YFlags,
                  Y.UserID      AS AuthorID,
                  Y.DisplayName AS AuthorName,
                  C.Body,
                  DateAdd(n, @TZOffsetMin, C.DateCreated) AS DateCreated,-- TZ shifted
                  C.IPAddress,
                  T.TopicID
            FROM  [{databaseOwner}].[ActiveForums_Topics]  R
            JOIN  [{databaseOwner}].[ActiveForums_Content] C ON R.ContentID  = C.ContentID
            JOIN  [{databaseOwner}].[{objectQualifier}Topic]            T ON R.TopicID    = T.oTopicID
            JOIN  [{databaseOwner}].[Users]                U ON C.AuthorID   = U.UserID
            JOIN  [{databaseOwner}].[{objectQualifier}User]             Y ON U.UserName   = Y.Name AND Y.BoardID = @BoardID
          ) S ON T.oContentID = S.ContentID
    WHEN NOT MATCHED THEN INSERT (  TopicID, ReplyTo, Position, Indent,     UserID, UserName, UserDisplayName,        Posted, Message,          IP, Edited,  Flags, EditReason, IsModeratorChanged, DeleteReason, ExternalMessageId, ReferenceMessageId, EditedBy,  oContentID)
                          VALUES (S.TopicID,    Null,        0,      0, S.AuthorID,     Null,    S.AuthorName, S.DateCreated,  S.Body, S.IPAddress,   Null, YFlags,       Null,                  0,         Null,              Null,               Null,     Null, S.ContentID);

    -- PRINT N'Copy Replies:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}Message] T
    USING (SELECT C.ContentID,
                  512 + 4 -- persistant and smilies allowed
                  + 1     -- containes HTML, else + 2
                  + CASE WHEN R.isDeleted  = 1 THEN    8 ELSE 0 END
                  + CASE WHEN R.IsApproved = 1 THEN   16 ELSE 0 END
                  + CASE WHEN X.IsLocked   = 1 THEN   32 ELSE 0 END AS YFlags,
                  Y.UserID      AS AuthorID,
                  Y.DisplayName AS AuthorName,
                  C.Subject,
                  C.Body,
                  DateAdd(n, @TZOffsetMin, C.DateCreated) AS DateCreated,-- TZ shifted
                  C.IPAddress,
                  T.TopicID,
                  M.MessageID
            FROM  [{databaseOwner}].[ActiveForums_Replies] R
            JOIN  [{databaseOwner}].[ActiveForums_Content] C ON R.ContentID  = C.ContentID
            JOIN  [{databaseOwner}].[ActiveForums_Topics]  X ON R.TopicID    = X.TopicID
            JOIN  [{databaseOwner}].[{objectQualifier}Topic]                             T ON R.TopicID    = T.oTopicID
            JOIN  [{databaseOwner}].[{objectQualifier}Message]                           M ON T.TopicID    = M.TopicID
            JOIN  [{databaseOwner}].[Users]                U ON C.AuthorID   = U.UserID
            JOIN  [{databaseOwner}].[{objectQualifier}User]                              Y ON U.UserName   = Y.Name AND Y.BoardID = @BoardID
          ) S ON T.oContentID = S.ContentID
    WHEN NOT MATCHED THEN INSERT (  TopicID,     ReplyTo, Position, Indent,     UserID, UserName, UserDisplayName,        Posted, Message,          IP, Edited,  Flags, EditReason, IsModeratorChanged, DeleteReason, ExternalMessageId, ReferenceMessageId, EditedBy,  oContentID)
                          VALUES (S.TopicID, S.MessageID,        1,      1, S.AuthorID,     Null,    S.AuthorName, S.DateCreated,  S.Body, S.IPAddress,   Null, YFlags,       Null,                  0,         Null,              Null,               Null,     Null, S.ContentID);

    -- PRINT N'Copy Attachments:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}Attachment] T
    USING (SELECT A.Filename,
                  A.FileData,
                  A.ContentType,
                  A.FileSize,
                  Y.UserID,
                  M.MessageID
            FROM  [{databaseOwner}].[activeforums_Attachments] A
            JOIN  [{databaseOwner}].[{objectQualifier}Message]                               M ON A.ContentID = M.oContentID
            JOIN  [{databaseOwner}].[Users]                    U ON A.UserID = U.UserID
            JOIN  [{databaseOwner}].[{objectQualifier}User]                                  Y ON U.UserName = Y.Name
          ) S ON T.FileName = S.FileName AND T.MessageID = S.MessageID
    WHEN NOT MATCHED THEN INSERT (  MessageID,   UserID,   FileName,      Bytes,   ContentType, Downloads, FileData)
                          VALUES (S.MessageID, S.USerID, S.FileName, S.FileSize, S.ContentType,       0, S.FileData);

    -- PRINT N'Update Thread Statistics:'
    UPDATE T
     SET   NumPosts         = N,
           LastMessageID    = MaxID,
           LastMessageFlags = 534
    FROM  [{databaseOwner}].[{objectQualifier}Topic] T
    JOIN (SELECT TopicID, Count(1) N, Max(MessageID) MaxID FROM  [{databaseOwner}].[{objectQualifier}Message] GROUP BY TopicID) M ON T.TopicID = M.TopicID
    WHERE NumPosts = 0

    -- PRINT N'Copy Forum Subscriptions (yaf_Watch):';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}WatchForum] T
    USING (SELECT Y.UserID,
                  N.ForumID,
                  DateAdd(n, @TZOffsetMin, Max(F.LastAccessDate)) AS LastAccessDate -- TZ shifted
            FROM [{databaseOwner}].[ActiveForums_Forums_Tracking] F
            JOIN [{databaseOwner}].[{objectQualifier}Forum]                    N On F.ForumID  = N.oForumID
            JOIN [{databaseOwner}].[Users]                        U On F.UserID   = U.UserID
            JOIN  [{databaseOwner}].[{objectQualifier}User]                     Y on U.UserName = Y.Name AND Y.BoardID = @BoardID
            GROUP BY Y.UserID, N.ForumID
          ) S ON T.ForumID = S.ForumID and T.UserID = S.UserID
    WHEN NOT MATCHED THEN INSERT (  ForumID,   UserID,          Created,     LastMail)
                          VALUES (S.ForumID, S.UserID, S.LastAccessDate, GetUTCDate());

    -- PRINT N'Copy Topic Subscriptions (yaf_Watch):';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}WatchTopic] T
    USING (SELECT Y.UserID,
                  N.TopicID,
                  DateAdd(n, @TZOffsetMin, Max(F.DateAdded)) AS DateAdded  -- TZ shifted
            FROM [{databaseOwner}].[ActiveForums_Topics_Tracking] F
            JOIN [{databaseOwner}].[{objectQualifier}Topic]                                     N On F.TopicID  = N.oTopicID
            JOIN [{databaseOwner}].[Users]                        U On F.UserID   = U.UserID
            JOIN [{databaseOwner}].[{objectQualifier}User]                                      Y on u.UserName = Y.Name AND Y.BoardID = @BoardID
            GROUP BY Y.UserID, N.TopicID
          ) S ON T.TopicID = S.TopicID and T.UserID = S.UserID
    WHEN NOT MATCHED THEN INSERT (  TopicID,   UserID,     Created,     LastMail)
                          VALUES (S.TopicID, S.UserID, S.DateAdded, GetUTCDate());

    -- PRINT N'Create Admin Access to Forums:';
    MERGE INTO  [{databaseOwner}].[{objectQualifier}ForumAccess] T
    USING (SELECT GroupID, ForumID, M.AccessMaskID
            FROM   [{databaseOwner}].[{objectQualifier}AccessMask] M
            JOIN   [{databaseOwner}].[{objectQualifier}Group]      G ON M.BoardID = G.BoardID AND G.Flags = 1
            JOIN   [{databaseOwner}].[{objectQualifier}Category]   C ON M.BoardID = C.BoardID AND M.Flags = 2047
            JOIN   [{databaseOwner}].[{objectQualifier}Forum]     F ON F.CategoryID = C.CategoryID
            WHERE M.BoardID = @BoardID
          ) S ON T.ForumID = S.ForumID AND T.GroupID = S.GroupID
    WHEN NOT MATCHED THEN INSERT (  GroupID,   ForumID,   AccessMaskID)
                        VALUES (S.GroupID, S.ForumID, S.AccessMaskID);

    -- Copy group & forum permission // skipped due to incompatible Permission format, please set manually
    -- AF Stores each permission for each forum and group as String of format N'0;13;|1134;||'
    -- | is delimiter for main parts:  RoleIDs Granted | UserIDs granted | SocialGroupID Owners granted | ?
    -- each part is a list of id's delimited by ; or :

END TRY

BEGIN CATCH
    -- PRINT N'Error '    + CAST(ERROR_NUMBER() AS nVarChar(11)) + N' in Line ' + CAST(ERROR_LINE()   AS nVarChar(11)) + N': ' + ERROR_MESSAGE();
    Rollback TRANSACTION
END CATCH

IF @@TRANCOUNT > 0  BEGIN
    COMMIT TRANSACTION
    -- PRINT N'*** Migration completed ***';
END

end
GO