-- Tạo DB + bảng mẫu cho BaseCrud
IF DB_ID(N'BaseCrudDb') IS NULL
    CREATE DATABASE BaseCrudDb;
GO
USE BaseCrudDb;
GO

IF OBJECT_ID(N'dbo.Customer', N'U') IS NOT NULL
    DROP TABLE dbo.Customer;
GO

CREATE TABLE dbo.Customer
(
    Id          UNIQUEIDENTIFIER NOT NULL,
    Code        NVARCHAR(20)     NOT NULL,
    Name        NVARCHAR(255)    NOT NULL,
    Email       NVARCHAR(255)    NULL,
    Phone       VARCHAR(20)      NULL,
    Status      INT              NOT NULL CONSTRAINT DF_Customer_Status DEFAULT (1),
    Balance     DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_Customer_Balance DEFAULT (0),
    DateOfBirth DATE             NULL,

    -- ISoftDelete
    IsDeleted   BIT              NOT NULL CONSTRAINT DF_Customer_IsDeleted DEFAULT (0),
    -- IAuditable
    CreatedAt   DATETIME2(3)     NOT NULL,
    CreatedBy   NVARCHAR(100)    NULL,
    UpdatedAt   DATETIME2(3)     NULL,
    UpdatedBy   NVARCHAR(100)    NULL,
    -- IHasRowVersion: SQL Server tự tăng mỗi lần UPDATE
    RowVersion  ROWVERSION       NOT NULL,

    -- Id do app sinh bằng SequentialGuid → clustered PK trên Guid vẫn insert tuần tự, ít phân mảnh
    CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED (Id)
);
GO

-- Unique có lọc: chỉ áp dụng cho bản ghi chưa xóa mềm
-- → xóa mềm KH "KH001" rồi tạo lại "KH001" vẫn được.
CREATE UNIQUE INDEX UX_Customer_Code ON dbo.Customer (Code) WHERE IsDeleted = 0;

-- Phục vụ sort mặc định (CreatedAt DESC) của màn hình danh sách
CREATE INDEX IX_Customer_CreatedAt ON dbo.Customer (CreatedAt DESC) WHERE IsDeleted = 0;
GO
