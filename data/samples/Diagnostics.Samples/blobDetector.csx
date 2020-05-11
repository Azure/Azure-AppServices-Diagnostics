[AppFilter(AppType = AppType.WebApp, PlatformType = PlatformType.Windows, StackType = StackType.All)]
[Definition(Id = "blobDetector", Name = "Blob Detector", Author = "reswamin", Category=Categories.AvailabilityAndPerformance, Description = "This detector is stored in azure storage")]
public async static Task<Response> Run(DataProviders dp, OperationContext<App> cxt, Response res)
{
    res.AddMarkdownView("This detector is stored in azure storage");
    return res;
}