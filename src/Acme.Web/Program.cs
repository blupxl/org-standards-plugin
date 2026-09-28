// A stand-in for Acme's design-system website: the stylesheet the design standards point at,
// and a page showing its tokens and components. The design owner's standards server is told
// where this site lives (see the AppHost), so standards never hard-code its address.
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Run();
