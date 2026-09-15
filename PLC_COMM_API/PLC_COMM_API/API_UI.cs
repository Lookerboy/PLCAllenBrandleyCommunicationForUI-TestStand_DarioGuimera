using libplctag;
using libplctag.DataTypes;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PLC_COMM_API
{
    public class API_UI
    {
        List<Tag> Tags = new List<Tag>();
        string APIGateWay = "192.168.1.10";
        int Max_Retyr_nmb = 6;
        int Retry_Delay = 100;
        public API_UI(string GateWay)
        {
            APIGateWay = GateWay;
        }

        public void AddVariable(string VariableName, int Type, int ArrayLength = 1, int TimeOut = 500)
        {
            int TagElementSize = 1;
            int TagElementCount = 1;

            switch (Type)
            {
                case 1: // String 
                    TagElementSize = 88;
                    TagElementCount = ArrayLength; 
                    break;

                case 2: // Bool (1 byte)
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

                case 3: // SINT / Int8 (1 byte)
                    TagElementSize = 1;
                    TagElementCount = ArrayLength;
                    break;

                case 4: // USINT / UInt8 (1 byte sin signo)
                    TagElementSize = 1;
                    TagElementCount = ArrayLength;
                    break;

                case 5: // INT / Int16 (2 bytes)
                    TagElementSize = 2;
                    TagElementCount = ArrayLength;
                    break;

                case 6: // UINT / UInt16 (2 bytes sin signo)
                    TagElementSize = 2;
                    TagElementCount = ArrayLength;
                    break;

                case 7: // DINT / Int32 (4 bytes)
                    TagElementSize = 4;
                    TagElementCount = ArrayLength;
                    break;

                case 8: // UDINT / UInt32 (4 bytes sin signo)
                    TagElementSize = 4;
                    TagElementCount = ArrayLength;
                    break;

                case 9: // LINT / Int64 (8 bytes)
                    TagElementSize = 8;
                    TagElementCount = ArrayLength;
                    break;

                case 10: // ULINT / UInt64 (8 bytes sin signo)
                    TagElementSize = 8;
                    TagElementCount = ArrayLength;
                    break;

                case 11: // REAL / Float32 (4 bytes con decimales)
                    TagElementSize = 4;
                    TagElementCount = ArrayLength;
                    break;

                case 12: // LREAL / Float64 (8 bytes con decimales)
                    TagElementSize = 8;
                    TagElementCount = ArrayLength;
                    break;

                default:
                    throw new Exception($"The data type doesn't exists: {Type}");
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
            foreach(var tag in Tags)
            {
                try
                {
                    tag.Dispose();
                }
                catch(Exception e)
                {
                    throw new Exception($"{e.Message}");
                }
            }

            Tags.Clear();
        }

        public async Task<string> GetVariableString(string VariableName)
        {
            int Count;
            int Count_Retry;
            int DataLen;
            string str = "";
            Tag CurrentTag = null;
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

            for(Count_Retry = 0; Count_Retry < Max_Retyr_nmb; Count_Retry++)
            {
                try
                {
                    await CurrentTag.ReadAsync();

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

                    await Task.Delay(Retry_Delay);
                }
            }

            return str;
        }

        public async Task<List<string>> GetVariableArrayString(string VariableName)
        {
            List<string> ReturnValue = new List<string>();
            int Count;
            int Count_Retry;
            int Count_Index;
            int DataLen;
            string str = "";
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();



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


                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;

        }

        public async Task<string> GetIndexArrayString(string VariableName, int Index)
        {
            List<string> FullArray = new List<string>();
            FullArray = await GetVariableArrayString(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<bool> GetVariableBool(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetBit(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return false;
        }

        public async Task<List<bool>> GetVariableArrayBool(string VariableName)
        {
            List<bool> ReturnValue = new List<bool>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount * 32; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetBit(Count));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<bool> GetIndexArrayBool(string VariableName, int Index)
        {
            List<bool> FullArray = new List<bool>();
            FullArray = await GetVariableArrayBool(VariableName);

            return FullArray[Index];
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public async Task<sbyte> GetVariableInt8(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetInt8(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return -1;
        }

        public async Task<List<sbyte>> GetVariableArrayInt8(string VariableName)
        {
            List<sbyte> ReturnValue = new List<sbyte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt8(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<sbyte> GetIndexArrayInt8(string VariableName, int Index)
        {
            List<sbyte> FullArray = new List<sbyte>();
            FullArray = await GetVariableArrayInt8(VariableName);

            return FullArray[Index];
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<byte> GetVariableUInt8(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetUInt8(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return 0;

        }

        public async Task<List<byte>> GetVariableArrayUInt8(string VariableName)
        {
            List<byte> ReturnValue = new List<byte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt8(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<byte> GetIndexArrayUInt8(string VariableName, int Index)
        {
            List<byte> FullArray = new List<byte>();
            FullArray = await GetVariableArrayUInt8(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<short> GetVariableInt16(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetInt16(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return -101;
        }

        public async Task<List<short>> GetVariableArrayInt16(string VariableName)
        {
            List<short> ReturnValue = new List<short>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt16(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<short> GetIndexArrayInt16(string VariableName, int Index)
        {
            List<short> FullArray = new List<short>();
            FullArray = await GetVariableArrayInt16(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ushort> GetVariableUInt16(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetUInt16(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return 0;

        }

        public async Task<List<ushort>> GetVariableArrayUInt16(string VariableName)
        {
            List<ushort> ReturnValue = new List<ushort>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt16(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<ushort> GetIndexArrayUInt16(string VariableName, int Index)
        {
            List<ushort> FullArray = new List<ushort>();
            FullArray = await GetVariableArrayUInt16(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<int> GetVariableInt32(string VariableName)
        {
            int Count;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetInt32(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return -101;
        }

        public async Task<List<int>> GetVariableArrayInt32(string VariableName)
        {
            List<int> ReturnValue = new List<int>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt32(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
            
            return ReturnValue;
        }

        public async Task<int> GetIndexArrayInt32(string VariableName, int Index)
        {
            List<int> FullArray = new List<int>();
            FullArray = await GetVariableArrayInt32(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<uint> GetVariableUInt32(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetUInt32(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return 0;
        }

        public async Task<List<uint>> GetVariableArrayUInt32(string VariableName)
        {
            List<uint> ReturnValue = new List<uint>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt32(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<uint> GetIndexArrayUInt32(string VariableName, int Index)
        {
            List<uint> FullArray = new List<uint>();
            FullArray = await GetVariableArrayUInt32(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<long> GetVariableInt64(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetInt64(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return -101;
        }

        public async Task<List<long>> GetVariableArrayInt64(string VariableName)
        {
            List<long> ReturnValue = new List<long>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetInt64(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<long> GetIndexArrayInt64(string VariableName, int Index)
        {
            List<long> FullArray = new List<long>();
            FullArray = await GetVariableArrayInt64(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ulong> GetVariableUInt64(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetUInt64(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return 0;
        }

        public async Task<List<ulong>> GetVariableArrayUInt64(string VariableName)
        {
            List<ulong> ReturnValue = new List<ulong>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetUInt64(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<ulong> GetIndexArrayUInt64(string VariableName, int Index)
        {
            List<ulong> FullArray = new List<ulong>();
            FullArray = await GetVariableArrayUInt64(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<float> GetVariableReal(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetFloat32(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }


            return -101;
        }

        public async Task<List<float>> GetVariableArrayReal(string VariableName)
        {
            List<float> ReturnValue = new List<float>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetFloat32(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }


            return ReturnValue;
        }

        public async Task<float> GetIndexArrayReal(string VariableName, int Index)
        {
            List<float> FullArray = new List<float>();
            FullArray = await GetVariableArrayReal(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<double> GetVariableLReal(string VariableName)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    return CurrentTag.GetFloat64(0);
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }


            return -101;
        }
        public async Task<List<double>> GetVariableArrayLReal(string VariableName)
        {
            List<double> ReturnValue = new List<double>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < CurrentTag.ElementCount; Count++)
                    {
                        ReturnValue.Add(CurrentTag.GetFloat64(Count * (int)CurrentTag.ElementSize));
                    }

                    return ReturnValue;
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

            return ReturnValue;
        }

        public async Task<double> GetIndexArrayLReal(string VariableName, int Index)
        {
            List<double> FullArray = new List<double>();
            FullArray = await GetVariableArrayLReal(VariableName);

            return FullArray[Index];
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public async Task SetVariableString(string VariableName, string VariableValue)
        {
            int Count;
            int Count_Retry;
            int DataLen;
            string str = "";
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetInt32(0, VariableValue.Length);

                    for (Count = 0; Count < VariableValue.Length; Count++)
                    {
                        CurrentTag.SetUInt8(4 + Count, (byte)VariableValue[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayString(string VariableName, List<string> VariableValues)
        {
            List<string> ReturnValue = new List<string>();
            int Count;
            int Count_Retry;
            int Count_Index;
            int DataLen;
            string str = "";
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count_Index = 0; Count_Index < VariableValues.Count; Count_Index++)
                    {
                        CurrentTag.SetInt32(Count_Index * (int)CurrentTag.ElementSize, VariableValues[Count_Index].Length);

                        for (Count = 0; Count < VariableValues[Count_Index].Length; Count++)
                        {
                            str = $"{str}{(char)CurrentTag.GetUInt8((4 + Count) + Count_Index * (int)CurrentTag.ElementSize)}";

                            CurrentTag.SetUInt8(4 + Count + Count_Index * (int)CurrentTag.ElementSize, (byte)VariableValues[Count_Index][Count]);
                        }
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

        }

        public async Task SetIndexArrayString(string VariableName, int Index, string VariableValue)
        {
            List<string> FullArray = new List<string>();
            FullArray = await GetVariableArrayString(VariableName);

            FullArray[Index] = VariableValue;

            SetVariableArrayString(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableBool(string VariableName, bool VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetBit(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayBool(string VariableName, List<bool> VariableValues)
        {
            List<bool> ReturnValue = new List<bool>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetBit(Count, VariableValues[Count]);
                    }
                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }

        }

        public async Task SetIndexArrayBool(string VariableName, int Index, bool VariableValue)
        {
            List<bool> FullArray = new List<bool>();
            FullArray = await GetVariableArrayBool(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayBool(VariableName, FullArray);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        public async Task SetVariableInt8(string VariableName, sbyte VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetInt8(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayInt8(string VariableName, List<sbyte> VariableValues)
        {
            List<sbyte> ReturnValue = new List<sbyte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt8(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayInt8(string VariableName, int Index, sbyte VariableValue)
        {
            List<sbyte> FullArray = new List<sbyte>();
            FullArray = await GetVariableArrayInt8(VariableName);

            FullArray[Index] = VariableValue;

            SetVariableArrayInt8(VariableName, FullArray);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableUInt8(string VariableName, byte VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetUInt8(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayUInt8(string VariableName, List<byte> VariableValues)
        {
            List<byte> ReturnValue = new List<byte>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt8(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayUInt8(string VariableName, int Index, byte VariableValue)
        {
            List<byte> FullArray = new List<byte>();
            FullArray = await GetVariableArrayUInt8(VariableName);

            FullArray[Index] = VariableValue;

            SetVariableArrayUInt8(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableInt16(string VariableName, short VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetInt16(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayInt16(string VariableName, List<short> VariableValues)
        {
            List<short> ReturnValue = new List<short>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt16(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayInt16(string VariableName, int Index, short VariableValue)
        {
            List<short> FullArray = new List<short>();
            FullArray = await GetVariableArrayInt16(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayInt16(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableUInt16(string VariableName, ushort VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetUInt16(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayUInt16(string VariableName, List<ushort> VariableValues)
        {
            List<ushort> ReturnValue = new List<ushort>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt16(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayUInt16(string VariableName, int Index, ushort VariableValue)
        {
            List<ushort> FullArray = new List<ushort>();
            FullArray = await GetVariableArrayUInt16(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayUInt16(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableInt32(string VariableName, int VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetInt32(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayInt32(string VariableName, List<int> VariableValues)
        {
            List<int> ReturnValue = new List<int>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt32(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayInt32(string VariableName, int Index, int VariableValue)
        {
            List<int> FullArray = new List<int>();
            FullArray = await GetVariableArrayInt32(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayInt32(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableUInt32(string VariableName, uint VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetUInt32(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayUInt32(string VariableName, List<uint> VariableValues)
        {
            List<uint> ReturnValue = new List<uint>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt32(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayUInt32(string VariableName, int Index, uint VariableValue)
        {
            List<uint> FullArray = new List<uint>();
            FullArray = await GetVariableArrayUInt32(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayUInt32(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableInt64(string VariableName, long VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetInt64(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayInt64(string VariableName, List<long> VariableValues)
        {
            List<long> ReturnValue = new List<long>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetInt64(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayInt64(string VariableName, int Index, long VariableValue)
        {
            List<long> FullArray = new List<long>();
            FullArray = await GetVariableArrayInt64(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayInt64(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableUInt64(string VariableName, ulong VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetUInt64(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayUInt64(string VariableName, List<ulong> VariableValues)
        {
            List<ulong> ReturnValue = new List<ulong>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetUInt64(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayUInt64(string VariableName, int Index, ulong VariableValue)
        {
            List<ulong> FullArray = new List<ulong>();
            FullArray = await GetVariableArrayUInt64(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayUInt64(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableReal(string VariableName, float VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetFloat32(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetVariableArrayReal(string VariableName, List<float> VariableValues)
        {
            List<float> ReturnValue = new List<float>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetFloat32(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayReal(string VariableName, int Index, float VariableValue)
        {
            List<float> FullArray = new List<float>();
            FullArray = await GetVariableArrayReal(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayReal(VariableName, FullArray);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SetVariableLReal(string VariableName, double VariableValue)
        {
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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
                    await CurrentTag.ReadAsync();

                    CurrentTag.SetFloat64(0, VariableValue);

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }
        public async Task SetVariableArrayLReal(string VariableName, List<double> VariableValues)
        {
            List<double> ReturnValue = new List<double>();
            int Count;
            int Count_Retry;
            Tag CurrentTag = null;
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

                    await CurrentTag.ReadAsync();

                    for (Count = 0; Count < VariableValues.Count; Count++)
                    {
                        CurrentTag.SetFloat64(Count * (int)CurrentTag.ElementSize, VariableValues[Count]);
                    }

                    await CurrentTag.WriteAsync();
                }
                catch (Exception ex)
                {
                    if (Count_Retry == Max_Retyr_nmb - 1)
                    {
                        throw new Exception($"Fallo al leer {VariableName}: {ex.Message}");
                    }

                    await Task.Delay(Retry_Delay);
                }
            }
        }

        public async Task SetIndexArrayLReal(string VariableName, int Index, double VariableValue)
        {
            List<double> FullArray = new List<double>();
            FullArray = await GetVariableArrayLReal(VariableName);

            FullArray[Index] = VariableValue;
            SetVariableArrayLReal(VariableName, FullArray);
        }
    }
}
