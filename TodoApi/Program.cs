using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using TodoApi;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql("name=ToDoDB", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.3.0-mysql")));
    
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Version = "v1" });
});


var app = builder.Build();

app.UseSwagger();


app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
    c.RoutePrefix = string.Empty; // Serve the Swagger UI at the root URL
});

app.UseCors();
// GET: /items
app.MapGet("/items", GetAllItems);

// POST: /items
app.MapPost("/items", CreateNewItem);

// PUT: /items/{id}
app.MapPut("/items/{id}", UpdateItem);

// DELETE: /items/{id}
app.MapDelete("/items/{id}", DeleteItem);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.Run();

// Functions
async Task GetAllItems(ToDoDbContext dbContext, HttpContext context)
{
    var items = await dbContext.Items.ToListAsync();
    await context.Response.WriteAsJsonAsync(items);
}

async Task CreateNewItem(ToDoDbContext dbContext, HttpContext context)
{
     var newItem = await context.Request.ReadFromJsonAsync<Item>();
     if(newItem!=null){
        dbContext.Items.Add(newItem);
    await dbContext.SaveChangesAsync();
    context.Response.StatusCode = 201;
     await context.Response.WriteAsJsonAsync(newItem);
     }
    
}

async Task UpdateItem(ToDoDbContext dbContext, HttpContext context, int id, Item updatedItem)
{
    if (updatedItem == null)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid task data");
        return;
    }
    var existingItem = await dbContext.Items.FindAsync(id);

    if (existingItem == null)
    {
        context.Response.StatusCode = 404; 
        await context.Response.WriteAsync("Item not found");
        return;
    }
    if (updatedItem.Name != null)
    {
        existingItem.Name = updatedItem.Name;

    }
    existingItem.IsComplete = updatedItem.IsComplete;
    context.Response.StatusCode = StatusCodes.Status200OK;
    await dbContext.SaveChangesAsync();
    await context.Response.WriteAsJsonAsync(existingItem);

}

async Task DeleteItem(ToDoDbContext dbContext, HttpContext context, int id)
{

    var itemToDelete = await dbContext.Items.FindAsync(id);

    if (itemToDelete != null)
    {
        dbContext.Items.Remove(itemToDelete);
        await dbContext.SaveChangesAsync();
        context.Response.StatusCode = 200; // No Content
    }
    else
    {
        context.Response.StatusCode = 404; // Not Found
        await context.Response.WriteAsync("Item not found");
    }
}
