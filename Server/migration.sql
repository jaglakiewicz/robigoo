CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

CREATE TABLE "ChangeLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ChangeLogs" PRIMARY KEY AUTOINCREMENT,
    "EntityName" TEXT NOT NULL,
    "EntityId" TEXT NOT NULL,
    "Changes" TEXT NOT NULL,
    "Who" TEXT NOT NULL,
    "When" TEXT NOT NULL
);

CREATE TABLE "Inspections" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Inspections" PRIMARY KEY AUTOINCREMENT,
    "VehiclePlate" TEXT NOT NULL,
    "InspectorName" TEXT NOT NULL,
    "InspectionDate" TEXT NOT NULL,
    "Notes" TEXT NULL
);

CREATE TABLE "Machines" (
    "SerialNumber" TEXT NOT NULL CONSTRAINT "PK_Machines" PRIMARY KEY,
    "SprayerName" TEXT NOT NULL,
    "Type" TEXT NOT NULL,
    "Kind" TEXT NOT NULL,
    "Manufacturer" TEXT NOT NULL,
    "ProductionYear" TEXT NOT NULL,
    "PurchaseDate" TEXT NULL,
    "PumpPiston" INTEGER NOT NULL,
    "PumpDiaphragm" INTEGER NOT NULL,
    "PumpOther" INTEGER NOT NULL,
    "PumpOtherType" TEXT NULL,
    "PumpFlowRate" TEXT NULL,
    "TankCapacity" TEXT NULL,
    "HasFlushing" INTEGER NOT NULL,
    "HasDiluter" INTEGER NOT NULL,
    "HasWashingDevice" INTEGER NOT NULL,
    "HasManometer" INTEGER NOT NULL,
    "HasComputer" INTEGER NOT NULL,
    "BoomWidth" TEXT NULL,
    "BoomWet" INTEGER NOT NULL,
    "BoomDry" INTEGER NOT NULL,
    "BoomDampeningMechanism" INTEGER NOT NULL,
    "SectionCount" INTEGER NULL,
    "NozzlesFieldFeatures" TEXT NULL,
    "NozzlesGardenFeatures" TEXT NULL,
    "FanType" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NULL
);

CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Login" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Phone" TEXT NOT NULL,
    "PermissionNumber" TEXT NOT NULL,
    "AvatarData" BLOB NULL,
    "Role" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "LastLoginAt" TEXT NULL,
    "Language" TEXT NOT NULL,
    "Theme" TEXT NOT NULL
);

CREATE TABLE "InspectionItems" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InspectionItems" PRIMARY KEY AUTOINCREMENT,
    "InspectionId" INTEGER NOT NULL,
    "Description" TEXT NOT NULL,
    "Passed" INTEGER NOT NULL,
    CONSTRAINT "FK_InspectionItems_Inspections_InspectionId" FOREIGN KEY ("InspectionId") REFERENCES "Inspections" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserSessions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_UserSessions" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "SessionToken" TEXT NOT NULL,
    "RefreshToken" TEXT NULL,
    "RefreshTokenExpiresAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "LastActivityAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "IpAddress" TEXT NULL,
    "UserAgent" TEXT NULL,
    "SessionTimeoutMinutes" INTEGER NULL,
    "InvalidatedAt" TEXT NULL,
    "InvalidationReason" TEXT NULL,
    CONSTRAINT "FK_UserSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_InspectionItems_InspectionId" ON "InspectionItems" ("InspectionId");

CREATE INDEX "IX_UserSessions_UserId" ON "UserSessions" ("UserId");

CREATE INDEX "IX_UserSessions_UserId_IsActive" ON "UserSessions" ("UserId", "IsActive");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260108133554_InitialCreate', '10.0.1');

ALTER TABLE "Machines" ADD "OwnerId" TEXT NULL;

ALTER TABLE "Machines" ADD "OwnerName" TEXT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260116083432_AddOwnerIdToMachine', '10.0.1');

