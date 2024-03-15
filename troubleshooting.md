# Troubleshooting

## error MSB4057

You may see the following error:

```dotnetcli
/home/rich/hello-dotnet/hello-dotnet.csproj : error MSB4057: The target "PublishContainer" does not exist in the project.
```

That likely means that you are using a .NET SDK version prior to 8.0.200 while trying to publish a non-Web or non-Worker project as a container. In that case, either upgrade to 8.0.200  or add the the [`Microsoft.NET.Build.Containers`](https://www.nuget.org/packages/Microsoft.NET.Build.Containers) package to that project to provide the required `PublishContainer` MSBuild Target.
