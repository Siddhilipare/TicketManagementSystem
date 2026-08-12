# Technical Stack Documentation: Ticket Management System

This document provides a comprehensive, minute-level detail of the technology stack, frameworks, libraries, database configurations, and project architecture for the **Ticket Management System** solution.

---

## 1. System Overview & Architecture

- **Architecture Pattern**: 3-Tier Multi-Project Architecture
  - **Presentation Tier**: ASP.NET MVC Web Application
  - **Data Access Tier (DAL)**: Repository Pattern with Enterprise Library & Stored Procedures
  - **Model/Domain Tier**: Data Transfer Objects (DTOs), Domain Models, and ViewModels
- **Application Type**: Monolithic ASP.NET MVC Web Application with Custom JWT and OWIN Cookie Authentication
- **IDE & Tooling**: 
  - **IDE**: Microsoft Visual Studio 2015 / Visual Studio Express 2014 for Web
  - **Solution Format**: Visual Studio Solution File Version 12.00
  - **MSBuild / Tool Version**: 14.0.23107.0

---

## 2. Core Frameworks & Runtime

| Component | Technology | Version / Details |
|---|---|---|
| **Target Framework** | .NET Framework | `net452` (.NET Framework 4.5.2) |
| **Programming Language** | C# | C# 6.0 (`/langversion:6`) |
| **Compiler Platform** | Roslyn / CodeDOM | `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` (v1.0.0) |
| **Native Compilers** | Microsoft.Net.Compilers | `v1.0.0` |
| **Web Engine** | System.Web / ASP.NET | ASP.NET MVC 5 Pipeline |

---

## 3. Solution & Project Structure Breakdown

The solution consists of **3 main projects**:

