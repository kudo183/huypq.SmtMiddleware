GO
/****** Object:  Table [dbo].[SmtDeletedItem]    Script Date: 4/29/2017 3:11:22 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SmtDeletedItem](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[DeletedID] [int] NOT NULL,
	[TableID] [int] NOT NULL,
	[CreateTime] [bigint] NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[TenantID] [int] NOT NULL,
 CONSTRAINT [PK_SmtDeletedItems] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SmtTable]    Script Date: 4/29/2017 3:11:22 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SmtTable](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[TableName] [varchar](50) NOT NULL,
 CONSTRAINT [PK_SmtTables] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SmtTenant]    Script Date: 4/29/2017 3:11:22 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SmtTenant](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[Email] [varchar](256) NOT NULL,
	[PasswordHash] [varchar](128) NOT NULL,
	[TenantName] [varchar](256) NOT NULL,
	[TokenValidTime] [bigint] NOT NULL,
	[LastUpdateTime] [bigint] NOT NULL,
	[IsConfirmed] [bit] NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[CreateTime] [bigint] NOT NULL,
 CONSTRAINT [PK_SmtTenant] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SmtUser]    Script Date: 4/29/2017 3:11:22 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SmtUser](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime2](7) NOT NULL,
	[Email] [varchar](256) NOT NULL,
	[PasswordHash] [varchar](128) NOT NULL,
	[UserName] [varchar](256) NOT NULL,
	[TenantID] [int] NOT NULL,
	[TokenValidTime] [bigint] NOT NULL,
	[LastUpdateTime] [bigint] NOT NULL,
	[IsConfirmed] [bit] NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[CreateTime] [bigint] NOT NULL,
 CONSTRAINT [PK_SmtUser] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[SmtUserClaim]    Script Date: 4/29/2017 3:11:22 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SmtUserClaim](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[Claim] [varchar](256) NOT NULL,
	[TenantID] [int] NOT NULL,
	[LastUpdateTime] [bigint] NOT NULL,
	[CreateTime] [bigint] NOT NULL,
 CONSTRAINT [PK_SmtUserClaim] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

/****** Object:  Table [dbo].[SmtFile]    Script Date: 06/09/2017 10:41:09 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SmtFile](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[TenantID] [int] NOT NULL,
	[FileName] [nvarchar](256) NOT NULL,
	[FileSize] [bigint] NOT NULL,
	[MimeType] [varchar](128) NOT NULL,
	[CreateTime] [bigint] NOT NULL,
	[LastUpdateTime] [bigint] NOT NULL,
 CONSTRAINT [PK_SmtFile] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

GO
ALTER TABLE [dbo].[SmtDeletedItem] ADD  CONSTRAINT [DF_SmtDeletedItems_CreateDate]  DEFAULT (getutcdate()) FOR [CreateDate]
GO
ALTER TABLE [dbo].[SmtTenant] ADD  CONSTRAINT [DF_SmtTenant_LastUpdateTime]  DEFAULT ((0)) FOR [LastUpdateTime]
GO
ALTER TABLE [dbo].[SmtTenant] ADD  CONSTRAINT [DF_SmtTenant_IsConfirmed]  DEFAULT ((0)) FOR [IsConfirmed]
GO
ALTER TABLE [dbo].[SmtTenant] ADD  CONSTRAINT [DF_SmtTenant_IsLocked]  DEFAULT ((0)) FOR [IsLocked]
GO
ALTER TABLE [dbo].[SmtUser] ADD  CONSTRAINT [DF_SmtUser_LastUpdateTime]  DEFAULT ((0)) FOR [LastUpdateTime]
GO
ALTER TABLE [dbo].[SmtUser] ADD  CONSTRAINT [DF_SmtUser_IsConfirmed]  DEFAULT ((0)) FOR [IsConfirmed]
GO
ALTER TABLE [dbo].[SmtUser] ADD  CONSTRAINT [DF_SmtUser_IsLocked]  DEFAULT ((0)) FOR [IsLocked]
GO
ALTER TABLE [dbo].[SmtUserClaim] ADD  CONSTRAINT [DF_SmtUserClaim_LastUpdateTime]  DEFAULT ((0)) FOR [LastUpdateTime]
GO
ALTER TABLE [dbo].[SmtUser]  WITH CHECK ADD  CONSTRAINT [FK_SmtUser_SmtTenant] FOREIGN KEY([TenantID])
REFERENCES [dbo].[SmtTenant] ([ID])
GO
ALTER TABLE [dbo].[SmtUser] CHECK CONSTRAINT [FK_SmtUser_SmtTenant]
GO
ALTER TABLE [dbo].[SmtUserClaim]  WITH CHECK ADD  CONSTRAINT [FK_SmtUserClaim_SmtTenant] FOREIGN KEY([TenantID])
REFERENCES [dbo].[SmtTenant] ([ID])
GO
ALTER TABLE [dbo].[SmtUserClaim] CHECK CONSTRAINT [FK_SmtUserClaim_SmtTenant]
GO
ALTER TABLE [dbo].[SmtUserClaim]  WITH CHECK ADD  CONSTRAINT [FK_SmtUserClaim_SmtUserClaim] FOREIGN KEY([UserID])
REFERENCES [dbo].[SmtUser] ([ID])
GO
ALTER TABLE [dbo].[SmtUserClaim] CHECK CONSTRAINT [FK_SmtUserClaim_SmtUserClaim]
GO
