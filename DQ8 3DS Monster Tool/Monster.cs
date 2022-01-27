using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DQ8_3DS_Monster_Tool
{
    public class MonsterFile
    {
        public string Header { get; set; } = "E8030000000000000000000000000000";
        public Dictionary<string, Monster> Contents { get; set; }

        public MonsterFile Clone()
        {
            MonsterFile newFile = new MonsterFile();
            newFile.Header = Header;
            newFile.Contents = new Dictionary<string, Monster>();
            foreach (KeyValuePair<string, Monster> kv in Contents)
            {
                Monster add = kv.Value.Clone();
                newFile.Contents.Add(kv.Key, add);
            }
            return newFile;
        }

        public static MonsterFile Read(MonsterFile base_file, string path)
        {
            MonsterFile result = base_file.Clone();

            byte[] bytes = File.ReadAllBytes(path);

            string loaded_header = AdditionalMethods.BytesToString(new ArraySegment<byte>(bytes, 0, 16).ToArray());
            bytes = new ArraySegment<byte>(bytes, 16, bytes.Length - 16).ToArray();

            if (loaded_header == result.Header && bytes.Length % 224 == 0)
            {
                try
                {
                    List<byte[]> byte_arrays = new List<byte[]>();

                    while (bytes.Length > 0)
                    {
                        byte_arrays.Add(new ArraySegment<byte>(bytes, 0, 224).ToArray());
                        bytes = new ArraySegment<byte>(bytes, 224, bytes.Length - 224).ToArray().ToArray();
                    }

                    foreach (Monster monster in byte_arrays.Select(b => Monster.FromBytes(b, result)))
                    {
                        if (result.Contents.ContainsKey(monster.ID))
                            result.Contents[monster.ID] = monster;
                        else
                            result.Contents.Add(monster.ID, monster);
                    }

                    return result;
                } catch
                {
                    return null;
                }

                return null;
            } else
            {
                return null;
            }

            return null;
        }
        public void SaveToFile(string path)
        {
            if (this != null)
            {
                string byteString = Header;
                foreach (Monster mon in Contents.Values)
                {
                    var addString = "";
                    addString += AdditionalMethods.ReverseHex(mon.ID)
                        + mon.Unk1
                        + AdditionalMethods.ReverseHex(mon.HP.ToString("X4")) + AdditionalMethods.ReverseHex(mon.MP.ToString("X4")) + AdditionalMethods.ReverseHex(mon.Attack.ToString("X4"))
                        + AdditionalMethods.ReverseHex(mon.Defence.ToString("X4")) + AdditionalMethods.ReverseHex(mon.Agility.ToString("X4")) + AdditionalMethods.ReverseHex(mon.Wisdom.ToString("X4"))
                        + AdditionalMethods.ReverseHex(mon.Experience.ToString("X8")) + AdditionalMethods.ReverseHex(mon.Gold.ToString("X8"));
                    foreach (string itemid in mon.Items)
                        addString += AdditionalMethods.ReverseHex(itemid);
                    addString += mon.Unk2 + string.Join("", mon.Resistances) + mon.Unk3;
                    foreach (string actid in mon.Actions)
                        addString += AdditionalMethods.ReverseHex(actid);
                    addString += mon.Unk4 + mon.Unk5 + mon.Unk6 + mon.Footer;

                    byteString += addString;
                }

                // now we convert byteString to an array of bytes
                AdditionalMethods.writeHexStringToFile(byteString, path);
            }
        }
    }
    public class Monster
    {
        public string ID { get; set; } = "0000";
        public string Name { get; set; } = "none";
        public string Unk1 { get; set; } = "000000000000";
        public int HP { get; set; } = 0;
        public int MP { get; set; } = 0;
        public int Attack { get; set; } = 0;
        public int Defence { get; set; } = 0;
        public int Agility { get; set; } = 0;
        public int Wisdom { get; set; } = 0;
        public int Experience { get; set; } = 0;
        public int Gold { get; set; } = 0;
        public string[] Items { get; set; } = new string[] { "0000", "0000" };
        public string Unk2 { get; set; } = "000000";
        public string[] Resistances { get; set; } = new string[] { "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00", "00" };
        public string Unk3 { get; set; } = "0000000000";
        public string[] Actions { get; set; } = new string[] { "0000", "0000", "0000", "0000", "0000", "0000" };
        public string Unk4 { get; set; } = "000000000000000000000000000000000000";
        public string Unk5 { get; set; } = "0000000000000000000000000000000000000000000000000000000000000000";
        public string Unk6 { get; set; } = "000000000000000000000000000000000000000000000000000000000000000000000000";
        public string Footer { get; set; } = "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
        public string Image { get; set; } = "0000";

        public Monster Clone()
        {
            Monster newMonster = new Monster();
            newMonster.Agility = Agility;
            newMonster.Attack = Attack;
            newMonster.Defence = Defence;
            newMonster.Wisdom = Wisdom;
            newMonster.Experience = Experience;
            newMonster.Footer = Footer;
            newMonster.Gold = Gold;
            newMonster.HP = HP;
            newMonster.ID = ID;
            newMonster.MP = MP;
            newMonster.Name = Name;
            newMonster.Unk1 = Unk1;
            newMonster.Unk2 = Unk2;
            newMonster.Unk3 = Unk3;
            newMonster.Unk4 = Unk4;
            newMonster.Unk5 = Unk5;
            newMonster.Unk6 = Unk6;
            newMonster.Image = Image;
            newMonster.Actions = new string[6];
            for (int i = 0; i < newMonster.Actions.Length; i++)
            {
                newMonster.Actions[i] = Actions[i];
            }
            newMonster.Items = new string[2];
            for (int i = 0; i < newMonster.Items.Length; i++)
            {
                newMonster.Items[i] = Items[i];
            }
            newMonster.Resistances = new string[22];
            for (int i = 0; i < newMonster.Resistances.Length; i++)
            {
                newMonster.Resistances[i] = Resistances[i];
            }

            return newMonster;
        }

        public static Monster FromBytes(byte[] b, MonsterFile base_file = null)
        {
            Monster monster;

            string new_id = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 0, 2).ToArray().Reverse().ToArray());

            if (base_file != null && base_file.Contents.ContainsKey(new_id))
                monster = base_file.Contents[new_id];
            else
                monster = new Monster();

            monster.ID = new_id;
            monster.Unk1       = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 2 , 6).ToArray());
            monster.HP         = AdditionalMethods.BytesToUShort (new ArraySegment<byte>(b, 8 , 2).ToArray());
            monster.MP         = AdditionalMethods.BytesToUShort (new ArraySegment<byte>(b, 10, 2).ToArray());
            monster.Attack     = AdditionalMethods.BytesToUShort (new ArraySegment<byte>(b, 12, 2).ToArray());
            monster.Defence    = AdditionalMethods.BytesToUShort (new ArraySegment<byte>(b, 14, 2).ToArray());
            monster.Agility    = AdditionalMethods.BytesToUShort (new ArraySegment<byte>(b, 16, 2).ToArray());
            monster.Wisdom     = AdditionalMethods.BytesToUShort (new ArraySegment<byte>(b, 18, 2).ToArray());
            monster.Experience = AdditionalMethods.BytesToInt   (new ArraySegment<byte>(b, 20, 4).ToArray());
            monster.Gold       = AdditionalMethods.BytesToInt   (new ArraySegment<byte>(b, 24, 4).ToArray());
            
            for (int i = 0; i < 2; i++)
            {
                monster.Items[i] = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 28 + (i * 2) , 2).ToArray().Reverse().ToArray());
            }

            monster.Unk3 = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 32, 3).ToArray());

            for (int i = 0; i < 22; i++)
            {
                monster.Resistances[i] = AdditionalMethods.BytesToString(new byte[] { b[35 + i] });
            }

            monster.Unk3 = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 57, 5).ToArray());

            for (int i = 0; i < 6; i++)
            {
                monster.Actions[i] = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 62 + (i * 2), 2).ToArray().Reverse().ToArray());
            }

            monster.Unk4   = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 74 , 18).ToArray());
            monster.Unk5   = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 92 , 32).ToArray());
            monster.Unk6   = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 124, 36).ToArray());
            monster.Footer = AdditionalMethods.BytesToString(new ArraySegment<byte>(b, 160, 64).ToArray());

            return monster;
        }
    }

    public class ComboBoxValueResource
    {
        public string ID { get; set; } = "0000";
        public string Content { get; set; } = "";
    }
}
