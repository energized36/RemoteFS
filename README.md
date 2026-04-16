# Configuration

## Setup
1. Navigate to the project folder's root
2. in appsettings.Development.json, specify the root folder you wish to serve, as well as the username and **hashed** password to login to the server

Example:
```
  "RootPath": "/Users/Michael",
  "AdminCredentials" : {
    "Username" : "username_here",
    "Password" : "hashed_password_here"
  }
```
3. in the terminal, run the following command: ```dotnet run```
4. go to http://localhost:5247
