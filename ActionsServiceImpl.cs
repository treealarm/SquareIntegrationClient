using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ObjectActions;
using static ObjectActions.ActionsService;

namespace SquareIntegrationClient;

// gRPC server adapter: Square calls into this (via Dapr service invocation on the producer's
// AppId) to list/execute/cancel commands. The producer supplies the actual command logic via
// IObjectActions.
public class ActionsServiceImpl : ActionsServiceBase
{
  private readonly IObjectActions _actions;

  public ActionsServiceImpl(IObjectActions actions)
  {
    _actions = actions;
  }

  public override Task<ProtoGetAvailableActionsResponse> GetAvailableActions(
    ProtoGetAvailableActionsRequest request, ServerCallContext context) =>
    _actions.GetAvailableActions(request);

  public override Task<ProtoExecuteActionResponse> ExecuteActions(
    ProtoExecuteActionRequest request, ServerCallContext context) =>
    _actions.ExecuteActions(request);

  public override Task<ProtoExecuteActionResponse> ExecuteActionGetResult(
    ProtoActionExe request, ServerCallContext context) =>
    _actions.ExecuteActionGetResult(request);

  public override Task<BoolValue> CancelActions(ProtoEnumList request, ServerCallContext context) =>
    _actions.CancelActions(request);
}
