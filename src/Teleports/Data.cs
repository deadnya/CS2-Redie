using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public class TeleportsData
{
    public TeleportsData(CBaseProp teleport, string model, string name)
    {
        Entity = teleport;
        Model = model;
        Name = name;
    }

    public CBaseProp Entity;
    public string Name { get; private set; }
    public string Model { get; private set; }
}

public class SavedData
{
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public VectorDTO Position { get; set; } = new VectorDTO(Vector.Zero);
    public QAngleDTO Rotation { get; set; } = new QAngleDTO(QAngle.Zero);
}

public class TeleportPair
{
    public TeleportsData Entry { get; set; }
    public TeleportsData Exit { get; set; }

    public TeleportPair(TeleportsData entry, TeleportsData exit)
    {
        Entry = entry;
        Exit = exit;
    }
}

public class TeleportPairDTO
{
    public SavedData Entry { get; set; } = new SavedData();
    public SavedData Exit { get; set; } = new SavedData();
}

public class VectorDTO
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public VectorDTO() { }

    public VectorDTO(Vector vector)
    {
        X = vector.X;
        Y = vector.Y;
        Z = vector.Z;
    }

    public Vector ToVector()
    {
        return new Vector(X, Y, Z);
    }
}

public class QAngleDTO
{
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float Roll { get; set; }

    public QAngleDTO() { }

    public QAngleDTO(QAngle qangle)
    {
        Pitch = qangle.X;
        Yaw = qangle.Y;
        Roll = qangle.Z;
    }

    public QAngle ToQAngle()
    {
        return new QAngle(Pitch, Yaw, Roll);
    }
}