CREATE TABLE "Clients" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Clients" PRIMARY KEY,
    "ClientType" TEXT NOT NULL,
    "DisplayName" TEXT NOT NULL,
    "FirstName" TEXT NULL,
    "LastName" TEXT NULL,
    "Pesel" TEXT NULL,
    "CompanyName" TEXT NULL,
    "Nip" TEXT NULL,
    "Regon" TEXT NULL,
    "Voivodeship" TEXT NULL,
    "City" TEXT NULL,
    "Street" TEXT NULL,
    "BuildingNumber" TEXT NULL,
    "ApartmentNumber" TEXT NULL,
    "ZipCode" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260116122825_AddClientTable', '10.0.1');

CREATE TABLE "InspectionProtocols" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_InspectionProtocols" PRIMARY KEY AUTOINCREMENT,
    "ProtocolNumber" TEXT NOT NULL,
    "InspectionDate" TEXT NOT NULL,
    "InspectionLocation" TEXT NULL,
    "InspectorName" TEXT NOT NULL,
    "InspectorLicenseNumber" TEXT NULL,
    "ClientId" TEXT NULL,
    "ClientName" TEXT NULL,
    "ClientAddress" TEXT NULL,
    "ClientTaxId" TEXT NULL,
    "CropSprayerSerialNumber" TEXT NULL,
    "CropSprayerName" TEXT NULL,
    "CropSprayerType" TEXT NULL,
    "CropSprayerKind" TEXT NULL,
    "CropSprayerManufacturer" TEXT NULL,
    "CropSprayerProductionYear" TEXT NULL,
    "TankCapacity" TEXT NULL,
    "BoomWidth" TEXT NULL,
    "SectionCount" INTEGER NULL,
    "GeneralConditionPassed" INTEGER NULL,
    "MarkingsReadablePassed" INTEGER NULL,
    "EquipmentCompletePassed" INTEGER NULL,
    "GeneralSectionNotes" TEXT NULL,
    "PumpOperationPassed" INTEGER NULL,
    "PumpSealingPassed" INTEGER NULL,
    "PressurePulsationPassed" INTEGER NULL,
    "PumpSectionNotes" TEXT NULL,
    "AgitatorOperationPassed" INTEGER NULL,
    "AgitatorSectionNotes" TEXT NULL,
    "TankConditionPassed" INTEGER NULL,
    "TankSealingPassed" INTEGER NULL,
    "LevelIndicatorPassed" INTEGER NULL,
    "FlushingSystemPassed" INTEGER NULL,
    "TankSectionNotes" TEXT NULL,
    "ManometerPassed" INTEGER NULL,
    "ManometerReading2Bar" TEXT NULL,
    "ManometerReading4Bar" TEXT NULL,
    "ManometerReading6Bar" TEXT NULL,
    "ManometerDialSizePassed" INTEGER NULL,
    "MeasuringSectionNotes" TEXT NULL,
    "PipesConditionPassed" INTEGER NULL,
    "ConnectionsSealingPassed" INTEGER NULL,
    "PipingSectionNotes" TEXT NULL,
    "SuctionFilterPassed" INTEGER NULL,
    "PressureFilterPassed" INTEGER NULL,
    "NozzleFiltersPassed" INTEGER NULL,
    "FiltrationSectionNotes" TEXT NULL,
    "FieldBoomConditionPassed" INTEGER NULL,
    "BoomStabilityPassed" INTEGER NULL,
    "BoomHeightPassed" INTEGER NULL,
    "BoomSymmetryPassed" INTEGER NULL,
    "OrchardSprayerConditionPassed" INTEGER NULL,
    "AirStreamDirectionPassed" INTEGER NULL,
    "BoomSectionNotes" TEXT NULL,
    "NozzleUniformityPassed" INTEGER NULL,
    "NozzleFlowRatePassed" INTEGER NULL,
    "NozzleConditionPassed" INTEGER NULL,
    "NozzleMeasurements" TEXT NULL,
    "NozzlesSectionNotes" TEXT NULL,
    "TransverseDistributionPassed" INTEGER NULL,
    "CoefficientOfVariation" TEXT NULL,
    "DistributionSectionNotes" TEXT NULL,
    "FinalResult" INTEGER NULL,
    "ValidUntil" TEXT NULL,
    "ControlStickerNumber" TEXT NULL,
    "GeneralNotes" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NULL,
    "ProtocolXml" TEXT NULL,
    "XslTemplateVersion" TEXT NULL,
    "XmlGeneratedAt" TEXT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260206200844_AddInspectionProtocolsTable', '10.0.1');

