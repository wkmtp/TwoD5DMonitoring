using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TwoD5DMonitoring.Infrastructure.PLC
{
    public enum PlcConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Reconnecting
    }

    public enum PlcDataType
    {
        Bool,
        Byte,
        Word,
        DWord,
        Int,
        DInt,
        Real,
        LReal,
        String,
        Array
    }

    public class PlcTag
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public PlcDataType DataType { get; set; } = PlcDataType.Bool;
        public int ArrayLength { get; set; } = 1;
        public string Description { get; set; } = "";
        public object? Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool Quality { get; set; } = true;
    }

    public class PlcValueChangedEventArgs : EventArgs
    {
        public string TagName { get; set; } = "";
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool Quality { get; set; } = true;
    }

    public class PlcConnectionEventArgs : EventArgs
    {
        public string PlcName { get; set; } = "";
        public PlcConnectionState OldState { get; set; }
        public PlcConnectionState NewState { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IPlcDriver : IDisposable
    {
        string Name { get; }
        string DriverType { get; }
        PlcConnectionState ConnectionState { get; }
        string? LastError { get; }
        DateTime? LastSuccessfulRead { get; }
        
        event EventHandler<PlcValueChangedEventArgs>? ValueChanged;
        event EventHandler<PlcConnectionEventArgs>? ConnectionStateChanged;

        Task<bool> ConnectAsync(PlcConnectionConfig config);
        Task DisconnectAsync();
        Task<bool> ReconnectAsync();
        
        Task<T?> ReadAsync<T>(string tagName);
        Task<object?> ReadAsync(string tagName, PlcDataType dataType);
        Task<Dictionary<string, object?>> ReadMultipleAsync(IEnumerable<string> tagNames);
        
        Task<bool> WriteAsync(string tagName, object value);
        Task<bool> WriteMultipleAsync(Dictionary<string, object> values);
        
        Task<IEnumerable<PlcTag>> BrowseTagsAsync(string? nodePath = null);
        Task<PlcTag?> GetTagInfoAsync(string tagName);
        
        Task<bool> SubscribeAsync(IEnumerable<string> tagNames, int publishingIntervalMs = 1000);
        Task<bool> UnsubscribeAsync(IEnumerable<string> tagNames);
        
        bool IsTagCached(string tagName);
        object? GetCachedValue(string tagName);
        void SetCachedValue(string tagName, object value, bool quality = true);
    }

    public class PlcConnectionConfig
    {
        public string Name { get; set; } = "";
        public string DriverType { get; set; } = "OpcUa";
        public string EndpointUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string CertificatePath { get; set; } = "";
        public bool UseSecurity { get; set; } = false;
        public int PublishingIntervalMs { get; set; } = 1000;
        public int SessionTimeoutMs { get; set; } = 60000;
        public Dictionary<string, string> NodeMappings { get; set; } = new();
        public Dictionary<string, object> AdditionalSettings { get; set; } = new();
    }
}