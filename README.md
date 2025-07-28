# EventEase
EventEase is a cloud-based event and venue booking system built with ASP.NET Core MVC and integrated with Azure services. 
It enables users to register, log in, manage venues and events, and make bookings with real-time venue availability tracking.
The system supports role-based access for regular users and booking specialists, ensuring secure and efficient event management.

# Key Features
### 1. User Authentication & Role Management
- Registration/Login using secure SHA256 password hashing.
#### Role-based access:
- Users: Create and manage their own bookings.
- Booking Specialists: View and manage all bookings.

### 2. Venue Management
- **CRUD operations** (Create, Read, Update, Delete) for venues.
- Stores **venue details** (name, capacity, location, image).
- Venue images stored on **Azure Blob Storage**.
- Automatic **availability updates** based on bookings.

 ### 3. Event Management
- **CRUD operations** for events (name, description, date, category, image).
- **Event type categorization** for better filtering.
- Automatic image handling with Azure Blob Storage.

### 4. Booking System
- Users can create, update, and delete bookings.
- Validation rules:
  - No past-date bookings.
  -  Prevents double bookings for the same venue and date.
- Booking specialists can filter bookings by:
  - Event type
  - Venue
  - Date range
  - Availability status

### 5. Real-Time Availability
- Venue availability status updates automatically when bookings are created, updated, or deleted.

# Cloud & Local Deployment Options
##  Cloud (Recommended)
- Uses Azure SQL Database for data storage.
- Uses Azure Blob Storage for images.
- Hosted via Azure Web App for public access.

##  Local Setup (Development)
- Can be configured to run on Local SQL Server instead of Azure.
- Update connection strings in appsettings.json to point to your local database.
- Run Entity Framework migrations to create the required tables locally.

## Technologies & Tools
- **Backend Framework:** ASP.NET Core MVC
- **ORM:** Entity Framework Core (EF Core)
- **Database:** Azure SQL Database / Local SQL Server
- **Storage:** Azure Blob Storage (for venue/event images)
- **Frontend:** Razor Views, Bootstrap 5, JavaScript
- **Hosting:** Azure Web App
- **Version Control:** Git & GitHub
- **IDE:** Visual Studio 2022

## Installation & Setup
### Prerequisites
#### For Local Development
- .NET SDK (version 6 or higher)
- Visual Studio 2022 (Community or higher) or Visual Studio Code
- Microsoft SQL Server (Express/Developer Edition) installed locally
- SQL Server Management Studio (SSMS) to manage your database (create, query, manage tables)
- Git (for cloning the repository)

#### For Cloud Deployment (Azure)
- Azure SQL Database for production data storage
- Azure Blob Storage for storing venue/event images
- Azure App Service for hosting the ASP.NET MVC application
- Azure CLI or Visual Studio with Azure workloads installed
- An active Azure subscription (with resource group configured)

## Setup Instructions
#### 1. Clone the repository

    git clone https://github.com/janetmatshidze/EventEase.git
    cd EventEase

#### 2. Configure Database Connection

    For Local SQL Server:

        Open appsettings.json and replace the DefaultConnection with your local SQL Server connection string:

       "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Database=EventEaseDB;Trusted_Connection=True;MultipleActiveResultSets=true"
       }
#### For Azure SQL Database:

    Use your Azure connection string with username and password:

        "ConnectionStrings": {
          "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=EventEaseDB;Persist Security Info=False;User ID=your-username;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        }

#### 3. Configure Azure Blob Storage

    Add your storage connection string and container name in appsettings.json:

       "BlobStorage": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net",
       "ContainerName": "venue-images"
       }

#### 4. Run Database Migrations

       dotnet ef database update

#### 5. Run Locally

       dotnet run
## Project Structure
- **Controllers:** Handle request logic (e.g., BookingController, VenueController).
- **Models:** Represent database entities (e.g., Booking, Event, Venue).
- **Views:** Razor pages for user interface.
- **Services:** Handle business logic (e.g., BlobStorageService).
-  **wwwroot:** Static files (CSS, JS, images).

## License
- This project is licensed under the MIT License.
  
