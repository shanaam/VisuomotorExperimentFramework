using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public class ExperimentContainer
{
    // For use in configurator
    public int Blocks { get; private set; }

    public Dictionary<string, object> Data { get; private set; }
    public Dictionary<string, object> MasterValues { get; private set; }

    // Special keywords in the master JSON that is used to define a specific data type
    public const string STRING_PROPERTY_ID = "%string%";
    public const string INTEGER_PROPERTY_ID = "%integer%";
    public const string BOOLEAN_PROPERTY_ID = "%bool%";
    public const string FLOAT_PROPERTY_ID = "%float%";

    // A master JSON allows us to type check input and only allow certain types to be saved for
    // a specific property. Here is an example:
    //
    // The property "per_block_n" should only accept integers as valid values. Thus the keyword we use is
    // %integer% in the JSON.
    //
    // How the master JSON works:
    // All values in the json are in the format: "key" : [values, ...]
    // key - Whenever a property is edited, we check the list pointed to by this string
    // values - A list of valid inputs. We can use one of the following:
    // %string%, %integer%, etc. This allows us to type check the input
    // [value1, value2, value3, ...]. This allows us to use a dropdown if we only want certain options.

    // Note: as of now, we can add properties to the master JSON using the editor. Type checking must be added
    // manually in this class if we want to check specific types other than the ones specified (like a float)

    public ExperimentContainer(Dictionary<string, object> data, Dictionary<string, object> masterData)
    {
        Data = data;
        MasterValues = masterData;
    }

    /// <summary>
    /// Appends the specified number of blocks
    /// </summary>
    /// <param name="numBlocks"></param>
    public void AddBlocks(int numBlocks)
    {
        // We only modify values where the key starts with per_block
        // Since these are all parallel arrays
        foreach (KeyValuePair<string, object> kp in Data)
        {
            if (kp.Key.StartsWith("per_block"))
            {
                Type t = kp.Value.GetType();
                List<Object> val = (List<object>)(kp.Value);
                ResizeList(val, val.Count + numBlocks);
            }
        }
    }

    /// <summary>
    /// Modifies a value stored in an array
    /// </summary>
    /// <param name="key"></param>
    /// <param name="index"></param>
    /// <param name="value"></param>
    public void ModifyArray(string key, int index, object value)
    {
        List<Object> array = (List<Object>) Data[key];
        array[index] = value;
    }

    /// <summary>
    /// Modifies a value stored as a single value
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void ModifyValue(string key, object value)
    {
        Data[key] = value;
    }

    /// <summary>
    /// Swaps block a with block b. Zero based index required
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public void SwapBlock(int a, int b)
    {
        // Note:
        // This may not work with referenced objects as
        // the swap does a shallow copy. For primitive types (strings, ints, floats, etc)
        // A deep copy is performed as intended. If class instances are being stored
        // in Data this may cause issues.
        foreach (KeyValuePair<string, object> kp in Data)
        {
            if (kp.Key.StartsWith("per_block"))
            {
                List<object> array = kp.Value as List<object>;

                object temp = array[a];
                array[b] = array[a];
                array[a] = temp;
            }
        }
    }

    /// <summary>
    /// Resizes the list to newLength
    /// </summary>
    /// <param name="array"></param>
    /// <param name="newLength"></param>
    private void ResizeList(List<object> array, int newLength)
    {
        Type elementType = array.GetType().GetElementType();
        for (int i = array.Count; i < newLength; i++)
        {
            if (elementType.GetTypeInfo().IsValueType)
            {
                array.Add(Activator.CreateInstance(elementType));
            }
            else
            {
                array.Add(null);
            }
        }
    }

    /// <summary>
    /// Using a master JSON, determine the type associated with a key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object GetDefaultValue(string key) 
    {
        if (MasterValues.ContainsKey(key))
        {
            List<object> list = MasterValues[key] as List<object>;

            if (list?.Count > 0)
            {
                switch (list[0].ToString())
                {
                    case "%integer%":
                        return 0;
                    case "%string%":
                        return "";
                    case "%bool%":
                        return false;
                    case "%float%":
                        return 0.0f;
                    default:
                        return list;
                }
            }
            else
            {
                throw new NullReferenceException("Master JSON is malformed for key: " + key + ". Please" +
                                                 " check the JSON.");
            }

        }
        else
        {
            throw new NullReferenceException(key + " does not exist in the master JSON");
        }
    }

    /// <summary>
    /// Converts a string into its correct type.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public object ConvertToCorrectType(string input)
    {
        // If the input is a number
        if (input.Length > 0 && input.All(char.IsDigit))
        {
            return int.Parse(input);
        }

        Regex rx = new Regex(@"[+-]?([0-9]*[.])?[0-9]+");

        if (rx.IsMatch(input))
        {
            return float.Parse(input);
        }


        // Convert the string "true" and "false" to the bool type
        if (input.ToLower() == "true")
        {
            return true;
        }
        
        if (input.ToLower() == "false")
        {
            return false;
        }

        // If there is no valid conversion, return the string itself
        return input;
    }
}
