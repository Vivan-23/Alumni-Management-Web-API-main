/* ============================
   DROP & CREATE DATABASE
   ============================ */

DROP DATABASE IF EXISTS alumni;
CREATE DATABASE alumni
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE alumni;

/* ============================
   USERS & ROLES
   ============================ */

CREATE TABLE roles (
  RoleId INT NOT NULL AUTO_INCREMENT,
  RoleName VARCHAR(200) NOT NULL,
  PRIMARY KEY (RoleId)
) ENGINE=InnoDB;

CREATE TABLE users (
  UserId INT NOT NULL AUTO_INCREMENT,
  UserName VARCHAR(100) NOT NULL,
  PasswordHash VARCHAR(255) NOT NULL,
  Email VARCHAR(150) NOT NULL,
  Phone VARCHAR(20),
  CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
  RoleId INT NOT NULL DEFAULT 1,
  IsFirstLogin TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (UserId),
  UNIQUE KEY UK_UserName (UserName),
  CONSTRAINT users_ibfk_1
    FOREIGN KEY (RoleId) REFERENCES roles(RoleId)
) ENGINE=InnoDB;

/* ============================
   ALUMNI PROFILES
   ============================ */

CREATE TABLE alumniprofiles (
  AlumniId INT NOT NULL AUTO_INCREMENT,
  CompanyName VARCHAR(150),
  Designation VARCHAR(150),
  LinkedinUrl VARCHAR(255),
  AlumniName VARCHAR(150) NOT NULL,
  Passout_year VARCHAR(10) NOT NULL,
  UserId INT NOT NULL,
  PRIMARY KEY (AlumniId),
  KEY UserId (UserId),
  CONSTRAINT alumniprofiles_ibfk_1
    FOREIGN KEY (UserId) REFERENCES users(UserId)
) ENGINE=InnoDB;

/* ============================
   EVENTS
   ============================ */

CREATE TABLE events (
  EventsId INT NOT NULL AUTO_INCREMENT,
  EventName VARCHAR(200) NOT NULL,
  EventDescription TEXT,
  EventTime TIME NOT NULL,
  EventDate DATE NOT NULL,
  EventLocation VARCHAR(200) NOT NULL,
  CreatedBy VARCHAR(100) NOT NULL,
  PRIMARY KEY (EventsId)
) ENGINE=InnoDB;

/* ============================
   EVENT REGISTRATIONS
   ============================ */

CREATE TABLE eventregistrations (
  RegistrationId INT NOT NULL AUTO_INCREMENT,
  EventId INT NOT NULL,
  AlumniId INT NOT NULL,
  Status ENUM('yes','no','unsure') NOT NULL,
  PRIMARY KEY (RegistrationId),
  KEY EventId (EventId),
  KEY AlumniId (AlumniId),
  CONSTRAINT eventregistrations_ibfk_1
    FOREIGN KEY (EventId) REFERENCES events(EventsId),
  CONSTRAINT eventregistrations_ibfk_2
    FOREIGN KEY (AlumniId) REFERENCES alumniprofiles(AlumniId)
) ENGINE=InnoDB;

/* ============================
   JOBS
   ============================ */

CREATE TABLE job (
  JobId INT NOT NULL AUTO_INCREMENT,
  Title VARCHAR(255),
  Description VARCHAR(255),
  Company VARCHAR(255),
  PostedDate DATETIME,
  ExpiryDate DATETIME,
  ApplyLink VARCHAR(255),
  AlumniId INT NOT NULL,
  PRIMARY KEY (JobId),
  KEY AlumniId (AlumniId),
  CONSTRAINT job_ibfk_1
    FOREIGN KEY (AlumniId) REFERENCES alumniprofiles(AlumniId)
) ENGINE=InnoDB;

/* ============================
   DONATIONS
   ============================ */

CREATE TABLE donations (
  DonationId INT NOT NULL AUTO_INCREMENT,
  Description TEXT,
  AlumniId INT NOT NULL,
  Amount DOUBLE NOT NULL,
  UserId INT DEFAULT NULL,
  PRIMARY KEY (DonationId),
  KEY AlumniId (AlumniId),
  CONSTRAINT donations_ibfk_1
    FOREIGN KEY (AlumniId) REFERENCES alumniprofiles(AlumniId),
  CONSTRAINT CK_Donation_Alumni_OR_User
    CHECK (
      (AlumniId IS NOT NULL AND UserId IS NULL)
      OR
      (AlumniId IS NULL AND UserId IS NOT NULL)
    )
) ENGINE=InnoDB;

/* ============================
   MENTORSHIP REQUESTS
   ============================ */

CREATE TABLE mentorshiprequests (
  RequestId INT NOT NULL AUTO_INCREMENT,
  AlumniId INT DEFAULT NULL,
  UserId INT NOT NULL,
  CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
  Status ENUM('Approved','Not_Approved','pending') DEFAULT 'pending',
  PRIMARY KEY (RequestId),
  KEY AlumniId (AlumniId),
  KEY UserId (UserId),
  CONSTRAINT mentorshiprequests_ibfk_1
    FOREIGN KEY (AlumniId) REFERENCES alumniprofiles(AlumniId),
  CONSTRAINT mentorshiprequests_ibfk_2
    FOREIGN KEY (UserId) REFERENCES users(UserId)
) ENGINE=InnoDB;
