// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Opc.Ua;
// using Opc.Ua.Client;
// using TwoD5DMonitoring.Configuration;
// using TwoD5DMonitoring.Infrastructure.Logging;

// namespace TwoD5DMonitoring.Infrastructure.PLC
// {
//     public class OmronDriver : OpcUaDriverBase
//     {
//         public override string Name => "Omron";
//         public override string DriverType => "OpcUa_Omron";

//         protected override ApplicationInstance CreateApplication()
//         {
//             var config = new ApplicationConfiguration
//             {
//                 ApplicationName = "TwoD5DMonitoring_Omron",
//                 ApplicationType = ApplicationType.Client,
//                 ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:TwoD5DMonitoring_Omron",
//                 ProductUri = "TwoD5DMonitoring_Omron",
//                 SecurityConfiguration = new SecurityConfiguration
//                 {
//                     ApplicationCertificate = new CertificateIdentifier
//                     {
//                         StoreType = "Directory",
//                         StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
//                         SubjectName = "CN=TwoD5DMonitoring_Omron"
//                     },
//                     TrustedIssuerCertificates = new CertificateTrustList
//                     {
//                         StoreType = "Directory",
//                         StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities"
//                     },
//                     TrustedPeerCertificates = new CertificateTrustList
//                     {
//                         StoreType = "Directory",
//                         StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications"
//                     },
//                     RejectedCertificateStore = new CertificateTrustList
//                     {
//                         StoreType = "Directory",
//                         StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates"
//                     },
//                     AutoAcceptUntrustedCertificates = true
//                 },
//                 TransportConfigurations = new TransportConfigurationCollection(),
//                 TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
//                 ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
//                 TraceConfiguration = new TraceConfiguration
//                 {
//                     OutputFilePath = "Logs/OmronDriver.log",
//                     DeleteOnLoad = true
//                 }
//             };

//             return new ApplicationInstance
//             {
//                 ApplicationName = config.ApplicationName,
//                 ApplicationType = config.ApplicationType,
//                 ApplicationConfiguration = config
//             };
//         }

//         protected override EndpointDescription SelectEndpoint(string endpointUrl)
//         {
//             var endpoints = Task.Run(async () => 
//             {
//                 var client = new Opc.Ua.Client.Session();
//                 return await client.GetEndpointsAsync(endpointUrl);
//             }).Result;

//             var selected = endpoints
//                 .Where(e => e.SecurityPolicyUri == SecurityPolicies.None || e.SecurityPolicyUri == SecurityPolicies.Basic256Sha256)
//                 .OrderBy(e => e.SecurityPolicyUri == SecurityPolicies.None ? 0 : 1)
//                 .FirstOrDefault();

//             if (selected == null && endpoints.Count > 0)
//                 selected = endpoints[0];

//             return selected ?? new EndpointDescription { EndpointUrl = endpointUrl };
//         }

//         protected override NodeId GetNodeId(string tagName)
//         {
//             if (_config.NodeMappings.TryGetValue(tagName, out var mappedAddress))
//             {
//                 return NodeId.Parse(mappedAddress);
//             }

//             // 欧姆龙 OPC UA 服务器通常使用 ns=2 或 ns=3
//             // 支持格式：ns=2;s=VariableName, ns=2;s=ProgramName.VariableName
//             if (tagName.StartsWith("ns="))
//             {
//                 return NodeId.Parse(tagName);
//             }

//             return new NodeId(tagName, 2); // 默认命名空间索引 2
//         }

//         protected override PlcDataType GetDataTypeForTag(string tagName)
//         {
//             var lower = tagName.ToLowerInvariant();
            
//             if (lower.Contains("bool") || lower.Contains("bit") || lower.Contains("state") || lower.Contains("status") || lower.Contains("alarm") || lower.Contains("flag"))
//                 return PlcDataType.Bool;
//             if (lower.Contains("temp") || lower.Contains("press") || lower.Contains("flow") || lower.Contains("level") || lower.Contains("speed") || lower.Contains("weight") || lower.Contains("pos"))
//                 return PlcDataType.Real;
//             if (lower.Contains("count") || lower.Contains("total") || lower.Contains("setpoint") || lower.Contains("recipe"))
//                 return PlcDataType.DInt;
//             if (lower.Contains("string") || lower.Contains("name") || lower.Contains("id"))
//                 return PlcDataType.String;
            
//             return PlcDataType.Real;
//         }

//         protected override PlcDataType MapDataType(NodeId dataType)
//         {
//             return dataType.Identifier switch
//             {
//                 (uint)BuiltInType.Boolean => PlcDataType.Bool,
//                 (uint)BuiltInType.SByte or (uint)BuiltInType.Byte => PlcDataType.Byte,
//                 (uint)BuiltInType.Int16 or (uint)BuiltInType.UInt16 => PlcDataType.Word,
//                 (uint)BuiltInType.Int32 or (uint)BuiltInType.UInt32 => PlcDataType.DWord,
//                 (uint)BuiltInType.Int64 or (uint)BuiltInType.UInt64 => PlcDataType.LReal,
//                 (uint)BuiltInType.Float => PlcDataType.Real,
//                 (uint)BuiltInType.Double => PlcDataType.LReal,
//                 (uint)BuiltInType.String => PlcDataType.String,
//                 _ => PlcDataType.Real
//             };
//         }

//         protected override Variant CreateVariant(object value, PlcDataType dataType)
//         {
//             return dataType switch
//             {
//                 PlcDataType.Bool => new Variant(Convert.ToBoolean(value)),
//                 PlcDataType.Byte => new Variant(Convert.ToByte(value)),
//                 PlcDataType.Word => new Variant(Convert.ToUInt16(value)),
//                 PlcDataType.DWord => new Variant(Convert.ToUInt32(value)),
//                 PlcDataType.Int => new Variant(Convert.ToInt16(value)),
//                 PlcDataType.DInt => new Variant(Convert.ToInt32(value)),
//                 PlcDataType.Real => new Variant(Convert.ToSingle(value)),
//                 PlcDataType.LReal => new Variant(Convert.ToDouble(value)),
//                 PlcDataType.String => new Variant(Convert.ToString(value) ?? ""),
//                 _ => new Variant(value)
//             };
//         }

//         protected override async Task<object?> ReadNodeAsync(NodeId nodeId, PlcDataType dataType)
//         {
//             if (_session == null) return null;

//             var nodesToRead = new ReadValueIdCollection
//             {
//                 new ReadValueId { NodeId = nodeId, AttributeId = Attributes.Value }
//             };

//             var results = await _session.ReadAsync(null, 0, TimestampsToReturn.Both, nodesToRead);
            
//             if (results.Results[0].StatusCode == StatusCodes.Good)
//             {
//                 return results.Results[0].Value?.Value;
//             }

//             return null;
//         }
//     }
// }