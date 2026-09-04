// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Opc.Ua;
// using Opc.Ua.Client;
// using TwoD5DMonitoring.Configuration;
// using TwoD5DMonitoring.Infrastructure.Logging;

// namespace TwoD5DMonitoring.Infrastructure.PLC
// {
//     public abstract class OpcUaDriverBase : IPlcDriver
//     {
//         private readonly ILogger _logger;
//         protected Session? _session;
//         protected Subscription? _subscription;
//         protected readonly ConcurrentDictionary<string, MonitoredItem> _monitoredItems = new();
//         protected readonly ConcurrentDictionary<string, PlcTag> _tagCache = new();
//         protected PlcConnectionConfig _config = new();
//         protected readonly Timer? _reconnectTimer;
//         protected int _reconnectAttempts = 0;
//         protected readonly SemaphoreSlim _connectionLock = new(1, 1);
//         protected bool _disposed;

//         protected OpcUaDriverBase()
//         {
//             _logger = LogManager.CreateModuleLogger(GetType().Name);
//             _reconnectTimer = new Timer(OnReconnectTimer, null, Timeout.Infinite, Timeout.Infinite);
//         }

//         public abstract string Name { get; }
//         public abstract string DriverType { get; }

//         public PlcConnectionState ConnectionState { get; protected set; } = PlcConnectionState.Disconnected;
//         public string? LastError { get; protected set; }
//         public DateTime? LastSuccessfulRead { get; protected set; }

//         public event EventHandler<PlcValueChangedEventArgs>? ValueChanged;
//         public event EventHandler<PlcConnectionEventArgs>? ConnectionStateChanged;

//         public async Task<bool> ConnectAsync(PlcConnectionConfig config)
//         {
//             await _connectionLock.WaitAsync();
//             try
//             {
//                 if (ConnectionState == PlcConnectionState.Connected)
//                     return true;

//                 _config = config ?? throw new ArgumentNullException(nameof(config));
//                 SetConnectionState(PlcConnectionState.Connecting);

//                 var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
//                 try
//                 {
//                     var application = CreateApplication();
//                     var endpoint = SelectEndpoint(config.EndpointUrl);
//                     var endpointConfiguration = EndpointConfiguration.Create(application);
//                     var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfiguration);
                    
//                     _session = await Session.Create(
//                         application,
//                         configuredEndpoint,
//                         true,
//                         Name,
//                         config.SessionTimeoutMs,
//                         new UserIdentity(config.Username, config.Password),
//                         null);

//                     _session.KeepAlive += OnKeepAlive;
                    
//                     // 创建订阅
//                     _subscription = new Subscription(_session.DefaultSubscription)
//                     {
//                         PublishingInterval = config.PublishingIntervalMs,
//                         Priority = 1
//                     };
                    
//                     _session.AddSubscription(_subscription);
//                     _subscription.Create();

//                     SetConnectionState(PlcConnectionState.Connected);
//                     _reconnectAttempts = 0;
//                     LastError = null;
                    
//                     LogManager.LogPlcCommunication(Name, "Connect", stopwatch.Elapsed, true);
//                     return true;
//                 }
//                 catch (Exception ex)
//                 {
//                     LastError = ex.Message;
//                     SetConnectionState(PlcConnectionState.Error);
//                     LogManager.LogPlcCommunication(Name, "Connect", stopwatch.Elapsed, false, ex.Message);
                    
//                     // 启动重连定时器
//                     if (config.MaxReconnectAttempts > 0)
//                     {
//                         _reconnectTimer?.Change(config.ReconnectIntervalMs, Timeout.Infinite);
//                     }
                    
//                     return false;
//                 }
//             }
//             finally
//             {
//                 _connectionLock.Release();
//             }
//         }

//         public async Task DisconnectAsync()
//         {
//             await _connectionLock.WaitAsync();
//             try
//             {
//                 _reconnectTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                
//                 if (_subscription != null)
//                 {
//                     _subscription.Delete(true);
//                     _subscription = null;
//                 }

//                 if (_session != null)
//                 {
//                     _session.KeepAlive -= OnKeepAlive;
//                     _session.Close();
//                     _session.Dispose();
//                     _session = null;
//                 }

//                 _monitoredItems.Clear();
//                 _tagCache.clear();
                
//                 SetConnectionState(PlcConnectionState.Disconnected);
//                 LogManager.LogOperation("PLC Disconnected", "Driver: {Name}", Name);
//             }
//             finally
//             {
//                 _connectionLock.Release();
//             }
//         }

//         public async Task<bool> ReconnectAsync()
//         {
//             LogManager.LogOperation("PLC Reconnecting", "Driver: {Name}, Attempt: {Attempt}", Name, _reconnectAttempts + 1);
            
//             SetConnectionState(PlcConnectionState.Reconnecting);
            
