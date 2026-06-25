-- AlumniManagement Database Schema
CREATE DATABASE IF NOT EXISTS AlumniManagement;
USE AlumniManagement;

CREATE TABLE Roles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    RoleName VARCHAR(100) NOT NULL
);

CREATE TABLE Users (
    Id CHAR(36) PRIMARY KEY,
    Email VARCHAR(255) NOT NULL,
    PasswordHash TEXT NOT NULL,
    RoleId INT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

CREATE TABLE AlumniProfiles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId CHAR(36) NOT NULL,

    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20),
    BatchYear INT,
    Degree VARCHAR(255),
    CurrentCompany VARCHAR(255),
    CurrentRole VARCHAR(255),
    Location VARCHAR(255),
    LinkedinURL VARCHAR(500),

    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE Events (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    EventName VARCHAR(255) NOT NULL,
    Description TEXT,
    EventDate DATETIME NOT NULL,
    Location VARCHAR(255),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE EventRSVPs (
    EventId INT NOT NULL,
    UserId CHAR(36) NOT NULL,
    RsvpStatus INT NOT NULL,
    PRIMARY KEY (EventId, UserId),
    FOREIGN KEY (EventId) REFERENCES Events(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE Donations (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    DonationDate DATETIME NOT NULL,
    RazorpayOrderId VARCHAR(255),
    RazorpayPaymentId VARCHAR(255),
    CreatedAt DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE DonationWebhookLogs (
    Id CHAR(36) PRIMARY KEY,
    DonationId INT NULL,
    RazorpayEventId VARCHAR(255) NOT NULL UNIQUE,
    EventType VARCHAR(100) NOT NULL,
    RawPayload LONGTEXT NOT NULL,
    SignatureValid BOOLEAN NOT NULL,
    Status INT NOT NULL,
    ReceivedAt DATETIME NOT NULL,
    ProcessedAt DATETIME NULL,
    FOREIGN KEY (DonationId) REFERENCES Donations(Id)
);

CREATE TABLE Notifications (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Type INT NOT NULL,
    Message TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    IsRead BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE AuditLogs (
    Id CHAR(36) PRIMARY KEY,
    UserId CHAR(36) NULL,
    Action VARCHAR(255) NOT NULL,
    EntityType VARCHAR(255) NOT NULL,
    EntityId VARCHAR(255) NOT NULL,
    Details LONGTEXT NULL,
    IpAddress VARCHAR(45) NULL,
    Timestamp DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE JobPostings (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId CHAR(36) NOT NULL,
    JobTitle VARCHAR(255) NOT NULL,
    JobDescription TEXT NOT NULL,
    CompanyName VARCHAR(255) NOT NULL,
    Location VARCHAR(255),
    PostedDate DATETIME NOT NULL,
    ApplicationDeadline DATETIME NOT NULL,
    ApplyUrl VARCHAR(500),
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
