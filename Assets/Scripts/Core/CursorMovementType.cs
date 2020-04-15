using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Author: Peter Caruana, Mark Voong
 *
 */

public abstract class CursorMovementType
{
    /// <summary>
    /// Transforms the position depending on the movement type
    /// </summary>
    public abstract Vector3 ConvertPosition(Vector3 position);
}

public class AlignedMovementType : CursorMovementType
{
    public override Vector3 ConvertPosition(Vector3 position)
    {
        return Vector3.zero;
    }
}


public class RotatedMovementType : CursorMovementType
{
    public override Vector3 ConvertPosition(Vector3 position)
    {
        return Vector3.zero;
    }
}

public class ClampedMovementType : CursorMovementType
{
    public override Vector3 ConvertPosition(Vector3 position)
    {
        throw new System.NotImplementedException();
    }
}
