Test Fixture Pattern

Use the local workspace fixture pattern instead of repository-agnostic examples.

Recommended Shape

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using <PackageNamespace>;
using Terrasoft.Web.Http.Abstractions;

[TestFixture(Category = "PreCommit")]
public class <ServiceName>TestFixture : BaseComposableAppTestFixture {
	private HttpApplicationState _application;
	private HttpContext _context;
	private HttpResponse _response;
	private HttpSessionState _session;
	private IHttpContextAccessor _httpContextAccessor;
	private I<Dependency> _dependency;

	protected override void SetUp() {
		base.SetUp();
		_dependency = Substitute.For<I<Dependency>>();
		<PackageNamespace>.<PackageNamespace>.InjectedServices =
			new[] { new Func<IServiceCollection, IServiceCollection>(services => {
				services.AddSingleton(_dependency);
				return services;
			}) };
		<PackageNamespace>.<PackageNamespace>.Instance.Reset();
		_application = Substitute.For<HttpApplicationState>();
		_context = Substitute.For<HttpContext>();
		_response = Substitute.For<HttpResponse>();
		_session = Substitute.For<HttpSessionState>();
		_context.Application.Returns(_application);
		_context.Response.Returns(_response);
		_context.Session.Returns(_session);
		_httpContextAccessor = CustomSetupHttpContextAccessor(_context, UserConnection);
	}

	protected override void TearDown() {
		<PackageNamespace>.<PackageNamespace>.InjectedServices = null;
		<PackageNamespace>.<PackageNamespace>.Instance.Reset();
		base.TearDown();
	}
}
```

Notes
- Do not copy [MockSettings(RequireMock.All)] unless the current test project actually supports it.
- Reset the package composition root in SetUp() when the package uses one, such as PkgOneApp.Instance.Reset().
- Register test doubles through InjectedServices before resetting the composition root, so the web service resolves the substitute from DI.
- Keep the fixture focused on HTTP context setup. Test business logic separately in service-layer tests when that improves clarity.
