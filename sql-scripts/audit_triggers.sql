-- Auto-generated: AFTER INSERT / AFTER UPDATE triggers for every table
-- that has CreatedAt_Ticks and UpdatedAt_Ticks columns.
-- Tables found: FilesDb (FileClassification, FileClassificationKeyword)
--               AppDb   (ConnectionStrings, FileClassificationCategories, ModelToIgnore,
--                         ScrapeConfiguration, ScrapeDirectories, SearchCategories,
--                         SearchConfiguration, TagToIgnore, UserConfiguration)

-- FilesDb
CREATE TRIGGER IF NOT EXISTS tr_FileClassification_update
   AFTER UPDATE
   ON FileClassification
BEGIN
 UPDATE FileClassification
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_FileClassification_insert
   AFTER INSERT
   ON FileClassification
BEGIN
 UPDATE FileClassification
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_FileClassificationKeyword_update
   AFTER UPDATE
   ON FileClassificationKeyword
BEGIN
 UPDATE FileClassificationKeyword
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_FileClassificationKeyword_insert
   AFTER INSERT
   ON FileClassificationKeyword
BEGIN
 UPDATE FileClassificationKeyword
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

-- AppDb
CREATE TRIGGER IF NOT EXISTS tr_ConnectionStrings_update
   AFTER UPDATE
   ON ConnectionStrings
BEGIN
 UPDATE ConnectionStrings
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_ConnectionStrings_insert
   AFTER INSERT
   ON ConnectionStrings
BEGIN
 UPDATE ConnectionStrings
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_FileClassificationCategories_update
   AFTER UPDATE
   ON FileClassificationCategories
BEGIN
 UPDATE FileClassificationCategories
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_FileClassificationCategories_insert
   AFTER INSERT
   ON FileClassificationCategories
BEGIN
 UPDATE FileClassificationCategories
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_ModelToIgnore_update
   AFTER UPDATE
   ON ModelToIgnore
BEGIN
 UPDATE ModelToIgnore
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_ModelToIgnore_insert
   AFTER INSERT
   ON ModelToIgnore
BEGIN
 UPDATE ModelToIgnore
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_ScrapeConfiguration_update
   AFTER UPDATE
   ON ScrapeConfiguration
BEGIN
 UPDATE ScrapeConfiguration
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_ScrapeConfiguration_insert
   AFTER INSERT
   ON ScrapeConfiguration
BEGIN
 UPDATE ScrapeConfiguration
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_ScrapeDirectories_update
   AFTER UPDATE
   ON ScrapeDirectories
BEGIN
 UPDATE ScrapeDirectories
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_ScrapeDirectories_insert
   AFTER INSERT
   ON ScrapeDirectories
BEGIN
 UPDATE ScrapeDirectories
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

-- SearchCategories has a composite PK (SearchConfigurationId, Id)
CREATE TRIGGER IF NOT EXISTS tr_SearchCategories_update
   AFTER UPDATE
   ON SearchCategories
BEGIN
 UPDATE SearchCategories
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE SearchConfigurationId = NEW.SearchConfigurationId AND Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_SearchCategories_insert
   AFTER INSERT
   ON SearchCategories
BEGIN
 UPDATE SearchCategories
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE SearchConfigurationId = NEW.SearchConfigurationId AND Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_SearchConfiguration_update
   AFTER UPDATE
   ON SearchConfiguration
BEGIN
 UPDATE SearchConfiguration
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_SearchConfiguration_insert
   AFTER INSERT
   ON SearchConfiguration
BEGIN
 UPDATE SearchConfiguration
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_TagToIgnore_update
   AFTER UPDATE
   ON TagToIgnore
BEGIN
 UPDATE TagToIgnore
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_TagToIgnore_insert
   AFTER INSERT
   ON TagToIgnore
BEGIN
 UPDATE TagToIgnore
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;

CREATE TRIGGER IF NOT EXISTS tr_UserConfiguration_update
   AFTER UPDATE
   ON UserConfiguration
BEGIN
 UPDATE UserConfiguration
    SET UpdatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
CREATE TRIGGER IF NOT EXISTS tr_UserConfiguration_insert
   AFTER INSERT
   ON UserConfiguration
BEGIN
 UPDATE UserConfiguration
    SET CreatedAt_Ticks = unixepoch('now','subsec')
    WHERE Id = NEW.Id;
END;
