// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Opc.Ua;
// using Opc.Ua.Client;
// using TwoD5DMonitoring.Configuration;
// using TwoD5DMonitoring.Infrastructure.Logging;

// namespace TwoD5DMonitoring.Infrastructure.PLC
// {
//     public class SiemensDriver : OpcUaDriverBase
//     {
//         public override string Name => "Siemens";
//         public override string DriverType => "OpcUa_Siemens";

//         protected override ApplicationInstance CreateApplication()
//         {
//             var config = new ApplicationConfiguration
//             {
//                 ApplicationName = "TwoD5DMonitoring_Siemens",
//                 ApplicationType = ApplicationType.Client,
//                 ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:TwoD5DMonitoring_Siemens",
//                 ProductUri = "TwoD5DMonitoring_Siemens",
//                 SecurityConfiguration = new SecurityConfiguration
//                 {
//                     ApplicationCertificate = new CertificateIdentifier
//                     {
//                         StoreType = "Directory",
//                         StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
//                         SubjectName = "CN=TwoD5DMonitoring_Siemens"
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
//                     OutputFilePath = "Logs/SiemensDriver.log",
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

//             // 优先选择无安全策略的端点，其次是Basic256Sha256
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
//             // 检查是否有配置的节点映射
//             if (_config.NodeMappings.TryGetValue(tagName, out var mappedAddress))
//             {
//                 return NodeId.Parse(mappedAddress);
//             }

//             // 西门子默认命名空间 ns=3 或 ns=4
//             // 支持多种格式：ns=3;s=TagName, ns=3;s=DB1.TagName 等
//             if (tagName.StartsWith("ns="))
//             {
//                 return NodeId.Parse(tagName);
//             }

//             return new NodeId(tagName, 3); // 默认命名空间索引 3
//         }

//         protected override PlcDataType GetDataTypeForTag(string tagName)
//         {
//             // 根据标签名称推断数据类型
//             var lower = tagName.ToLowerInvariant();
            
//             if (lower.Contains("bool") || lower.Contains("bit") || lower.Contains("state") || lower.Contains("status") || lower.Contains("alarm"))
//                 return PlcDataType.Bool;
//             if (lower.Contains("temp") || lower.Contains("pressure") || lower.Contains("flow") || lower.Contains("level") || lower.Contains("speed") || lower.Contains("weight"))
//                 return PlcDataType.Real;
//             if (lower.Contains("count") || lower.Contains("total") || lower.Contains("setpoint"))
//                 return PlcDataType.DInt;
            
//             return PlcDataType.Real; // 默认
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