﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using N_Tier.Api.IntegrationTests.Config;
using N_Tier.Api.IntegrationTests.Config.Constants;
using N_Tier.Api.IntegrationTests.Helpers;
using N_Tier.Application.Models;
using N_Tier.Application.Models.TodoList;
using N_Tier.Core.Entities;
using N_Tier.DataAccess.Persistence;
using NUnit.Framework;

namespace N_Tier.Api.IntegrationTests.Tests;

[TestFixture]
public class TodoListEndpointTests : BaseOneTimeSetup
{
    [Test]
    public async Task Create_Should_Add_TodoList_In_Database()
    {
        // Arrange
        var context = Host.Services.GetRequiredService<DatabaseContext>();

        var createTodoListModel = Builder<CreateTodoListModel>.CreateNew()
            .Build();

        // Act
        var apiResponse = await Client.PostAsync("/api/todoLists", new JsonContent(createTodoListModel));

        // Assert
        var response = await ResponseHelper.GetApiResultAsync<CreateTodoListResponseModel>(apiResponse);
        var todoListFromDatabase =
            await context.TodoLists.Where(u => u.Id == response.Result.Id).FirstOrDefaultAsync();
        CheckResponse.Succeeded(response);
        todoListFromDatabase.Should().NotBeNull();
        todoListFromDatabase.Title.Should().Be(createTodoListModel.Title);
    }

    [Test]
    public async Task Create_Should_Return_BadRequest_If_Title_Is_Incorrect()
    {
        // Arrange
        var context = Host.Services.GetRequiredService<DatabaseContext>();

        var createTodoListModel = Builder<CreateTodoListModel>.CreateNew()
            .With(ctl => ctl.Title = "1")
            .Build();

        // Act
        var apiResponse = await Client.PostAsync("/api/todoLists", new JsonContent(createTodoListModel));

        // Assert
        var response = await ResponseHelper.GetApiResultAsync<string>(apiResponse);
        var todoListFromDatabase = await context.TodoLists.Where(tl => tl.Title == createTodoListModel.Title)
            .FirstOrDefaultAsync();
        apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CheckResponse.Failure(response);
        todoListFromDatabase.Should().BeNull();
    }

    [Test]
    public async Task Update_Should_Update_Todo_List_From_Database()
    {
        // Arrange
        var context = Host.Services.GetRequiredService<DatabaseContext>();

        var user = await context.Users.Where(u => u.Email == UserConstants.DefaultUserDb.Email).FirstOrDefaultAsync();

        var todoList = Builder<TodoList>.CreateNew()
            .With(tl => tl.Id = Guid.NewGuid())
            .With(tl => tl.CreatedBy = user.Id)
            .Build();

        var todoListFromDatabase = (await context.TodoLists.AddAsync(todoList)).Entity;

        await ((DbContext) context).SaveChangesAsync();

        var updateTodoListModel = Builder<UpdateTodoListModel>.CreateNew()
            .With(utl => utl.Title = "UpdateTodoListTitleIntegration").Build();

        // Act
        var apiResponse = await Client.PutAsync($"/api/todoLists/{todoListFromDatabase.Id}",
            new JsonContent(updateTodoListModel));

        // Assert
        context = (await GetNewHostAsync()).Services.GetRequiredService<DatabaseContext>();
        var response = await ResponseHelper.GetApiResultAsync<UpdateTodoListResponseModel>(apiResponse);
        var updatedTodoListFromDatabase = await context.TodoLists
            .Where(tl => tl.Id == response.Result.Id)
            .FirstOrDefaultAsync();
        CheckResponse.Succeeded(response);
        updatedTodoListFromDatabase.Should().NotBeNull();
        updatedTodoListFromDatabase.Title.Should().Be(updateTodoListModel.Title);
    }

    [Test]
    public async Task Update_Should_Return_NotFound_If_Todo_List_Does_Not_Exist_Anymore()
    {
        // Arrange
        var context = Host.Services.GetRequiredService<DatabaseContext>();

        var updateTodoListModel = Builder<UpdateTodoListModel>.CreateNew()
            .With(utl => utl.Title = "UpdateTodoListIntegration").Build();

        // Act
        var apiResponse =
            await Client.PutAsync($"/api/todoLists/{Guid.NewGuid()}", new JsonContent(updateTodoListModel));

        // Assert
        var response = await ResponseHelper.GetApiResultAsync<string>(apiResponse);
        var updatedTodoListFromDatabase = await context.TodoLists
            .Where(tl => tl.Title == updateTodoListModel.Title)
            .FirstOrDefaultAsync();
        apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CheckResponse.Failure(response);
        updatedTodoListFromDatabase.Should().BeNull();
    }

