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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE TABLE [BusinessSettings] (
        [Id] int NOT NULL IDENTITY,
        [BusinessName] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [GSTIN] nvarchar(max) NOT NULL,
        [TaxEnabled] bit NOT NULL,
        [DefaultGSTPercentage] decimal(5,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [BillPrefix] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_BusinessSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(450) NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [Discount] decimal(18,2) NOT NULL,
        [Tax] decimal(18,2) NOT NULL,
        [GrandTotal] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [CustomerName] nvarchar(max) NULL,
        [CustomerPhone] nvarchar(max) NULL,
        [CustomerEmail] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CategoryId] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [IsVegetarian] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsAvailable] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuItems_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [MenuItemId] int NOT NULL,
        [ItemName] nvarchar(max) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [IsVegetarian] bit NOT NULL DEFAULT CAST(1 AS bit),
        [GSTPercentage] decimal(5,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BillPrefix', N'BusinessName', N'Currency', N'DefaultGSTPercentage', N'GSTIN', N'Phone', N'TaxEnabled') AND [object_id] = OBJECT_ID(N'[BusinessSettings]'))
        SET IDENTITY_INSERT [BusinessSettings] ON;
    EXEC(N'INSERT INTO [BusinessSettings] ([Id], [Address], [BillPrefix], [BusinessName], [Currency], [DefaultGSTPercentage], [GSTIN], [Phone], [TaxEnabled])
    VALUES (1, N'''', N''ORD'', N''Kitchen Counter'', N''INR'', 5.0, N'''', N'''', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BillPrefix', N'BusinessName', N'Currency', N'DefaultGSTPercentage', N'GSTIN', N'Phone', N'TaxEnabled') AND [object_id] = OBJECT_ID(N'[BusinessSettings]'))
        SET IDENTITY_INSERT [BusinessSettings] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([Id], [CreatedAt], [IsActive], [Name], [UpdatedAt])
    VALUES (1, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''Burgers'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'CreatedAt', N'Description', N'IsAvailable', N'IsVegetarian', N'Name', N'Price', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[MenuItems]'))
        SET IDENTITY_INSERT [MenuItems] ON;
    EXEC(N'INSERT INTO [MenuItems] ([Id], [CategoryId], [CreatedAt], [Description], [IsAvailable], [IsVegetarian], [Name], [Price], [UpdatedAt])
    VALUES (1, 1, ''2026-01-01T00:00:00.0000000Z'', N''House beef burger'', CAST(1 AS bit), CAST(1 AS bit), N''Classic Burger'', 120.0, ''2026-01-01T00:00:00.0000000Z''),
    (2, 1, ''2026-01-01T00:00:00.0000000Z'', N''Crispy salted fries'', CAST(1 AS bit), CAST(1 AS bit), N''French Fries'', 80.0, ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'CreatedAt', N'Description', N'IsAvailable', N'IsVegetarian', N'Name', N'Price', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[MenuItems]'))
        SET IDENTITY_INSERT [MenuItems] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_MenuItems_CategoryId_IsAvailable] ON [MenuItems] ([CategoryId], [IsAvailable]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE INDEX [IX_Orders_OrderDate] ON [Orders] ([OrderDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260921111859_InitialSqlServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260921111859_InitialSqlServer', N'10.0.6');
END;

COMMIT;
GO

