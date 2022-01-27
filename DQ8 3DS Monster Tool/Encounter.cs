using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace DQ8_3DS_Monster_Tool
{
    public class EncountFile
    {
        public string Header { get; set; }
        public Dictionary<string, EncountTable> Contents { get; set; }
        public EncountFile Clone ()
        {
            EncountFile copy = new EncountFile();

            copy.Header = Header;
            copy.Contents = new Dictionary<string, EncountTable>();

            foreach (KeyValuePair<string, EncountTable> kv in Contents)
            {
                copy.Contents.Add(kv.Key, kv.Value.Clone());
            }

            return copy;
        }

        public static EncountFile Read(EncountFile base_file, string path)
        {
            EncountFile result = base_file.Clone();

            byte[] bytes = File.ReadAllBytes(path);

            string loaded_header = AdditionalMethods.BytesToString(new ArraySegment<byte>(bytes, 0, 16).ToArray());
            bytes = new ArraySegment<byte>(bytes, 16, bytes.Length - 16).ToArray();

            if (loaded_header == result.Header && bytes.Length % 96 == 0)
            {
                //try
                //{
                List<byte[]> byte_arrays = new List<byte[]>();

                while (bytes.Length > 0)
                {
                    byte_arrays.Add(new ArraySegment<byte>(bytes, 0, 96).ToArray());
                    bytes = new ArraySegment<byte>(bytes, 96, bytes.Length - 96).ToArray().ToArray();
                }

                foreach (EncountTable table in byte_arrays.Select(b => EncountTable.FromBytes(b, result)))
                {
                    if (result.Contents.ContainsKey(table.ID))
                        result.Contents[table.ID] = table;
                    else
                        result.Contents.Add(table.ID, table);
                }

                return result;
                //} catch
                //{
                //    return null;
                //}

                return null;
            }
            else
            {
                return null;
            }

            return null;
        }
        public void SaveToFile(string path)
        {
            if (this != null)
            {
                string byteString = this.Header;
                foreach (EncountTable table in this.Contents.Values)
                {
                    string id = table.ID;
                    id = AdditionalMethods.ReverseHex(id);
                    byteString += id + table.Header;
                    foreach (EncountTableEntry entry in table.Contents)
                    {
                        id = entry.ID;
                        id = AdditionalMethods.ReverseHex(id);
                        byteString += entry.Arg1 + entry.Arg2 + id + entry.Footer;
                    }
                    foreach (EncountTableSetEntry entry in table.SetEncounters)
                    {
                        byteString += entry.Weight + entry.ID;
                    }
                }
                AdditionalMethods.writeHexStringToFile(byteString, path);
            }
        }
    }
    [Serializable]
    public class EncountTable
    {
        public string ID { get; set; } = "0000";
        public string Image { get; set; } = "0000";
        public string Region { get; set; } = "none";
        public string Area { get; set; } = "none";
        public string Header { get; set; } = "00000000000000000000";
        public EncountTableEntry[] Contents { get; set; } = new EncountTableEntry[10];
        public EncountTableSetEntry[] SetEncounters { get; set; } = new EncountTableSetEntry[2];
        public EncountTable Clone()
        {
            EncountTable copy = new EncountTable();

            copy.ID = ID;
            copy.Region = Region;
            copy.Area = Area;
            copy.Header = Header;
            copy.Image = Image;

            for (int i = 0; i < 10; i++)
            {
                copy.Contents[i] = Contents[i].Clone();
            }
            for (int i = 0; i < 2; i++)
            {
                copy.SetEncounters[i] = SetEncounters[i].Clone();
            }

            return copy;
        }
        public void populate()
        {
            for (int i = 0; i < Contents.Length; i++)
            {
                if (Contents[i] == null)
                    Contents[i] = new EncountTableEntry();
            }
            for (int i = 0; i < SetEncounters.Length; i++)
            {
                if (SetEncounters[i] == null)
                    SetEncounters[i] = new EncountTableSetEntry();
            }
        }
        public static EncountTable FromBytes(byte[] b, EncountFile base_file)
        {
            EncountTable encountTable;

            string new_id = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 0, 2).ToArray().Reverse().ToArray());

            if (base_file != null && base_file.Contents.ContainsKey(new_id))
                encountTable = base_file.Contents[new_id];
            else
                encountTable = new EncountTable();

            encountTable.populate();

            encountTable.ID = new_id;

            encountTable.Header = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 2, 10).ToArray());

            for (int i = 0; i < encountTable.Contents.Length; i++)
            {
                EncountTableEntry entry = encountTable.Contents[i];

                entry.Arg1   = AdditionalMethods.BytesToString(new byte[] { b[12 + (i * 8)] });
                entry.Arg2   = AdditionalMethods.BytesToString(new byte[] { b[13 + (i * 8)] });
                entry.ID     = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 14 + (i * 8), 2).ToArray().Reverse().ToArray());
                entry.Footer = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 16 + (i * 8), 4).ToArray());
            }

            for (int i = 0; i < encountTable.SetEncounters.Length; i++)
            {
                EncountTableSetEntry entry = encountTable.SetEncounters[i];

                entry.Weight = AdditionalMethods.BytesToString(new byte[] { b[92 + (i * 2)] });
                entry.ID     = AdditionalMethods.BytesToString(new byte[] { b[93 + (i * 2)] });
            }

            return encountTable;
        }

        public override string ToString()
        {
            if (Area != "" && Area != null && Area != "." && Area != "none")
                return Region + " (" + Area + ")";
            else
                return Region;
        }
    }
    [Serializable]
    public class EncountTableEntry
    {
        public string Arg1 { get; set; } = "00";
        public string Arg2 { get; set; } = "00";
        public string ID { get; set; } = "0000";
        public string Footer { get; set; } = "00000000";
        public EncountTableEntry Clone()
        {
            EncountTableEntry copy = new EncountTableEntry();

            copy.Arg1 = Arg1;
            copy.Arg2 = Arg2;
            copy.ID = ID;
            copy.Footer = Footer;

            return copy;
        }
    }
    [Serializable]
    public class EncountTableSetEntry
    {
        public string Weight { get; set; } = "00";
        public string ID { get; set; } = "86";
        public EncountTableSetEntry Clone()
        {
            EncountTableSetEntry copy = new EncountTableSetEntry();

            copy.ID = ID;
            copy.Weight = Weight;

            return copy;
        }
    }

}
