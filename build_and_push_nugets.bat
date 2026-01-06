dotnet build -c Release ./Zen.DbAccess.Standard.csproj
dotnet pack -c Release ./Zen.DbAccess.Standard.csproj

dotnet build -c Release ./Zen.DbAccess.Oracle.Standard/Zen.DbAccess.Oracle.Standard.csproj
dotnet pack -c Release ./Zen.DbAccess.Oracle.Standard/Zen.DbAccess.Oracle.Standard.csproj

dotnet build -c Release ./Zen.DbAccess.Postgresql.Standard/Zen.DbAccess.Postgresql.Standard.csproj
dotnet pack -c Release ./Zen.DbAccess.Postgresql.Standard/Zen.DbAccess.Postgresql.Standard.csproj

dotnet build -c Release ./Zen.DbAccess.MariaDb.Standard/Zen.DbAccess.MariaDb.Standard.csproj
dotnet pack -c Release ./Zen.DbAccess.MariaDb.Standard/Zen.DbAccess.MariaDb.Standard.csproj

dotnet build -c Release ./Zen.DbAccess.Sqlite.Standard/Zen.DbAccess.Sqlite.Standard.csproj
dotnet pack -c Release ./Zen.DbAccess.Sqlite.Standard/Zen.DbAccess.Sqlite.Standard.csproj

dotnet build -c Release ./Zen.DbAccess.SqlServer.Standard/Zen.DbAccess.SqlServer.Standard.csproj
dotnet pack -c Release ./Zen.DbAccess.SqlServer.Standard/Zen.DbAccess.SqlServer.Standard.csproj

set "PYTHON_EXE=python"

set "PYTHON_SCRIPT=read_proj_version.py"

set "PROJ_FILE=./Zen.DbAccess.Standard.csproj"

FOR /F "delims=" %%i IN ('%PYTHON_EXE% "%PYTHON_SCRIPT%" "%PROJ_FILE%"') DO (
    set "VERSION=%%i"
)

echo "%VERSION%"

dotnet nuget push ./bin/Release/Zen.DbAccess.Standard.%VERSION%.nupkg --skip-duplicate --api-key %DBACCESS_NUGET_API_KEY% --source https://api.nuget.org/v3/index.json

set "PROJ_FILE=./Zen.DbAccess.Oracle.Standard/Zen.DbAccess.Oracle.Standard.csproj"

FOR /F "delims=" %%i IN ('%PYTHON_EXE% "%PYTHON_SCRIPT%" "%PROJ_FILE%"') DO (
    set "VERSION=%%i"
)

echo "%VERSION%"

dotnet nuget push ./Zen.DbAccess.Oracle.Standard/bin/Release/Zen.DbAccess.Oracle.Standard.%VERSION%.nupkg --skip-duplicate --api-key %DBACCESS_NUGET_API_KEY% --source https://api.nuget.org/v3/index.json

set "PROJ_FILE=./Zen.DbAccess.Postgresql.Standard/Zen.DbAccess.Postgresql.Standard.csproj"

FOR /F "delims=" %%i IN ('%PYTHON_EXE% "%PYTHON_SCRIPT%" "%PROJ_FILE%"') DO (
    set "VERSION=%%i"
)

echo "%VERSION%"

dotnet nuget push ./Zen.DbAccess.Postgresql.Standard/bin/Release/Zen.DbAccess.Postgresql.Standard.%VERSION%.nupkg --skip-duplicate --api-key %DBACCESS_NUGET_API_KEY% --source https://api.nuget.org/v3/index.json

set "PROJ_FILE=./Zen.DbAccess.MariaDb.Standard/Zen.DbAccess.MariaDb.Standard.csproj"

FOR /F "delims=" %%i IN ('%PYTHON_EXE% "%PYTHON_SCRIPT%" "%PROJ_FILE%"') DO (
    set "VERSION=%%i"
)

echo "%VERSION%"

dotnet nuget push ./Zen.DbAccess.MariaDb.Standard/bin/Release/Zen.DbAccess.MariaDb.Standard.%VERSION%.nupkg --skip-duplicate --api-key %DBACCESS_NUGET_API_KEY% --source https://api.nuget.org/v3/index.json

set "PROJ_FILE=./Zen.DbAccess.Sqlite.Standard/Zen.DbAccess.Sqlite.Standard.csproj"

FOR /F "delims=" %%i IN ('%PYTHON_EXE% "%PYTHON_SCRIPT%" "%PROJ_FILE%"') DO (
    set "VERSION=%%i"
)

echo "%VERSION%"

dotnet nuget push ./Zen.DbAccess.Sqlite.Standard/bin/Release/Zen.DbAccess.Sqlite.Standard.%VERSION%.nupkg --skip-duplicate --api-key %DBACCESS_NUGET_API_KEY% --source https://api.nuget.org/v3/index.json

set "PROJ_FILE=./Zen.DbAccess.SqlServer.Standard/Zen.DbAccess.SqlServer.Standard.csproj"

FOR /F "delims=" %%i IN ('%PYTHON_EXE% "%PYTHON_SCRIPT%" "%PROJ_FILE%"') DO (
    set "VERSION=%%i"
)

echo "%VERSION%"

dotnet nuget push ./Zen.DbAccess.SqlServer.Standard/bin/Release/Zen.DbAccess.SqlServer.Standard.%VERSION%.nupkg --skip-duplicate --api-key %DBACCESS_NUGET_API_KEY% --source https://api.nuget.org/v3/index.json


