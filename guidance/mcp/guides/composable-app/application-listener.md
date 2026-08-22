# Creatio Application And Session Lifecycle Listeners

## Scope And Ownership

- Use this guide when package C# must react to Creatio application start/end or web-session start/end.
- `IAppEventListener` is the platform interface. `AppEventListenerBase` is its no-op base implementation and is the preferred extension point when only selected hooks are needed.
- This guide owns listener discovery, instance lifetime, all four lifecycle hooks, delegation to package-owned state, failure behavior, and tests.
- Also read `creatio-composable-app-development` for package ownership and the package application/DI root.
- Do not transfer the private-field state rule from `configuration-entity-event-listener`: application listeners have a different activation lifetime.

## Platform Contract

- Creatio discovers `IAppEventListener` implementations in the compiled workspace assemblies. No DI registration or custom dispatcher registration is required.
- The listener class MUST be concrete and MUST have a public parameterless constructor. Prefer a `public sealed` class with no explicit constructor as the package convention. Construction and static initialization MUST be side-effect-free and non-fallible; move service resolution and work into the hook bodies.
- Creatio creates a fresh listener instance for each dispatched event. A field written during `OnAppStart` is not available to `OnAppEnd`; the same rule applies between session hooks.
- The listener MUST NOT own cross-hook state in instance fields. Resolve and delegate to package-owned singleton state instead.
- Creatio invokes every discovered listener. Do not depend on ordering between listener types.
- After a listener is activated successfully, Creatio catches and logs exceptions from its hook, then continues dispatching other listeners. A constructor or static-initializer failure is outside that hook exception boundary and can abort dispatch before later listeners run.
- All hooks are synchronous `void` methods. Keep them short and delegate long-lived work to an owner with explicit start/stop behavior.
- `AppEventContext` carries application state only. It does not expose the session object or a portable user-identity contract.
- Session hooks MUST NOT be used as authentication, authorization, revocation, mandatory-audit, impersonation, or privileged-action boundaries. Do not authorize from `AppEventContext` or ambient principal/context state. Any identity-sensitive work requires a separately verified runtime-specific `UserConnection` and the normal permission checks; enforce security-critical outcomes outside this fail-open listener path.

## The Four Hooks

| Hook | Use | Required boundary |
| --- | --- | --- |
| `OnAppStart(AppEventContext)` | Start package-owned process-local runtime state after the application host raises its start event. | Start MUST be idempotent. One callback is per application host, not cluster-global ownership. |
| `OnAppEnd(AppEventContext)` | Stop and release package-owned process-local runtime state during graceful host shutdown. | Shutdown MUST be bounded and consumes a shared host deadline with every other listener. Do not assume this hook runs after a crash or forced process termination. |
| `OnSessionStart(AppEventContext)` | Observe that the current host raised a session-start event. | Keep work constant-time and thread-safe. Do not treat the hook as a portable login event or infer a user/session identifier from `AppEventContext`. |
| `OnSessionEnd(AppEventContext)` | Observe that the current host raised a session-end event. | Keep work constant-time and thread-safe. Do not use it as the only durable logout, cleanup, or commit boundary. |

`OnSessionExpired` is not an `IAppEventListener` hook. The public listener surface contains only the four methods above.

The .NET Framework and .NET Core hosts raise the same four callbacks, but their ordering relative to unrelated platform services and their session-establishment paths differ. Package code MUST NOT depend on one host's surrounding order unless that dependency is separately verified for the target runtime and version.

In a web farm, session start and end are not guaranteed to run on the same node or form an exactly-once pair. Do not keep paired session counts or correlation state in a process-local singleton. If a separately verified runtime API provides a stable session key and correlation is required, process the notification outside the hook using idempotent cluster-aware storage.

## Stateless Listener Pattern

Keep the reflection-created listener as an adapter. The service names below are package-owned placeholders, not Creatio platform interfaces.

Place the source at `packages/<PACKAGE_NAME>/Files/src/cs/EntryPoints/ApplicationListeners/<ListenerName>.cs` under namespace `<PackageNamespace>.EntryPoints.ApplicationListeners`, unless the package already has a stricter entry-point convention.

```csharp
using Terrasoft.Web.Common;

namespace UsrPackage.EntryPoints {
	public sealed class UsrApplicationListener : AppEventListenerBase {
		public override void OnAppStart(AppEventContext context) {
			UsrApplication.Instance
				.GetRequiredService<IApplicationRuntime>()
				.Start();
		}

		public override void OnAppEnd(AppEventContext context) {
			UsrApplication.Instance
				.GetRequiredService<IApplicationRuntime>()
				.Stop();
		}

		public override void OnSessionStart(AppEventContext context) {
			UsrApplication.Instance
				.GetRequiredService<ISessionLifecycleObserver>()
				.OnStarted(context);
		}

		public override void OnSessionEnd(AppEventContext context) {
			UsrApplication.Instance
				.GetRequiredService<ISessionLifecycleObserver>()
				.OnEnded(context);
		}
	}
}
```

