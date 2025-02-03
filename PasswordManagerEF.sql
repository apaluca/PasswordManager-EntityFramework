-- Create database
CREATE DATABASE PasswordManagerEF;
GO

USE PasswordManagerEF;
GO

-- Create Roles table
CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

-- Create Users table
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    RoleId INT NOT NULL,
    TwoFactorEnabled BIT DEFAULT 0,
    TwoFactorSecret NVARCHAR(MAX) NULL,
    LastLoginDate DATETIME NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-- Create PasswordGroups table
CREATE TABLE PasswordGroups (
    GroupId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    GroupName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Create StoredPasswords table
CREATE TABLE StoredPasswords (
    PasswordId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    GroupId INT NULL,
    SiteName NVARCHAR(100) NOT NULL,
    SiteUrl NVARCHAR(255) NULL,
    Username NVARCHAR(100) NOT NULL,
    EncryptedPassword NVARCHAR(MAX) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ExpirationDate DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (GroupId) REFERENCES PasswordGroups(GroupId)
);

-- Create LoginAttempts table
CREATE TABLE LoginAttempts (
    AttemptId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    AttemptDate DATETIME DEFAULT GETDATE(),
    IsSuccessful BIT NOT NULL,
    IPAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(255) NULL
);

-- Create AuditLog table
CREATE TABLE AuditLog (
    LogId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NULL,
    ActionDate DATETIME DEFAULT GETDATE(),
    Action NVARCHAR(50) NOT NULL,
    Details NVARCHAR(MAX) NULL,
    IPAddress NVARCHAR(45) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Create PasswordPolicies table
CREATE TABLE PasswordPolicies (
    PolicyId INT PRIMARY KEY IDENTITY(1,1),
    MinLength INT NOT NULL,
    RequireUppercase BIT NOT NULL,
    RequireLowercase BIT NOT NULL,
    RequireNumbers BIT NOT NULL,
    RequireSpecialChars BIT NOT NULL,
    MaxLoginAttempts INT NOT NULL,
    LockoutDurationMinutes INT NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME DEFAULT GETDATE(),
    ModifiedByUserId INT,
    IsActive BIT NULL,
    FOREIGN KEY (ModifiedByUserId) REFERENCES Users(UserId)
);

-- Create SecurityAlerts table
CREATE TABLE SecurityAlerts (
    AlertId INT PRIMARY KEY IDENTITY(1,1),
    AlertType VARCHAR(50) NOT NULL, -- 'BruteForce', 'SuspiciousIP', 'MultipleFailures', etc.
    Severity VARCHAR(20) NOT NULL, -- 'Low', 'Medium', 'High', 'Critical'
    Description NVARCHAR(MAX) NOT NULL,
    Source VARCHAR(100) NOT NULL,
    IPAddress VARCHAR(45) NULL,
    Username VARCHAR(50) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    IsResolved BIT NULL,
    ResolvedByUserId INT NULL,
    ResolvedDate DATETIME NULL,
    Notes NVARCHAR(MAX) NULL,
    FOREIGN KEY (ResolvedByUserId) REFERENCES Users(UserId)
);

-- Create IP Blacklist Management
CREATE TABLE BlockedIPs (
    BlockId INT PRIMARY KEY IDENTITY(1,1),
    IPAddress VARCHAR(45) NOT NULL,
    Reason NVARCHAR(255) NOT NULL,
    BlockedDate DATETIME DEFAULT GETDATE(),
    BlockedByUserId INT NOT NULL,
    ExpiryDate DATETIME NULL,
    IsActive BIT NULL,
    Notes NVARCHAR(MAX) NULL,
    FOREIGN KEY (BlockedByUserId) REFERENCES Users(UserId)
);

-- Create indexes for better performance
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_StoredPasswords_UserId ON StoredPasswords(UserId);
CREATE INDEX IX_StoredPasswords_GroupId ON StoredPasswords(GroupId);
CREATE INDEX IX_PasswordGroups_UserId ON PasswordGroups(UserId);
CREATE INDEX IX_LoginAttempts_Username ON LoginAttempts(Username);
CREATE INDEX IX_LoginAttempts_AttemptDate ON LoginAttempts(AttemptDate);
CREATE INDEX IX_AuditLog_UserId ON AuditLog(UserId);
CREATE INDEX IX_AuditLog_ActionDate ON AuditLog(ActionDate);
CREATE INDEX IX_PasswordPolicies_ModifiedByUserId ON PasswordPolicies(ModifiedByUserId);
CREATE INDEX IX_PasswordPolicies_IsActive ON PasswordPolicies(IsActive);
CREATE INDEX IX_SecurityAlerts_AlertType ON SecurityAlerts(AlertType);
CREATE INDEX IX_SecurityAlerts_Severity ON SecurityAlerts(Severity);
CREATE INDEX IX_SecurityAlerts_CreatedDate ON SecurityAlerts(CreatedDate);
CREATE INDEX IX_SecurityAlerts_ResolvedByUserId ON SecurityAlerts(ResolvedByUserId);
CREATE INDEX IX_SecurityAlerts_IsResolved ON SecurityAlerts(IsResolved);
CREATE INDEX IX_BlockedIPs_IPAddress ON BlockedIPs(IPAddress);
CREATE INDEX IX_BlockedIPs_BlockedByUserId ON BlockedIPs(BlockedByUserId);
CREATE INDEX IX_BlockedIPs_IsActive ON BlockedIPs(IsActive);
CREATE INDEX IX_BlockedIPs_BlockedDate ON BlockedIPs(BlockedDate);

-- Insert default roles
INSERT INTO Roles (RoleName) 
VALUES 
	('User'),
	('Administrator'),
	('IT_Specialist');

-- Create admin, it and user account with passwords admin123
INSERT INTO Users (Username, PasswordHash, Email, RoleId, IsActive)
VALUES 
    ('admin', 'As/cFCZmfqCtKTmZtwqjkn+MRro3lu3HDh/fONmsf8Bj4WFjAgMUcakUJADwRUfU', 'admin@example.com', 2, 1),
    ('it', 'As/cFCZmfqCtKTmZtwqjkn+MRro3lu3HDh/fONmsf8Bj4WFjAgMUcakUJADwRUfU', 'it@example.com', 3, 1),
    ('user', 'As/cFCZmfqCtKTmZtwqjkn+MRro3lu3HDh/fONmsf8Bj4WFjAgMUcakUJADwRUfU', 'user@example.com', 1, 1);

-- Insert default password policy
INSERT INTO PasswordPolicies (
    MinLength, 
    RequireUppercase, 
    RequireLowercase, 
    RequireNumbers, 
    RequireSpecialChars,
    MaxLoginAttempts,
    LockoutDurationMinutes,
    ModifiedByUserId)
VALUES (
    8,    -- MinLength
    1,    -- RequireUppercase
    1,    -- RequireLowercase
    1,    -- RequireNumbers
    1,    -- RequireSpecialChars
    5,    -- MaxLoginAttempts
    30,   -- LockoutDurationMinutes
    1     -- ModifiedByUserId (assuming admin has UserId = 1)
);