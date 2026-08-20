clio MCP backend-to-frontend WebSocket messaging guide

Scope and ownership
- Use this guide when Creatio backend C# must notify a Freedom UI page through Creatio's built-in message channel.
- This guide owns user-channel selection, `SimpleMessage` construction, sender/body routing, frontend subscription lifecycle, transient-delivery semantics, and live acceptance.
- For the configuration web-service envelope, read `configuration-webservice`; for its focused tests, read `configuration-webservice-tests`.
- For page-body mechanics, also read `page-schema-handlers` and `page-schema-creatio-devkit-common`.
- Use the platform message channel. Do not create another WebSocket server or custom reconnect protocol for this workflow.

Delivery contract
- Treat the message channel as a transient notification path, not a durable queue.
- Delivery requires `MsgChannelManager.IsRunning` and an active channel for the target system user.
- `MsgChannelManager.Instance.FindItemByUId(userId)` returns `null` when the user has no connected browser. Return an expected non-delivery result; do not dereference it or claim offline delivery.
- `SimpleMessage.Header.Sender` is the exact frontend subscription key. Define one stable sender constant and use the same case-sensitive value on both sides.
- Serialize a defined DTO to valid JSON and assign that string to `SimpleMessage.Body`. The modern frontend service JSON-parses string bodies before invoking the callback; malformed JSON is dropped.
- Generate a new `SimpleMessage.Id`. Include a correlation ID in the DTO when the HTTP request or background operation must be matched to the received event.
- Target the authenticated user with `UserConnection.CurrentUser.Id` unless an independently authorized server-side workflow owns another user ID. Do not trust a browser-supplied target user ID.

Backend pattern
```csharp
internal WebSocketPublishResult Publish(Guid userId, WebSocketNotification payload) {
	if (!MsgChannelManager.IsRunning) {
		return WebSocketPublishResult.NotDelivered(
			"The Creatio message channel is not running.");
	}

	IMsgChannel channel = MsgChannelManager.Instance.FindItemByUId(userId);
	if (channel == null) {
		return WebSocketPublishResult.NotDelivered(
			"The current user has no active browser channel.");
	}

	Guid eventId = Guid.NewGuid();
	IMsg message = new SimpleMessage {
		Id = eventId,
		Body = JsonConvert.SerializeObject(payload)
	};
	message.Header.Sender = "WebsocketLab.Message";
	try {
		channel.PostMessage(message);
	} catch (Exception) {
		return WebSocketPublishResult.NotDelivered(
			"The active user channel closed before the message could be posted.");
	}
	return WebSocketPublishResult.Delivered(eventId);
}
```
- Obtain the running manager from the guarded `MsgChannelManager.Instance` singleton. Do not assume `ClassFactory.Get<IMsgChannelManager>()` has a legacy-container binding on modern .NET Creatio.
- Keep the publisher behind a package-owned interface and inject an accessor or adapter so unit tests can substitute the manager and channel without changing the platform singleton.
- Keep a web-service endpoint thin: validate the request, derive `UserConnection.CurrentUser.Id`, delegate to the publisher, and return a concrete response.
- Treat a `PostMessage` exception as a transient disconnect race: log it without payload data and return non-delivery instead of turning an expected browser-close event into HTTP 500.
- A successful `PostMessage` means the active server channel accepted the transient event. It is not browser acknowledgement or durable processing.

