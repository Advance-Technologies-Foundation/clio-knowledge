Composition Root Pattern

Keep the service thin and push non-transport logic into an application service.

Service Layer Example

```csharp
public interface ICalculatorEngine {
	ErrorOr<double> Calculate(double left, double right, string operation);
}

internal sealed class CalculatorEngine : ICalculatorEngine {
	public ErrorOr<double> Calculate(double left, double right, string operation) {
		// Core logic lives here, not in the web service.
	}
}
```

Package Registration Example

Register new services in the package composition root.

```csharp
private static ServiceProvider Init() {
	ServiceCollection serviceCollection = new ServiceCollection();
	// UserConnection is owned by the Creatio platform (the per-request connection).
	// Never register it as a container-managed (scoped/transient) service: the DI
	// container disposes any IDisposable it resolves from a factory when the scope
	// closes, which would tear down the platform connection's DB executors and clear
	// UserConnection.Current mid-request. Expose it through a Func accessor instead so
	// the container never owns its lifetime; the platform disposes it at request end.
	serviceCollection.AddTransient<Func<UserConnection>>(sp => () => UserConnection);
	serviceCollection.AddSingleton<ICalculatorEngine, CalculatorEngine>();
	return serviceCollection.BuildServiceProvider();
}
```

A service that genuinely needs the connection takes the accessor and resolves it per call
(never store the resolved connection in a field, and never dispose it):

```csharp
internal sealed class ContactRepository : IContactRepository {
	private readonly Func<UserConnection> _userConnectionAccessor;

	public ContactRepository(Func<UserConnection> userConnectionAccessor) {
		_userConnectionAccessor = userConnectionAccessor;
	}

	public int CountContacts() {
		UserConnection userConnection = _userConnectionAccessor();
		// Use userConnection for the current request (ESQ/Select/etc.).
		// Do not cache it across requests and do not dispose it.
		return new EntitySchemaQuery(userConnection.EntitySchemaManager, "Contact")
			.GetEntityCollection(userConnection).Count;
	}
}
```

Web Service Usage Example

```csharp
public CalculatorResponse Calculate(CalculatorRequest request) {
	CalculatorRequest model = request ?? new CalculatorRequest();
	using (IServiceScope scope = PkgOneApp.Instance.CreateScope()) {
		ICalculatorEngine calculator = scope.ServiceProvider.GetRequiredService<ICalculatorEngine>();
		ErrorOr<double> result = calculator.Calculate(model.Left, model.Right, model.Operation);
		if (result.IsError) {
			SetStatusCode(400);
			return new CalculatorResponse {
				Success = false,
				Message = result.FirstError.Description,
				Operation = model.Operation ?? string.Empty
			};
		}
		return new CalculatorResponse {
			Success = true,
			Result = result.Value,
			Message = "Calculation completed.",
			Operation = model.Operation ?? string.Empty
		};
	}
}
```

UserConnection Lifetime Rule
- The platform owns the per-request UserConnection and disposes it at request end.
- Never register UserConnection as a scoped or transient service, and never dispose it
  from package code. Inject Func<UserConnection> and call it where the connection is needed.
- Service scopes are still fine for genuinely package-owned scoped services; just keep
  UserConnection out of the container's lifetime tracking.