- Register `IApplicationRuntime` and `ISessionLifecycleObserver` as package singletons, or delegate both concerns to one cohesive singleton when the package is small.
- The application runtime MUST guard duplicate `Start`/`Stop` calls and shared transitions with synchronization.
- Do not clear runtime state until workers have actually stopped. If bounded shutdown times out, retain the live state and log an actionable warning so a second runtime cannot start over it. When the worker later exits, its completion path MUST dispose resources and atomically clear that same owner exactly once so a future start can recover.
- Session callbacks can overlap across sessions. Any shared observer state MUST be thread-safe.
- Session hooks MUST NOT wait for synchronous network or database I/O. For external observation, perform only a non-blocking enqueue into a bounded queue with an explicit overload/drop policy, then process it outside the hook.
- Do not start a thread, timer, consumer, or native client directly in the listener and keep its handle in a listener field.
- Do not perform unbounded network, database, or thread joins inside any hook.
- Every application-end listener runs serially and consumes the same host shutdown deadline. Cancel immediately and use a small configurable wait; never treat the host's entire shutdown allowance as this package's private timeout.
- Log lifecycle stage and safe correlation identifiers, but never credentials, connection strings, tokens, or unrestricted session data.

## Unit-Test Acceptance

1. Inject substitutes through the package application root and reset singleton state between tests.
2. Construct separate listener instances for start and end calls. This prevents a test from accidentally depending on instance reuse that Creatio does not provide.
3. Invoke all four hooks and verify exact delegation:
   - application start calls the singleton runtime `Start` once;
   - application end calls the same singleton runtime `Stop` once;
   - session start and end call the singleton observer once each and pass the exact `AppEventContext`.
4. Assert that repeated runtime `Start` does not create duplicate workers and repeated `Stop` is safe.
5. Exercise concurrent session notifications when the observer mutates shared state.
6. Verify each session callback returns within the package's small latency budget without waiting for downstream processing, including when its bounded queue is full.
7. Exercise a shutdown timeout in the runtime owner and verify it preserves live state, prevents a duplicate start, and emits a warning. Then complete the worker and verify cleanup happens exactly once and one later start succeeds.
8. Exercise the package's small application-end wait alongside other listeners' waits and verify the cumulative time remains within the shared host shutdown budget.

Test the listener only as a delegation boundary. Test worker loops, cancellation, joins, resource disposal, and failure recovery on the singleton runtime that owns them.

## Optional Live Acceptance

Use a disposable or explicitly approved environment when the package owns a real background runtime.

1. Follow `core-rules` for the confirmation-gated C# compile and runtime-specific restart cycle. Trigger a restart only when that delivery cycle requires it to load the listener or when an approved lifecycle test specifically requires a new application-start event.
2. Require one attributable application-start signal per host process and verify the singleton runtime is active.
3. Create and end a test session only through the target runtime's supported flow. Treat session identity and surrounding ordering as runtime-specific observations, not portable API promises.
4. With explicit approval for that destructive lifecycle test, perform a graceful application stop and require the package runtime to stop within its configured bound.
5. Restart and confirm no stale worker, duplicate subscription, native handle, or thread remains.

Do not use a shared or production environment merely to prove lifecycle dispatch.

## Failure Signals And Recovery

| Signal | Meaning | Recovery |
| --- | --- | --- |
| Listener creation error in `AppEventDispatcher` | the type is abstract or lacks a public parameterless constructor | make the entry point concrete, provide an empty public constructor, and remove constructor dependencies |
| Start succeeds but end sees `null` state | cross-hook state was stored on the listener instance | move state to the package singleton and keep the listener stateless |
| Application/session continues after a hook failure | the dispatcher logged and isolated the exception after successful activation | inspect platform logs; add package-stage logging and make failure state observable in the owner |
| Dispatch stops before later listeners run | a constructor or static initializer failed before the hook exception boundary | keep activation side-effect-free and non-fallible; move resolution and work into hook bodies |
| Duplicate workers or subscriptions | runtime start is not idempotent, or multiple host processes were treated as one | synchronize process-local state and design cluster ownership separately when required |
| Session hook cannot identify the user | `AppEventContext` was mistaken for session/user context | remove the assumption or verify a separate runtime-specific identity API before using it |
| Security-critical action is missed after a session-hook failure | a fail-open notification hook was used as an authorization, revocation, or mandatory-audit boundary | enforce the outcome in the authoritative security flow and keep the listener observational |
| Shutdown hangs | stop performs an unbounded join or external call | cancel first, wait for a fixed bound, retain live state on timeout, and log the remaining owner |
| Expired-session cleanup slows as a batch grows | a session hook waits for synchronous I/O and serial dispatch amplifies the latency | enqueue without blocking into a bounded queue with an explicit overload policy and process elsewhere |
| Worker exits after a shutdown timeout but restart stays blocked | retained ownership never converged after late completion | dispose and atomically clear the retained owner exactly once from the worker completion path |

## Evidence And Applicability

- The public four-hook surface is documented by Creatio's [`AppEventListenerBase` API](https://academy.creatio.com/api/netcoreapi/7.18.0/api/Terrasoft.Web.Common.AppEventListenerBase.html).
- Discovery, fresh-instance activation, hook exception isolation, application/session host ordering, and the application-only event context were verified in the private `engineering/core` repository at commit `e0d0f98b80c8fd26e305804c7cb3242b76baf072`, tagged `builds-linux/10.1.83`, for both .NET Framework and .NET Core hosts. Apply these observations to platform builds containing that revision; reverify them against Core source before relying on them in an older or materially different version.
- Use the published `atf.creatio.kafka-reference` catalog item for a pinned example of the verified application-listener-to-singleton boundary. That example covers `OnAppStart` and `OnAppEnd`; it is not evidence for session identity or session ordering.
