Status Code Patterns

Route Prefixes
- NET472: /0/rest/<ServiceName>/<MethodName>
- NETSTANDARD2_0 and newer runtime: /rest/<ServiceName>/<MethodName>

Document both routes when the package targets both runtimes.

Set Status Code

```csharp
private void SetStatusCode(int statusCode) {
#if NETSTANDARD2_0
	HttpContextAccessor.GetInstance().Response.StatusCode = statusCode;
#else
	WebOperationContext.Current.OutgoingResponse.StatusCode = (HttpStatusCode)statusCode;
#endif
}
```

Choose The Right Response Style
Use DTO return when:
- the endpoint is a normal JSON API
- Creatio serialization is acceptable
- you only need to set status code before returning

Use void and manual body writing when:
- you need explicit control over body and status
- the transport contract is the main concern

Use Stream when:
- returning HTML, text, assets, or binary content

Manual Body Example

```csharp
[OperationContract]
[WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json,
	ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
public void Ping() {
	string json = "{\"status\":\"accepted\"}";
	byte[] payload = Encoding.UTF8.GetBytes(json);

#if NETSTANDARD2_0
	var response = HttpContextAccessor.GetInstance().Response;
	response.ContentType = "application/json; charset=utf-8";
	response.StatusCode = 202;
	response.OutputStream.Write(payload, 0, payload.Length);
	response.OutputStream.Flush();
#else
	WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
	WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Accepted;
	HttpContextAccessor.GetInstance().Response.OutputStream.Write(payload, 0, payload.Length);
	HttpContextAccessor.GetInstance().Response.OutputStream.Flush();
#endif
}
```

Forbidden Pattern
- Do not return 204 NoContent and also write a response body.
