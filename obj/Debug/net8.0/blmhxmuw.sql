IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'00000000000000_CreateIdentitySchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'00000000000000_CreateIdentitySchema', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208113840_IdentityDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260208113840_IdentityDetails', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209034110_AddDepartmentTable'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209034110_AddDepartmentTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209034110_AddDepartmentTable', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209034602_secondmigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209034602_secondmigration', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209035300_AddPositionAndEmployeeTables'
)
BEGIN
    CREATE TABLE [Positions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_Positions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209035300_AddPositionAndEmployeeTables'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [EmployeeNumber] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [MiddleName] nvarchar(max) NULL,
        [LastName] nvarchar(max) NOT NULL,
        [DepartmentId] int NOT NULL,
        [PositionId] int NOT NULL,
        [DateHired] datetime2 NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Employees_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209035300_AddPositionAndEmployeeTables'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209035300_AddPositionAndEmployeeTables'
)
BEGIN
    CREATE INDEX [IX_Employees_PositionId] ON [Employees] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209035300_AddPositionAndEmployeeTables'
)
BEGIN
    CREATE INDEX [IX_Employees_UserId] ON [Employees] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209035300_AddPositionAndEmployeeTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209035300_AddPositionAndEmployeeTables', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209035414_thirdmigartion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209035414_thirdmigartion', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210031847_BatchMigration'
)
BEGIN
    CREATE TABLE [DepartmentHeads] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(50) NOT NULL,
        [MiddleName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [DepartmentId] int NOT NULL,
        [Contact] nvarchar(20) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_DepartmentHeads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DepartmentHeads_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210031847_BatchMigration'
)
BEGIN
    CREATE TABLE [Executives] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(50) NOT NULL,
        [MiddleName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [Contact] nvarchar(20) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Executives] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210031847_BatchMigration'
)
BEGIN
    CREATE TABLE [HumanResources] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(50) NOT NULL,
        [MiddleName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [Contact] nvarchar(20) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_HumanResources] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210031847_BatchMigration'
)
BEGIN
    CREATE TABLE [ProjectManagers] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(50) NOT NULL,
        [MiddleName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [DepartmentId] int NOT NULL,
        [Contact] nvarchar(20) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ProjectManagers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectManagers_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210031847_BatchMigration'
)
BEGIN
    CREATE INDEX [IX_DepartmentHeads_DepartmentId] ON [DepartmentHeads] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210031847_BatchMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectManagers_DepartmentId] ON [ProjectManagers] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210031847_BatchMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260210031847_BatchMigration', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [PositionId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [PositionId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [Executives] ADD [PositionId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [PositionId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    CREATE INDEX [IX_ProjectManagers_PositionId] ON [ProjectManagers] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    CREATE INDEX [IX_HumanResources_PositionId] ON [HumanResources] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    CREATE INDEX [IX_Executives_PositionId] ON [Executives] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    CREATE INDEX [IX_DepartmentHeads_PositionId] ON [DepartmentHeads] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD CONSTRAINT [FK_DepartmentHeads_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [Executives] ADD CONSTRAINT [FK_Executives_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [HumanResources] ADD CONSTRAINT [FK_HumanResources_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD CONSTRAINT [FK_ProjectManagers_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [Positions] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211095507_Changes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211095507_Changes', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211100412_AddPositionFieldsToRoleModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211100412_AddPositionFieldsToRoleModels', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112258_AddedEmployeeNumber'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [EmployeeNumber] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112258_AddedEmployeeNumber'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [EmployeeNumber] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112258_AddedEmployeeNumber'
)
BEGIN
    ALTER TABLE [Executives] ADD [EmployeeNumber] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112258_AddedEmployeeNumber'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [EmployeeNumber] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112258_AddedEmployeeNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211112258_AddedEmployeeNumber', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112722_RulesApplied'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [DepartmentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112722_RulesApplied'
)
BEGIN
    ALTER TABLE [Executives] ADD [DepartmentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112722_RulesApplied'
)
BEGIN
    CREATE INDEX [IX_HumanResources_DepartmentId] ON [HumanResources] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112722_RulesApplied'
)
BEGIN
    CREATE INDEX [IX_Executives_DepartmentId] ON [Executives] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112722_RulesApplied'
)
BEGIN
    ALTER TABLE [Executives] ADD CONSTRAINT [FK_Executives_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112722_RulesApplied'
)
BEGIN
    ALTER TABLE [HumanResources] ADD CONSTRAINT [FK_HumanResources_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211112722_RulesApplied'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211112722_RulesApplied', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211114455_FixEmployeeNumberAndNullableFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211114455_FixEmployeeNumberAndNullableFields', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211114637_RulesApplieddd'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211114637_RulesApplieddd', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211122020_RulesApplieddddd'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProjectManagers]') AND [c].[name] = N'EmployeeNumber');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ProjectManagers] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [ProjectManagers] ALTER COLUMN [EmployeeNumber] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211122020_RulesApplieddddd'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HumanResources]') AND [c].[name] = N'EmployeeNumber');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [HumanResources] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [HumanResources] ALTER COLUMN [EmployeeNumber] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211122020_RulesApplieddddd'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Executives]') AND [c].[name] = N'EmployeeNumber');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Executives] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Executives] ALTER COLUMN [EmployeeNumber] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211122020_RulesApplieddddd'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DepartmentHeads]') AND [c].[name] = N'EmployeeNumber');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [DepartmentHeads] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [DepartmentHeads] ALTER COLUMN [EmployeeNumber] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260211122020_RulesApplieddddd'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260211122020_RulesApplieddddd', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213021417_AddProjectProposalTable'
)
BEGIN
    CREATE TABLE [ProjectProposals] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [FileAttachment] nvarchar(255) NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_ProjectProposals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectProposals_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213021417_AddProjectProposalTable'
)
BEGIN
    CREATE INDEX [IX_ProjectProposals_EmployeeId] ON [ProjectProposals] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213021417_AddProjectProposalTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213021417_AddProjectProposalTable', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213021728_AddStatusToProjectProposal'
)
BEGIN
    ALTER TABLE [ProjectProposals] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213021728_AddStatusToProjectProposal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213021728_AddStatusToProjectProposal', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213024231_ProjectProposal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213024231_ProjectProposal', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213123559_ToLiveDatabase'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213123559_ToLiveDatabase', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216113023_Changes_to_the_ProjectProposal'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProjectProposals]') AND [c].[name] = N'EndDate');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ProjectProposals] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [ProjectProposals] DROP COLUMN [EndDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216113023_Changes_to_the_ProjectProposal'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProjectProposals]') AND [c].[name] = N'FileAttachment');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ProjectProposals] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [ProjectProposals] DROP COLUMN [FileAttachment];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216113023_Changes_to_the_ProjectProposal'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProjectProposals]') AND [c].[name] = N'StartDate');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [ProjectProposals] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [ProjectProposals] DROP COLUMN [StartDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216113023_Changes_to_the_ProjectProposal'
)
BEGIN
    EXEC sp_rename N'[ProjectProposals].[Description]', N'Input', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216113023_Changes_to_the_ProjectProposal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216113023_Changes_to_the_ProjectProposal', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216125350_Added_attributes_to_PropjectProposal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216125350_Added_attributes_to_PropjectProposal', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216125532_Added_attributes_to_PropjectProposall'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216125532_Added_attributes_to_PropjectProposall', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216125820_Added_attributes_to_PropjectProposalll'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216125820_Added_attributes_to_PropjectProposalll', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216130418_Added_attributes_to_PropjectProposallll'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216130418_Added_attributes_to_PropjectProposallll', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216135000_AddDepartmentHeadAndNoteToProjectProposal'
)
BEGIN
    ALTER TABLE [ProjectProposals] ADD [DepartmentHeadId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216135000_AddDepartmentHeadAndNoteToProjectProposal'
)
BEGIN
    ALTER TABLE [ProjectProposals] ADD [Note] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216135000_AddDepartmentHeadAndNoteToProjectProposal'
)
BEGIN
    CREATE INDEX [IX_ProjectProposals_DepartmentHeadId] ON [ProjectProposals] ([DepartmentHeadId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216135000_AddDepartmentHeadAndNoteToProjectProposal'
)
BEGIN
    ALTER TABLE [ProjectProposals] ADD CONSTRAINT [FK_ProjectProposals_DepartmentHeads_DepartmentHeadId] FOREIGN KEY ([DepartmentHeadId]) REFERENCES [DepartmentHeads] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216135000_AddDepartmentHeadAndNoteToProjectProposal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216135000_AddDepartmentHeadAndNoteToProjectProposal', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE TABLE [Milestones] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_Milestones] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE TABLE [ProjectRoles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_ProjectRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE TABLE [Projects] (
        [Id] int NOT NULL IDENTITY,
        [ProjectProposalId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [ProjectManagerId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Projects_ProjectManagers_ProjectManagerId] FOREIGN KEY ([ProjectManagerId]) REFERENCES [ProjectManagers] ([Id]),
        CONSTRAINT [FK_Projects_ProjectProposals_ProjectProposalId] FOREIGN KEY ([ProjectProposalId]) REFERENCES [ProjectProposals] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE TABLE [Members] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [ProjectRoleId] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_Members] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Members_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_Members_ProjectRoles_ProjectRoleId] FOREIGN KEY ([ProjectRoleId]) REFERENCES [ProjectRoles] ([Id]),
        CONSTRAINT [FK_Members_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE TABLE [ProjectMilestones] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [MilestoneId] int NOT NULL,
        [SequenceOrder] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_ProjectMilestones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectMilestones_Milestones_MilestoneId] FOREIGN KEY ([MilestoneId]) REFERENCES [Milestones] ([Id]),
        CONSTRAINT [FK_ProjectMilestones_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE TABLE [ProjectTasks] (
        [Id] int NOT NULL IDENTITY,
        [ProjectMilestoneId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Input] nvarchar(max) NOT NULL,
        [Instructions] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [Status] nvarchar(20) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [ProjectManagerId] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_ProjectTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectTasks_ProjectManagers_ProjectManagerId] FOREIGN KEY ([ProjectManagerId]) REFERENCES [ProjectManagers] ([Id]),
        CONSTRAINT [FK_ProjectTasks_ProjectMilestones_ProjectMilestoneId] FOREIGN KEY ([ProjectMilestoneId]) REFERENCES [ProjectMilestones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE TABLE [TaskMembers] (
        [Id] int NOT NULL IDENTITY,
        [ProjectTaskId] int NOT NULL,
        [MemberId] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskMembers_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]),
        CONSTRAINT [FK_TaskMembers_ProjectTasks_ProjectTaskId] FOREIGN KEY ([ProjectTaskId]) REFERENCES [ProjectTasks] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_Members_EmployeeId] ON [Members] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_Members_ProjectId] ON [Members] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_Members_ProjectRoleId] ON [Members] ([ProjectRoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_MilestoneId] ON [ProjectMilestones] ([MilestoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_ProjectId] ON [ProjectMilestones] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_Projects_ProjectManagerId] ON [Projects] ([ProjectManagerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_Projects_ProjectProposalId] ON [Projects] ([ProjectProposalId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_ProjectTasks_ProjectManagerId] ON [ProjectTasks] ([ProjectManagerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_ProjectTasks_ProjectMilestoneId] ON [ProjectTasks] ([ProjectMilestoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_TaskMembers_MemberId] ON [TaskMembers] ([MemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    CREATE INDEX [IX_TaskMembers_ProjectTaskId] ON [TaskMembers] ([ProjectTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217034656_NewlyAddedTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217034656_NewlyAddedTables', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217035258_FixProjectForeignKeyDeleteBehavior'
)
BEGIN
    ALTER TABLE [Projects] DROP CONSTRAINT [FK_Projects_ProjectProposals_ProjectProposalId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217035258_FixProjectForeignKeyDeleteBehavior'
)
BEGIN
    ALTER TABLE [Projects] ADD CONSTRAINT [FK_Projects_ProjectProposals_ProjectProposalId] FOREIGN KEY ([ProjectProposalId]) REFERENCES [ProjectProposals] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217035258_FixProjectForeignKeyDeleteBehavior'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217035258_FixProjectForeignKeyDeleteBehavior', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Members] DROP CONSTRAINT [FK_Members_Employees_EmployeeId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Members] DROP CONSTRAINT [FK_Members_ProjectRoles_ProjectRoleId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Members] DROP CONSTRAINT [FK_Members_Projects_ProjectId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectMilestones] DROP CONSTRAINT [FK_ProjectMilestones_Milestones_MilestoneId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectMilestones] DROP CONSTRAINT [FK_ProjectMilestones_Projects_ProjectId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Projects] DROP CONSTRAINT [FK_Projects_ProjectManagers_ProjectManagerId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectTasks] DROP CONSTRAINT [FK_ProjectTasks_ProjectManagers_ProjectManagerId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectTasks] DROP CONSTRAINT [FK_ProjectTasks_ProjectMilestones_ProjectMilestoneId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [TaskMembers] DROP CONSTRAINT [FK_TaskMembers_Members_MemberId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [TaskMembers] DROP CONSTRAINT [FK_TaskMembers_ProjectTasks_ProjectTaskId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProjectTasks]') AND [c].[name] = N'Input');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ProjectTasks] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [ProjectTasks] ALTER COLUMN [Input] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Members] ADD CONSTRAINT [FK_Members_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Members] ADD CONSTRAINT [FK_Members_ProjectRoles_ProjectRoleId] FOREIGN KEY ([ProjectRoleId]) REFERENCES [ProjectRoles] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Members] ADD CONSTRAINT [FK_Members_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectMilestones] ADD CONSTRAINT [FK_ProjectMilestones_Milestones_MilestoneId] FOREIGN KEY ([MilestoneId]) REFERENCES [Milestones] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectMilestones] ADD CONSTRAINT [FK_ProjectMilestones_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [Projects] ADD CONSTRAINT [FK_Projects_ProjectManagers_ProjectManagerId] FOREIGN KEY ([ProjectManagerId]) REFERENCES [ProjectManagers] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectTasks] ADD CONSTRAINT [FK_ProjectTasks_ProjectManagers_ProjectManagerId] FOREIGN KEY ([ProjectManagerId]) REFERENCES [ProjectManagers] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [ProjectTasks] ADD CONSTRAINT [FK_ProjectTasks_ProjectMilestones_ProjectMilestoneId] FOREIGN KEY ([ProjectMilestoneId]) REFERENCES [ProjectMilestones] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [TaskMembers] ADD CONSTRAINT [FK_TaskMembers_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    ALTER TABLE [TaskMembers] ADD CONSTRAINT [FK_TaskMembers_ProjectTasks_ProjectTaskId] FOREIGN KEY ([ProjectTaskId]) REFERENCES [ProjectTasks] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217110719_Input_nullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217110719_Input_nullable', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217135623_ProjectProposalVersion'
)
BEGIN
    CREATE TABLE [ProjectProposalVersions] (
        [Id] int NOT NULL IDENTITY,
        [ProjectProposalId] int NOT NULL,
        [VersionNumber] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Input] nvarchar(max) NOT NULL,
        [EmployeeId] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_ProjectProposalVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectProposalVersions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]),
        CONSTRAINT [FK_ProjectProposalVersions_ProjectProposals_ProjectProposalId] FOREIGN KEY ([ProjectProposalId]) REFERENCES [ProjectProposals] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217135623_ProjectProposalVersion'
)
BEGIN
    CREATE INDEX [IX_ProjectProposalVersions_EmployeeId] ON [ProjectProposalVersions] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217135623_ProjectProposalVersion'
)
BEGIN
    CREATE INDEX [IX_ProjectProposalVersions_ProjectProposalId] ON [ProjectProposalVersions] ([ProjectProposalId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260217135623_ProjectProposalVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260217135623_ProjectProposalVersion', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220051411_ProposalNoteVersion_ModifiedProjectProposal'
)
BEGIN
    ALTER TABLE [ProjectProposals] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220051411_ProposalNoteVersion_ModifiedProjectProposal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220051411_ProposalNoteVersion_ModifiedProjectProposal', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220051923_ProposalNoteVersion'
)
BEGIN
    CREATE TABLE [ProposalNoteVersions] (
        [Id] int NOT NULL IDENTITY,
        [ProjectProposalId] int NOT NULL,
        [VersionNumber] int NOT NULL,
        [Note] nvarchar(max) NULL,
        [DepartmentHeadId] int NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_ProposalNoteVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProposalNoteVersions_DepartmentHeads_DepartmentHeadId] FOREIGN KEY ([DepartmentHeadId]) REFERENCES [DepartmentHeads] ([Id]),
        CONSTRAINT [FK_ProposalNoteVersions_ProjectProposals_ProjectProposalId] FOREIGN KEY ([ProjectProposalId]) REFERENCES [ProjectProposals] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220051923_ProposalNoteVersion'
)
BEGIN
    CREATE INDEX [IX_ProposalNoteVersions_DepartmentHeadId] ON [ProposalNoteVersions] ([DepartmentHeadId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220051923_ProposalNoteVersion'
)
BEGIN
    CREATE INDEX [IX_ProposalNoteVersions_ProjectProposalId] ON [ProposalNoteVersions] ([ProjectProposalId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260220051923_ProposalNoteVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220051923_ProposalNoteVersion', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260224034944_CompletedAt'
)
BEGIN
    ALTER TABLE [ProjectTasks] ADD [CompletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260224034944_CompletedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260224034944_CompletedAt', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226014432_task_versions'
)
BEGIN
    CREATE TABLE [ProjectTaskVersions] (
        [Id] int NOT NULL IDENTITY,
        [ProjectTaskId] int NOT NULL,
        [Input] nvarchar(max) NULL,
        [TaskMemberId] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_ProjectTaskVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectTaskVersions_ProjectTasks_ProjectTaskId] FOREIGN KEY ([ProjectTaskId]) REFERENCES [ProjectTasks] ([Id]),
        CONSTRAINT [FK_ProjectTaskVersions_TaskMembers_TaskMemberId] FOREIGN KEY ([TaskMemberId]) REFERENCES [TaskMembers] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226014432_task_versions'
)
BEGIN
    CREATE TABLE [TaskNoteVersions] (
        [Id] int NOT NULL IDENTITY,
        [ProjectTaskId] int NOT NULL,
        [Note] nvarchar(max) NULL,
        [ProjectManagerId] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskNoteVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskNoteVersions_ProjectManagers_ProjectManagerId] FOREIGN KEY ([ProjectManagerId]) REFERENCES [ProjectManagers] ([Id]),
        CONSTRAINT [FK_TaskNoteVersions_ProjectTasks_ProjectTaskId] FOREIGN KEY ([ProjectTaskId]) REFERENCES [ProjectTasks] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226014432_task_versions'
)
BEGIN
    CREATE INDEX [IX_ProjectTaskVersions_ProjectTaskId] ON [ProjectTaskVersions] ([ProjectTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226014432_task_versions'
)
BEGIN
    CREATE INDEX [IX_ProjectTaskVersions_TaskMemberId] ON [ProjectTaskVersions] ([TaskMemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226014432_task_versions'
)
BEGIN
    CREATE INDEX [IX_TaskNoteVersions_ProjectManagerId] ON [TaskNoteVersions] ([ProjectManagerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226014432_task_versions'
)
BEGIN
    CREATE INDEX [IX_TaskNoteVersions_ProjectTaskId] ON [TaskNoteVersions] ([ProjectTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226014432_task_versions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260226014432_task_versions', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226033309_AddVersionNumberToTaskVersions'
)
BEGIN
    ALTER TABLE [ProjectTaskVersions] ADD [VersionNumber] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226033309_AddVersionNumberToTaskVersions'
)
BEGIN
    ALTER TABLE [TaskNoteVersions] ADD [VersionNumber] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226033309_AddVersionNumberToTaskVersions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260226033309_AddVersionNumberToTaskVersions', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226121135_IsArchived'
)
BEGIN
    ALTER TABLE [Projects] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226121135_IsArchived'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260226121135_IsArchived', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228023452_AddAuditLogTable'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [UserName] nvarchar(200) NOT NULL,
        [Role] nvarchar(100) NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [Module] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [EntityType] nvarchar(200) NULL,
        [EntityId] nvarchar(200) NULL,
        [IpAddress] nvarchar(100) NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228023452_AddAuditLogTable'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228023452_AddAuditLogTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228023452_AddAuditLogTable', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228053302_AddNotificationsTable'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [RecipientId] nvarchar(450) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Icon] nvarchar(100) NULL,
        [Link] nvarchar(500) NULL,
        [Module] nvarchar(100) NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ReadAt] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_RecipientId] FOREIGN KEY ([RecipientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228053302_AddNotificationsTable'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientId] ON [Notifications] ([RecipientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228053302_AddNotificationsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228053302_AddNotificationsTable', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [AddressLine] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [Barangay] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [Province] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [Region] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [AddressLine] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [Barangay] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [Province] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [Region] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Executives] ADD [AddressLine] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Executives] ADD [Barangay] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Executives] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Executives] ADD [Province] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Executives] ADD [Region] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Employees] ADD [AddressLine] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Employees] ADD [Barangay] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Employees] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Employees] ADD [Province] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [Employees] ADD [Region] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [AddressLine] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [Barangay] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [Province] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [Region] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228063313_AddAddressFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228063313_AddAddressFields', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228064656_Nullable_MiddleNames'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProjectManagers]') AND [c].[name] = N'MiddleName');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ProjectManagers] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [ProjectManagers] ALTER COLUMN [MiddleName] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228064656_Nullable_MiddleNames'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HumanResources]') AND [c].[name] = N'MiddleName');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [HumanResources] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [HumanResources] ALTER COLUMN [MiddleName] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228064656_Nullable_MiddleNames'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Executives]') AND [c].[name] = N'MiddleName');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Executives] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Executives] ALTER COLUMN [MiddleName] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228064656_Nullable_MiddleNames'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DepartmentHeads]') AND [c].[name] = N'MiddleName');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [DepartmentHeads] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [DepartmentHeads] ALTER COLUMN [MiddleName] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228064656_Nullable_MiddleNames'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228064656_Nullable_MiddleNames', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228081335_AddDocumentTables'
)
BEGIN
    CREATE TABLE [Documents] (
        [Id] int NOT NULL IDENTITY,
        [OwnerEmployeeId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [IsArchived] bit NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        [LastModified] datetime2 NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Documents_Employees_OwnerEmployeeId] FOREIGN KEY ([OwnerEmployeeId]) REFERENCES [Employees] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228081335_AddDocumentTables'
)
BEGIN
    CREATE TABLE [DocumentCollaborators] (
        [Id] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [DateAdded] datetime2 NOT NULL,
        CONSTRAINT [PK_DocumentCollaborators] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentCollaborators_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentCollaborators_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228081335_AddDocumentTables'
)
BEGIN
    CREATE INDEX [IX_DocumentCollaborators_DocumentId] ON [DocumentCollaborators] ([DocumentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228081335_AddDocumentTables'
)
BEGIN
    CREATE INDEX [IX_DocumentCollaborators_EmployeeId] ON [DocumentCollaborators] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228081335_AddDocumentTables'
)
BEGIN
    CREATE INDEX [IX_Documents_OwnerEmployeeId] ON [Documents] ([OwnerEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228081335_AddDocumentTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228081335_AddDocumentTables', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228085132_AddDocumentVersionTable'
)
BEGIN
    CREATE TABLE [DocumentVersions] (
        [Id] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [VersionNumber] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [EmployeeId] int NOT NULL,
        [DateCreated] datetime2 NOT NULL,
        CONSTRAINT [PK_DocumentVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentVersions_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([Id]),
        CONSTRAINT [FK_DocumentVersions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228085132_AddDocumentVersionTable'
)
BEGIN
    CREATE INDEX [IX_DocumentVersions_DocumentId] ON [DocumentVersions] ([DocumentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228085132_AddDocumentVersionTable'
)
BEGIN
    CREATE INDEX [IX_DocumentVersions_EmployeeId] ON [DocumentVersions] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228085132_AddDocumentVersionTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228085132_AddDocumentVersionTable', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    CREATE TABLE [Conversations] (
        [Id] int NOT NULL IDENTITY,
        [IsGroup] bit NOT NULL,
        [GroupName] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [AttachmentUrl] nvarchar(500) NULL,
        [AttachmentType] nvarchar(100) NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ChatMessages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    CREATE TABLE [ConversationParticipants] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [JoinedAt] datetime2 NOT NULL,
        [LastReadAt] datetime2 NULL,
        CONSTRAINT [PK_ConversationParticipants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConversationParticipants_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ConversationParticipants_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ConversationId] ON [ChatMessages] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    CREATE INDEX [IX_ConversationParticipants_ConversationId] ON [ConversationParticipants] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    CREATE INDEX [IX_ConversationParticipants_UserId] ON [ConversationParticipants] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228093251_AddMessagingTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228093251_AddMessagingTables', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228113654_AddProfileImageToModels'
)
BEGIN
    ALTER TABLE [ProjectManagers] ADD [ProfileImage] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228113654_AddProfileImageToModels'
)
BEGIN
    ALTER TABLE [HumanResources] ADD [ProfileImage] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228113654_AddProfileImageToModels'
)
BEGIN
    ALTER TABLE [Executives] ADD [ProfileImage] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228113654_AddProfileImageToModels'
)
BEGIN
    ALTER TABLE [Employees] ADD [ProfileImage] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228113654_AddProfileImageToModels'
)
BEGIN
    ALTER TABLE [DepartmentHeads] ADD [ProfileImage] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260228113654_AddProfileImageToModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260228113654_AddProfileImageToModels', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303103927_AddCreatedByToConversation'
)
BEGIN
    ALTER TABLE [Conversations] ADD [CreatedByUserId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303103927_AddCreatedByToConversation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260303103927_AddCreatedByToConversation', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303112507_AddProjectStatus'
)
BEGIN
    ALTER TABLE [Projects] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'Unfinished';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303112507_AddProjectStatus'
)
BEGIN
    UPDATE Projects SET Status = 'Unfinished' WHERE Status = '' OR Status IS NULL
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260303112507_AddProjectStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260303112507_AddProjectStatus', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305040643_IsArchived_ProjectTask'
)
BEGIN
    ALTER TABLE [ProjectTasks] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305040643_IsArchived_ProjectTask'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260305040643_IsArchived_ProjectTask', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305084929_IsArchived_Milestone'
)
BEGIN
    ALTER TABLE [ProjectMilestones] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305084929_IsArchived_Milestone'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260305084929_IsArchived_Milestone', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306105123_AddIsArchived_Member'
)
BEGIN
    ALTER TABLE [Members] ADD [ArchivedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306105123_AddIsArchived_Member'
)
BEGIN
    ALTER TABLE [Members] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306105123_AddIsArchived_Member'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306105123_AddIsArchived_Member', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306135628_AddIsArchivedToMilestoneAndProjectRole'
)
BEGIN

        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Milestones]') AND name = 'IsArchived')
        BEGIN
            ALTER TABLE [Milestones] ADD [IsArchived] bit NOT NULL DEFAULT 0;
        END
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectRoles]') AND name = 'IsArchived')
        BEGIN
            ALTER TABLE [ProjectRoles] ADD [IsArchived] bit NOT NULL DEFAULT 0;
        END
        
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306135628_AddIsArchivedToMilestoneAndProjectRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306135628_AddIsArchivedToMilestoneAndProjectRole', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306143000_is_archived'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306143000_is_archived', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306150748_AddIsArchivedToDepartmentAndPosition'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306150748_AddIsArchivedToDepartmentAndPosition', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306215504_dept_pos_arch'
)
BEGIN
    ALTER TABLE [Positions] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306215504_dept_pos_arch'
)
BEGIN
    ALTER TABLE [Departments] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260306215504_dept_pos_arch'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260306215504_dept_pos_arch', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260309004909_Added_Contact'
)
BEGIN
    ALTER TABLE [Employees] ADD [Contact] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260309004909_Added_Contact'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260309004909_Added_Contact', N'8.0.23');
END;
GO

COMMIT;
GO

