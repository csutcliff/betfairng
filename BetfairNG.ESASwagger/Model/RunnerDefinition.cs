using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;
using System.Text;

namespace Betfair.ESASwagger.Model
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///     Initializes a new instance of the <see cref="RunnerDefinition" /> class.
    ///     Initializes a new instance of the <see cref="RunnerDefinition" />class.
    /// </remarks>
    /// <param name="SortPriority">SortPriority.</param>
    /// <param name="RemovalDate">RemovalDate.</param>
    /// <param name="Id">Selection Id - the id of the runner (selection).</param>
    /// <param name="Hc">Handicap - the handicap of the runner (selection) (null if not applicable).</param>
    /// <param name="AdjustmentFactor">AdjustmentFactor.</param>
    /// <param name="Bsp">Bsp.</param>
    /// <param name="Status">Status.</param>
    [DataContract]
    public class RunnerDefinition(
        int? SortPriority = null,
        DateTime? RemovalDate = null,
        long? Id = null,
        double? Hc = null,
        double? AdjustmentFactor = null,
        double? Bsp = null,
RunnerDefinition.StatusEnum? Status = null) : IEquatable<RunnerDefinition>
    {

        /// <summary>
        ///     Gets or Sets Status
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StatusEnum
        {
            [EnumMember(Value = "ACTIVE")]
            Active,

            [EnumMember(Value = "WINNER")]
            Winner,

            [EnumMember(Value = "LOSER")]
            Loser,

            [EnumMember(Value = "REMOVED")]
            Removed,

            [EnumMember(Value = "REMOVED_VACANT")]
            RemovedVacant,

            [EnumMember(Value = "HIDDEN")]
            Hidden,

            [EnumMember(Value = "PLACED")]
            Placed
        }

        /// <summary>
        ///     Gets or Sets AdjustmentFactor
        /// </summary>
        [DataMember(Name = "adjustmentFactor", EmitDefaultValue = false)]
        public double? AdjustmentFactor { get; set; } = AdjustmentFactor;

        /// <summary>
        ///     Gets or Sets Bsp
        /// </summary>
        [DataMember(Name = "bsp", EmitDefaultValue = false)]
        public double? Bsp { get; set; } = Bsp;

        /// <summary>
        ///     Handicap - the handicap of the runner (selection) (null if not applicable)
        /// </summary>
        /// <value>Handicap - the handicap of the runner (selection) (null if not applicable)</value>
        [DataMember(Name = "hc", EmitDefaultValue = false)]
        public double? Hc { get; set; } = Hc;

        /// <summary>
        ///     Selection Id - the id of the runner (selection)
        /// </summary>
        /// <value>Selection Id - the id of the runner (selection)</value>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public long? Id { get; set; } = Id;

        /// <summary>
        ///     Gets or Sets RemovalDate
        /// </summary>
        [DataMember(Name = "removalDate", EmitDefaultValue = false)]
        public DateTime? RemovalDate { get; set; } = RemovalDate;

        /// <summary>
        ///     Gets or Sets SortPriority
        /// </summary>
        [DataMember(Name = "sortPriority", EmitDefaultValue = false)]
        public int? SortPriority { get; set; } = SortPriority;

        /// <summary>
        ///     Gets or Sets Status
        /// </summary>
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public StatusEnum? Status { get; set; } = Status;

        /// <summary>
        ///     Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            return Equals(obj as RunnerDefinition);
        }

        /// <summary>
        ///     Returns true if RunnerDefinition instances are equal
        /// </summary>
        /// <param name="other">Instance of RunnerDefinition to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(RunnerDefinition other)
        {
            // credit: http://stackoverflow.com/a/10454552/677735
            if (other == null)
                return false;

            return (SortPriority == other.SortPriority || SortPriority != null && SortPriority.Equals(other.SortPriority)) &&
                   (RemovalDate == other.RemovalDate || RemovalDate != null && RemovalDate.Equals(other.RemovalDate)) &&
                   (Id == other.Id || Id != null && Id.Equals(other.Id)) &&
                   (Hc == other.Hc || Hc != null && Hc.Equals(other.Hc)) &&
                   (AdjustmentFactor == other.AdjustmentFactor || AdjustmentFactor != null && AdjustmentFactor.Equals(other.AdjustmentFactor)) &&
                   (Bsp == other.Bsp || Bsp != null && Bsp.Equals(other.Bsp)) &&
                   (Status == other.Status || Status != null && Status.Equals(other.Status));
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

                if (SortPriority != null)
                    hash = hash * 59 + SortPriority.GetHashCode();

                if (RemovalDate != null)
                    hash = hash * 59 + RemovalDate.GetHashCode();

                if (Id != null)
                    hash = hash * 59 + Id.GetHashCode();

                if (Hc != null)
                    hash = hash * 59 + Hc.GetHashCode();

                if (AdjustmentFactor != null)
                    hash = hash * 59 + AdjustmentFactor.GetHashCode();

                if (Bsp != null)
                    hash = hash * 59 + Bsp.GetHashCode();

                if (Status != null)
                    hash = hash * 59 + Status.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        ///     Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
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
            sb.Append("class RunnerDefinition {\n");
            sb.Append("  SortPriority: ")
                .Append(SortPriority)
                .Append("\n");
            sb.Append("  RemovalDate: ")
                .Append(RemovalDate)
                .Append("\n");
            sb.Append("  Id: ")
                .Append(Id)
                .Append("\n");
            sb.Append("  Hc: ")
                .Append(Hc)
                .Append("\n");
            sb.Append("  AdjustmentFactor: ")
                .Append(AdjustmentFactor)
                .Append("\n");
            sb.Append("  Bsp: ")
                .Append(Bsp)
                .Append("\n");
            sb.Append("  Status: ")
                .Append(Status)
                .Append("\n");

            sb.Append("}\n");
            return sb.ToString();
        }
    }
}