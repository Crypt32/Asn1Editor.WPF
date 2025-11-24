using System;

namespace SysadminsLV.Asn1Editor.Core.ASN;

/// <summary>
/// Represents ASN.1 node text value object. Is used by TreeView and TagDataEditor controls.
/// </summary>
public class AsnViewValue : NotifyPropertyChanged {
    String? textValue;
    Byte unusedBits;
    AsnViewValueOptions options;

    /// <summary>
    /// Gets or sets the ASN.1 node text value. Can be either printable string or hex dump.
    /// </summary>
    public String? TextValue {
        get => textValue;
        set {
            if (textValue != value) {
                textValue = value;
                OnPropertyChanged();
            }
        }
    }
    public Byte UnusedBits {
        get => unusedBits;
        set {
            if (unusedBits != value) {
                unusedBits = value;
                OnPropertyChanged();
            }
        }
    }
    /// <summary>
    /// Gets or sets the ASN.1 node text value options, such as whether it can be viewed as printable text.
    /// </summary>
    public AsnViewValueOptions Options {
        get => options;
        set {
            if (options != value) {
                options = value;
                OnPropertyChanged();
            }
        }
    }

    public AsnViewValue Clone() {
        return new AsnViewValue {
            TextValue = TextValue,
            UnusedBits = UnusedBits,
            Options = Options
        };
    }

    #region Equals

    public override Boolean Equals(Object? obj) {
        return obj is not null &&
               (ReferenceEquals(this, obj) || obj.GetType() == GetType() && Equals((AsnViewValue)obj));
    }
    protected Boolean Equals(AsnViewValue other) {
        return textValue == other.textValue && unusedBits == other.unusedBits;
    }
    public override Int32 GetHashCode() {
        unchecked {
            return ((textValue is not null
                ? textValue.GetHashCode()
                : 0) * 397) ^ unusedBits.GetHashCode();
        }
    }

    #endregion
}