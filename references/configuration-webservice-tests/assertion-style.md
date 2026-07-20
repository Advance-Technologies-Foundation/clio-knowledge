Assertion Style

Required Style
- Use explicit // Arrange, // Act, and // Assert comments in every test.
- Add [Description("...")] to every test method.
- Add because: "..." to every assertion.

Example

```csharp
[Test]
[Description("Returns HTTP 400 when division by zero is requested")]
public void Calculate_DivideByZero_SetsBadRequestStatus() {
	// Arrange
	var sut = new CalculatorService {
		HttpContextAccessor = _httpContextAccessor
	};
	var request = new CalculatorRequest {
		Left = 10,
		Right = 0,
		Operation = "divide"
	};

	// Act
	CalculatorResponse response = sut.Calculate(request);

	// Assert
	response.Success.Should().BeFalse(
		because: "division by zero should be returned as a validation failure");
	response.Message.Should().Be("Right operand must not be zero.",
		because: "the endpoint should explain why the request was rejected");
	_response.StatusCode.Should().Be(400,
		because: "validation failures should be exposed as HTTP 400");
}
```
