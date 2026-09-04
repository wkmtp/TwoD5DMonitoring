// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Opc.Ua;
// using Opc.Ua.Client;
// using TwoD5DMonitoring.Configuration;
// using TwoD5DMonitoring.Infrastructure.Logging;

// namespace TwoD5DMonitoring.Infrastructure.PLC
// {
//     public class MitsubishiDriver : OpcUaDriverBase
//     {
//         public override string Name => "Mitsubishi";
//         public override string DriverType => "OpcUa_Mitsubishi";

//         protected override ApplicationInstance CreateApplication()
//         {
//             var config = new ApplicationConfiguration
//             {
//                 ApplicationName = "TwoD5DMonitoring_Mitsubishi",
//                 ApplicationType = ApplicationType.Client,
//                 ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:TwoD5DMonitoring_Mitsubishi",
//                 ProductUri = "TwoD5DMonitoring_Mitsubishi",
//                 SecurityConfiguration = new SecurityConfiguration
//                 {
//                     ApplicationCertificate = new CertificateIdentifier
//                     {
//                         StoreType = "Directory",
//                         StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
//                         SubjectName = "CN=TwoD5DMonitoring_Mitsubishi"
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
//                     OutputFilePath = "Logs/MitsubishiDriver.log",
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

//             // 三菱 OPC UA 服务器通常使用 ns=2 或 ns=3
//             // 设备地址格式：ns=2;s=D100, ns=2;s=M100, ns=2;s=X100, ns=2;s=Y100
//             if (tagName.StartsWith("ns="))
//             {
//                 return NodeId.Parse(tagName);
//             }

//             // 自动识别设备类型前缀
//             var upper = tagName.ToUpperInvariant();
//             string prefix = "";
            
//             if (upper.StartsWith("D") || upper.StartsWith("SD") || upper.StartsWith("Z") || upper.StartsWith("R"))
//                 prefix = "D"; // 数据寄存器
//             else if (upper.StartsWith("M") || upper.StartsWith("SM") || upper.StartsWith("L") || upper.StartsWith("F"))
//                 prefix = "M"; // 内部继电器
//             else if (upper.StartsWith("X"))
//                 prefix = "X"; // 输入
//             else if (upper.StartsWith("Y"))
//                 prefix = "Y"; // 输出
//             else if (upper.StartsWith("B"))
//                 prefix = "B"; // 链接继电器
//             else if (upper.StartsWith("W"))
//                 prefix = "W"; // 链接寄存器
//             else if (upper.StartsWith("TS") || upper.StartsWith("TC") || upper.StartsWith("TN"))
//                 prefix = "T"; // 定时器
//             else if (upper.StartsWith("CS") || upper.StartsWith("CC") || upper.StartsWith("CN"))
//                 prefix = "C"; // 计数器

//             var nodeIdString = string.IsNullOrEmpty(prefix) ? tagName : $"{prefix}{tagName}";
//             return new NodeId(nodeIdString, 2); // 默认命名空间索引 2
//         }

//         protected override PlcDataType GetDataTypeForTag(string tagName)
//         {
//             var upper = tagName.ToUpperInvariant();
            
//             // 位设备
//             if (upper.StartsWith("M") || upper.StartsWith("SM") || upper.StartsWith("L") || 
//                 upper.StartsWith("F") || upper.StartsWith("X") || upper.StartsWith("Y") || 
//                 upper.StartsWith("B") || upper.StartsWith("TS") || upper.StartsWith("TC") || 
//                 upper.StartsWith("TN") || upper.StartsWith("CS") || upper.StartsWith("CC") || 
//                 upper.StartsWith("CN"))
//                 return PlcDataType.Bool;
            
//             // 字/双字设备
//             if (upper.StartsWith("D") || upper.StartsWith("SD") || upper.StartsWith("Z") || 
//                 upper.StartsWith("R") || upper.StartsWith("W") || upper.StartsWith("SW"))
//             {
//                 // 根据后缀判断是否为双字
//                 if (upper.Contains("L") || upper.Contains("F") || upper.Contains("REAL"))
//                     return PlcDataType.Real;
//                 return PlcDataType.DInt;
//             }
            
//             return PlcDataType.DInt;
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
//                 _ => PlcDataType.DInt
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