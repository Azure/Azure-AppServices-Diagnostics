#load "DetectorUtils"
#load "CredentialTrapper"

[AppFilter(AppType = AppType.WebApp, PlatformType = PlatformType.Windows, StackType = StackType.All)]
[Definition(Id = "testdetectorwithgist", Name = "Test Detector with Gist", Author = "dev", Description = "")]
public async static Task<Response> Run(DataProviders dp, OperationContext<App> cxt, Response res)
{
    res.AddMarkdownView("Hello World!");
    return res;
}