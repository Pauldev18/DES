using System;

AppContext.SetSwitch("System.Net.DisableIPv6", true);
Console.WriteLine("IPv6 Disabled: " + AppContext.TryGetSwitch("System.Net.DisableIPv6", out var disabled) + " = " + disabled);
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
