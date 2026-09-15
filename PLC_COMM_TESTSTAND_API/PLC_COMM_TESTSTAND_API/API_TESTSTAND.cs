using libplctag;

namespace PLC_COMM_TESTSTAND_API
{
    public class API_TESTSTAND
    {
        List<Tag> Tags = new List<Tag>();
        string APIGateWay = "192.168.1.10";
        int Max_Retyr_nmb = 6;
        int Retry_Delay = 100;
        public API_TESTSTAND(string GateWay)
        {
            APIGateWay = GateWay;
        }

        public void AddVariable(string VariableName, string Type, int ArrayLength = 1, int TimeOut = 500)
        {
            int TagElementSize = 1;
            int TagElementCount = 1;

            for(int i = 0; i<Tags.Count; i++)
            {
                if (Tags[i].Name == VariableName)
                    return;
            }

            switch (Type)
            {
                case "STRING": // String 
                    TagElementSize = 88;
                    TagElementCount = ArrayLength;
                    break;

                case "BOOL": // Bool (1 byte)
                    if (ArrayLength == 1)
                    {
                        TagElementSize = 1;
                        TagElementCount = 1;
                    }
                    else
                    {
                        TagElementSize = 4;
                        TagElementCount = (int)Math.Ceiling(ArrayLength / 32.0);
                    }
                    break;

                case "SINT": // SINT / Int8 (1 byte)
                    TagElementSize = 1;
                    TagElementCount = ArrayLength;
                    break;

                case "USINT": // USINT / UInt8 (1 byte sin signo)
                    TagElementSize = 1;
                    TagElementCount = ArrayLength;
                    break;

                case "INT": // INT / Int16 (2 bytes)
                    TagElementSize = 2;
                    TagElementCount = ArrayLength;
                    break;

                case "UINT": // UINT / UInt16 (2 bytes sin signo)
                    TagElementSize = 2;
                    TagElementCount = ArrayLength;
                    break;

                case "DINT": // DINT / Int32 (4 bytes)
                    TagElementSize = 4;
                    TagElementCount = ArrayLength;
                    break;

                case "UDINT": // UDINT / UInt32 (4 bytes sin signo)
                    TagElementSize = 4;
                    TagElementCount = ArrayLength;
                    break;

                case "LINT": // LINT / Int64 (8 bytes)
                    TagElementSize = 8;
                    TagElementCount = ArrayLength;
                    break;

                case "ULINT": // ULINT / UInt64 (8 bytes sin signo)
                    TagElementSize = 8;
                    TagElementCount = ArrayLength;
                    break;

                case "REAL": // REAL / Float32 (4 bytes con decimales)
                    TagElementSize = 4;
                    TagElementCount = ArrayLength;
                    break;

                case "LREAL": // LREAL / Float64 (8 bytes con decimales)
                    TagElementSize = 8;
                    TagElementCount = ArrayLength;
                    break;

                default:
                    throw new Exception($"The data type doesn't exists: {Type}, please insert one of the following types:\nSTRING\nBOOL\nSINT\nUSINT\nINT\nUINT\nDINT\nUDINT\nLINT\nULINT\nREAL\nLREAL.");
            }

            Tag NewTag = new Tag()
            {
                PlcType = PlcType.ControlLogix,
                Path = "1,0",
                Name = VariableName,
                Gateway = APIGateWay,
                Protocol = Protocol.ab_eip,
                Timeout = TimeSpan.FromMilliseconds(TimeOut),
                ElementSize = TagElementSize,
                ElementCount = TagElementCount,
                UseConnectedMessaging = true
            };

            Tags.Add(NewTag);

        }

        public void CloseConnection()
        {
            foreach (var tag in Tags)
            {
                try
                {
                    tag.Dispose();
                }
                catch (Exception e)
                {
                    throw new Exception($"{e.Message}");
                }
            }

            Tags.Clear();
        }

        public string GetVariableString(string VariableName)
        {
            int Count;
            int Count_Retry;
            int DataLen;
            string str = "";
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    DataLen = CurrentTag.GetInt32(0);


                    if (DataLen < 0 || DataLen > 82)
                    {
                        DataLen = 0;
                    }

                    for (Count = 0; Count < DataLen; Count++)
                    {
                        str = $"{str}{(char)CurrentTag.GetUInt8(4 + Count)}";
                    }

                    return str;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return str;
        }

