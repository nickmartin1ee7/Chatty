using ChatHubClient;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSingleton<ChatHub>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#if !DEBUG
app.UseHttpsRedirection();
#endif

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ChatHub>("/chathub");
});

app.MapGet("/healthcheck",
        () => "Healthy")
.WithName("GetHealthCheck");

app.MapGet("/chathub/clients",
        ([FromServices] ChatHub chatHub) =>
            chatHub.ConnectedUsers)
    .WithName("GetChatHubClients");

app.MapGet("/chathub/message",
        ([FromServices] ChatHub chatHub) =>
            chatHub.Messages)
    .WithName("GetChatHubMessage");

app.MapPost("/chathub/message",
        async ([FromServices] ChatHub chatHub, [FromBody] Message message) =>
            await chatHub.SendMessage(message))
    .WithName("PostChatHubMessage");

app.MapDelete("/chathub/message",
        ([FromServices] ChatHub chatHub) =>
            chatHub.Messages.Clear())
    .WithName("DeleteChatHubMessage");

app.Run();
