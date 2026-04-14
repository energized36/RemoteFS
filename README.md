## Configuration

in appsettings.json, specify the root folder you wish to serve, as well as the username and **hashed** password to login to the server

Example:
```
  "RootPath": "/Users/Michael",
  "AdminCredentials" : {
    "Username" : "admin",
    "Password" : "hashed_password_here"
  }
```

## Setup
1. dotnet run
2. go to http://localhost:5247/