Freedom UI subscription pattern
```javascript
define("UsrSome_Page", /**SCHEMA_DEPS*/["@creatio-devkit/common"]/**SCHEMA_DEPS*/,
	function/**SCHEMA_ARGS*/(sdk)/**SCHEMA_ARGS*/ {
	const senderName = "WebsocketLab.Message";
	return {
		handlers: /**SCHEMA_HANDLERS*/[
			{
				request: "crt.HandleViewModelResumeRequest",
				handler: async (request, next) => {
					await next?.handle(request);
					if (request.$context.websocketSubscription ||
						request.$context.websocketSubscriptionPending) {
						return;
					}
					const channel = new sdk.MessageChannelService();
					const pending = channel.subscribe(
						senderName,
						async event => {
							await request.$context.set("UsrIncomingMessage", event.body.message);
						}
					);
					request.$context.websocketSubscriptionPending = pending;
					const subscription = await pending;
					if (request.$context.websocketSubscriptionPending !== pending) {
						return;
					}
					request.$context.websocketSubscriptionPending = null;
					request.$context.websocketSubscription = subscription;
				}
			},
			{
				request: "crt.HandleViewModelPauseRequest",
				handler: async (request, next) => {
					request.$context.websocketSubscription?.unsubscribe();
					request.$context.websocketSubscription = null;
					const pending = request.$context.websocketSubscriptionPending;
					if (pending) {
						request.$context.websocketSubscriptionPending = null;
						(await pending).unsubscribe();
					}
					return next?.handle(request);
				}
			}
		]/**SCHEMA_HANDLERS*/
	};
});
```
- Use public `new sdk.MessageChannelService()` from `@creatio-devkit/common` for new Freedom UI code. Do not start new code with legacy `Terrasoft.ServerChannel.on/un`.
- Pair lifecycle requests. Prefer resume/pause for a page that can be suspended while its view model remains alive; init/destroy is also valid when the subscription should span the whole view-model lifetime.
- Guard both the resolved handle and the in-flight subscription promise. Creatio can dispatch concurrent resume requests before the first `subscribe` resolves; checking only the resolved handle can leak a duplicate callback.
- In the paired teardown handler, unsubscribe the resolved handle and take ownership of any in-flight promise by clearing and awaiting it. This closes the resume/pause race without a custom lifecycle framework.
- Store subscription handles as transient runtime references on `$context`; do not declare them as serializable page attributes.
- Read the parsed payload from `event.body`; `event.id` is the backend `SimpleMessage.Id`.

Calling a backend endpoint from the page
- Prefer `new sdk.HttpClientService()` for the custom configuration web-service call when the page already uses the SDK.
- On .NET 8 use `/rest/<ServiceName>/<MethodName>`; follow `configuration-webservice` for runtime-specific routing and DTO rules.
- Treat the HTTP response and WebSocket event as separate signals. The response acknowledges publication; the subscription carries the event.

Unit-test acceptance
- Substitute `IMsgChannelManager` and `IMsgChannel` behind the package publisher seam.
- Verify the exact user ID passed to `FindItemByUId`.
- Capture the posted `IMsg` and assert its non-empty ID, exact `Header.Sender`, and JSON body fields.
- Cover a missing active channel and unavailable manager as failure values without `PostMessage`.
- Cover a channel that throws during `PostMessage` as a transient non-delivery result.
- For a web-service adapter, verify current-user targeting, input validation, correlation mapping, and non-delivery mapping.
- Follow `configuration-webservice-tests`: NUnit, explicit Arrange/Act/Assert, `[Description]`, and a `because` explanation for every assertion.

Live acceptance
1. Build and load the package through the environment's current deployment-mode guidance.
2. Open the subscribed page as the same authenticated user the backend targets. Wait for the resume handler to establish the subscription.
3. Trigger the backend through the real UI or approved service boundary.
4. Require independent signals:
   - the REST or operation response reports success and a non-empty event ID;
   - the visible page receives the exact payload through `event.body`;
   - the browser console has no new message parsing or handler errors.
5. Exercise whitespace/invalid input without a backend call and navigate away/back before another send to detect leaked duplicate subscriptions.

Failure signals and recovery
| Signal | Meaning | Recovery |
| --- | --- | --- |
| manager not running | Creatio messaging has not started or is stopping | return non-delivery; confirm app readiness before retrying |
| `FindItemByUId` returns `null` | target user has no active browser channel | ask the user to open/resume the page or use persisted state for offline work |
| frontend callback never runs but backend posts | sender mismatch, malformed JSON body, subscription not established, or wrong user | compare exact sender strings, parse the serialized body, inspect lifecycle timing, verify target system-user ID |
| callback runs more than once | leaked subscription or unguarded concurrent resume | guard the resolved handle and pending promise, then pair lifecycle cleanup |
| `ClassFactory` activation error for `IMsgChannelManager` | modern runtime has no matching legacy-container binding | use guarded `MsgChannelManager.Instance` |
| HTTP succeeds but no browser acknowledgement exists | publication and client processing were conflated | treat visible callback as the client signal; persist state if processing must be durable |

Security and limits
- Do not include secrets, authorization headers, unrestricted business data, or large result bodies in a message.
- Persist important or large state and send only a record ID, correlation ID, or refresh signal.
- Do not use `PostToAll` by default. It broadcasts to every active user channel and requires an explicit authorization and data-exposure decision.
- One user may have multiple physical browser connections represented by the user-scoped channel. Do not promise tab-specific delivery.
- Cluster transport is platform-owned. Application code must stay on the same `MsgChannelManager` contract and must not implement its own Redis coordination.

Reference implementation
- Use the published `atf.creatio.websocket-reference` catalog item for the complete package, Freedom UI page, unit tests, screenshots, and lab evidence.
