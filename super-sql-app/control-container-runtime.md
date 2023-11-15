# Controlling how your containers run

The .NET SDK generates container images that run in a  _rootless_ configuration by default - this means that the user the application runs on doesn't have root permissions. This has a number of implications, but broadly means that rootless applications cannot perform actions like

* write files to their local application directory
* bind to ports lower than 1024
* *insert more here*

Let's take a look at how that might appear for an application that stores data in a local sqlite database.


## Publish the console application

Let's publish our console application using the SDK to our local Docker engine:

```bash
$ dotnet publish -t:PublishContainer
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Debug\net7.0\linux-x64\super-sql-app.dll
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Debug\net7.0\linux-x64\publish\
  Building image 'super-sql-app' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:7.0'.
  Pushed image 'super-sql-app:latest' to local registry via 'docker'.
```

Our app is targeting .NET 7 currently. This application is a very simple storage app that either

* writes a given name to a sqlite database, or
* retrieves the id of a name from the sqlite database 

Sqlite writes data to a local database, which in our app is called `hello.db`. Let's run our new app:

```bash
$ docker run -it --rm super-sql-app set Chet
Inserted Chet as 1
```

## Update the application to .NET 8

Great, now lets update our app to target .NET 8 by updating the TargetFramework property

```bash
$ grep TargetFramework super-sql-app.csproj
TargetFramework>net8.0</TargetFramework>
```

Now publish (note that the .NET 8 base images are used):

```bash
> dotnet publish -t:PublishContainer
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Release\net8.0\super-sql-app.dll
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Release\net8.0\publish\
  Building image 'super-sql-app' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'super-sql-app:latest' to local registry via 'docker'.
```

and run:

```bash
$  docker run -it --rm super-sql-app set Chet
Unhandled exception. Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 14: 'unable to open database file'.
   at Microsoft.Data.Sqlite.SqliteException.ThrowExceptionForRC(Int32 rc, sqlite3 db)
   at Microsoft.Data.Sqlite.SqliteConnectionInternal..ctor(SqliteConnectionStringBuilder connectionOptions, SqliteConnectionPool pool)
   at Microsoft.Data.Sqlite.SqliteConnectionPool.GetConnection()
   at Microsoft.Data.Sqlite.SqliteConnectionFactory.GetConnection(SqliteConnection outerConnection)
   at Microsoft.Data.Sqlite.SqliteConnection.Open()
   at Program.<>c__DisplayClass0_0.<<Main>$>g__EnsureTable|2() in C:\Users\chethusk\Code\container-workshop\super-sql-app\Program.cs:line 76
   at Program.<>c__DisplayClass0_2.<<Main>$>b__4() in C:\Users\chethusk\Code\container-workshop\super-sql-app\Program.cs:line 46
   at Program.<Main>$(String[] args) in C:\Users\chethusk\Code\container-workshop\super-sql-app\Program.cs:line 10   
```

What happened?! In .NET 8 our container is running as nonroot and our application tries to create a sqlite database file in the application directory.  At this point we have a few options

### Run the same container and override the user via Docker

```bash
$ docker run -it --rm --user root super-sql-app set Chet
Inserted Chet as 1
```

This works, but requires you to change your deployment.

### Tell the SDK to use a root user

> Note - this is currently bugged in .NET SDK 8.0.100 - follow [this issue](https://github.com/dotnet/sdk-container-builds/issues/520) for details.

By setting `ContainerUser` to root, you can tell the SDK explicitly to operate in a root-capable mode. You can set this via the command line or project properties:

```bash
$ dotnet publish -t:PublishContainer -p ContainerUser=root
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Release\net8.0\super-sql-app.dll
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Release\net8.0\publish\
  Building image 'super-sql-app' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'super-sql-app:latest' to local registry via 'docker'.
```

Now run the container and you should see the application successfully write to the database:

```bash
$ docker run -it --rm super-sql-app set Chet
Inserted Chet as 1
```

### Change your application to react to non-root permissions

In this application the database is being written to the local app directory. A more safe thing to do might be to write the database to the user's home directory instead:

```csharp
var dataSource = "/home/app/hello.db";
```

Now, publish the app:

```bash
$ dotnet publish -t:PublishContainer
MSBuild version 17.8.3+195e7f5a3 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Release\net8.0\super-sql-app.dll
  super-sql-app -> C:\Users\chethusk\Code\container-workshop\super-sql-app\bin\Release\net8.0\publish\
  Building image 'super-sql-app' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime:8.0'.
  Pushed image 'super-sql-app:latest' to local registry via 'docker'.
```

and run it one final time:

```bash
$ docker run -it --rm super-sql-app set Chet
Inserted Chet as 1
```