    [Test]
    public async Task Update_Should_Return_BadRequest_If_Todo_List_Does_Not_Belong_To_User()
    {
        // Arrange  
        var context = Host.Services.GetRequiredService<DatabaseContext>();

        var todoList = Builder<TodoList>.CreateNew()
            .With(tl => tl.Id = Guid.NewGuid())
            .Build();

        var todoListFromDatabase = (await context.TodoLists.AddAsync(todoList)).Entity;

        await context.SaveChangesAsync();

        var updateTodoListModel = Builder<UpdateTodoListModel>.CreateNew().Build();

        // Act
        var apiResponse = await Client.PutAsync($"/api/todoLists/{todoListFromDatabase.Id}",
            new JsonContent(updateTodoListModel));

        // Assert
        var response = await ResponseHelper.GetApiResultAsync<string>(apiResponse);
        var updatedTodoListFromDatabase = await context.TodoLists.Where(tl => tl.Title == updateTodoListModel.Title)
            .FirstOrDefaultAsync();
        apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CheckResponse.Failure(response);
        updatedTodoListFromDatabase.Should().NotBeNull();
        updatedTodoListFromDatabase.Title.Should().Be(todoListFromDatabase.Title);
    }

    [Test]
    public async Task Delete_Should_Delete_Todo_List_From_Database()
    {
        // Arrange
        var context = Host.Services.GetRequiredService<DatabaseContext>();

        var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

        var todoList = Builder<TodoList>.CreateNew()
            .With(tl => tl.Id = Guid.NewGuid())
            .With(tl => tl.CreatedBy = user.Id)
            .Build();

        var todoListFromDatabase = (await context.TodoLists.AddAsync(todoList)).Entity;

        await context.SaveChangesAsync();

        // Act
        var apiResponse = await Client.DeleteAsync($"/api/todoLists/{todoListFromDatabase.Id}");

        // Assert
        var response = await ResponseHelper.GetApiResultAsync<BaseResponseModel>(apiResponse);
        var updatedTodoListFromDatabase =
            await context.TodoLists.Where(tl => tl.Id == response.Result.Id).FirstOrDefaultAsync();
        CheckResponse.Succeeded(response);
        updatedTodoListFromDatabase.Should().BeNull();
    }

    [Test]
    public async Task Delete_Should_Return_NotFound_If_Todo_List_Does_Not_Exist_Anymore()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var apiResponse = await Client.DeleteAsync($"/api/todoLists/{randomId}");

        // Assert
        var response = await ResponseHelper.GetApiResultAsync<string>(apiResponse);
        apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CheckResponse.Failure(response);
    }

    [Test]
    public async Task Get_Todo_Lists_Should_Return_All_Todo_Lists_For_Specified_User_From_Database()
    {
        // Arrange
        var context = Host.Services.GetRequiredService<DatabaseContext>();

        var user = await context.Users.Where(u => u.Email == UserConstants.DefaultUserDb.Email)
            .FirstOrDefaultAsync();

        context.TodoLists.RemoveRange(context.TodoLists.ToList());

        var todoLists = Builder<TodoList>.CreateListOfSize(10).All()
            .With(tl => tl.Id = Guid.NewGuid())
            .With(tl => tl.CreatedBy = user.Id)
            .Build();

        var todoListsNotBelongToTheUser = Builder<TodoList>.CreateListOfSize(10).All()
            .With(tl => tl.Id = Guid.NewGuid())
            .With(tl => tl.CreatedBy = Guid.NewGuid())
            .Build();

        await context.TodoLists.AddRangeAsync(todoLists);
        await context.TodoLists.AddRangeAsync(todoListsNotBelongToTheUser);

        await ((DbContext) context).SaveChangesAsync();

        // Act
        var apiResponse = await Client.GetAsync("/api/todoLists");

        // Assert
        var response = await ResponseHelper.GetApiResultAsync<IEnumerable<TodoListResponseModel>>(apiResponse);
        CheckResponse.Succeeded(response);
        response.Result.Should().HaveCount(10);
    }
}
