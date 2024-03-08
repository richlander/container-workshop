# Troubleshooting

## error MSB4057

You may see the following error:

```dotnetcli
/home/rich/hello-dotnet/hello-dotnet.csproj : error MSB4057: The target "PublishContainer" does not exist in the project.
```

That likely means that you are using a .NET SDK version prior to 8.0.200. In that case, either upgrade to 8.0.200 or install the `Microsoft.NET.Build.Containers` package (which provides the `PublishContainer` task).
