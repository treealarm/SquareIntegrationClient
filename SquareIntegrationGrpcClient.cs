using Dapr.Client;
using LeafletAlarmsGrpc;

namespace SquareIntegrationClient;

// gRPC implementation of ISquareIntegration over Dapr service invocation. Transport (channel
// creation) is encapsulated here — producers never see it.
public class SquareIntegrationGrpcClient : ISquareIntegration
{
  private readonly TreeAlarmsGrpcService.TreeAlarmsGrpcServiceClient _tracksClient;
  private readonly IntegroService.IntegroServiceClient _integroClient;
  private readonly Dictionary<string, string> _objectIdCache = new();
  private readonly object _cacheLock = new();

  public bool Enabled => true;

  public string AppId { get; }

  public SquareIntegrationGrpcClient(string sinkAppId, string appId)
  {
    AppId = appId;

    var invoker = DaprClient.CreateInvocationInvoker(sinkAppId);
    _tracksClient = new TreeAlarmsGrpcService.TreeAlarmsGrpcServiceClient(invoker);
    _integroClient = new IntegroService.IntegroServiceClient(invoker);
  }

  public async Task<string?> GenerateObjectId(string prefix, long number)
  {
    var key = $"{prefix}_{number}";
    lock (_cacheLock)
    {
      if (_objectIdCache.TryGetValue(key, out var cached))
      {
        return cached;
      }
    }

    var request = new GenerateObjectIdRequest();
    request.Input.Add(new GenerateObjectIdData { Input = key, Version = "1.0" });

    var result = await _integroClient.GenerateObjectIdAsync(request);
    var objectId = result?.Output.FirstOrDefault()?.ObjectId;

    if (!string.IsNullOrEmpty(objectId))
    {
      lock (_cacheLock)
      {
        _objectIdCache[key] = objectId;
      }
    }

    return objectId;
  }

  // --- Write ---

  public async Task<List<ProtoObject>?> UpsertObjects(ProtoObjectList objects)
  {
    var response = await _tracksClient.UpdateObjectsAsync(objects);
    return response?.Objects.ToList();
  }

  public Task PushFigures(ProtoFigures figures) =>
    _tracksClient.UpdateFiguresAsync(figures).ResponseAsync;

  public Task PushTracks(TrackPointsProto tracks) =>
    _tracksClient.UpdateTracksAsync(tracks).ResponseAsync;

  public Task PushProperties(ProtoObjPropsList properties) =>
    _tracksClient.UpdatePropertiesAsync(properties).ResponseAsync;

  public Task PushStates(ProtoObjectStates states) =>
    _tracksClient.UpdateStatesAsync(states).ResponseAsync;

  public Task PushEvents(EventsProto events) =>
    _tracksClient.UpdateEventsAsync(events).ResponseAsync;

  public Task PushValues(ValuesProto values) =>
    _tracksClient.UpdateValuesAsync(values).ResponseAsync;

  public Task UploadFile(UploadFileProto file) =>
    _tracksClient.UploadFileAsync(file).ResponseAsync;

  public Task PushDiagramTypes(DiagramTypesProto diagramTypes) =>
    _tracksClient.UpdateDiagramTypesAsync(diagramTypes).ResponseAsync;

  public Task PushDiagrams(DiagramsProto diagrams) =>
    _tracksClient.UpdateDiagramsAsync(diagrams).ResponseAsync;

  // --- Integration registration (i_name is stamped here, callers don't set it) ---

  public Task RegisterIntegro(IntegroListProto integros)
  {
    foreach (var integro in integros.Objects)
    {
      integro.IName = AppId;
    }
    return _integroClient.UpdateIntegroAsync(integros).ResponseAsync;
  }

  public Task RegisterIntegroTypes(IntegroTypesProto types)
  {
    foreach (var type in types.Types_)
    {
      type.IName = AppId;
    }
    return _integroClient.UpdateIntegroTypesAsync(types).ResponseAsync;
  }

  // --- Commands ---

  public Task ReportActionStatus(ProtoActionExeResultRequest results) =>
    _integroClient.UpdateActionResultsAsync(results).ResponseAsync;

  // --- Read ---

  public async Task<List<ProtoObject>?> GetObjects(IEnumerable<string> ids)
  {
    var request = new Common.ProtoObjectIds();
    request.Ids.AddRange(ids);
    var response = await _tracksClient.RequestObjectsAsync(request);
    return response?.Objects.ToList();
  }

  public async Task<List<ProtoObjProps>?> GetProperties(IEnumerable<string> ids)
  {
    var request = new Common.ProtoObjectIds();
    request.Ids.AddRange(ids);
    var response = await _tracksClient.RequestPropertiesAsync(request);
    return response?.Objects.ToList();
  }

  public async Task<List<IntegroProto>?> GetIntegroByType(string type)
  {
    var request = new GetListByTypeRequest { IName = AppId, IType = type };
    var response = await _integroClient.GetListByTypeAsync(request);
    return response?.Objects.ToList();
  }

  public async Task<List<IntegroProto>?> GetIntegroByIds(IEnumerable<string> ids)
  {
    var request = new Common.ProtoObjectIds();
    request.Ids.AddRange(ids);
    var response = await _integroClient.GetListByIdsAsync(request);
    return response?.Objects.ToList();
  }
}
