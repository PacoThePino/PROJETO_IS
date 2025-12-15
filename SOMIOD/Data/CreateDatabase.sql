USE master;
GO

-- 1. Se a base de dados já existir (com erros), apaga-a para começar de fresco
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'somiod_db')
BEGIN
    ALTER DATABASE somiod_db SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE somiod_db;
END
GO

-- 2. Criar a base de dados nova
CREATE DATABASE somiod_db;
GO

-- 3. Entrar na base de dados
USE somiod_db;
GO

-- 4. Criar as Tabelas
CREATE TABLE Application (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE, 
    CreationDate DATETIME DEFAULT GETDATE()
);

CREATE TABLE Container (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    CreationDate DATETIME DEFAULT GETDATE(),
    ParentAppId INT NOT NULL, 
    FOREIGN KEY (ParentAppId) REFERENCES Application(Id) ON DELETE CASCADE,
    UNIQUE(Name, ParentAppId) 
);

CREATE TABLE ContentInstance (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL, 
    ContentType NVARCHAR(50) NOT NULL, 
    CreationDate DATETIME DEFAULT GETDATE(),
    ParentContainerId INT NOT NULL,
    FOREIGN KEY (ParentContainerId) REFERENCES Container(Id) ON DELETE CASCADE,
    UNIQUE(Name, ParentContainerId)
);

CREATE TABLE Subscription (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Event NVARCHAR(10) NOT NULL, 
    Endpoint NVARCHAR(255) NOT NULL, 
    CreationDate DATETIME DEFAULT GETDATE(),
    ParentContainerId INT NOT NULL,
    FOREIGN KEY (ParentContainerId) REFERENCES Container(Id) ON DELETE CASCADE,
    UNIQUE(Name, ParentContainerId)
); 

--depois de um post de uma aplicação query para vereficar se foi sucedida 

SELECT * FROM Application;