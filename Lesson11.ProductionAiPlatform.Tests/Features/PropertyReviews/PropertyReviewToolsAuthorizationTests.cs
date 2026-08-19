using System.Security.Claims;
using Lesson11.ProductionAiPlatform.Features.PropertyReviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lesson11.ProductionAiPlatform.Tests.Features.PropertyReviews;

public sealed class PropertyReviewToolsAuthorizationTests
{
	[Fact]
	public async Task ReaderCannotCreateProposalThroughAiTool()
	{
		using var services = CreateAuthorizationServices();
		var service = CreatePropertyReviewService();
		var tools = CreateTools(service, services, CreateUser("Reader"));

		var result = await tools.ProposePropertyReviewAsync(
			"0304-12-0042",
			"Client disputes the assessment.",
			PropertyReviewPriority.High);

		Assert.False(result.Authorized);
		Assert.Null(result.Proposal);
		Assert.Empty(service.GetPending());
	}

	[Fact]
	public async Task ReviewerCanCreatePendingProposalThroughAiTool()
	{
		using var services = CreateAuthorizationServices();
		var service = CreatePropertyReviewService();
		var tools = CreateTools(service, services, CreateUser("Reader", "Reviewer"));

		var result = await tools.ProposePropertyReviewAsync(
			"0304-12-0042",
			"Client disputes the assessment.",
			PropertyReviewPriority.High);

		Assert.True(result.Authorized);
		Assert.NotNull(result.Proposal);
		Assert.Equal(PendingPropertyReviewStatus.PendingApproval, result.Proposal.Status);
		Assert.Single(service.GetPending());
		Assert.Empty(service.GetReviews());
	}

	[Fact]
	public async Task MissingHttpContextCannotCreateProposalThroughAiTool()
	{
		using var services = CreateAuthorizationServices();
		var service = CreatePropertyReviewService();
		var accessor = new HttpContextAccessor();
		var tools = new PropertyReviewTools(
			service,
			accessor,
			services.GetRequiredService<IAuthorizationService>());

		var result = await tools.ProposePropertyReviewAsync(
			"0304-12-0042",
			"Client disputes the assessment.",
			PropertyReviewPriority.High);

		Assert.False(result.Authorized);
		Assert.Empty(service.GetPending());
	}

	private static ServiceProvider CreateAuthorizationServices()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddAuthorization(
			options =>
			{
				options.AddPolicy(
					"Reviewer",
					policy =>
					{
						policy.RequireAuthenticatedUser();
						policy.RequireRole("Reviewer");
					});
			});

		return services.BuildServiceProvider();
	}

	private static PropertyReviewTools CreateTools(
		PropertyReviewService service,
		IServiceProvider services,
		ClaimsPrincipal user)
	{
		var accessor = new HttpContextAccessor
		{
			HttpContext = new DefaultHttpContext
			{
				User = user
			}
		};

		return new PropertyReviewTools(
			service,
			accessor,
			services.GetRequiredService<IAuthorizationService>());
	}

	private static PropertyReviewService CreatePropertyReviewService()
	{
		return new PropertyReviewService(
			new InMemoryPendingPropertyReviewRepository(),
			new InMemoryPropertyReviewRepository());
	}

	private static ClaimsPrincipal CreateUser(params string[] roles)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, "test-user@example.com")
		};

		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		return new ClaimsPrincipal(
			new ClaimsIdentity(
				claims,
				authenticationType: "Test"));
	}
}
