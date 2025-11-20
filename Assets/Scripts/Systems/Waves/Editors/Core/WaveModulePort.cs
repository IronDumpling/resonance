using System;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Represents an input or output port on a wave module
    /// Ports are connection points for patch cables
    /// </summary>
    [Serializable]
    public class WaveModulePort
    {
        /// <summary>
        /// Unique identifier for this port within the module
        /// </summary>
        public string portID;
        
        /// <summary>
        /// Display name of the port
        /// </summary>
        public string portName;
        
        /// <summary>
        /// Port type (Input or Output)
        /// </summary>
        public WaveModulePortType portType;
        
        /// <summary>
        /// Whether this port is required for the module to function
        /// </summary>
        public bool isRequired;
        
        /// <summary>
        /// Whether this port can accept multiple connections (for outputs)
        /// </summary>
        public bool allowMultipleConnections;
        
        /// <summary>
        /// Current number of connections to this port
        /// </summary>
        public int connectionCount;
        
        /// <summary>
        /// Maximum number of connections allowed (for outputs)
        /// </summary>
        public int maxConnections;
        
        // Properties for backward compatibility
        public string PortID { get => portID; set => portID = value; }
        public string PortName { get => portName; set => portName = value; }
        public WaveModulePortType PortType { get => portType; set => portType = value; }
        public bool IsRequired { get => isRequired; set => isRequired = value; }
        public bool AllowMultipleConnections { get => allowMultipleConnections; set => allowMultipleConnections = value; }
        public int ConnectionCount { get => connectionCount; set => connectionCount = value; }
        public int MaxConnections { get => maxConnections; set => maxConnections = value; }
        
        public WaveModulePort()
        {
            portID = Guid.NewGuid().ToString();
            portName = "Port";
            portType = WaveModulePortType.Input;
            isRequired = false;
            allowMultipleConnections = false;
            connectionCount = 0;
            maxConnections = 1;
        }
        
        public WaveModulePort(string portID, string portName, WaveModulePortType portType, bool isRequired = false)
        {
            this.portID = portID;
            this.portName = portName;
            this.portType = portType;
            this.isRequired = isRequired;
            this.allowMultipleConnections = portType == WaveModulePortType.Output;
            connectionCount = 0;
            maxConnections = portType == WaveModulePortType.Output ? 8 : 1; // Outputs can have multiple connections
        }
        
        /// <summary>
        /// Check if this port can accept another connection
        /// </summary>
        public bool CanConnect()
        {
            if (portType == WaveModulePortType.Input)
            {
                // Inputs can only have one connection
                return connectionCount == 0;
            }
            else
            {
                // Outputs can have multiple connections (up to MaxConnections)
                return allowMultipleConnections && connectionCount < maxConnections;
            }
        }
        
        /// <summary>
        /// Increment connection count
        /// </summary>
        public void AddConnection()
        {
            if (CanConnect())
            {
                connectionCount++;
            }
        }
        
        /// <summary>
        /// Decrement connection count
        /// </summary>
        public void RemoveConnection()
        {
            if (connectionCount > 0)
            {
                connectionCount--;
            }
        }
    }
    
    /// <summary>
    /// Port type enumeration
    /// </summary>
    public enum WaveModulePortType
    {
        Input,
        Output
    }
}

