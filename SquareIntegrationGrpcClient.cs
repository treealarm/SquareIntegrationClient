using System.Collections.Concurrent;
using Dapr.Client;
using LeafletAlarmsGrpc;

namespace SquareIntegrationClient;

// gRPC implementation of ISquareIntegration over Dapr service invocation. Transport (channel
// creation) is encapsulated here — producers never see it.
public class SquareIntegrationGrpcClient : ISquareIntegration
{
  private readonly TreeAlarmsGrpcService.TreeAlarmsGrpcServiceClient _tracksClient;
  private readonly IntegroService.IntegroServiceClient _integroClient;

  // Lazy<Task<T>> (not a plain Dictionary<string,string>+lock) so concurrent callers requesting the
  // same key share one in-flight RPC instead of racing — a lock around "check cache, await RPC,
  // write cache" can't make the await itself atomic, so two callers for the same key could
  // otherwise both miss the cache and both call GenerateObjectIdAsync. ConcurrentDictionary.GetOrAdd
  // guarantees only one Lazy is published per key, and Lazy<T>'s default thread-safety mode runs
  // the factory exactly once even if multiple threads call .Value concurrently.
  private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _objectIdCache = new();

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
    var lazy = _objectIdCache.GetOrAdd(key, k => new Lazy<Task<string?>>(() => FetchObjectIdAsync(k)));

    try
    {
      var objectId = await lazy.Value;
      if (string.IsNullOrEmpty(objectId))
      {
        // Don't poison the cache forever with a failed/empty lookup — let the next caller retry.
        _objectIdCache.TryRemove(key, out _);
      }
      return objectId;
    }
    catch
    {
      // Lazy<T>'s default thread-safety mode caches a thrown exception on the Lazy instance
      // forever — remove it from the dictionary so the next call gets a fresh Lazy (and a fresh
      // attempt) instead of replaying the same exception indefinitely.
      _objectIdCache.TryRemove(key, out _);
      throw;
    }
  }

  private async Task<string?> FetchObjectIdAsync(string key)
  {
    var request = new GenerateObjectIdRequest();
    request.Input.Add(new GenerateObjectIdData { Input = key, Version = "1.0" });

    var result = await _integroClient.GenerateObjectIdAsync(request);
    return result?.Output.FirstOrDefault()?.ObjectId;
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

  // Callers (IntegrationSync.InitMainObject, IntegrationSyncFull.InitAll) loop on "null means not
  // found yet, keep retrying" — they are not equipped to handle a thrown exception from a
  // transient RPC/Dapr-sidecar hiccup, so one is converted into the "retry" signal they expect
  // instead of propagating and crashing whatever hosted service called them.
  public async Task<List<IntegroProto>?> GetIntegroByType(string type)
  {
    try
    {
      var request = new GetListByTypeRequest { IName = AppId, IType = type };
      var response = await _integroClient.GetListByTypeAsync(request);
      return response?.Objects.ToList();
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"GetIntegroByType('{type}') failed: {ex}");
      return null;
    }
  }

  public async Task<List<IntegroProto>?> GetIntegroByIds(IEnumerable<string> ids)
  {
    var request = new Common.ProtoObjectIds();
    request.Ids.AddRange(ids);
    var response = await _integroClient.GetListByIdsAsync(request);
    return response?.Objects.ToList();
  }
}
