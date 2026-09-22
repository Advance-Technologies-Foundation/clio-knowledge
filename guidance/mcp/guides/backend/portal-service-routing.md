# Custom configuration services for external users

Use `SspServiceRoute` to register an authenticated external-user route for a custom configuration
web service. Use `DefaultServiceRoute` as well when employees must call the same service.
The attributes select routes; they do not create, convert or impersonate a user connection.
For the general service shape, DTOs and package composition, read `configuration-webservice`.

## Choose the routes

Import `Terrasoft.Web.Common.ServiceRouting` and put the attributes on the service class.

| Class attributes | Registered REST routes relative to the application |
| --- | --- |
| `[DefaultServiceRoute]` | `/rest/<Service>/<Method>` |
| `[SspServiceRoute]` | `/ssp/rest/<Service>/<Method>` |
| `[DefaultServiceRoute, SspServiceRoute]` | Both routes above |

An explicit SSP attribute alone does not retain the default route. Add both when both are needed.
These are configuration-service routes, not `/ServiceModel/<Service>.svc` URLs. On .NET Framework,
the application is normally under `/0`, giving `/0/rest/...` and `/0/ssp/rest/...`; that prefix is
source-backed here, not live-tested in the lab. Do not add `/0` twice through a client that adds it.

## Minimal dual-route service

```csharp
namespace Terrasoft.Configuration
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;
    using System.Web.SessionState;
    using Terrasoft.Web.Common;
    using Terrasoft.Web.Common.ServiceRouting;

    /// <summary>Returns the authenticated caller's identifier.</summary>
    [ServiceContract]
    [DefaultServiceRoute, SspServiceRoute]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class UsrPortalIdentityService : BaseService, IReadOnlySessionState
    {
        /// <summary>Reads the existing request connection without elevation.</summary>
        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
        public Guid WhoAmI() => UserConnection.CurrentUser.Id;
    }
}
```

Authenticate the actual external account and POST `{}` to
`/ssp/rest/UsrPortalIdentityService/WhoAmI` using its session cookies and the required CSRF header.
An employee session uses `/rest/UsrPortalIdentityService/WhoAmI`. Use the established Creatio client
authentication flow; do not copy an administrator session into the external-user test.

## Connection and permission boundaries

- `BaseService.UserConnection` is the authenticated request connection. In the verified external
  session its runtime class is `Terrasoft.Core.SSPUserConnection`; the employee session uses
  `Terrasoft.Core.UserConnection`.
- Both attributes expose the same operations through two routes. They are not method-level
  authorization controls. Split services or explicitly authorize operations if audiences differ.
- MUST NOT replace the caller with `AppConnection.SystemUserConnection` to make the service work.
  A caller-controlled user ID or Account ID is not authority to impersonate that user or organization.
- Route access does not grant entity, record, operation or organization-specific business access.
  Explicitly enforce the endpoint's intended permissions, and ensure data operations respect the
  caller's rights. Direct SQL is not a substitute for permission-checked entity operations.
- Organization affiliation is a separate model owned by `get-guidance name=external-organizations`.
  An unaffiliated external user can call an SSP service; the attribute does not require or infer
  organization membership or apply an organization filter to returned data.
- The attributes do not enable anonymous access, provision users or assign licenses.

## Deploy and prove it

1. Read `core-rules` and `fsm-mode`; determine the environment mode before choosing the deployment path.
2. Build/deploy the affected package for the target runtime, restart and await readiness using their
   canonical activation sequence. Do not infer activation from a successful build alone.
3. Use separate fresh employee and external-user sessions. Assert both the returned user ID and,
   in a diagnostic fixture, the actual connection type. A response from a privileged test account
   does not prove portal support.
4. Exercise matching routes, mismatched routes and an unauthenticated request. For a custom service
   outside compatibility exclusions, the verified dual-route matrix is:

   | Caller | `/rest/...` | `/ssp/rest/...` |
   | --- | --- | --- |
   | Employee | 200, employee identity | 403 |
   | External user | 403 | 200, external identity |
   | Anonymous | Authentication failure | Authentication failure |

5. Independently test the business operation's data permissions and cross-organization isolation
   before claiming that a production endpoint is safe. The identity probe verifies routing only.

Clio 8.1.0.132 has no generic `call-service` MCP tool: do not invent one or dispatch that CLI name
through `clio-run`. Use `clio call-service` for a registered test identity or the Creatio client in
integration tests. MCP can provision and inspect fixtures through its discovered administration,
DataService and ESQ contracts. Keep passwords out of command arguments and evidence.

## Failure signals

- **403 on a registered route:** first check the session identity and route domain. Adding
  `DefaultServiceRoute` does not let an external connection call the default route.
- **404:** check the selected attributes, exact service/method name, application prefix and active DLL.
  In the lab, the missing counterpart of each single-route service returned 404.
- **Login/CSRF rejection:** correct authentication, account state, license or CSRF handling before
  diagnosing the attribute. Do not make the service anonymous or elevate its connection.
- **A platform service behaves differently:** source includes `SspServiceList.txt` /
  `CustomerSspServiceList.txt` compatibility routes and a `UseSspServicesListAsAccessExclusions` switch.
  Do not generalize a listed built-in service's behavior to an unlisted custom service, and do not
  edit exclusion lists as the normal way to expose a new service.

## Evidence boundary

Live-tested: Creatio 10.1.585.0, .NET 8, PostgreSQL, cookie authentication, Clio 8.1.0.132.
Three custom service classes, employee plus three external identities, and anonymous requests:
26 route cases passed. Five additional identity/organization cases passed.
See [lab validation](docs://knowledge/com.creatio.clio/reference.portal-service-lab-validation).
SOAP, OAuth, .NET Framework runtime execution, other versions, web farms, and production business
data authorization are not verified by this fixture.
