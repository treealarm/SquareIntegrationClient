using Google.Protobuf.WellKnownTypes;
using ObjectActions;

namespace SquareIntegrationClient;

// Implemented by the producer to handle commands sent by Square (Dapr service invocation on the
// producer's own AppId). Hosted via ActionsServiceImpl.
public interface IObjectActions
{
  Task<ProtoGetAvailableActionsResponse> GetAvailableActions(ProtoGetAvailableActionsRequest request);
  Task<ProtoExecuteActionResponse> ExecuteActions(ProtoExecuteActionRequest request);
  Task<ProtoExecuteActionResponse> ExecuteActionGetResult(ProtoActionExe action);
  Task<BoolValue> CancelActions(ProtoEnumList request);
}