        public string[] GetVariableArrayString(string VariableName)
        {
            List<string> ReturnValue = new List<string>();
            int Count;
            int Count_Retry;
            int Count_Index;
            int DataLen;
            string str = "";
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();



                    for (Count_Index = 0; Count_Index < CurrentTag.ElementCount; Count_Index++)
                    {
                        DataLen = CurrentTag.GetInt32(Count_Index * (int)CurrentTag.ElementSize);
                        str = "";

                        if (DataLen < 0 || DataLen > 82)
                        {
                            DataLen = 0;
                        }

                        for (Count = 0; Count < DataLen; Count++)
                        {
                            str = $"{str}{(char)CurrentTag.GetUInt8((4 + Count) + Count_Index * (int)CurrentTag.ElementSize)}";
                        }
                        ReturnValue.Add(str);
                    }


                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();

        }

        public string GetIndexArrayString(string VariableName, int Index)
        {
            List<string> FullArray = new List<string>();
            FullArray = GetVariableArrayString(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool GetVariableBool(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetBit(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return false;
        }

        public bool[] GetVariableArrayBool(string VariableName)
        {
            List<bool> ReturnValue = new List<bool>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount * 32; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetBit(Count));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public bool GetIndexArrayBool(string VariableName, int Index)
        {
            List<bool> FullArray = new List<bool>();
            FullArray = GetVariableArrayBool(VariableName).ToList();

            return FullArray[Index];
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public sbyte GetVariableSINT(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetInt8(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return -1;
        }

        public sbyte[] GetVariableArraySINT(string VariableName)
        {
            List<sbyte> ReturnValue = new List<sbyte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt8(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public sbyte GetIndexArraySINT(string VariableName, int Index)
        {
            List<sbyte> FullArray = new List<sbyte>();
            FullArray = GetVariableArraySINT(VariableName).ToList();

            return FullArray[Index];
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public byte GetVariableUSINT(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }



            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetUInt8(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1 - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return 0;

        }

        public byte[] GetVariableArrayUSINT(string VariableName)
        {
            List<byte> ReturnValue = new List<byte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt8(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public byte GetIndexArrayUSINT(string VariableName, int Index)
        {
            List<byte> FullArray = new List<byte>();
            FullArray = GetVariableArrayUSINT(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public short GetVariableINT(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetInt16(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return -101;
        }

        public short[] GetVariableArrayINT(string VariableName)
        {
            List<short> ReturnValue = new List<short>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt16(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public short GetIndexArrayINT(string VariableName, int Index)
        {
            List<short> FullArray = new List<short>();
            FullArray = GetVariableArrayINT(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ushort GetVariableUINT(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }



            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetUInt16(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return 0;

        }

        public ushort[] GetVariableArrayUINT(string VariableName)
        {
            List<ushort> ReturnValue = new List<ushort>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt16(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public ushort GetIndexArrayUINT(string VariableName, int Index)
        {
            List<ushort> FullArray = new List<ushort>();
            FullArray = GetVariableArrayUINT(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public int GetVariableDINT(string VariableName)
        {
            int Count;
            Tag CurrentTag = new Tag();
            int Count_Retry;
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetInt32(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return -101;
        }

        public int[] GetVariableArrayDINT(string VariableName)
        {
            List<int> ReturnValue = new List<int>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt32(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public int GetIndexArrayDINT(string VariableName, int Index)
        {
            List<int> FullArray = new List<int>();
            FullArray = GetVariableArrayDINT(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public uint GetVariableUDINT(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetUInt32(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return 0;
        }

        public uint[] GetVariableArrayUDINT(string VariableName)
        {
            List<uint> ReturnValue = new List<uint>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt32(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public uint GetIndexArrayUDINT(string VariableName, int Index)
        {
            List<uint> FullArray = new List<uint>();
            FullArray = GetVariableArrayUDINT(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public long GetVariableLINT(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetInt64(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return -101;
        }

        public long[] GetVariableArrayLINT(string VariableName)
        {
            List<long> ReturnValue = new List<long>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt64(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public long GetIndexArrayLINT(string VariableName, int Index)
        {
            List<long> FullArray = new List<long>();
            FullArray = GetVariableArrayLINT(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ulong GetVariableULINT(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetUInt64(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return 0;
        }

        public ulong[] GetVariableArrayULINT(string VariableName)
        {
            List<ulong> ReturnValue = new List<ulong>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt64(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public ulong GetIndexArrayULINT(string VariableName, int Index)
        {
            List<ulong> FullArray = new List<ulong>();
            FullArray = GetVariableArrayULINT(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public float GetVariableREAL(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetFloat32(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }


            return -101;
        }

        public float[] GetVariableArrayREAL(string VariableName)
        {
            List<float> ReturnValue = new List<float>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetFloat32(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }


            return ReturnValue.ToArray();
        }

        public float GetIndexArrayREAL(string VariableName, int Index)
        {
            List<float> FullArray = new List<float>();
            FullArray = GetVariableArrayREAL(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public double GetVariableLREAL(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    return CurrentTag.GetFloat64(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }


            return -101;
        }
        public double[] GetVariableArrayLREAL(string VariableName)
        {
            List<double> ReturnValue = new List<double>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetFloat64(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue.ToArray();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

            return ReturnValue.ToArray();
        }

        public double GetIndexArrayLREAL(string VariableName, int Index)
        {
            List<double> FullArray = new List<double>();
            FullArray = GetVariableArrayLREAL(VariableName).ToList();

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public void SetVariableString(string VariableName, string VariableValue)
        {
            int Count;
            int Count_Retry;
            int DataLen;
            string str = "";
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetInt32(0, VariableValue.Length);

                    for (Count = 0; Count < VariableValue.Length; Count++)
                    {
                        CurrentTag.SetUInt8(4 + Count, (byte)VariableValue[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayString(string VariableName, List<string> VariableValues)
        {
            List<string> ReturnValue = new List<string>();
            int Count;
            int Count_Retry;
            int Count_Index;
            int DataLen;
            string str = "";
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count_Index = 0; Count_Index < VariableValues.Count; Count_Index++)
                    {
                        CurrentTag.SetInt32(Count_Index * (int)CurrentTag.ElementSize, VariableValues[Count_Index].Length);

                        for (Count = 0; Count < VariableValues[Count_Index].Length; Count++)
                        {
                            str = $"{str}{(char)CurrentTag.GetUInt8((4 + Count) + Count_Index * (int)CurrentTag.ElementSize)}";

                            CurrentTag.SetUInt8(4 + Count + Count_Index * (int)CurrentTag.ElementSize, (byte)VariableValues[Count_Index][Count]);
                        }
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

        }

        public void SetIndexArrayString(string VariableName, int Index, string VariableValue)
        {
            List<string> FullArray = new List<string>();
            FullArray = GetVariableArrayString(VariableName).ToList();

            FullArray[Index] = VariableValue;

            SetVariableArrayString(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableBool(string VariableName, bool VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetBit(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayBool(string VariableName, List<bool> VariableValues)
        {
            List<bool> ReturnValue = new List<bool>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount * 32)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetBit(Count, VariableValues[Count]);
                    }
                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }

        }

        public void SetIndexArrayBool(string VariableName, int Index, bool VariableValue)
        {
            List<bool> FullArray = new List<bool>();
            FullArray = GetVariableArrayBool(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayBool(VariableName, FullArray);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public void SetVariableSINT(string VariableName, sbyte VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetInt8(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArraySINT(string VariableName, List<sbyte> VariableValues)
        {
            List<sbyte> ReturnValue = new List<sbyte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt8(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArraySINT(string VariableName, int Index, sbyte VariableValue)
        {
            List<sbyte> FullArray = new List<sbyte>();
            FullArray = GetVariableArraySINT(VariableName).ToList();

            FullArray[Index] = VariableValue;

            SetVariableArraySINT(VariableName, FullArray);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableUSINT(string VariableName, byte VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetUInt8(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayUSINT(string VariableName, List<byte> VariableValues)
        {
            List<byte> ReturnValue = new List<byte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt8(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayUSINT(string VariableName, int Index, byte VariableValue)
        {
            List<byte> FullArray = new List<byte>();
            FullArray = GetVariableArrayUSINT(VariableName).ToList();

            FullArray[Index] = VariableValue;

            SetVariableArrayUSINT(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableINT(string VariableName, short VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetInt16(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayINT(string VariableName, List<short> VariableValues)
        {
            List<short> ReturnValue = new List<short>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt16(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayINT(string VariableName, int Index, short VariableValue)
        {
            List<short> FullArray = new List<short>();
            FullArray = GetVariableArrayINT(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayINT(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableUINT(string VariableName, ushort VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetUInt16(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayUINT(string VariableName, List<ushort> VariableValues)
        {
            List<ushort> ReturnValue = new List<ushort>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt16(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayUINT(string VariableName, int Index, ushort VariableValue)
        {
            List<ushort> FullArray = new List<ushort>();
            FullArray = GetVariableArrayUINT(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayUINT(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableDINT(string VariableName, int VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetInt32(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayDINT(string VariableName, List<int> VariableValues)
        {
            List<int> ReturnValue = new List<int>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt32(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayDINT(string VariableName, int Index, int VariableValue)
        {
            List<int> FullArray = new List<int>();
            FullArray = GetVariableArrayDINT(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayDINT(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableUDINT(string VariableName, uint VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetUInt32(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayUDINT(string VariableName, List<uint> VariableValues)
        {
            List<uint> ReturnValue = new List<uint>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt32(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayUDINT(string VariableName, int Index, uint VariableValue)
        {
            List<uint> FullArray = new List<uint>();
            FullArray = GetVariableArrayUDINT(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayUDINT(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableLINT(string VariableName, long VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetInt64(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayLINT(string VariableName, List<long> VariableValues)
        {
            List<long> ReturnValue = new List<long>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt64(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayLINT(string VariableName, int Index, long VariableValue)
        {
            List<long> FullArray = new List<long>();
            FullArray = GetVariableArrayLINT(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayLINT(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableULINT(string VariableName, ulong VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }


            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetUInt64(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayULINT(string VariableName, List<ulong> VariableValues)
        {
            List<ulong> ReturnValue = new List<ulong>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt64(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayULINT(string VariableName, int Index, ulong VariableValue)
        {
            List<ulong> FullArray = new List<ulong>();
            FullArray = GetVariableArrayULINT(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayULINT(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableREAL(string VariableName, float VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetFloat32(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetVariableArrayREAL(string VariableName, List<float> VariableValues)
        {
            List<float> ReturnValue = new List<float>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetFloat32(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayREAL(string VariableName, int Index, float VariableValue)
        {
            List<float> FullArray = new List<float>();
            FullArray = GetVariableArrayREAL(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayREAL(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void SetVariableLREAL(string VariableName, double VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context");
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    CurrentTag.Read();

                    CurrentTag.SetFloat64(0, VariableValue);

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }
        public void SetVariableArrayLREAL(string VariableName, List<double> VariableValues)
        {
            List<double> ReturnValue = new List<double>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = new Tag();
            for (Count = 0; Count < Tags.Count; Count++)
            {
                if (Tags[Count].Name == VariableName)
                {
                    CurrentTag = Tags[Count];
                    break;
                }
            }

            if (Count == Tags.Count)
            {
                throw new Exception("Variable doesn't exists in the current context"); //The variable doesnt exists in the API.
            }

            for (Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    if (VariableValues.Count > CurrentTag.ElementCount)
                        throw new Exception("You exceeded the maximum number of array length");

                    CurrentTag.Read();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetFloat64(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    CurrentTag.Write();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    Thread.Sleep(Retry_Delay);
                }
            }
        }

        public void SetIndexArrayLREAL(string VariableName, int Index, double VariableValue)
        {
            List<double> FullArray = new List<double>();
            FullArray = GetVariableArrayLREAL(VariableName).ToList();

            FullArray[Index] = VariableValue;
            SetVariableArrayLREAL(VariableName, FullArray);
        }
    }
}
