using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

using System.IO;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DQ8_3DS_Monster_Tool
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            suppressDataUpdate = true;
            InitializeComponent();
            doToolTips();
            suppressDataUpdate = false;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                tryOpenFile(args[1]);
            }
        }

        // variables
        private Monster selectedMonster;
        private object selectedItem;

        private EncountTable selectedTable;

        private MonsterFile vanillaMonsterFile;
        private MonsterFile monsterFile;

        private EncountFile vanillaEncountFile;
        private EncountFile encountFile;

        private string sourceFilePath;
        private bool pendingChanges;
        private bool suppressDataUpdate;

        public Dictionary<string, ComboBoxValueResource> Items;
        public Dictionary<string, ComboBoxValueResource> Actions;

        public Dictionary<string, ComboBoxValueResource> Encounters;
        public Dictionary<string, ComboBoxValueResource> SetEncounters;

        private Dictionary<ComboBox, object> LastSelectedItems = new Dictionary<ComboBox, object>();

        // -- actions

        // generic actions
        private void MainWindow_DragDrop(object sender, DragEventArgs e)
        {
            Console.WriteLine("MainWindow_DragDrop");

            var fileName = e.Data.GetData("FileDrop", true) as string[];
            tryOpenFile(fileName[0]);
        }
        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop"))
                e.Effect = DragDropEffects.Copy;
        }
        private void comboBox_Validate(object sender, EventArgs e, string default_label = "nothing", int hex_length = 4)
        {
            ComboBox comboBox = (ComboBox)sender;

            if (comboBox.SelectedItem == null && ((comboBox.Text.Length < hex_length && AdditionalMethods.isHex(comboBox.Text)) || (comboBox.Text.Length >= hex_length && AdditionalMethods.isHex(comboBox.Text.Substring(0, hex_length)))))
            {
                string newHex = comboBox.Text.ToUpper();
                if (newHex.Length > hex_length)
                    newHex = newHex.Substring(0, hex_length);
                else
                    newHex = AdditionalMethods.addLeadingZeroes(newHex, hex_length);

                List<object> objectList = AdditionalMethods.getComboBoxObjectList(comboBox);
                object newItem = objectList.Find(obj => obj.ToString().ToUpper().StartsWith(newHex));

                if (newItem != null)
                {
                    comboBox.SelectedItem = newItem;
                }
                else
                {
                    comboBox.Text = newHex + " - " + default_label;
                }

                comboBox_setLastSelectedItem(sender, e);
            }
            else
            {
                if (LastSelectedItems.ContainsKey(comboBox))
                    comboBox.SelectedItem = LastSelectedItems[comboBox];
            }
        }
        private void comboBox_setLastSelectedItem(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;

            if (comboBox != null && comboBox.SelectedItem != null)
            {
                if (LastSelectedItems.ContainsKey(comboBox))
                {
                    LastSelectedItems[comboBox] = comboBox.SelectedItem;
                }
                else
                {
                    LastSelectedItems.Add(comboBox, comboBox.SelectedItem);
                }
            }
        }

        // toolstrip actions
        private void monstertblToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tryCloseEditor())
                initMonsterPanel();
        }
        private void encounttblToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tryCloseEditor())
                initEncounterPanel();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dragon Quest VIII TBL File (*.tbl)|*.tbl";
            openFileDialog.ShowDialog();

            tryOpenFile(openFileDialog.FileName);
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            trySave();
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            trySaveAs();
        }
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tryCloseEditor();
        }
        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/pIayinful/DQ8-3DS-Monster-Tool/blob/master/README.md");
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Dragon Quest VIII 3DS Monster Tool\nDeveloped by playinful\n\npIayinful on GitHub\n@playinful on Twitter", "About DQ8 3DS Monster Tool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void learnAboutMonstersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/pIayinful/dq8-3ds-tools");
        }

        // monster panel actions
        private void comboBox_monsterID_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateSelectedMonster();
        }
        private void comboBox_monsterID_Validated(object sender, EventArgs e)
        {
            updateSelectedMonster();
        }
        private void comboBox_monsterID_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                updateSelectedMonster();
            }
        }
        private void monster_ValueChanged(object sender, EventArgs e)
        {
            if (selectedMonster != null && !suppressDataUpdate)
            {
                if (sender == numericUpDown_experience)
                    selectedMonster.Experience = (int)numericUpDown_experience.Value;
                if (sender == numericUpDown_gold)
                    selectedMonster.Gold = (int)numericUpDown_gold.Value;
                if (sender == numericUpDown_HP)
                    selectedMonster.HP = (int)numericUpDown_HP.Value;
                if (sender == numericUpDown_MP)
                    selectedMonster.MP = (int)numericUpDown_MP.Value;
                if (sender == numericUpDown_Attack)
                    selectedMonster.Attack = (int)numericUpDown_Attack.Value;
                if (sender == numericUpDown_Defence)
                    selectedMonster.Defence = (int)numericUpDown_Defence.Value;
                if (sender == numericUpDown_Agility)
                    selectedMonster.Agility = (int)numericUpDown_Agility.Value;
                if (sender == numericUpDown_Wisdom)
                    selectedMonster.Wisdom = (int)numericUpDown_Wisdom.Value;

                if (new ComboBox[] { comboBox_item1, comboBox_item2 }.Contains(sender))
                {
                    //MessageBox.Show("Item changed");
                }

                if (new ComboBox[] { comboBox_action1, comboBox_action2, comboBox_action3, comboBox_action4, comboBox_action5, comboBox_action6 }.Contains(sender))
                {
                    //MessageBox.Show("Action changed");
                }

                if (new DomainUpDown[] { domainUpDown_resistance1, domainUpDown_resistance2, domainUpDown_resistance3, domainUpDown_resistance4, domainUpDown_resistance5, domainUpDown_resistance6, domainUpDown_resistance7, domainUpDown_resistance8, domainUpDown_resistance9, domainUpDown_resistance10, domainUpDown_resistance11, domainUpDown_resistance12, domainUpDown_resistance13, domainUpDown_resistance14, domainUpDown_resistance15, domainUpDown_resistance16, domainUpDown_resistance17, domainUpDown_resistance18, domainUpDown_resistance19, domainUpDown_resistance20, domainUpDown_resistance21, domainUpDown_resistance22, domainUpDown_resistance23, domainUpDown_resistance24 }.Contains(sender))
                {
                    //MessageBox.Show("Resistance changed");
                }

                pendingChanges = true;
            }
        }
        private void comboBox_item_ValueChanged(object sender, EventArgs e)
        {
            comboBox_setLastSelectedItem(sender, e);

            if (selectedMonster != null && !suppressDataUpdate)
            {
                comboBox_Validate(sender, e, "nothing");

                ComboBox comboBox = sender as ComboBox;

                if (comboBox != null)
                {
                    int itemIndex = int.Parse(comboBox.Name.Substring(comboBox.Name.Length - 1)) - 1;

                    if (comboBox.Text.Length >= 4 && AdditionalMethods.isHex(comboBox.Text.Substring(0, 4)))
                    {
                        selectedMonster.Items[itemIndex] = comboBox.Text.Substring(0, 4).ToUpper();
                    }
                }
            }
            //MessageBox.Show("Item changed");
        }
        private void comboBox_item_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                comboBox_item_ValueChanged(sender, e);
            }
        }
        private void comboBox_action_ValueChanged(object sender, EventArgs e)
        {
            comboBox_setLastSelectedItem(sender, e);

            if (selectedMonster != null && !suppressDataUpdate)
            {
                comboBox_Validate(sender, e, "N/A");

                ComboBox comboBox = sender as ComboBox;

                if (comboBox != null)
                {
                    int itemIndex = int.Parse(comboBox.Name.Substring(comboBox.Name.Length - 1)) - 1;

                    if (comboBox.Text.Length >= 4 && AdditionalMethods.isHex(comboBox.Text.Substring(0, 4)))
                    {
                        selectedMonster.Actions[itemIndex] = comboBox.Text.Substring(0, 4).ToUpper();
                    }
                }
            }
            //MessageBox.Show("Action changed");
        }
        private void comboBox_action_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                comboBox_action_ValueChanged(sender, e);
            }
        }
        private void domainUpDown_resistance_ValueChanged(object sender, EventArgs e)
        {
            if (selectedMonster != null && !suppressDataUpdate)
            {
                DomainUpDown domainUpDown = sender as DomainUpDown;

                if (domainUpDown != null)
                {
                    int itemIndex = int.Parse(domainUpDown.Name.Substring(23));

                    selectedMonster.Resistances[itemIndex - 3] = "0" + ((domainUpDown.SelectedIndex - 3) * -1).ToString();
                }
            }
            //MessageBox.Show("Resistance changed");
        }

        // encounter panel actions
        private void comboBox_location_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox_setLastSelectedItem(sender, e);
            comboBox_Validate(sender, e);
            updateSelectedLocation();
        }
        private void comboBox_location_Validated(object sender, EventArgs e)
        {
            comboBox_setLastSelectedItem(sender, e);
            comboBox_Validate(sender, e);
            updateSelectedLocation();
        }
        private void comboBox_location_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                comboBox_setLastSelectedItem(sender, e);
                comboBox_Validate(sender, e);
                updateSelectedLocation();
            }
        }
        private void comboBox_randomEncounter_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox_setLastSelectedItem(sender, e);

            if (selectedTable != null && !suppressDataUpdate)
            {
                comboBox_Validate(sender, e, "unknown");

                ComboBox comboBox = sender as ComboBox;

                if (comboBox != null)
                {
                    int itemIndex = int.Parse(comboBox.Name.Substring(24)) - 1;

                    if (comboBox.Text.Length >= 4 && AdditionalMethods.isHex(comboBox.Text.Substring(0, 4)))
                    {
                        selectedTable.Contents[itemIndex].ID = comboBox.Text.Substring(0, 4).ToUpper();
                    }
                }
            }
        }
        private void comboBox_randomEncounter_Validated(object sender, EventArgs e)
        {
            comboBox_randomEncounter_SelectedIndexChanged(sender, e);
        }
        private void comboBox_randomEncounter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                comboBox_randomEncounter_SelectedIndexChanged(sender, e);
            }
        }
        private void comboBox_setEncounter_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox_setLastSelectedItem(sender, e);

            if (selectedTable != null && !suppressDataUpdate)
            {
                comboBox_Validate(sender, e, "unknown", 2);

                ComboBox comboBox = sender as ComboBox;

                if (comboBox != null)
                {
                    int itemIndex = int.Parse(comboBox.Name.Substring(comboBox.Name.Length - 1)) - 1;

                    if (comboBox.Text.Length >= 2 && AdditionalMethods.isHex(comboBox.Text.Substring(0, 2)))
                    {
                        selectedTable.SetEncounters[itemIndex].ID = comboBox.Text.Substring(0, 2).ToUpper();
                    }
                }
            }
        }
        private void comboBox_setEncounter_Validated(object sender, EventArgs e)
        {
            comboBox_setEncounter_SelectedIndexChanged(sender, e);
        }
        private void comboBox_setEncounter_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                comboBox_setEncounter_SelectedIndexChanged(sender, e);
            }
        }
        private void encountTable_ValueChanged(object sender, EventArgs e)
        {
            if (selectedTable != null && !suppressDataUpdate)
            {
                NumericUpDown numericUpDown = sender as NumericUpDown;

                if (numericUpDown.Name.StartsWith("numericUpDown_Unk"))
                {
                    int itemIndex = int.Parse(numericUpDown.Name.Substring(19))-1;

                    if (numericUpDown.Name[17] == '1')
                    {
                        selectedTable.Contents[itemIndex].Arg1 = ((int)numericUpDown.Value).ToString("X2");
                    } else if (numericUpDown.Name[17] == '2')
                    {
                        selectedTable.Contents[itemIndex].Arg2 = ((int)numericUpDown.Value).ToString("X2");
                    }

                } else if (numericUpDown.Name.StartsWith("numericUpDown_setEncounter_Unk"))
                {
                    int itemIndex = int.Parse(numericUpDown.Name.Substring(30))-1;

                    selectedTable.SetEncounters[itemIndex].Weight = ((int)numericUpDown.Value).ToString("X2");
                }
            }
        }

        // -- methods --

        // generic methods
        private void loadJSONAndPopulateComboBoxes()
        {
            if (vanillaMonsterFile == null)
            {
                vanillaMonsterFile = JsonSerializer.Deserialize<MonsterFile>(System.Text.Encoding.Default.GetString(Properties.Resources.Monsters));
                comboBox_monsterID.Items.AddRange(vanillaMonsterFile.Contents.Values.Select(item => item.ID + " - " + item.Name).ToArray());
            }
            if (vanillaEncountFile == null)
            {
                vanillaEncountFile = JsonSerializer.Deserialize<EncountFile>(System.Text.Encoding.Default.GetString(Properties.Resources.EncountTbl));
                comboBox_location.Items.AddRange(vanillaEncountFile.Contents.Values.Select(item => item.ID + " - " + item.ToString()).ToArray());
            }

            if (Items == null)
            {
                Items = JsonSerializer.Deserialize<Dictionary<string, ComboBoxValueResource>>(System.Text.Encoding.Default.GetString(Properties.Resources.Items));
                object[] items = Items.Values.Select(item => item.ID + " - " + item.Content).ToArray();
                foreach (ComboBox comboBox in new ComboBox[] { comboBox_item1, comboBox_item2 })
                {
                    comboBox.Items.AddRange(items);
                }
            }
            if (Actions == null)
            {
                Actions = JsonSerializer.Deserialize<Dictionary<string, ComboBoxValueResource>>(System.Text.Encoding.Default.GetString(Properties.Resources.Actions));
                object[] items = Actions.Values.Select(item => item.ID + " - " + item.Content).ToArray();
                foreach (ComboBox comboBox in new ComboBox[] { comboBox_action1, comboBox_action2, comboBox_action3, comboBox_action4, comboBox_action5, comboBox_action6, })
                {
                    comboBox.Items.AddRange(items);
                }
            }
            if (Encounters == null)
            {
                Encounters = JsonSerializer.Deserialize<Dictionary<string, ComboBoxValueResource>>(System.Text.Encoding.Default.GetString(Properties.Resources.Encounters));
                object[] items = Encounters.Values.Select(item => item.ID + " - " + item.Content).ToArray();
                foreach (ComboBox comboBox in new ComboBox[] { comboBox_randomEncounter1, comboBox_randomEncounter2, comboBox_randomEncounter3, comboBox_randomEncounter4, comboBox_randomEncounter5, comboBox_randomEncounter6, comboBox_randomEncounter7, comboBox_randomEncounter8, comboBox_randomEncounter9, comboBox_randomEncounter10 })
                {
                    comboBox.Items.AddRange(items);
                }
            }
            if (SetEncounters == null)
            {
                SetEncounters = JsonSerializer.Deserialize<Dictionary<string, ComboBoxValueResource>>(System.Text.Encoding.Default.GetString(Properties.Resources.SetEncounters));
                object[] items = SetEncounters.Values.Select(item => item.ID + " - " + item.Content).ToArray();
                foreach (ComboBox comboBox in new ComboBox[] { comboBox_setEncounter1, comboBox_setEncounter2 })
                {
                    comboBox.Items.AddRange(items);
                }
            }
        }
        private bool trySave()
        {
            if (panel_monsterEditor.Visible)
                return trySaveMonsterFile();
            else if (panel_encountEditor.Visible)
                return trySaveEncountFile();
            else
                return false;
        }
        private bool trySaveAs()
        {
            if (panel_monsterEditor.Visible)
                return trySaveMonsterFileAs();
            else if (panel_encountEditor.Visible)
                return trySaveEncountFileAs();
            else
                return false;
        }
        private bool trySaveMonsterFile(bool saveAs = false)
        {
            if (saveAs || sourceFilePath == "" || sourceFilePath == null)
            {
                return trySaveMonsterFileAs();
            }
            else
            {
                return saveMonsterFile(sourceFilePath);
            }
        }
        private bool trySaveMonsterFileAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".tbl";
            saveFileDialog.Filter = "Dragon Quest VIII TBL File (*.tbl)|*.tbl";
            saveFileDialog.ShowDialog();

            return saveMonsterFile(saveFileDialog.FileName);
        }
        private bool trySaveEncountFile(bool saveAs = false)
        {
            if (saveAs || sourceFilePath == "" || sourceFilePath == null)
            {
                return trySaveEncountFileAs();
            }
            else
            {
                return saveEncountFile(sourceFilePath);
            }
            return false;
        }
        private bool trySaveEncountFileAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".tbl";
            saveFileDialog.Filter = "Dragon Quest VIII TBL File (*.tbl)|*.tbl";
            saveFileDialog.ShowDialog();

            return saveEncountFile(saveFileDialog.FileName);

            return false;
        }
        private bool saveMonsterFile(string path, bool close_after = false)
        {
            if (path != "" && path != null)
            {
                try
                {
                    if (panel_monsterEditor.Visible && monsterFile != null)
                    {
                        monsterFile.SaveToFile(path);
                        sourceFilePath = path;
                        pendingChanges = false;
                        if (close_after)
                            tryCloseEditor();
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Could not save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                catch
                {
                    MessageBox.Show("Could not save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
                return false;
        }
        private bool saveEncountFile(string path, bool close_after = false)
        {
            if (path != "" && path != null)
            {
                try
                {
                    if (panel_encountEditor.Visible && encountFile != null)
                    {
                        encountFile.SaveToFile(path);
                        sourceFilePath = path;
                        pendingChanges = false;
                        if (close_after)
                            tryCloseEditor();
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Could not save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                catch
                {
                    MessageBox.Show("Could not save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
                return false;
        }
        private bool tryCloseEditor()
        {
            bool succ = false;
            //Close();
            if (pendingChanges)
            {
                DialogResult result = MessageBox.Show("You have unsaved changes. Would you like to save?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                switch (result)
                {
                    case DialogResult.Yes:
                        succ = trySave();
                        pendingChanges = !succ;
                        break;

                    case DialogResult.No:
                        closeEditor();
                        succ = true;
                        pendingChanges = false;
                        break;

                    case DialogResult.Cancel:
                        succ = false;
                        break;
                }
            }
            else
            {
                closeEditor();
                pendingChanges = false;
                succ = true;
            }

            return succ;
        }
        private void closeEditor()
        {
            panel_monsterEditor.Visible = false;
            panel_encountEditor.Visible = false;

            saveToolStripMenuItem.Enabled   = false;
            saveAsToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Enabled  = false;
        }
        private void tryOpenFile(string filename)
        {
            if (filename != "" && filename != null)
            {
                if (File.Exists(filename))
                {
                    //openMonsterFile(filename);
                    loadFile(filename);
                }
                else
                {
                    MessageBox.Show("The file could not be loaded." + filename, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void loadFile(string filename)
        {
            byte[] file_bytes = File.ReadAllBytes(filename);

            if (file_bytes != null)
            {
                string header = AdditionalMethods.BytesToString(new ArraySegment<byte>(file_bytes, 0, 16).ToArray());

                if (header == "E8030000000000000000000000000000")
                {
                    openMonsterFile(filename);
                } else if (header == "19020000313530353237313035303432")
                {
                    openEncounterFile(filename);
                } else
                {
                    MessageBox.Show("The file could not be loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } else
            {
                MessageBox.Show("The file could not be loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void doToolTips()
        {
            // Rate (?)
            StringBuilder sb = new StringBuilder();
            foreach (string str in "Controls spawn rates for the respective monster(s).$$ - The exact relevance of this value is unknown. Natural$values are 1 to 11; anything higher than this may have$unpredictable results.$ - When  set to 0, the monster(s) will not spawn. Always$set to 0 for empty spawns.".Split('$'))
                sb.AppendLine(str);

            foreach (Control ctrl in new Control[] { label_spawnRate1, label_spawnRate2, label_spawnRate3, label_spawnRate4,
                numericUpDown_Unk1_1, numericUpDown_Unk1_2, numericUpDown_Unk1_3, numericUpDown_Unk1_4, numericUpDown_Unk1_5, numericUpDown_Unk1_6, numericUpDown_Unk1_7, numericUpDown_Unk1_8, numericUpDown_Unk1_9, numericUpDown_Unk1_10,
                numericUpDown_Unk2_1, numericUpDown_Unk2_2, numericUpDown_Unk2_3, numericUpDown_Unk2_4, numericUpDown_Unk2_5, numericUpDown_Unk2_6, numericUpDown_Unk2_7, numericUpDown_Unk2_8, numericUpDown_Unk2_9, numericUpDown_Unk2_10,
                numericUpDown_setEncounter_Unk1, numericUpDown_setEncounter_Unk2 })
                toolTip1.SetToolTip(ctrl, sb.ToString());

            // Wisdom (?)
            sb = new StringBuilder();
            foreach (string str in "Presumably wisdom; possibly unused. By default, this is always equal to Agility.".Split('$'))
                sb.AppendLine(str);

            foreach (Control ctrl in new Control[] { label_wisdom, numericUpDown_Wisdom })
                toolTip1.SetToolTip(ctrl, sb.ToString());
        }

        // monster panel methods
        private void initMonsterPanel()
        {
            suppressDataUpdate = true;

            loadJSONAndPopulateComboBoxes();

            monsterFile = vanillaMonsterFile.Clone();

            if (comboBox_monsterID.Items.Count > 0)
                comboBox_monsterID.SelectedIndex = 1;
            updateSelectedMonster();

            sourceFilePath = null;

            pendingChanges = false;
            suppressDataUpdate = false;

            panel_monsterEditor.Visible = true;

            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
        }
        private void openMonsterFile(string filename)
        {
            loadJSONAndPopulateComboBoxes();

            MonsterFile newMonsterFile = MonsterFile.Read(vanillaMonsterFile, filename);

            if (newMonsterFile != null)
            {
                if (tryCloseEditor())
                {
                    initMonsterPanel();
                    monsterFile = newMonsterFile;
                    //comboBox_monsterID.SelectedIndex = 1;
                    updateSelectedMonster();
                    sourceFilePath = filename;
                }
            }
            else
            {
                MessageBox.Show("The file could not be loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void updateSelectedMonster()
        {
            if (comboBox_monsterID.SelectedItem != null)
            {
                selectedItem = comboBox_monsterID.SelectedItem;

            } else if ((comboBox_monsterID.Text.Length < 4 && AdditionalMethods.isHex(comboBox_monsterID.Text)) || (comboBox_monsterID.Text.Length >= 4 && AdditionalMethods.isHex(comboBox_monsterID.Text.Substring(0, 4))))
            {
                string newHex = comboBox_monsterID.Text.ToUpper();
                if (newHex.Length > 4)
                    newHex = newHex.Substring(0, 4);
                else
                    newHex = AdditionalMethods.addLeadingZeroes(newHex, 4);

                List<object> objectList = AdditionalMethods.getComboBoxObjectList(comboBox_monsterID);
                object newItem = objectList.Find(obj => obj.ToString().ToUpper().StartsWith(newHex));

                if (newItem != null)
                {
                    comboBox_monsterID.SelectedItem = newItem;
                } else
                {
                    comboBox_monsterID.Text = newHex + " - none";
                    if (!monsterFile.Contents.ContainsKey(newHex))
                        monsterFile.Contents.Add(newHex, new Monster());
                }
            } else
            {
                comboBox_monsterID.SelectedItem = selectedItem;
            }

            if (comboBox_monsterID.Text.Length >= 4 && monsterFile.Contents.ContainsKey(comboBox_monsterID.Text.Substring(0,4)))
            {
                selectedMonster = monsterFile.Contents[comboBox_monsterID.Text.Substring(0, 4)];
            }

            updateMonsterData();
        }
        private void updateMonsterData()
        {
            suppressDataUpdate = true;

            object resource = DQ8_3DS_Monster_Tool.Properties.Resources.ResourceManager.GetObject("monster_" + selectedMonster.Image);
            if (resource != null)
            {
                pictureBox_monsterDisplay.Image = (Image)resource;
            } else
            {
                pictureBox_monsterDisplay.Image = (Image)DQ8_3DS_Monster_Tool.Properties.Resources.ResourceManager.GetObject("monster_0000");
            }

            label_monsterID.Text = selectedMonster.ID;
            label_monsterName.Text = selectedMonster.Name;

            numericUpDown_experience.Value = selectedMonster.Experience;
            numericUpDown_gold.Value = selectedMonster.Gold;
            numericUpDown_HP.Value = selectedMonster.HP;
            numericUpDown_MP.Value = selectedMonster.MP;
            numericUpDown_Attack.Value = selectedMonster.Attack;
            numericUpDown_Defence.Value = selectedMonster.Defence;
            numericUpDown_Agility.Value = selectedMonster.Agility;
            numericUpDown_Wisdom.Value = selectedMonster.Wisdom;

            int i = 0;
            foreach (ComboBox comboBox in new ComboBox[] { comboBox_item1, comboBox_item2 })
            {
                List<object> itemList = AdditionalMethods.getComboBoxObjectList(comboBox);

                object newItem = itemList.Find(obj => obj.ToString().StartsWith(selectedMonster.Items[i]));

                if (newItem != null)
                {
                    comboBox.SelectedItem = newItem;
                } else
                {
                    comboBox.Text = selectedMonster.Items[i] + " - nothing";
                }

                i++;
            }

            i = 0;
            foreach (ComboBox comboBox in new ComboBox[] { comboBox_action1, comboBox_action2, comboBox_action3, comboBox_action4, comboBox_action5, comboBox_action6 })
            {
                List<object> itemList = AdditionalMethods.getComboBoxObjectList(comboBox);

                object newItem = itemList.Find(obj => obj.ToString().StartsWith(selectedMonster.Actions[i]));

                if (newItem != null)
                {
                    comboBox.SelectedItem = newItem;
                }
                else
                {
                    comboBox.Text = selectedMonster.Items[i] + " - N/A";
                }

                i++;
            }

            i = 0;
            foreach (DomainUpDown domainUpDown in new DomainUpDown[] { /*domainUpDown_resistance1, domainUpDown_resistance2,*/ domainUpDown_resistance3, domainUpDown_resistance4, domainUpDown_resistance5, domainUpDown_resistance6, domainUpDown_resistance7, domainUpDown_resistance8, domainUpDown_resistance9, domainUpDown_resistance10, domainUpDown_resistance11, domainUpDown_resistance12, domainUpDown_resistance13, domainUpDown_resistance14, domainUpDown_resistance15, domainUpDown_resistance16, domainUpDown_resistance17, domainUpDown_resistance18, domainUpDown_resistance19, domainUpDown_resistance20, domainUpDown_resistance21, domainUpDown_resistance22, domainUpDown_resistance23, domainUpDown_resistance24 })
            {
                domainUpDown.SelectedIndex = (int.Parse(selectedMonster.Resistances[i]) - 3) * -1;

                i++;
            }

            suppressDataUpdate = false;
        }

        // encounter panel methods
        private void initEncounterPanel()
        {
            suppressDataUpdate = true;

            loadJSONAndPopulateComboBoxes();

            encountFile = vanillaEncountFile.Clone();

            if (comboBox_location.Items.Count > 0)
                comboBox_location.SelectedIndex = 0;
            updateSelectedLocation();

            sourceFilePath = null;

            pendingChanges = false;
            suppressDataUpdate = false;

            panel_encountEditor.Visible     = true;

            saveToolStripMenuItem.Enabled   = true;
            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled  = true;
        }
        private void openEncounterFile(string filename)
        {
            loadJSONAndPopulateComboBoxes();

            EncountFile newEncountFile = EncountFile.Read(vanillaEncountFile, filename);

            if (newEncountFile != null)
            {
                if (tryCloseEditor())
                {
                    initEncounterPanel();
                    encountFile = newEncountFile;
                    //comboBox_location.SelectedIndex = 0;
                    updateSelectedLocation();
                    sourceFilePath = filename;
                }
            }
            else
            {
                MessageBox.Show("The file could not be loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void updateSelectedLocation()
        {
            comboBox_Validate(comboBox_location, null, "none");

            if (comboBox_location.Text.Length >= 4 && encountFile.Contents.ContainsKey(comboBox_location.Text.Substring(0, 4)))
            {
                selectedTable = encountFile.Contents[comboBox_location.Text.Substring(0, 4)];
            } else if (comboBox_location.Text.Length >= 4 && AdditionalMethods.isHex(comboBox_location.Text.Substring(0, 4)))
            {
                selectedTable = new EncountTable();
                selectedTable.populate();
                encountFile.Contents.Add(comboBox_location.Text.Substring(0, 4), selectedTable);
            }

            updateEncounterData();
        }
        private void updateEncounterData()
        {
            suppressDataUpdate = true;

            object resource = DQ8_3DS_Monster_Tool.Properties.Resources.ResourceManager.GetObject("map_" + selectedTable.Image);
            if (resource != null)
            {
                pictureBox_mapDisplay.Image = (Image)resource;
            }
            else
            {
                pictureBox_mapDisplay.Image = (Image)DQ8_3DS_Monster_Tool.Properties.Resources.ResourceManager.GetObject("map_0000");
            }

            int i = 0;
            foreach (ComboBox comboBox in new ComboBox[] { comboBox_randomEncounter1, comboBox_randomEncounter2, comboBox_randomEncounter3, comboBox_randomEncounter4, comboBox_randomEncounter5, comboBox_randomEncounter6, comboBox_randomEncounter7, comboBox_randomEncounter8, comboBox_randomEncounter9, comboBox_randomEncounter10 })
            {
                List<object> itemList = AdditionalMethods.getComboBoxObjectList(comboBox);

                object newItem = itemList.Find(obj => obj.ToString().StartsWith(selectedTable.Contents[i].ID));

                if (newItem != null)
                {
                    comboBox.SelectedItem = newItem;
                }
                else
                {
                    comboBox.Text = selectedTable.Contents[i].ID + " - unknown";
                }

                i++;
            }
            i = 0;
            foreach (NumericUpDown numericUpDown in new NumericUpDown[] { numericUpDown_Unk1_1, numericUpDown_Unk1_2, numericUpDown_Unk1_3, numericUpDown_Unk1_4, numericUpDown_Unk1_5, numericUpDown_Unk1_6, numericUpDown_Unk1_7, numericUpDown_Unk1_8, numericUpDown_Unk1_9, numericUpDown_Unk1_10 })
            {
                numericUpDown.Value = int.Parse(selectedTable.Contents[i].Arg1, System.Globalization.NumberStyles.HexNumber);

                i++;
            }
            i = 0;
            foreach (NumericUpDown numericUpDown in new NumericUpDown[] { numericUpDown_Unk2_1, numericUpDown_Unk2_2, numericUpDown_Unk2_3, numericUpDown_Unk2_4, numericUpDown_Unk2_5, numericUpDown_Unk2_6, numericUpDown_Unk2_7, numericUpDown_Unk2_8, numericUpDown_Unk2_9, numericUpDown_Unk2_10 })
            {
                numericUpDown.Value = int.Parse(selectedTable.Contents[i].Arg2, System.Globalization.NumberStyles.HexNumber);

                i++;
            }

            i = 0;
            foreach (ComboBox comboBox in new ComboBox[] { comboBox_setEncounter1, comboBox_setEncounter2 })
            {
                List<object> itemList = AdditionalMethods.getComboBoxObjectList(comboBox);

                object newItem = itemList.Find(obj => obj.ToString().StartsWith(selectedTable.SetEncounters[i].ID));

                if (newItem != null)
                {
                    comboBox.SelectedItem = newItem;
                }
                else
                {
                    comboBox.Text = selectedMonster.Items[i] + " - unknown";
                }

                i++;
            }
            i = 0;
            foreach (NumericUpDown numericUpDown in new NumericUpDown[] { numericUpDown_setEncounter_Unk1, numericUpDown_setEncounter_Unk2 })
            {
                numericUpDown.Value = int.Parse(selectedTable.SetEncounters[i].Weight, System.Globalization.NumberStyles.HexNumber);

                i++;
            }

            suppressDataUpdate = false;
        }

    }

    public static class AdditionalMethods
    {
        public static string BytesToString(byte[] bytes)
        {
            return string.Join("", new List<string>(bytes.Select(b => {
                string s = Convert.ToString(b, 16);
                while (s.Length < 2)
                    s = "0" + s;

                return s.ToUpper();
            })));
        }
        public static short BytesToShort(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();

            return BitConverter.ToInt16(bytes, 0);
        }
        public static int BytesToInt(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();

            return BitConverter.ToInt32(bytes, 0);
        }
        public static ushort BytesToUShort(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();

            return BitConverter.ToUInt16(bytes, 0);
        }
        public static uint BytesToUInt(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();

            return BitConverter.ToUInt32(bytes, 0);
        }
        public static string ReverseHex(string hex)
        {
            if (hex == null)
                return null;
            if (hex.Length % 2 != 0)
                hex = "0" + hex;
            string reversedHex = "";
            for (int i = hex.Length - 2; i >= 0; i -= 2)
            {
                reversedHex += hex.Substring(i, 2);
            }
            return reversedHex;
        }
        public static bool isHex(string hexStr)
        {
            return hexStr.All(a => "abcdefABCDEF0123456789".Contains(a));
        }
        public static List<object> getComboBoxObjectList(ComboBox comboBox)
        {
            List<object> comboBoxObjectList = new List<object>();

            foreach (object item in comboBox.Items)
            {
                comboBoxObjectList.Add(item);
            }

            return comboBoxObjectList;
        }
        public static string addLeadingZeroes(string str, int zeroes)
        {
            while (str.Length < zeroes)
                str = "0" + str;

            return str;
        }
        public static void writeHexStringToFile(string byteString, string path)
        {
            byte[] bytes = new byte[byteString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 0; // just in case
                string hex = byteString.Substring(i * 2, 2);
                int intValue = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                bytes[i] = (byte)intValue;
            }
            File.WriteAllBytes(path, bytes);
        }
    }
}
