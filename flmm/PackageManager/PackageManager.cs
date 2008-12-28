using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace fomm.PackageManager {
    public partial class PackageManager : Form {

        private readonly List<fomod> mods=new List<fomod>();
        private readonly List<string> groups;
        private readonly List<string> lgroups;

        private void AddFomodToList(fomod mod) {
            if(!cbGroups.Checked) {
                ListViewItem lvi=new ListViewItem(new string[] { mod.Name, mod.VersionS, mod.Author });
                lvi.Tag=mod;
                lvi.Checked=mod.IsActive;
                lvModList.Items.Add(lvi);
                return;
            }
            bool added=false;
            for(int i=0;i<groups.Count;i++) {
                if(Array.IndexOf<string>(mod.groups, lgroups[i])!=-1) {
                    added=true;
                    ListViewItem lvi=new ListViewItem(new string[] { mod.Name, mod.VersionS, mod.Author });
                    lvi.Tag=mod;
                    lvi.Checked=mod.IsActive;
                    lvModList.Items.Add(lvi);
                    lvModList.Groups[i+1].Items.Add(lvi);
                }
            }
            if(!added) {
                ListViewItem lvi=new ListViewItem(new string[] { mod.Name, mod.VersionS, mod.Author });
                lvi.Tag=mod;
                lvi.Checked=mod.IsActive;
                lvModList.Items.Add(lvi);
                lvModList.Groups[0].Items.Add(lvi);
            }
        }
        private void RebuildListView() {
            lvModList.SuspendLayout();

            lvModList.Clear();
            lvModList.Groups.Clear();

            if(!cbGroups.Checked) {
                lvModList.ShowGroups=false;
            } else {
                ListViewGroup lvg=new ListViewGroup("No group");
                lvModList.Groups.Add(lvg);

                for(int i=0;i<groups.Count;i++) {
                    lvg=new ListViewGroup(groups[i]);
                    lvModList.Groups.Add(lvg);
                }
                lvModList.ShowGroups=true;
            }

            if(lvModList.Columns.Count==0) {
                lvModList.Columns.Add("Name");
                lvModList.Columns[0].Width=200;
                lvModList.Columns.Add("Version");
                lvModList.Columns.Add("Author");
            }

            foreach(fomod mod in mods) AddFomodToList(mod);

            lvModList.ResumeLayout();
        }
        private void ReaddFomodToList(fomod mod) {
            lvModList.SuspendLayout();
            for(int i=0;i<lvModList.Items.Count;i++) if(lvModList.Items[i].Tag==mod) lvModList.Items.RemoveAt(i--);
            AddFomodToList(mod);
            lvModList.ResumeLayout();
        }

        private void AddFomod(string modpath) {
            fomod mod;
            try {
                mod=new fomod(modpath);
            } catch(Exception ex) {
                MessageBox.Show("Error loading '"+Path.GetFileName(modpath)+"'\n"+ex.Message);
                return;
            }
            mods.Add(mod);
        }
        public PackageManager() {
            InitializeComponent();
            Settings.GetWindowPosition("PackageManager", this);
            foreach(string modpath in Directory.GetFiles(Program.PackageDir, "*.fomod.zip")) {
                if(!File.Exists(Path.ChangeExtension(modpath, null))) File.Move(modpath, Path.ChangeExtension(modpath, null));
            }

            string[] groups=Settings.GetStringArray("fomodGroups");
            if(groups==null) {
                groups=new string[] {
                    "Items",
                    "Items/Guns",
                    "Items/Armor",
                    "Items/Misc",
                    "Locations",
                    "Locations/Houses",
                    "Locations/Interiors",
                    "Locations/Exteriors",
                    "Gameplay",
                    "Gameplay/Perks",
                    "Gameplay/Realism",
                    "Gameplay/Combat",
                    "Gameplay/Loot",
                    "Gameplay/Enemies",
                    "Quests",
                    "Companions",
                    "ModResource",
                    "UI",
                    "Music",
                    "Replacers",
                    "Replacers/Meshes",
                    "Replacers/Textures",
                    "Replacers/Sounds",
                    "Tweaks",
                    "Fixes",
                    "Cosmetic",
                    "Cosmetic/Races",
                    "Cosmetic/Eyes",
                    "Cosmetic/Hair"
                };
                Settings.SetStringArray("fomodGroups", groups);
            }
            this.groups=new List<string>(groups);
            this.lgroups=new List<string>(groups.Length);
            for(int i=0;i<groups.Length;i++) lgroups.Add(groups[i].ToLowerInvariant());

            if(Settings.GetBool("PackageManagerShowsGroups")) {
                cbGroups.Checked=true;
            }
            foreach(string modpath in Directory.GetFiles(Program.PackageDir, "*.fomod")) {
                AddFomod(modpath);
            }

            RebuildListView();
        }

        private void lvModList_SelectedIndexChanged(object sender, EventArgs e) {
            if(lvModList.SelectedItems.Count==0) return;
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            if(mod.HasInfo) tbModInfo.Text=mod.Description;
            else tbModInfo.Text="Warning: info.xml is missing from this fomod.";

            if(!mod.IsActive) bActivate.Text="Activate";
            else bActivate.Text="Deactivate";

            if(mod.HasScript) bEditScript.Text="Edit script";
            else bEditScript.Text="Create script";

            pictureBox1.Image=mod.GetScreenshot();
        }

        private void lvModList_ItemCheck(object sender, ItemCheckEventArgs e) {
            if(((fomod)lvModList.Items[e.Index].Tag).IsActive) e.NewValue=CheckState.Checked;
            else e.NewValue=CheckState.Unchecked;
        }

        private void bEditScript_Click(object sender, EventArgs e) {
            if(lvModList.SelectedItems.Count!=1) return;
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            string result=TextEditor.ShowEditor(mod.GetScript(), TextEditorType.Script);
            if(result!=null) mod.SetScript(result);
        }

        private void bEditReadme_Click(object sender, EventArgs e) {
            if(lvModList.SelectedItems.Count!=1) return;
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            string result=null;
            if(!mod.HasReadme) {
                result=TextEditor.ShowEditor("", TextEditorType.Text);
            } else {
                string readme=mod.GetReadme();
                switch(mod.ReadmeExt) {
                case ".txt":
                    result=TextEditor.ShowEditor(readme, TextEditorType.Text);
                    break;
                case ".rtf":
                    result=TextEditor.ShowEditor(readme, TextEditorType.Rtf);
                    break;
                case ".htm":
                case ".html":
                    Form f=new Form();
                    WebBrowser wb=new WebBrowser();
                    f.Controls.Add(wb);
                    wb.Dock=DockStyle.Fill;
                    wb.DocumentCompleted+=delegate(object unused1, WebBrowserDocumentCompletedEventArgs unused2)
                    {
                        if(wb.DocumentTitle!=null&&wb.DocumentTitle!="") f.Text=wb.DocumentTitle;
                        else f.Text="Readme";
                    };
                    wb.WebBrowserShortcutsEnabled=false;
                    wb.AllowWebBrowserDrop=false;
                    wb.AllowNavigation=false;
                    wb.DocumentText=readme;
                    f.ShowDialog();
                    break;
                default:
                    MessageBox.Show("fomod had an unrecognised readme type", "Error");
                    return;
                }
            }
           
            if(result!=null) mod.SetReadme(result);
        }

        private void PackageManager_FormClosing(object sender, FormClosingEventArgs e) {
            Settings.SetWindowPosition("PackageManager", this);
            foreach(ListViewItem lvi in lvModList.Items) {
                ((fomod)lvi.Tag).Dispose();
            }
        }

        private void bEditInfo_Click(object sender, EventArgs e) {
            if(lvModList.SelectedItems.Count!=1) return;
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            if((new InfoEditor(mod)).ShowDialog()==DialogResult.OK) {
                if(cbGroups.Checked) ReaddFomodToList(mod);
                else {
                    ListViewItem lvi=lvModList.SelectedItems[0];
                    lvi.SubItems[0].Text=mod.Name;
                    lvi.SubItems[1].Text=mod.VersionS;
                    lvi.SubItems[2].Text=mod.Author;
                    tbModInfo.Text=mod.Description;
                    pictureBox1.Image=mod.GetScreenshot();
                }
            }
            
        }

        private void bActivate_Click(object sender, EventArgs e) {
            if(lvModList.SelectedItems.Count!=1) return;
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            if(!mod.IsActive) mod.Activate();
            else mod.Deactivate();
            lvModList.SelectedItems[0].Checked=mod.IsActive;
            if(!mod.IsActive) bActivate.Text="Activate";
            else bActivate.Text="Deactivate";
        }

        private void bAddNew_Click(object sender, EventArgs e) {
            if(openFileDialog1.ShowDialog()!=DialogResult.OK) return;
            string oldpath=openFileDialog1.FileName, newpath;
            if(oldpath.EndsWith(".fomod", StringComparison.InvariantCultureIgnoreCase)) {
                newpath=Path.Combine(Program.PackageDir, Path.GetFileName(oldpath));
            } else if(oldpath.EndsWith(".fomod.zip", StringComparison.InvariantCultureIgnoreCase)) {
                newpath=Path.Combine(Program.PackageDir, Path.GetFileNameWithoutExtension(oldpath));
            } else if(oldpath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase)) {
                //Insert checks that this is a valid fomod here
                newpath=Path.Combine(Program.PackageDir, Path.ChangeExtension(Path.GetFileName(oldpath), ".fomod"));
            } else {
                MessageBox.Show("Unknown file type", "Error");
                return;
            }
            if(File.Exists(newpath)) {
                MessageBox.Show("A fomod with the same name is already installed", "Error");
                return;
            }
            if(MessageBox.Show("Make a copy of the original file?", "", MessageBoxButtons.YesNo)!=DialogResult.Yes) {
                File.Move(oldpath, newpath);
            } else {
                File.Copy(oldpath, newpath);
            }
            AddFomod(newpath);
        }

        private void fomodContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
            if(lvModList.SelectedItems.Count!=1) {
                e.Cancel=true;
                return;
            }
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            if(mod.email=="") emailAuthorToolStripMenuItem.Visible=false;
            else emailAuthorToolStripMenuItem.Visible=true;
            if(mod.website=="") visitWebsiteToolStripMenuItem.Visible=false;
            else visitWebsiteToolStripMenuItem.Visible=true;
        }

        private void visitWebsiteToolStripMenuItem_Click(object sender, EventArgs e) {
            if(lvModList.SelectedItems.Count!=1) return;
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            System.Diagnostics.Process.Start(mod.website, "");
        }

        private void emailAuthorToolStripMenuItem_Click(object sender, EventArgs e) {
            if(lvModList.SelectedItems.Count!=1) return;
            fomod mod=(fomod)lvModList.SelectedItems[0].Tag;
            System.Diagnostics.Process.Start("mailto://"+mod.email, "");
        }

        private void cbGroups_CheckedChanged(object sender, EventArgs e) {
            RebuildListView();
            Settings.SetBool("PackageManagerShowsGroups", cbGroups.Checked);
        }

        private void bEditGroups_Click(object sender, EventArgs e) {
            Form f=new Form();
            Settings.GetWindowPosition("GroupEditor", f);
            f.Text="Groups";
            TextBox tb=new TextBox();
            f.Controls.Add(tb);
            tb.Dock=DockStyle.Fill;
            tb.AcceptsReturn=true;
            tb.Multiline=true;
            tb.ScrollBars=ScrollBars.Vertical;
            tb.Text=string.Join(Environment.NewLine, groups.ToArray());
            tb.Select(0, 0);
            f.FormClosing+=delegate(object sender2, FormClosingEventArgs args2)
            {
                Settings.SetWindowPosition("GroupEditor", f);
            };
            f.ShowDialog();
            groups.Clear();
            groups.AddRange(tb.Lines);
            for(int i=0;i<groups.Count;i++) {
                if(groups[i]=="") groups.RemoveAt(i--);
            }
            lgroups.Clear();
            for(int i=0;i<groups.Count;i++) lgroups.Add(groups[i].ToLowerInvariant());
            RebuildListView();
            Settings.SetStringArray("fomodGroups", groups.ToArray());
        }
    }
}