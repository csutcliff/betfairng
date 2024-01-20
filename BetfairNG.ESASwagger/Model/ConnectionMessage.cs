using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Text;

namespace Betfair.ESASwagger.Model
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///     Initializes a new instance of the <see cref="ConnectionMessage" /> class.
    ///     Initializes a new instance of the <see cref="ConnectionMessage" />class.
    /// </remarks>
    /// <param name="Op">The operation type.</param>
    /// <param name="Id">Client generated unique id to link request with response (like json rpc).</param>
    /// <param name="ConnectionId">The connection id.</param>
    [DataContract]
    public class ConnectionMessage(string Op = null, int? Id = null, string ConnectionId = null) : ResponseMessage, IEquatable<ConnectionMessage>
    {

        /// <summary>
        ///     The connection id
        /// </summary>
        /// <value>The connection id</value>
        [DataMember(Name = "connectionId", EmitDefaultValue = false)]
        public string ConnectionId { get; set; } = ConnectionId;

        /// <summary>
        ///     Client generated unique id to link request with response (like json rpc)
        /// </summary>
        /// <value>Client generated unique id to link request with response (like json rpc)</value>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public new int? Id { get; set; } = Id;

        /// <summary>
        ///     The operation type
        /// </summary>
        /// <value>The operation type</value>
        [DataMember(Name = "op", EmitDefaultValue = false)]
        public new string Op { get; set; } = Op;

        /// <summary>
        ///     Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            return Equals(obj as ConnectionMessage);
        }

        /// <summary>
        ///     Returns true if ConnectionMessage instances are equal
        /// </summary>
        /// <param name="other">Instance of ConnectionMessage to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(ConnectionMessage other)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            if (other == null)
                return false;

            return (Op == other.Op || Op != null && Op.Equals(other.Op)) &&
                   (Id == other.Id || Id != null && Id.Equals(other.Id)) &&
                   (ConnectionId == other.ConnectionId || ConnectionId != null && ConnectionId.Equals(other.ConnectionId));
        }

        /// <summary>
        ///     Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            // credit: http://stackoverflow.com/a/263416/677735
            unchecked // Overflow is fine, just wrap
            {
                var hash = 41;
                // Suitable nullity checks etc, of course :)

                if (Op != null)
                    hash = hash * 59 + Op.GetHashCode();

                if (Id != null)
                    hash = hash * 59 + Id.GetHashCode();

                if (ConnectionId != null)
                    hash = hash * 59 + ConnectionId.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        ///     Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public new string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ConnectionMessage {\n");
            sb.Append("  Op: ")
                .Append(Op)
                .Append("\n");
            sb.Append("  Id: ")
                .Append(Id)
                .Append("\n");
            sb.Append("  ConnectionId: ")
                .Append(ConnectionId)
                .Append("\n");

            sb.Append("}\n");
            return sb.ToString();
        }
    }
}