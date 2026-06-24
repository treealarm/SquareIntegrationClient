using LeafletAlarmsGrpc;

namespace SquareIntegrationClient;

// Shared protocol layer for any producer pushing its object tree into Square (the read-only
// consumer/host — it never creates or edits objects pushed through this interface, it only
// displays them). This is the SAME interface Square's own internal producers (AASubService,
// GrpcTracksClient) use, via the SquareIntegrationClient submodule — see
// docs/square-integration-plan.md.
//
// Direction of data:
//  - the producer PUSHES its object tree, properties, states, events, geometry;
//  - the producer REGISTERS objects as integration objects (under its own AppId = i_name);
//  - Square sends the producer commands (see IObjectActions), the producer reports status back
//    (ReportActionStatus).
public interface ISquareIntegration
{
  bool Enabled { get; }

  /// <summary>Dapr APP_ID of the current producer (= i_name of its objects in Square).</summary>
  string AppId { get; }

  /// <summary>Stable object_id for (prefix, number); cached by Square.</summary>
  Task<string?> GenerateObjectId(string prefix, long number);

  // --- Write: object tree and its data ---

  /// <summary>Create/update base tree objects; returns the objects as stored.</summary>
  Task<List<ProtoObject>?> UpsertObjects(ProtoObjectList objects);

  /// <summary>Push geometry/location (map figures).</summary>
  Task PushFigures(ProtoFigures figures);

  /// <summary>Push track points (movement history).</summary>
  Task PushTracks(TrackPointsProto tracks);

  /// <summary>Push properties. Always merge unless ReplaceProps is set — old properties are not dropped.</summary>
  Task PushProperties(ProtoObjPropsList properties);

  /// <summary>Push object states.</summary>
  Task PushStates(ProtoObjectStates states);

  /// <summary>Push events.</summary>
  Task PushEvents(EventsProto events);

  /// <summary>Push values (sensors/variables).</summary>
  Task PushValues(ValuesProto values);

  /// <summary>Upload a file (snapshot/image etc).</summary>
  Task UploadFile(UploadFileProto file);

  /// <summary>Push diagram types.</summary>
  Task PushDiagramTypes(DiagramTypesProto diagramTypes);

  /// <summary>Push diagrams.</summary>
  Task PushDiagrams(DiagramsProto diagrams);

  // --- Integration registration ---

  /// <summary>Register objects as integration objects (i_name is set to AppId).</summary>
  Task RegisterIntegro(IntegroListProto integros);

  /// <summary>Register the integration type hierarchy (i_name is set to AppId).</summary>
  Task RegisterIntegroTypes(IntegroTypesProto types);

  // --- Commands ---

  /// <summary>Report progress/result of a command execution (by uid).</summary>
  Task ReportActionStatus(ProtoActionExeResultRequest results);

  // --- Read from Square ---

  /// <summary>Base objects by id.</summary>
  Task<List<ProtoObject>?> GetObjects(IEnumerable<string> ids);

  /// <summary>Object properties by id.</summary>
  Task<List<ProtoObjProps>?> GetProperties(IEnumerable<string> ids);

  /// <summary>This producer's integration objects by type.</summary>
  Task<List<IntegroProto>?> GetIntegroByType(string type);

  /// <summary>Integration objects by id.</summary>
  Task<List<IntegroProto>?> GetIntegroByIds(IEnumerable<string> ids);
}
