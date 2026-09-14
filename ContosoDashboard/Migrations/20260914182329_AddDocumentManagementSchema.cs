using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoDashboard.Migrations;

/// <summary>
/// Adds the document feature to databases created by the legacy EnsureCreated startup path.
/// The guards keep this safe for a newly initialized database, which already has the current model.
/// </summary>
public partial class AddDocumentManagementSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[Documents]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Documents] (
                    [DocumentId] int NOT NULL IDENTITY, [Title] nvarchar(255) NOT NULL, [Description] nvarchar(2000) NULL,
                    [Category] nvarchar(100) NOT NULL, [Tags] nvarchar(1000) NULL, [OriginalFileName] nvarchar(255) NOT NULL,
                    [FilePath] nvarchar(1000) NOT NULL, [FileType] nvarchar(255) NOT NULL, [FileSizeBytes] bigint NOT NULL,
                    [UploadedDate] datetime2 NOT NULL, [UpdatedDate] datetime2 NOT NULL, [UploadedByUserId] int NOT NULL,
                    [ProjectId] int NULL, [TaskId] int NULL,
                    CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]),
                    CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Documents_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([TaskId]) ON DELETE NO ACTION
                );
            END
            """);
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NULL
            BEGIN
                CREATE TABLE [DocumentActivities] (
                    [DocumentActivityId] int NOT NULL IDENTITY, [DocumentId] int NULL, [UserId] int NOT NULL,
                    [Action] nvarchar(40) NOT NULL, [OccurredDate] datetime2 NOT NULL, [Details] nvarchar(2000) NULL,
                    CONSTRAINT [PK_DocumentActivities] PRIMARY KEY ([DocumentActivityId]),
                    CONSTRAINT [FK_DocumentActivities_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE SET NULL,
                    CONSTRAINT [FK_DocumentActivities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                );
            END
            """);
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL
            BEGIN
                CREATE TABLE [DocumentShares] (
                    [DocumentShareId] int NOT NULL IDENTITY, [DocumentId] int NOT NULL, [SharedWithUserId] int NULL,
                    [SharedWithDepartment] nvarchar(100) NULL, [SharedByUserId] int NOT NULL, [SharedDate] datetime2 NOT NULL, [IsActive] bit NOT NULL,
                    CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([DocumentShareId]),
                    CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_DocumentShares_Users_SharedWithUserId] FOREIGN KEY ([SharedWithUserId]) REFERENCES [Users] ([UserId]),
                    CONSTRAINT [FK_DocumentShares_Users_SharedByUserId] FOREIGN KEY ([SharedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                );
            END
            """);
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_FilePath' AND object_id = OBJECT_ID(N'[Documents]')) CREATE UNIQUE INDEX [IX_Documents_FilePath] ON [Documents] ([FilePath]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_UploadedByUserId_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_UploadedByUserId_UploadedDate] ON [Documents] ([UploadedByUserId], [UploadedDate]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ProjectId_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_ProjectId_UploadedDate] ON [Documents] ([ProjectId], [UploadedDate]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_Category_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_Category_UploadedDate] ON [Documents] ([Category], [UploadedDate]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_TaskId' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_TaskId] ON [Documents] ([TaskId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentActivities_DocumentId' AND object_id = OBJECT_ID(N'[DocumentActivities]')) CREATE INDEX [IX_DocumentActivities_DocumentId] ON [DocumentActivities] ([DocumentId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentActivities_UserId' AND object_id = OBJECT_ID(N'[DocumentActivities]')) CREATE INDEX [IX_DocumentActivities_UserId] ON [DocumentActivities] ([UserId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShares_DocumentId' AND object_id = OBJECT_ID(N'[DocumentShares]')) CREATE INDEX [IX_DocumentShares_DocumentId] ON [DocumentShares] ([DocumentId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShares_SharedWithUserId' AND object_id = OBJECT_ID(N'[DocumentShares]')) CREATE INDEX [IX_DocumentShares_SharedWithUserId] ON [DocumentShares] ([SharedWithUserId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShares_SharedByUserId' AND object_id = OBJECT_ID(N'[DocumentShares]')) CREATE INDEX [IX_DocumentShares_SharedByUserId] ON [DocumentShares] ([SharedByUserId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately non-destructive: this migration upgrades a pre-existing training database.
    }
}
