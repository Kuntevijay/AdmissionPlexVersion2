-- ============================================================
-- AdmissionPlex - PostgreSQL Database Initialization Script
-- Run this against your PostgreSQL database if EnsureCreated
-- doesn't work, or for production setup.
-- ============================================================

-- Drop existing tables (in dependency order) if starting fresh
-- Uncomment these if you want a clean reset:
-- DROP SCHEMA public CASCADE; CREATE SCHEMA public;

-- ========================
-- ASP.NET Identity Tables
-- ========================

CREATE TABLE IF NOT EXISTS users (
    "Id" BIGSERIAL PRIMARY KEY,
    "Uuid" UUID NOT NULL DEFAULT gen_random_uuid(),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LastLoginAt" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_Uuid" ON users ("Uuid");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_NormalizedEmail" ON users ("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_NormalizedUserName" ON users ("NormalizedUserName");

CREATE TABLE IF NOT EXISTS roles (
    "Id" BIGSERIAL PRIMARY KEY,
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_roles_NormalizedName" ON roles ("NormalizedName");

CREATE TABLE IF NOT EXISTS user_roles (
    "UserId" BIGINT NOT NULL REFERENCES users("Id") ON DELETE CASCADE,
    "RoleId" BIGINT NOT NULL REFERENCES roles("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

CREATE TABLE IF NOT EXISTS user_claims (
    "Id" SERIAL PRIMARY KEY,
    "UserId" BIGINT NOT NULL REFERENCES users("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE IF NOT EXISTS user_logins (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" BIGINT NOT NULL REFERENCES users("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey")
);

CREATE TABLE IF NOT EXISTS user_tokens (
    "UserId" BIGINT NOT NULL REFERENCES users("Id") ON DELETE CASCADE,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name")
);

CREATE TABLE IF NOT EXISTS role_claims (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" BIGINT NOT NULL REFERENCES roles("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

-- ========================
-- User Profiles
-- ========================

CREATE TABLE IF NOT EXISTS coordinator_profiles (
    "Id" BIGSERIAL PRIMARY KEY,
    "UserId" BIGINT NOT NULL UNIQUE REFERENCES users("Id"),
    "FullName" VARCHAR(200) NOT NULL,
    "Designation" VARCHAR(100),
    "Phone" VARCHAR(15),
    "CommissionPct" DECIMAL(5,2) DEFAULT 10.00,
    "TotalReferrals" INTEGER DEFAULT 0,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS student_profiles (
    "Id" BIGSERIAL PRIMARY KEY,
    "UserId" BIGINT NOT NULL UNIQUE REFERENCES users("Id"),
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "DateOfBirth" DATE,
    "Gender" VARCHAR(10),
    "CurrentClass" VARCHAR(20),
    "SchoolName" VARCHAR(255),
    "City" VARCHAR(100),
    "State" VARCHAR(100),
    "Board" VARCHAR(10),
    "Stream" VARCHAR(15) DEFAULT 'Undecided',
    "ParentPhone" VARCHAR(15),
    "ParentEmail" VARCHAR(255),
    "AvatarUrl" VARCHAR(500),
    "ReferredByCode" VARCHAR(20),
    "CoordinatorId" BIGINT REFERENCES coordinator_profiles("Id"),
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS counsellor_profiles (
    "Id" BIGSERIAL PRIMARY KEY,
    "UserId" BIGINT NOT NULL UNIQUE REFERENCES users("Id"),
    "FullName" VARCHAR(200) NOT NULL,
    "Qualification" VARCHAR(255) NOT NULL,
    "Specialization" VARCHAR(255),
    "Bio" TEXT,
    "ExperienceYears" INTEGER DEFAULT 0,
    "HourlyRate" DECIMAL(10,2),
    "IsAvailable" BOOLEAN DEFAULT TRUE,
    "Rating" DECIMAL(3,2) DEFAULT 0,
    "TotalSessions" INTEGER DEFAULT 0,
    "AvatarUrl" VARCHAR(500),
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS coordinator_schools (
    "Id" BIGSERIAL PRIMARY KEY,
    "CoordinatorId" BIGINT NOT NULL REFERENCES coordinator_profiles("Id"),
    "SchoolName" VARCHAR(255) NOT NULL,
    "SchoolCity" VARCHAR(100) NOT NULL,
    "SchoolBoard" VARCHAR(50),
    "StudentCount" INTEGER DEFAULT 0,
    "AgreementSigned" BOOLEAN DEFAULT FALSE
);

-- ========================
-- Interest & Aptitude Categories
-- ========================

CREATE TABLE IF NOT EXISTS interest_categories (
    "Id" BIGSERIAL PRIMARY KEY,
    "Code" VARCHAR(10) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" TEXT NOT NULL,
    "DisplayOrder" INTEGER NOT NULL,
    "IsActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS aptitude_categories (
    "Id" BIGSERIAL PRIMARY KEY,
    "Code" VARCHAR(10) NOT NULL UNIQUE,
    "Name" VARCHAR(100) NOT NULL,
    "Description" TEXT NOT NULL,
    "DisplayOrder" INTEGER NOT NULL,
    "IsActive" BOOLEAN DEFAULT TRUE
);

-- ========================
-- Questions & Tests
-- ========================

CREATE TABLE IF NOT EXISTS questions (
    "Id" BIGSERIAL PRIMARY KEY,
    "Uuid" UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    "QuestionText" TEXT NOT NULL,
    "QuestionType" VARCHAR(15) NOT NULL,
    "SectionType" VARCHAR(20) NOT NULL,
    "InterestCategoryId" BIGINT REFERENCES interest_categories("Id"),
    "AptitudeCategoryId" BIGINT REFERENCES aptitude_categories("Id"),
    "Difficulty" VARCHAR(10) DEFAULT 'Medium',
    "Weightage" DECIMAL(5,2) DEFAULT 1.00,
    "MaxScore" DECIMAL(5,2) DEFAULT 1.00,
    "Explanation" TEXT,
    "ImageUrl" VARCHAR(500),
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedBy" BIGINT NOT NULL,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS question_options (
    "Id" BIGSERIAL PRIMARY KEY,
    "QuestionId" BIGINT NOT NULL REFERENCES questions("Id") ON DELETE CASCADE,
    "OptionText" VARCHAR(500) NOT NULL,
    "OptionOrder" INTEGER NOT NULL,
    "IsCorrect" BOOLEAN DEFAULT FALSE,
    "ScoreValue" DECIMAL(5,2) DEFAULT 0,
    "StreamTag" VARCHAR(15)
);

CREATE TABLE IF NOT EXISTS tests (
    "Id" BIGSERIAL PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "Slug" VARCHAR(255) NOT NULL UNIQUE,
    "TestType" VARCHAR(20) NOT NULL,
    "Description" TEXT,
    "DurationMinutes" INTEGER NOT NULL,
    "TotalQuestions" INTEGER NOT NULL,
    "Price" DECIMAL(10,2) DEFAULT 0,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "IncludesCounsellorSession" BOOLEAN DEFAULT FALSE,
    "Instructions" TEXT,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS test_sections (
    "Id" BIGSERIAL PRIMARY KEY,
    "TestId" BIGINT NOT NULL REFERENCES tests("Id") ON DELETE CASCADE,
    "Title" VARCHAR(255) NOT NULL,
    "SectionType" VARCHAR(20) NOT NULL,
    "SectionOrder" INTEGER NOT NULL,
    "TimeLimitMinutes" INTEGER,
    "InterestCategoryId" BIGINT REFERENCES interest_categories("Id"),
    "AptitudeCategoryId" BIGINT REFERENCES aptitude_categories("Id")
);

CREATE TABLE IF NOT EXISTS test_section_questions (
    "Id" BIGSERIAL PRIMARY KEY,
    "SectionId" BIGINT NOT NULL REFERENCES test_sections("Id") ON DELETE CASCADE,
    "QuestionId" BIGINT NOT NULL REFERENCES questions("Id"),
    "QuestionOrder" INTEGER NOT NULL
);

-- ========================
-- Test Attempts & Results
-- ========================

CREATE TABLE IF NOT EXISTS test_attempts (
    "Id" BIGSERIAL PRIMARY KEY,
    "Uuid" UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    "StudentId" BIGINT NOT NULL REFERENCES student_profiles("Id"),
    "TestId" BIGINT NOT NULL REFERENCES tests("Id"),
    "Status" VARCHAR(15) DEFAULT 'Started',
    "StartedAt" TIMESTAMPTZ DEFAULT NOW(),
    "CompletedAt" TIMESTAMPTZ,
    "RecommendedStream" VARCHAR(15),
    "OverallIqScore" INTEGER,
    "IqCategory" VARCHAR(20),
    "PdfReportUrl" VARCHAR(500),
    "PaymentId" BIGINT,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS test_responses (
    "Id" BIGSERIAL PRIMARY KEY,
    "AttemptId" BIGINT NOT NULL REFERENCES test_attempts("Id") ON DELETE CASCADE,
    "QuestionId" BIGINT NOT NULL REFERENCES questions("Id"),
    "SelectedOptionId" BIGINT REFERENCES question_options("Id"),
    "OpenAnswer" TEXT,
    "ScoreObtained" DECIMAL(5,2) DEFAULT 0,
    "TimeTakenSeconds" INTEGER,
    "AnsweredAt" TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE ("AttemptId", "QuestionId")
);

CREATE TABLE IF NOT EXISTS interest_scores (
    "Id" BIGSERIAL PRIMARY KEY,
    "AttemptId" BIGINT NOT NULL REFERENCES test_attempts("Id") ON DELETE CASCADE,
    "InterestCategoryId" BIGINT NOT NULL REFERENCES interest_categories("Id"),
    "RawScore" DECIMAL(8,2) NOT NULL,
    "MaxPossibleScore" DECIMAL(8,2) NOT NULL,
    "PercentileScore" DECIMAL(5,2) NOT NULL,
    "RankOrder" INTEGER NOT NULL,
    UNIQUE ("AttemptId", "InterestCategoryId")
);

CREATE TABLE IF NOT EXISTS aptitude_scores (
    "Id" BIGSERIAL PRIMARY KEY,
    "AttemptId" BIGINT NOT NULL REFERENCES test_attempts("Id") ON DELETE CASCADE,
    "AptitudeCategoryId" BIGINT NOT NULL REFERENCES aptitude_categories("Id"),
    "RawScore" DECIMAL(8,2) NOT NULL,
    "MaxPossibleScore" DECIMAL(8,2) NOT NULL,
    "PercentileScore" DECIMAL(5,2) NOT NULL,
    "RankOrder" INTEGER NOT NULL,
    UNIQUE ("AttemptId", "AptitudeCategoryId")
);

-- ========================
-- Careers
-- ========================

CREATE TABLE IF NOT EXISTS career_streams (
    "Id" BIGSERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Description" TEXT,
    "IconUrl" VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS careers (
    "Id" BIGSERIAL PRIMARY KEY,
    "Slug" VARCHAR(255) NOT NULL UNIQUE,
    "Title" VARCHAR(255) NOT NULL,
    "StreamId" BIGINT NOT NULL REFERENCES career_streams("Id"),
    "Summary" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "EducationPath" TEXT,
    "EducationCostRange" VARCHAR(100),
    "AdmissionInfo" TEXT,
    "AvgSalaryMin" DECIMAL(12,2),
    "AvgSalaryMax" DECIMAL(12,2),
    "GrowthOutlook" VARCHAR(15) DEFAULT 'Medium',
    "JobMarketSize" VARCHAR(50),
    "SkillsRequired" JSONB,
    "TopColleges" JSONB,
    "EntranceExams" JSONB,
    "SuitabilityCutoffPct" DECIMAL(5,2) DEFAULT 80.00,
    "ImageUrl" VARCHAR(500),
    "IsPublished" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS career_subjects (
    "Id" BIGSERIAL PRIMARY KEY,
    "CareerId" BIGINT NOT NULL REFERENCES careers("Id") ON DELETE CASCADE,
    "SubjectName" VARCHAR(100) NOT NULL,
    "Importance" VARCHAR(15) DEFAULT 'Recommended'
);

CREATE TABLE IF NOT EXISTS career_suitability_scores (
    "Id" BIGSERIAL PRIMARY KEY,
    "AttemptId" BIGINT NOT NULL REFERENCES test_attempts("Id") ON DELETE CASCADE,
    "CareerId" BIGINT NOT NULL REFERENCES careers("Id"),
    "SuitabilityPct" DECIMAL(5,2) NOT NULL,
    "IsRecommended" BOOLEAN DEFAULT FALSE,
    "IsCanBeConsidered" BOOLEAN DEFAULT FALSE,
    "RankOrder" INTEGER NOT NULL,
    UNIQUE ("AttemptId", "CareerId")
);

CREATE TABLE IF NOT EXISTS career_interest_weights (
    "Id" BIGSERIAL PRIMARY KEY,
    "CareerId" BIGINT NOT NULL REFERENCES careers("Id") ON DELETE CASCADE,
    "InterestCategoryId" BIGINT NOT NULL REFERENCES interest_categories("Id"),
    "Weight" DECIMAL(5,2) NOT NULL,
    "MinPercentile" DECIMAL(5,2) DEFAULT 0,
    UNIQUE ("CareerId", "InterestCategoryId")
);

CREATE TABLE IF NOT EXISTS career_aptitude_weights (
    "Id" BIGSERIAL PRIMARY KEY,
    "CareerId" BIGINT NOT NULL REFERENCES careers("Id") ON DELETE CASCADE,
    "AptitudeCategoryId" BIGINT NOT NULL REFERENCES aptitude_categories("Id"),
    "Weight" DECIMAL(5,2) NOT NULL,
    "MinPercentile" DECIMAL(5,2) DEFAULT 0,
    UNIQUE ("CareerId", "AptitudeCategoryId")
);

-- ========================
-- Cutoffs
-- ========================

CREATE TABLE IF NOT EXISTS colleges (
    "Id" BIGSERIAL PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Code" VARCHAR(20) UNIQUE,
    "University" VARCHAR(255),
    "City" VARCHAR(100) NOT NULL,
    "State" VARCHAR(100) NOT NULL,
    "Type" VARCHAR(15) NOT NULL,
    "Website" VARCHAR(500),
    "IsActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS branches (
    "Id" BIGSERIAL PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Code" VARCHAR(20),
    "IsActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS cutoff_data (
    "Id" BIGSERIAL PRIMARY KEY,
    "CollegeId" BIGINT NOT NULL REFERENCES colleges("Id"),
    "BranchId" BIGINT NOT NULL REFERENCES branches("Id"),
    "Exam" VARCHAR(20) NOT NULL,
    "Year" INTEGER NOT NULL,
    "Round" INTEGER DEFAULT 1,
    "Category" VARCHAR(30) DEFAULT 'OPEN',
    "Gender" VARCHAR(10) DEFAULT 'all',
    "CutoffRank" INTEGER,
    "CutoffPercentile" DECIMAL(6,3),
    "CutoffScore" DECIMAL(8,2),
    "SeatsAvailable" INTEGER,
    UNIQUE ("CollegeId", "BranchId", "Exam", "Year", "Round", "Category")
);

-- ========================
-- Chat
-- ========================

CREATE TABLE IF NOT EXISTS career_chat_sessions (
    "Id" BIGSERIAL PRIMARY KEY,
    "Uuid" UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    "StudentId" BIGINT NOT NULL REFERENCES student_profiles("Id"),
    "Title" VARCHAR(255),
    "ContextJson" JSONB,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS career_chat_messages (
    "Id" BIGSERIAL PRIMARY KEY,
    "SessionId" BIGINT NOT NULL REFERENCES career_chat_sessions("Id") ON DELETE CASCADE,
    "Role" VARCHAR(15) NOT NULL,
    "Content" TEXT NOT NULL,
    "MetadataJson" JSONB,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);

-- ========================
-- Counselling
-- ========================

CREATE TABLE IF NOT EXISTS counsellor_sessions (
    "Id" BIGSERIAL PRIMARY KEY,
    "Uuid" UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    "StudentId" BIGINT NOT NULL REFERENCES student_profiles("Id"),
    "CounsellorId" BIGINT NOT NULL REFERENCES counsellor_profiles("Id"),
    "TestAttemptId" BIGINT REFERENCES test_attempts("Id"),
    "SessionType" VARCHAR(15) DEFAULT 'Video',
    "Status" VARCHAR(15) DEFAULT 'Scheduled',
    "ScheduledAt" TIMESTAMPTZ NOT NULL,
    "DurationMinutes" INTEGER DEFAULT 30,
    "MeetingLink" VARCHAR(500),
    "Notes" TEXT,
    "StudentFeedback" TEXT,
    "Rating" SMALLINT,
    "PaymentId" BIGINT,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS counsellor_availabilities (
    "Id" BIGSERIAL PRIMARY KEY,
    "CounsellorId" BIGINT NOT NULL REFERENCES counsellor_profiles("Id"),
    "DayOfWeek" SMALLINT NOT NULL,
    "StartTime" TIME NOT NULL,
    "EndTime" TIME NOT NULL,
    "IsAvailable" BOOLEAN DEFAULT TRUE
);

-- ========================
-- Referrals
-- ========================

CREATE TABLE IF NOT EXISTS referral_codes (
    "Id" BIGSERIAL PRIMARY KEY,
    "UserId" BIGINT NOT NULL,
    "Code" VARCHAR(20) NOT NULL UNIQUE,
    "Type" VARCHAR(20) NOT NULL,
    "MaxUses" INTEGER,
    "TimesUsed" INTEGER DEFAULT 0,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS referrals (
    "Id" BIGSERIAL PRIMARY KEY,
    "ReferrerUserId" BIGINT NOT NULL,
    "ReferredUserId" BIGINT NOT NULL,
    "ReferralCodeId" BIGINT NOT NULL REFERENCES referral_codes("Id"),
    "Status" VARCHAR(15) DEFAULT 'Pending',
    "ConvertedAt" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS referral_rewards (
    "Id" BIGSERIAL PRIMARY KEY,
    "ReferralId" BIGINT NOT NULL REFERENCES referrals("Id"),
    "UserId" BIGINT NOT NULL,
    "RewardType" VARCHAR(15) NOT NULL,
    "Amount" DECIMAL(10,2) NOT NULL,
    "Status" VARCHAR(15) DEFAULT 'Pending',
    "CreditedAt" TIMESTAMPTZ
);

-- ========================
-- Payments (CCAvenue)
-- ========================

CREATE TABLE IF NOT EXISTS payments (
    "Id" BIGSERIAL PRIMARY KEY,
    "Uuid" UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    "UserId" BIGINT NOT NULL,
    "OrderId" VARCHAR(50) NOT NULL UNIQUE,
    "Amount" DECIMAL(10,2) NOT NULL,
    "Currency" CHAR(3) DEFAULT 'INR',
    "PaymentFor" VARCHAR(20) NOT NULL,
    "ReferenceId" BIGINT,
    "Status" VARCHAR(15) DEFAULT 'Initiated',
    "CcavenueTrackingId" VARCHAR(50),
    "CcavenueBankRefNo" VARCHAR(100),
    "CcavenueOrderStatus" VARCHAR(50),
    "PaymentMode" VARCHAR(50),
    "CardName" VARCHAR(100),
    "StatusMessage" VARCHAR(500),
    "CcavenueResponseJson" JSONB,
    "DiscountCode" VARCHAR(50),
    "DiscountAmount" DECIMAL(10,2) DEFAULT 0,
    "PaidAt" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

-- ========================
-- Content / CMS
-- ========================

CREATE TABLE IF NOT EXISTS pages (
    "Id" BIGSERIAL PRIMARY KEY,
    "Slug" VARCHAR(255) NOT NULL UNIQUE,
    "Title" VARCHAR(255) NOT NULL,
    "Content" TEXT NOT NULL,
    "MetaTitle" VARCHAR(255),
    "MetaDescription" TEXT,
    "PageType" VARCHAR(15) DEFAULT 'Static',
    "IsPublished" BOOLEAN DEFAULT FALSE,
    "PublishedAt" TIMESTAMPTZ,
    "AuthorId" BIGINT NOT NULL,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS faqs (
    "Id" BIGSERIAL PRIMARY KEY,
    "Category" VARCHAR(100) NOT NULL,
    "Question" TEXT NOT NULL,
    "Answer" TEXT NOT NULL,
    "DisplayOrder" INTEGER DEFAULT 0,
    "IsPublished" BOOLEAN DEFAULT TRUE
);

-- ========================
-- Notifications
-- ========================

CREATE TABLE IF NOT EXISTS notifications (
    "Id" BIGSERIAL PRIMARY KEY,
    "UserId" BIGINT NOT NULL,
    "Type" VARCHAR(25) NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Message" TEXT NOT NULL,
    "ActionUrl" VARCHAR(500),
    "IsRead" BOOLEAN DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS "IX_notifications_UserId_IsRead" ON notifications ("UserId", "IsRead");

-- ========================
-- Seed Data
-- ========================

-- Interest Categories (10)
INSERT INTO interest_categories ("Code", "Name", "Description", "DisplayOrder") VALUES
('FA', 'Fine Arts', 'Measures interest in activities such as drawing, painting, etc.', 1),
('PA', 'Performing Arts', 'Measures interest in activities such as singing, dancing, acting, etc.', 2),
('MT', 'Machines & Tools', 'Measures interest for working with machines and mechanisms.', 3),
('ME', 'Methodical', 'Measures interest in activities that require high level of meticulousness.', 4),
('PI', 'People Interaction', 'Measures interest in activities involving convincing people.', 5),
('SO', 'Social', 'Measures interest in activities involving contribution towards social causes.', 6),
('WN', 'Working With Numbers', 'Measures interest in activities involving numbers.', 7),
('RA', 'Research & Analysis', 'Measures interest in activities that are scientific in nature.', 8),
('LU', 'Language Usage', 'Measures interest in activities involving languages.', 9),
('OS', 'Outdoor & Sports', 'Measures interest in activities that keeps one outdoors.', 10)
ON CONFLICT ("Code") DO NOTHING;

-- Aptitude Categories (7)
INSERT INTO aptitude_categories ("Code", "Name", "Description", "DisplayOrder") VALUES
('SA', 'Speed & Accuracy', 'Measures aptitude for quick and accurate decision making.', 1),
('NC', 'Number Calculations', 'Measures aptitude for basic mathematical calculations.', 2),
('MA', 'Mechanical Ability', 'Measures aptitude of understanding basic scientific principles.', 3),
('NA', 'Number Application', 'Measures aptitude for applying mathematical principles to practical problems.', 4),
('VA', 'Verbal Ability', 'Measures aptitude for proficiency in language.', 5),
('LA', 'Logical Ability', 'Measures aptitude for interpretation of data in a logical manner.', 6),
('SP', 'Spatial Ability', 'Measures aptitude for visualization of shapes and figures in multiple dimensions.', 7)
ON CONFLICT ("Code") DO NOTHING;

-- Career Streams
INSERT INTO career_streams ("Name", "Description") VALUES
('Science', 'Careers in science, technology, engineering, and mathematics.'),
('Commerce', 'Careers in business, finance, accounting, and economics.'),
('Arts', 'Careers in humanities, social sciences, languages, and creative fields.')
ON CONFLICT ("Name") DO NOTHING;

-- Roles
INSERT INTO roles ("Name", "NormalizedName", "ConcurrencyStamp") VALUES
('Admin', 'ADMIN', gen_random_uuid()::text),
('Student', 'STUDENT', gen_random_uuid()::text),
('Counsellor', 'COUNSELLOR', gen_random_uuid()::text),
('Coordinator', 'COORDINATOR', gen_random_uuid()::text)
ON CONFLICT ("NormalizedName") DO NOTHING;

-- ============================================================
-- Done! All 30+ tables created with seed data.
-- Admin user will be created on first app startup via DbSeeder.
-- ============================================================