//             var config = _config;
//             await DisconnectAsync();
//             return await ConnectAsync(config);
//         }

//         public async Task<T?> ReadAsync<T>(string tagName)
//         {
//             var value = await ReadAsync(tagName, GetDataTypeForTag(tagName));
//             if (value == null) return default;
            
//             try
//             {
//                 return (T)Convert.ChangeType(value, typeof(T));
//             }
//             catch
//             {
//                 return default;
//             }
//         }

//         public async Task<object?> ReadAsync(string tagName, PlcDataType dataType)
//         {
//             if (_session == null || ConnectionState != PlcConnectionState.Connected)
//             {
//                 // 返回缓存值
//                 if (_tagCache.TryGetValue(tagName, out var cachedTag))
//                 {
//                     return cachedTag.Value;
//                 }
//                 return null;
//             }

//             try
//             {
//                 var nodeId = GetNodeId(tagName);
//                 var value = await ReadNodeAsync(nodeId, dataType);
//                 UpdateTagCache(tagName, value, true);
//                 LastSuccessfulRead = DateTime.Now;
//                 return value;
//             }
//             catch (Exception ex)
//             {
//                 UpdateTagCache(tagName, null, false);
//                 LastError = ex.Message;
//                 LogManager.LogException(ex, "PLC Read failed: {TagName}", tagName);
//                 return null;
//             }
//         }

//         public async Task<Dictionary<string, object?>> ReadMultipleAsync(IEnumerable<string> tagNames)
//         {
//             var result = new Dictionary<string, object?>();
//             var tags = tagNames.ToList();
            
//             if (_session == null || ConnectionState != PlcConnectionState.Connected)
//             {
//                 foreach (var tag in tags)
//                 {
//                     if (_tagCache.TryGetValue(tag, out var cached))
//                         result[tag] = cached.Value;
//                     else
//                         result[tag] = null;
//                 }
//                 return result;
//             }

//             try
//             {
//                 var nodesToRead = tags.Select(t => new ReadValueId
//                 {
//                     NodeId = GetNodeId(t),
//                     AttributeId = Attributes.Value
//                 }).ToList();

//                 var results = await _session.ReadAsync(null, 0, TimestampsToReturn.Both, nodesToRead);
                
//                 for (int i = 0; i < tags.Count; i++)
//                 {
//                     var tagName = tags[i];
//                     if (results.Results[i].StatusCode == StatusCodes.Good)
//                     {
//                         var value = results.Results[i].Value;
//                         result[tagName] = value?.Value;
//                         UpdateTagCache(tagName, value?.Value, true);
//                     }
//                     else
//                     {
//                         result[tagName] = null;
//                         UpdateTagCache(tagName, null, false);
//                     }
//                 }
                
//                 LastSuccessfulRead = DateTime.Now;
//             }
//             catch (Exception ex)
//             {
//                 LastError = ex.Message;
//                 LogManager.LogException(ex, "PLC ReadMultiple failed");
//                 return result;
//             }
            
//             return result;
//         }

//         public async Task<bool> WriteAsync(string tagName, object value)
//         {
//             if (_session == null || ConnectionState != PlcConnectionState.Connected)
//             {
//                 LastError = "Not connected";
//                 return false;
//             }

//             try
//             {
//                 var nodeId = GetNodeId(tagName);
//                 var dataType = GetDataTypeForTag(tagName);
//                 var variant = CreateVariant(value, dataType);
                
//                 var writeValue = new WriteValue
//                 {
//                     NodeId = nodeId,
//                     AttributeId = Attributes.Value,
//                     Value = new DataValue(variant)
//                 };

//                 var results = await _session.WriteAsync(null, new WriteValueCollection { writeValue });
                
//                 if (results[0].StatusCode == StatusCodes.Good)
//                 {
//                     UpdateTagCache(tagName, value, true);
//                     return true;
//                 }
//                 else
//                 {
//                     LastError = results[0].StatusCode.ToString();
//                     return false;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 LastError = ex.Message;
//                 LogManager.LogException(ex, "PLC Write failed: {TagName}", tagName);
//                 return false;
//             }
//         }

//         public async Task<bool> WriteMultipleAsync(Dictionary<string, object> values)
//         {
//             if (_session == null || ConnectionState != PlcConnectionState.Connected)
//             {
//                 LastError = "Not connected";
//                 return false;
//             }

//             try
//             {
//                 var writeValues = new WriteValueCollection();
                
//                 foreach (var kvp in values)
//                 {
//                     var dataType = GetDataTypeForTag(kvp.Key);
//                     var variant = CreateVariant(kvp.Value, dataType);
                    
//                     writeValues.Add(new WriteValue
//                     {
//                         NodeId = GetNodeId(kvp.Key),
//                         AttributeId = Attributes.Value,
//                         Value = new DataValue(variant)
//                     });
//                 }

