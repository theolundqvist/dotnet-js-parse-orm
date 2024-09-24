using Project.FirebaseORM.Authentication;
using Project.FirebaseORM.Database.Data;
using RSG;

namespace Project.FirebaseORM.Database.Transform;
[Obsolete("Use new Project.Backend")]
public class DocumentTransform
{
    
    private List<string> _transforms = new();
    private readonly string _path;

    public DocumentTransform(string documentPath)
    {
        _path = FirebaseCredentials.dbPath + documentPath;
        if (string.IsNullOrEmpty(_path)) throw new Exception("Document path must not be null or empty.");
    }
    
    public string GetTransformPayload()
    {
        string document = "\"document\":\"" + _path + "\"";
        string fieldTransforms = "["+ string.Join(", ", _transforms) + "]";
        string transform = "\"transform\":{" + document + ",\"fieldTransforms\":"+fieldTransforms+"}";
        string transformPayload = "{\"writes\": [{" + transform + "}]}";
        return transformPayload;
    }

    public IPromise Send(Database db) => db.PostTransform(this);

    private DocumentTransform AddFieldTransform(string path, string name, string obj)
    {
        var data = "\"" + name + "\":"+obj;
        _transforms.Add("{" +
               "\"fieldPath\":\"" + path + "\"," +
               data +
               "}");
        return this;
    }

    private DocumentTransform IntegerTransform(string path, string name, int val)
        => AddFieldTransform(path, name, "{\"integerValue\": \"" + val + "\"}");

    private DocumentTransform DoubleTransform(string path, string name, double val)
        => AddFieldTransform(path, name, "{\"doubleValue\": \"" + val + "\"}");

    /// <summary>
    /// Will only do basic values
    /// </summary>
    private DocumentTransform ArrayTransform<T>(string path, string name, T[] values)
    {
        var typeStr = FirestoreObject.GetTypeString(typeof(T));
        var d = typeStr is "doubleValue" or "mapValue"  ? "" : "\"";
        var s = values.Select(x => "{\"" + typeStr + "\": " + d + x + d + "}");
        var data = "{\"values\":[" + string.Join(", ", s) + "]}";
        return AddFieldTransform(path, name, data);
    }

    /// <inheritdoc cref="Increment(string, double)"/>>
    public DocumentTransform Increment(string fieldName, int inc) => IntegerTransform(fieldName, "increment", inc);

    /// <summary>
    /// Adds the given value to the field's current value.
    /// This must be an integer or a double value. If the field is not an integer or double, or if the field does not yet exist, the transformation will set the field to the given value. If either of the given value or the current field value are doubles, both values will be interpreted as doubles. Double arithmetic and representation of double values follow IEEE 754 semantics. If there is positive/negative integer overflow, the field is resolved to the largest magnitude positive/negative integer.
    /// </summary>
    /// <param name="fieldName"> The name/path of the field. ex "balance"</param>
    /// <param name="inc"> </param>
    public DocumentTransform Increment(string fieldName, double inc) => DoubleTransform(fieldName,"increment", inc);

    /// <inheritdoc cref="Maximum(string, double)"/>>
    public DocumentTransform Maximum(string fieldName, int inc) => IntegerTransform(fieldName, "maximum", inc);

    /// <summary>
    /// Sets the field to the maximum of its current value and the given value.
    /// This must be an integer or a double value. If the field is not an integer or double, or if the field does not yet exist, the transformation will set the field to the given value. If a maximum operation is applied where the field and the input value are of mixed types (that is - one is an integer and one is a double) the field takes on the type of the larger operand. If the operands are equivalent (e.g. 3 and 3.0), the field does not change. 0, 0.0, and -0.0 are all zero. The maximum of a zero stored value and zero input value is always the stored value. The maximum of any numeric value x and NaN is NaN.
    /// </summary>
    /// <param name="fieldName"> The name/path of the field. ex "balance" </param>
    /// <param name="inc"> </param>
    public DocumentTransform Maximum(string fieldName, double inc) => DoubleTransform(fieldName, "maximum", inc);

    /// <inheritdoc cref="Minimum(string, double)"/>>
    public DocumentTransform Minimum(string fieldName,int inc) => IntegerTransform(fieldName, "minimum", inc);

    /// <summary>
    /// Sets the field to the minimum of its current value and the given value.
    /// This must be an integer or a double value. If the field is not an integer or double, or if the field does not yet exist, the transformation will set the field to the input value. If a minimum operation is applied where the field and the input value are of mixed types (that is - one is an integer and one is a double) the field takes on the type of the smaller operand. If the operands are equivalent (e.g. 3 and 3.0), the field does not change. 0, 0.0, and -0.0 are all zero. The minimum of a zero stored value and zero input value is always the stored value. The minimum of any numeric value x and NaN is NaN.
    /// </summary>
    /// <param name="fieldName"> The name/path of the field. ex "balance" </param>
    /// <param name="inc"> </param>
    public DocumentTransform Minimum(string fieldName,double inc) => DoubleTransform(fieldName, "minimum", inc);

    /// <summary>
    /// The time at which the server processed the request, with millisecond precision. If used on multiple fields (same or different documents) in a transaction, all the fields will get the same server timestamp.
    /// </summary>
    public DocumentTransform SetToRequestTime(string fieldName)
        => AddFieldTransform(fieldName, "setToServerValue", "REQUEST_TIME");

    /// <summary>
    /// Append the given elements in order if they are not already present in the current field value. If the field is not an array, or if the field does not yet exist, it is first set to the empty array.
    /// Equivalent numbers of different types (e.g. 3L and 3.0) are considered equal when checking if a value is missing. NaN is equal to NaN, and Null is equal to Null. If the input contains multiple equivalent values, only the first will be considered.
    /// The corresponding transform_result will be the null value.
    /// </summary>
    /// <param name="fieldName"> The name/path of the field. ex "balance" </param>
    /// <param name="elements"></param>
    /// <typeparam name="T"> Supports all the base types supported by <see cref="FirestoreObject"/>, i.e. bool, string, int, double </typeparam>
    public DocumentTransform AppendMissingElements<T>(string fieldName, params T[] elements) => ArrayTransform(fieldName, "appendMissingElements", elements);

    /// <summary>
    /// Remove all of the given elements from the array in the field. If the field is not an array, or if the field does not yet exist, it is set to the empty array.
    /// Equivalent numbers of the different types (e.g. 3L and 3.0) are considered equal when deciding whether an element should be removed. NaN is equal to NaN, and Null is equal to Null. This will remove all equivalent values if there are duplicates.
    /// The corresponding transform_result will be the null value.
    /// </summary>
    /// <param name="fieldName"> The name/path of the field. ex "balance" </param>
    /// <param name="elements"></param>
    /// <typeparam name="T"> Supports all the base types supported by <see cref="FirestoreObject"/>, i.e. bool, string, int, double </typeparam>
    public DocumentTransform RemoveAllFromArray<T>(string fieldName, params T[] elements) => ArrayTransform(fieldName, "removeAllFromArray", elements);
}