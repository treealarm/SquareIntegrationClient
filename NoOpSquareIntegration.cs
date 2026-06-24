using LeafletAlarmsGrpc;

namespace SquareIntegrationClient;

// Used when no Square sink is configured — the producer runs fully standalone, with zero
// dependency on Square being present.
public class NoOpSquareIntegration : ISquareIntegration
{
  public bool Enabled => false;

  public string AppId => "";

  public Task<string?> GenerateObjectId(string prefix, long number) => Task.FromResult<string?>(null);

  public Task<List<ProtoObject>?> UpsertObjects(ProtoObjectList objects) => Task.FromResult<List<ProtoObject>?>(null);

  public Task PushFigures(ProtoFigures figures) => Task.CompletedTask;

  public Task PushTracks(TrackPointsProto tracks) => Task.CompletedTask;

  public Task PushProperties(ProtoObjPropsList properties) => Task.CompletedTask;

  public Task PushStates(ProtoObjectStates states) => Task.CompletedTask;

  public Task PushEvents(EventsProto events) => Task.CompletedTask;

  public Task PushValues(ValuesProto values) => Task.CompletedTask;

  public Task UploadFile(UploadFileProto file) => Task.CompletedTask;

  public Task PushDiagramTypes(DiagramTypesProto diagramTypes) => Task.CompletedTask;

  public Task PushDiagrams(DiagramsProto diagrams) => Task.CompletedTask;

  public Task RegisterIntegro(IntegroListProto integros) => Task.CompletedTask;

  public Task RegisterIntegroTypes(IntegroTypesProto types) => Task.CompletedTask;

  public Task ReportActionStatus(ProtoActionExeResultRequest results) => Task.CompletedTask;

  public Task<List<ProtoObject>?> GetObjects(IEnumerable<string> ids) => Task.FromResult<List<ProtoObject>?>(null);

  public Task<List<ProtoObjProps>?> GetProperties(IEnumerable<string> ids) => Task.FromResult<List<ProtoObjProps>?>(null);

  public Task<List<IntegroProto>?> GetIntegroByType(string type) => Task.FromResult<List<IntegroProto>?>(null);

  public Task<List<IntegroProto>?> GetIntegroByIds(IEnumerable<string> ids) => Task.FromResult<List<IntegroProto>?>(null);
}
