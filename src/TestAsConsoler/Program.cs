using DestallMaterials.WheelProtection.DataStructures.Serialization;

var streamForWriting = new MemoryStream();

var data = (DateTime.Now, DateTime.Now.AddDays(2));

TuplesSerialization.SerializeTuple(data, streamForWriting);

var streamForReading = new MemoryStream(streamForWriting.ToArray());

var data2 = TuplesSerialization.DeserializeTuple<DateTime, DateTime>(streamForReading);

return 0;