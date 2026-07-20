DTO Patterns

Contract Rules
- Mark request and response DTOs with [DataContract].
- Mark serialized members with [DataMember].
- Return a concrete DTO type from web-service methods.
- Do not return an interface.
- Do not return object.

Concrete DTO Example

```csharp
[DataContract(Name = "calculator-request")]
public class CalculatorRequest {
	[DataMember(Name = "left")]
	public double Left { get; set; }

	[DataMember(Name = "right")]
	public double Right { get; set; }

	[DataMember(Name = "operation")]
	public string Operation { get; set; }
}

[DataContract(Name = "calculator-response")]
public class CalculatorResponse {
	[DataMember(Name = "success")]
	public bool Success { get; set; }

	[DataMember(Name = "result", EmitDefaultValue = false)]
	public double Result { get; set; }

	[DataMember(Name = "message")]
	public string Message { get; set; }

	[DataMember(Name = "operation")]
	public string Operation { get; set; }
}
```

DTO Return Pattern

```csharp
[OperationContract]
[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
	ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
public CalculatorResponse Calculate(CalculatorRequest request) {
	CalculatorRequest model = request ?? new CalculatorRequest();
	if (string.IsNullOrWhiteSpace(model.Operation)) {
		SetStatusCode(400);
		return new CalculatorResponse {
			Success = false,
			Message = "Operation is required.",
			Operation = string.Empty
		};
	}

	return new CalculatorResponse {
		Success = true,
		Result = 42,
		Message = "Calculation completed.",
		Operation = model.Operation
	};
}
```

Forbidden Patterns
- public ICalculatorResponse Calculate(...)
- public object Calculate(...)
- DTOs with object payload fields when a concrete payload type is known