//                 var results = await _session.WriteAsync(null, writeValues);
                
//                 bool allSuccess = true;
//                 for (int i = 0; i < results.Count; i++)
//                 {
//                     if (results[i].StatusCode != StatusCodes.Good)
//                     {
//                         allSuccess = false;
//                         var tagName = values.Keys.ElementAt(i);
//                         UpdateTagCache(tagName, null, false);
//                     }
//                     else
//                     {
//                         var tagName = values.Keys.ElementAt(i);
//                         UpdateTagCache(tagName, values[tagName], true);
//                     }
//                 }
                
//                 return allSuccess;
//             }
//             catch (Exception ex)
//             {
//                 LastError = ex.Message;
//                 LogManager.LogException(ex, "PLC WriteMultiple failed");
//                 return false;
//             }
//         }

//         public async Task<IEnumerable<PlcTag>> BrowseTagsAsync(string? nodePath = null)
//         {
//             if (_session == null || ConnectionState != PlcConnectionState.Connected)
//                 return Enumerable.Empty<PlcTag>();

//             try
//             {
//                 var nodeId = string.IsNullOrEmpty(nodePath) ? Objects.ObjectsFolder : GetNodeId(nodePath);
//                 var references = await _session.FetchReferences(nodeId);
                
//                 var tags = new List<PlcTag>();
//                 foreach (var reference in references)
//                 {
//                     if (reference.IsForward && reference.NodeClass == NodeClass.Variable)
//                     {
//                         var tag = await GetTagInfoAsync(reference.DisplayName.Text);
//                         if (tag != null)
//                             tags.Add(tag);
//                     }
//                 }
                
//                 return tags;
//             }
//             catch (Exception ex)
//             {
//                 LogManager.LogException(ex, "PLC Browse failed");
//                 return Enumerable.Empty<PlcTag>();
//             }
//         }

//         public async Task<PlcTag?> GetTagInfoAsync(string tagName)
//         {
//             if (_session == null || ConnectionState != PlcConnectionState.Connected)
//             {
//                 return _tagCache.TryGetValue(tagName, out var cached) ? cached : null;
//             }

//             try
//             {
//                 var nodeId = GetNodeId(tagName);
//                 var node = await _session.ReadNodeAsync(nodeId);
                
//                 if (node == null) return null;

//                 var tag = new PlcTag
//                 {
//                     Name = tagName,
//                     Address = nodeId.ToString(),
//                     DataType = MapDataType(node.DataType),
//                     Description = node.Description?.Text ?? "",
//                     Value = _tagCache.TryGetValue(tagName, out var cached) ? cached.Value : null,
//                     Timestamp = DateTime.Now,
//                     Quality = true
//                 };

//                 _tagCache[tagName] = tag;
//                 return tag;
//             }
//             catch (Exception ex)
//             {
//                 LogManager.LogException(ex, "PLC GetTagInfo failed: {TagName}", tagName);
//                 return null;
//             }
//         }

//         public async Task<bool> SubscribeAsync(IEnumerable<string> tagNames, int publishingIntervalMs = 1000)
//         {
//             if (_session == null || _subscription == null || ConnectionState != PlcConnectionState.Connected)
//                 return false;

//             try
//             {
//                 var itemsToCreate = new List<MonitoredItem>();
                
//                 foreach (var tagName in tagNames)
//                 {
//                     if (_monitoredItems.ContainsKey(tagName))
//                         continue;

//                     var nodeId = GetNodeId(tagName);
//                     var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
//                     {
//                         StartNodeId = nodeId,
//                         AttributeId = Attributes.Value,
//                         SamplingInterval = publishingIntervalMs
//                     };

//                     monitoredItem.Notification += OnMonitoredItemNotification;
//                     itemsToCreate.Add(monitoredItem);
//                     _monitoredItems[tagName] = monitoredItem;
//                 }

//                 if (itemsToCreate.Count > 0)
//                 {
//                     await _subscription.CreateMonitoredItemsAsync(itemsToCreate);
//                     _subscription.ApplyChanges();
//                 }

//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 LogManager.LogException(ex, "PLC Subscribe failed");
//                 return false;
//             }
//         }

//         public async Task<bool> UnsubscribeAsync(IEnumerable<string> tagNames)
//         {
//             if (_subscription == null)
//                 return false;

//             try
//             {
//                 var itemsToDelete = new List<MonitoredItem>();
                
//                 foreach (var tagName in tagNames)
//                 {
//                     if (_monitoredItems.TryRemove(tagName, out var item))
//                     {
//                         item.Notification -= OnMonitoredItemNotification;
//                         itemsToDelete.Add(item);
//                     }
//                 }

//                 if (itemsToDelete.Count > 0)
//                 {
//                     await _subscription.DeleteMonitoredItemsAsync(itemsToDelete);
//                     _subscription.ApplyChanges();
//                 }

