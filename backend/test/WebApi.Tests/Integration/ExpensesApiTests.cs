using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebApi.Common.Caching;
using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Tests.Integration;

public sealed class ExpensesApiTests
{
    [Fact(DisplayName = "POST /expenses should create expense and return 201 Created")]
    public async Task PostExpense_ShouldReturnCreated()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new CreateExpenseRequest
        {
            Description = "Integration lunch",
            Amount = 42.50m,
            Category = "Meals",
            Date = DateTime.UtcNow
        };

        var response = await client.PostAsJsonAsync("/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdExpense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        createdExpense.Should().NotBeNull();
        createdExpense!.Id.Should().NotBeEmpty();
        createdExpense.Description.Should().Be("Integration lunch");
    }

    [Fact(DisplayName = "GET /expenses should return paginated expenses")]
    public async Task GetExpenses_ShouldReturnPagedExpenses()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/expenses", new CreateExpenseRequest
        {
            Description = "Train ticket",
            Amount = 25m,
            Category = "Travel",
            Date = DateTime.UtcNow
        });

        var response = await client.GetAsync("/expenses?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<ExpenseResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact(DisplayName = "GET /expenses/{id} should return expense when it exists")]
    public async Task GetExpenseById_ShouldReturnExpense_WhenItExists()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/expenses", new CreateExpenseRequest
        {
            Description = "Hotel night",
            Amount = 120m,
            Category = "Accommodation",
            Date = DateTime.UtcNow
        });

        var createdExpense = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var response = await client.GetAsync($"/expenses/{createdExpense!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        expense.Should().NotBeNull();
        expense!.Id.Should().Be(createdExpense.Id);
    }

    [Fact(DisplayName = "GET /expenses/{id} should return 404 when expense does not exist")]
    public async Task GetExpenseById_ShouldReturnNotFound_WhenItDoesNotExist()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/expenses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "POST /expenses should return 400 when request is invalid")]
    public async Task PostExpense_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new CreateExpenseRequest
        {
            Description = "",
            Amount = 0,
            Category = "",
            Date = DateTime.UtcNow
        };

        var response = await client.PostAsJsonAsync("/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}