### 1. `Ticket_Management_System` (Web Application Project)
- **Path**: [Ticket_Management_System/Ticket_Management_System.csproj](file:///c:/Users/Administrator/OneDrive%20-%20Rheal%20Software%20Pvt%20Ltd/Documents/Visual%20Studio%202015/Projects/Ticket_Management_System/Ticket_Management_System/Ticket_Management_System.csproj)
- **Type**: ASP.NET MVC 5 Web Application
- **Responsibilities**:
  - Controllers (`AccountController`, `AdminController`, `HomeController`, `ManageController`, `SupportController`, `TicketController`)
  - Razor Views (`.cshtml`) under `Views/`
  - Action Filters (`JwtAuthenticationFilter.cs`)
  - Security & Authentication Helpers (`JwtTokenService.cs`, `RefreshTokenService.cs`, `AuthCookieHelper.cs`, `PasswordHasherResult.cs`)
  - Asset Bundling & Route Configuration (`BundleConfig.cs`, `RouteConfig.cs`, `Startup.Auth.cs`)

### 2. `TicketDAL` (Data Access Layer Project)
- **Path**: [TicketDAL/TicketDAL.csproj](file:///c:/Users/Administrator/OneDrive%20-%20Rheal%20Software%20Pvt%20Ltd/Documents/Visual%20Studio%202015/Projects/Ticket_Management_System/TicketDAL/TicketDAL.csproj)
- **Type**: C# Class Library
- **Responsibilities**:
  - Stored Procedure execution using Microsoft Enterprise Library Data Access Application Block (DAAB)
  - Data Repositories: `TicketRepository` (`TicketDAL.cs`), `UserDAL.cs`, `TokenDAL.cs`

### 3. `TicketModel` (Domain & ViewModel Layer Project)
- **Path**: [TicketModel/TicketModel.csproj](file:///c:/Users/Administrator/OneDrive%20-%20Rheal%20Software%20Pvt%20Ltd/Documents/Visual%20Studio%202015/Projects/Ticket_Management_System/TicketModel/TicketModel.csproj)
- **Type**: C# Class Library
- **Responsibilities**:
  - Domain Data Models: `TicketModel`, `UserModel`, `RoleModel`, `TicketAttachmentModel`, `TicketCommentModel`, `UserDetailModel`
  - View Models: `CreateTicketViewModel`, `EditTicketViewModel`, `LoginViewModel`, `RegisterViewModel`

---

## 4. Backend Tech Stack & Complete Dependency Matrix

The following table lists every NuGet package and library referenced across the solution with exact versioning and purpose:

| Package / Library ID | Exact Version | Target Framework | Applied Project(s) | Description / Purpose |
|---|---|---|---|---|
| `Microsoft.AspNet.Mvc` | `5.2.3` | `net452` | `Ticket_Management_System` | Model-View-Controller framework for web UI rendering |
| `Microsoft.AspNet.Razor` | `3.2.3` | `net452` | `Ticket_Management_System` | ASP.NET Razor template engine |
| `Microsoft.AspNet.WebPages` | `3.2.3` | `net452` | `Ticket_Management_System` | ASP.NET Web Pages execution runtime |
| `Microsoft.Web.Infrastructure` | `1.0.0.0` | `net452` | `Ticket_Management_System` | Infrastructure interface for dynamic module registration |
| `Microsoft.Owin` | `3.0.1` | `net452` | `Ticket_Management_System` | Open Web Interface for .NET abstraction layer |
| `Microsoft.Owin.Host.SystemWeb` | `3.0.1` | `net452` | `Ticket_Management_System` | OWIN server adapter for IIS ASP.NET request pipeline |
| `Microsoft.Owin.Security` | `3.0.1` | `net452` | `Ticket_Management_System` | Security middleware base infrastructure |
| `Microsoft.Owin.Security.Cookies` | `3.0.1` | `net452` | `Ticket_Management_System` | Cookie-based authentication middleware |
| `Microsoft.Owin.Security.OAuth` | `3.0.1` | `net452` | `Ticket_Management_System` | OAuth 2.0 server & authorization middleware |
| `Microsoft.Owin.Security.Facebook` | `3.0.1` | `net452` | `Ticket_Management_System` | OWIN middleware for Facebook OAuth login |
| `Microsoft.Owin.Security.Google` | `3.0.1` | `net452` | `Ticket_Management_System` | OWIN middleware for Google OAuth login |
| `Microsoft.Owin.Security.MicrosoftAccount` | `3.0.1` | `net452` | `Ticket_Management_System` | OWIN middleware for Microsoft Account login |
| `Microsoft.Owin.Security.Twitter` | `3.0.1` | `net452` | `Ticket_Management_System` | OWIN middleware for Twitter OAuth login |
| `Owin` | `1.0` | `net452` | `Ticket_Management_System` | Core OWIN interface specifications |
| `Microsoft.AspNet.Identity.Core` | `2.2.1` | `net452` | `Ticket_Management_System` | Core ASP.NET Identity user management engine |
| `Microsoft.AspNet.Identity.EntityFramework` | `2.2.1` | `net452` | `Ticket_Management_System` | EF provider for ASP.NET Identity storage |
| `Microsoft.AspNet.Identity.Owin` | `2.2.1` | `net452` | `Ticket_Management_System` | OWIN integration context for ASP.NET Identity |
| `EntityFramework` | `6.1.3` | `net452` | `Ticket_Management_System` | Object-Relational Mapper for identity database schemas |
| `Microsoft.Practices.EnterpriseLibrary.Data` | `4.1.0.0` | `net452` / GAC | `TicketDAL`, `Ticket_Management_System` | Enterprise Library Data Access Application Block (DAAB) |
| `System.IdentityModel.Tokens.Jwt` | `5.7.0` | `net452` | All Projects | Handler for creating, parsing, and validating JWT tokens |
| `Microsoft.IdentityModel.Tokens` | `5.7.0` | `net452` | All Projects | Security token, cryptographic key, and issuer validation |
| `Microsoft.IdentityModel.JsonWebTokens` | `5.7.0` | `net452` | All Projects | Fast token processing engine |
| `Microsoft.IdentityModel.Logging` | `5.7.0` | `net452` | All Projects | Identity logging diagnostic handlers |
| `Newtonsoft.Json` | `13.0.1` | `net452` | All Projects | JSON serialization and deserialization engine |
| `Microsoft.AspNet.Web.Optimization` | `1.1.3` | `net452` | `Ticket_Management_System` | Bundling and minification framework for scripts & stylesheets |
| `WebGrease` | `1.5.2` | `net452` | `Ticket_Management_System` | CSS and JavaScript optimization engine |
| `Antlr` | `3.4.1.9004` | `net452` | `Ticket_Management_System` | Parsing engine relied upon by WebGrease |
| `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` | `1.0.0` | `net452` | `Ticket_Management_System` | Roslyn compiler provider for ASP.NET runtime compilation |
| `Microsoft.Net.Compilers` | `1.0.0` | `net452` | `Ticket_Management_System` | Embedded C# 6 and VB compiler tools |
| `bootstrap` | `3.0.0` | `net452` | `Ticket_Management_System` | Responsive CSS framework |
| `jQuery` | `1.10.2` | `net452` | `Ticket_Management_System` | Core JavaScript DOM manipulation library |
| `jQuery.Validation` | `1.11.1` | `net452` | `Ticket_Management_System` | Client-side form validation library |
| `Microsoft.jQuery.Unobtrusive.Validation` | `3.2.3` | `net452` | `Ticket_Management_System` | HTML5 data-val attribute based validation bridge |
| `Modernizr` | `2.6.2` | `net452` | `Ticket_Management_System` | HTML5/CSS3 feature detection library |
| `Respond` | `1.2.0` | `net452` | `Ticket_Management_System` | Polyfill for CSS3 Media Queries in legacy IE (IE8) |

---

## 5. Database Architecture & Data Access Details

- **Database Engine**: Microsoft SQL Server / SQL Server Express
- **Configured Instance**: `VPNSERVER1\SQLEXPRESS`
- **Database Catalog**: `Training_DB_Siddhi_Lipare`
- **Provider Name**: `System.Data.SqlClient`
- **Connection Strings configured**: `constr`, `DefaultConnection`
- **Data Access Pattern**:
  - **Stored Procedures via Enterprise Library 4.1 DAAB**:
    - Invoked via `DatabaseFactory.CreateDatabase()`, `db.GetStoredProcCommand(...)`, `db.AddInParameter(...)`, `db.ExecuteReader(...)`, `db.ExecuteScalar(...)`, and `db.ExecuteNonQuery(...)`.
  - **Key Stored Procedures**:
    - `Ticket_Create`: Creates new support ticket
    - `Ticket_GetByUserId`: Retrieves filtered tickets by user with keyword, status, and priority filters
    - `Ticket_GetById`: Fetches specific ticket details
    - `Ticket_Update`: Updates ticket title/description
    - `Ticket_Delete`: Removes a ticket
    - `TicketAttachment_Insert` & `TicketAttachment_GetByTicketId`: Manages file attachments for tickets
    - `TicketComment_Insert` & `TicketComment_GetByTicketId`: Manages ticket comments and discussion threads
    - `Token_Save`, `Token_Get`, `Token_Revoke`: Manages JWT refresh tokens in database
    - `User_GetByEmail`, `User_Create`, `User_GetById`: User account management

---

## 6. Frontend & UI Tech Stack

- **View Engine**: ASP.NET Razor Syntax (`.cshtml`)
- **CSS Architecture**:
  - **Framework**: Twitter Bootstrap v3.0.0 (`bootstrap.css`, `bootstrap.min.css`)
  - **Custom Styles**: `Content/Site.css`
  - **Typography & Icons**: Bootstrap Glyphicons Halflings (EOT, SVG, TTF, WOFF)
- **Client-Side Scripts**:
  - **jQuery**: `v1.10.2` (`jquery-1.10.2.js`, `jquery-1.10.2.min.js`)
  - **Form Validation**: `jquery.validate.js` (v1.11.1), `jquery.validate.unobtrusive.js` (v3.2.3)
  - **Browser Compatibility**: `modernizr-2.6.2.js`, `respond.js` (v1.2.0)
- **Asset Bundling**: Configured via `BundleConfig.cs` using `System.Web.Optimization` (combining and minifying `~/bundles/jquery`, `~/bundles/jqueryval`, `~/bundles/modernizr`, `~/bundles/bootstrap`, `~/Content/css`).

---

## 7. Security, Authentication & Authorization Architecture

- **Token-Based JWT Authentication**:
  - **Secret Key Algorithm**: HMAC-SHA256 (256-bit base64 secret `JwtSecretKey`)
  - **Issuer / Audience**: Issuer: `TicketMS`, Audience: `TicketMS_Users`
  - **Token Lifetimes**:
    - **Access Token**: 15 minutes (`AccessTokenExpiryMinutes = 15`)
    - **Refresh Token**: 7 days (`RefreshTokenExpiryDays = 7`)
  - **Authentication Filter**: Custom MVC Filter `JwtAuthenticationFilter` (`OnAuthentication`, `OnAuthenticationChallenge`) validating tokens from HTTP `Authorization` headers (`Bearer <token>`) or fallback authentication cookies.
- **OWIN / ASP.NET Identity Authentication**:
  - OWIN Cookie Authentication middleware initialized in `Startup.Auth.cs`
  - Role-based authorization controls (Roles: `Admin`, `Support`, `User`)

---

## 8. Project Configuration & Metadata Files

- **`Web.config`**: Main application configuration containing database connection strings, JWT appSettings keys, assembly binding redirects, compilation settings, and HTTP modules.
- **`packages.config`**: Project-level dependency tracking files (`Ticket_Management_System/packages.config`, `TicketDAL/packages.config`, `TicketModel/packages.config`).
- **`Bundle.config`**: Asset bundle configuration for style and script optimization.
- **`Global.asax.cs`**: ASP.NET application startup handler (registers Global Filters, Routes, and Asset Bundles).
- **`Startup.cs` / `Startup.Auth.cs`**: OWIN startup class for configuring authentication pipeline.

---

*Document generated automatically for Ticket Management System codebase.*
