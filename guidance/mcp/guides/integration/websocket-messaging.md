clio MCP WebSocket messaging guide

Scope and ownership
- Use this guide when Creatio backend C# must notify a Freedom UI page, or when a Freedom UI page must bridge a message to the same user's connections or broadcast an announcement through Creatio's built-in message channel.
- This guide owns backend user-channel selection, `SimpleMessage` construction, frontend `PTP` and `BROADCAST` routing, sender/body routing, frontend subscription lifecycle, transient-delivery semantics, and live acceptance.
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
private readonly ILog _logger;

internal WebSocketPublishResult Publish(Guid userId, WebSocketNotification payload) {
	IMsgChannel channel;
	try {
		if (!MsgChannelManager.IsRunning) {
			return WebSocketPublishResult.NotDelivered(
				"The Creatio message channel is not running.");
		}
		channel = MsgChannelManager.Instance.FindItemByUId(userId);
		if (channel == null) {
			return WebSocketPublishResult.NotDelivered(
				"The current user has no active browser channel.");
		}
	} catch (Exception exception) {
		_logger.Warn($"The WebSocket channel for user [{userId}] could not be resolved.", exception);
		return WebSocketPublishResult.NotDelivered(
			"The Creatio message channel is unavailable.");
	}

	Guid eventId = Guid.NewGuid();
	IMsg message = new SimpleMessage {
		Id = eventId,
		Body = JsonConvert.SerializeObject(payload)
	};
	message.Header.Sender = "WebsocketLab.Message";
	try {
		channel.PostMessage(message);
	} catch (Exception exception) {
		_logger.Warn(
			$"WebSocket event [{eventId}] could not be posted to user channel [{userId}].",
			exception);
		return WebSocketPublishResult.NotDelivered(
			"The message could not be posted to the active user channel.");
	}
	return WebSocketPublishResult.Delivered(eventId);
}
```
- Obtain the running manager from the guarded `MsgChannelManager.Instance` singleton. Do not assume `ClassFactory.Get<IMsgChannelManager>()` has a legacy-container binding on modern .NET Creatio.
- Keep the publisher behind a package-owned interface and inject an accessor or adapter so unit tests can substitute the manager and channel without changing the platform singleton.
- Keep a web-service endpoint thin: validate the request, derive `UserConnection.CurrentUser.Id`, delegate to the publisher, and return a concrete response.
- Use `Common.Logging.ILog` for `_logger` and register it in the package service collection with `LogManager.GetLogger(<package logger name>)`; inject that package logger into the publisher rather than resolving it through legacy `ClassFactory`.
- Treat a manager-resolution or `PostMessage` exception as non-delivery: log identifiers and exception details without payload data, then return a non-committal failure value instead of misclassifying every exception as a browser disconnect.
- A successful `PostMessage` means the active server channel accepted the transient event. It is not browser acknowledgement or durable processing.

Frontend PTP and BROADCAST pattern
```javascript
const channel = new sdk.MessageChannelService();

await channel.sendMessage(
	"WebsocketLab.Ptp",
	{ message, sentAtUtc: new Date().toISOString() },
	sdk.MessageChannelType.PTP
);