//                 return true;
//             }
//             catch (Exception ex)
//             {
//                 LogManager.LogException(ex, "PLC Unsubscribe failed");
//                 return false;
//             }
//         }

//         public bool IsTagCached(string tagName) => _tagCache.ContainsKey(tagName);

//         public object? GetCachedValue(string tagName) => _tagCache.TryGetValue(tagName, out var tag) ? tag.Value : null;

//         public void SetCachedValue(string tagName, object value, bool quality = true)
//         {
//             UpdateTagCache(tagName, value, quality);
//         }

//         protected virtual void OnKeepAlive(Session session, KeepAliveEventArgs e)
//         {
//             if (e.Status != null && ServiceResult.IsBad(e.Status))
//             {
//                 LastError = e.Status.ToString();
//                 LogManager.LogOperation("PLC KeepAlive Failed", "Driver: {Name}, Status: {Status}", Name, e.Status);
                
//                 if (ConnectionState == PlcConnectionState.Connected)
//                 {
//                     SetConnectionState(PlcConnectionState.Error);
                    
//                     if (_config.MaxReconnectAttempts > 0)
//                     {
//                         _reconnectTimer?.Change(_config.ReconnectIntervalMs, Timeout.Infinite);
//                     }
//                 }
//             }
//         }

//         protected virtual void OnMonitoredItemNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
//         {
//             try
//             {
//                 foreach (var value in item.DequeueValues())
//                 {
//                     var tagName = item.StartNodeId?.ToString() ?? "";
//                     var oldValue = _tagCache.TryGetValue(tagName, out var cached) ? cached.Value : null;
//                     var newValue = value.Value?.Value;
//                     
//                     UpdateTagCache(tagName, newValue, value.StatusCode == StatusCodes.Good);
                    
//                     ValueChanged?.Invoke(this, new PlcValueChangedEventArgs
//                     {
//                         TagName = tagName,
//                         OldValue = oldValue,
//                         NewValue = newValue,
//                         Timestamp = value.SourceTimestamp,
//                         Quality = value.StatusCode == StatusCodes.Good
//                     });
//                 }
//             }
//             catch (Exception ex)
//             {
//                 LogManager.LogException(ex, "PLC MonitoredItem Notification error");
//             }
//         }

//         protected virtual void OnReconnectTimer(object? state)
//         {
//             if (_disposed) return;
            
//             _reconnectAttempts++;
//             if (_reconnectAttempts > _config.MaxReconnectAttempts)
//             {
//                 LogManager.LogOperation("PLC Max Reconnect Attempts Reached", "Driver: {Name}", Name);
//                 return;
//             }

//             _ = Task.Run(async () => await ReconnectAsync());
//         }

//         protected virtual void SetConnectionState(PlcConnectionState newState)
//         {
//             var oldState = ConnectionState;
//             if (oldState != newState)
//             {
//                 ConnectionState = newState;
//                 ConnectionStateChanged?.Invoke(this, new PlcConnectionEventArgs
//                 {
//                     PlcName = Name,
//                     OldState = oldState,
//                     NewState = newState,
//                     ErrorMessage = LastError
//                 });
                
//                 LogManager.LogOperation("PLC State Changed", "Driver: {Name}, {OldState} -> {NewState}", Name, oldState, newState);
//             }
//         }

//         protected virtual void UpdateTagCache(string tagName, object? value, bool quality)
//         {
//             _tagCache.AddOrUpdate(tagName, 
//                 new PlcTag { Name = tagName, Value = value, Timestamp = DateTime.Now, Quality = quality },
//                 (key, existing) => 
//                 {
//                     existing.Value = value;
//                     existing.Timestamp = DateTime.Now;
//                     existing.Quality = quality;
//                     return existing;
//                 });
//         }

//         protected abstract ApplicationInstance CreateApplication();
//         protected abstract EndpointDescription SelectEndpoint(string endpointUrl);
//         protected abstract NodeId GetNodeId(string tagName);
//         protected abstract PlcDataType GetDataTypeForTag(string tagName);
//         protected abstract PlcDataType MapDataType(NodeId dataType);
//         protected abstract Variant CreateVariant(object value, PlcDataType dataType);
//         protected abstract Task<object?> ReadNodeAsync(NodeId nodeId, PlcDataType dataType);

//         public void Dispose()
//         {
//             Dispose(true);
//             GC.SuppressFinalize(this);
//         }

//         protected virtual void Dispose(bool disposing)
//         {
//             if (!_disposed && disposing)
//             {
//                 _disposed = true;
//                 _reconnectTimer?.Dispose();
//                 _connectionLock?.Dispose();
//                 _ = DisconnectAsync();
//             }
//         }

//         ~OpcUaDriverBase()
//         {
//             Dispose(false);
//         }
//     }
// }