await channel.sendMessage(
	"WebsocketLab.Broadcast",
	{ message, sentAtUtc: new Date().toISOString() },
	sdk.MessageChannelType.BROADCAST
);
```
- `PTP` routes the frontend-originated message to the authenticated user's connected browser channels. Use it as a same-user frontend bridge, for example between tabs or browser sessions. It is user-scoped, not tab-scoped.
- `BROADCAST` routes the frontend-originated message to every active user channel. The browser route has no package-owned server permission check, so use it only when every authenticated user is allowed to send the message and keep the payload appropriate for every connected user. For a trusted system announcement, call a backend endpoint that checks an operation permission and then uses a backend broadcast primitive.
- Give each route its own stable sender and subscribe to that exact sender. The reference uses `WebsocketLab.Ptp` and `WebsocketLab.Broadcast` so the page can display the flows independently.
- The frontend body must be JSON-compatible. Treat both routes as transient delivery; neither persists work for disconnected users.
- Use the same lifecycle-safe subscription pattern below for backend, PTP, and BROADCAST senders, and unsubscribe every returned handle.

SERVER route boundary
- `sdk.MessageChannelType.SERVER` maps to the platform's internal `ServerMsg` route, but ordinary standalone package code has no verified public backend receive-registration primitive on Creatio 10.0.0.858 .NET 8.
- Creatio's own SERVER consumers are wired through internal core dependency injection. `ClassFactory.Get<IMsgServiceLayer>()` failed live because no matching legacy-container binding was available to the package.
- Treat frontend-to-backend SERVER handling as **INTERNAL/UNSUPPORTED** for application packages. Do not use reflection to reach the core container and do not publish a SERVER example as a supported extension pattern.
- For a supported bidirectional application workflow, send the frontend command through a configuration web service, perform backend work there, and publish the transient backend result through the user channel described in this guide.

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
							const body = event.body;
							if (!body || typeof body.message !== "string" || body.message.length > 1000) {
								return;
							}
							await request.$context.set("UsrIncomingMessage", body.message);
						}
					);
					request.$context.websocketSubscriptionPending = pending;
					try {
						const subscription = await pending;
						if (request.$context.websocketSubscriptionPending !== pending) {
							return;
						}
						request.$context.websocketSubscriptionPending = null;
						request.$context.websocketSubscription = subscription;
					} catch (error) {
						if (request.$context.websocketSubscriptionPending === pending) {
							request.$context.websocketSubscriptionPending = null;
							await request.$context.set("UsrWebSocketStatus", "The WebSocket subscription could not be established.");
						}
					}
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
						try {
							(await pending).unsubscribe();
						} catch (error) {
							// A rejected subscription has no handle to release.
						}
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
- Treat frontend PTP and BROADCAST bodies as untrusted client input. Validate the expected object shape and length before display, render it as plain text, and never drive a privileged action, navigation target, HTML/markup, or backend command directly from the received payload.

Calling a backend endpoint from the page
- Prefer `new sdk.HttpClientService()` for the custom configuration web-service call when the page already uses the SDK.
- On .NET 8 use `/rest/<ServiceName>/<MethodName>`; follow `configuration-webservice` for runtime-specific routing and DTO rules.
- Treat the HTTP response and WebSocket event as separate signals. The response acknowledges publication; the subscription carries the event.

Unit-test acceptance
- Substitute `IMsgChannelManager` and `IMsgChannel` behind the package publisher seam.
- Verify the exact user ID passed to `FindItemByUId`.
- Capture the posted `IMsg` and assert its non-empty ID, exact `Header.Sender`, and JSON body fields.
- Cover a missing active channel, unavailable manager, and throwing manager/channel lookup as failure values without `PostMessage`.
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
6. For PTP, open two tabs as the same user, send from one, and require both subscribed tabs to display the exact body.
7. For BROADCAST, use separately authorized connected users when available and require every subscribed client to display the announcement. Two tabs for one user prove routing but do not prove cross-user authorization or audience behavior.

Failure signals and recovery
| Signal | Meaning | Recovery |
| --- | --- | --- |
| manager not running | Creatio messaging has not started or is stopping | return non-delivery; confirm app readiness before retrying |
| `FindItemByUId` returns `null` | target user has no active browser channel | ask the user to open/resume the page or use persisted state for offline work |
| frontend callback never runs but backend posts | sender mismatch, malformed JSON body, subscription not established, or wrong user | compare exact sender strings, parse the serialized body, inspect lifecycle timing, verify target system-user ID |
| callback runs more than once | leaked subscription or unguarded concurrent resume | guard the resolved handle and pending promise, then pair lifecycle cleanup |
| `ClassFactory` activation error for `IMsgChannelManager` | modern runtime has no matching legacy-container binding | use guarded `MsgChannelManager.Instance` |
| `ClassFactory` activation error for `IMsgServiceLayer` | package code attempted to consume the internal SERVER receive path | remove the SERVER handler; use a configuration web service for frontend-to-backend commands |
| HTTP succeeds but no browser acknowledgement exists | publication and client processing were conflated | treat visible callback as the client signal; persist state if processing must be durable |

Security and limits
- Do not include secrets, authorization headers, unrestricted business data, or large result bodies in a message.
- Persist important or large state and send only a record ID, correlation ID, or refresh signal.
- Do not use backend `PostToAll` or frontend `MessageChannelType.BROADCAST` by default. Both broadcast to every active user channel. Guard backend broadcast with an operation permission; treat frontend BROADCAST as low-trust because package code cannot enforce that permission on the browser route.
- Validate every frontend-originated body at the subscriber boundary. A sender string routes messages; it does not authenticate a trusted publisher.
- One user may have multiple physical browser connections represented by the user-scoped channel. Do not promise tab-specific delivery.
- Cluster transport is platform-owned. Application code must stay on the same `MsgChannelManager` contract and must not implement its own Redis coordination.

Reference implementation
- Use the published `atf.creatio.websocket-reference` catalog item for the complete package, Freedom UI page, unit tests, and lab evidence.
