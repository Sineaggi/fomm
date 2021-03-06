﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fomm.Games.Fallout3.Tools.AutoSorter
{
  public class LoadOrderSorter
  {
    private struct ModInfo
    {
      public readonly string name;
      public double id;
      public readonly bool hadEntry;

      public ModInfo(string s, double id, bool hadEntry)
      {
        name = s;
        this.id = id;
        this.hadEntry = hadEntry;
      }
    }

    private struct RecordInfo
    {
      public readonly int id;
      public string[] requires;
      public string[] conflicts;
      public string[] comments;

      public RecordInfo(int id)
      {
        this.id = id;
        requires = null;
        conflicts = null;
        comments = null;
      }
    }

    private static readonly string m_strLoadOrderTemplatePath = Path.Combine(Program.GameMode.InstallInfoDirectory,
                                                                             "lotemplate.txt");

    private Dictionary<string, RecordInfo> m_dicMasterList;
    private int duplicateCount;
    private int fileVersion;

    public bool HasMasterList
    {
      get
      {
        return File.Exists(m_strLoadOrderTemplatePath);
      }
    }

    public static string LoadOrderTemplatePath
    {
      get
      {
        return m_strLoadOrderTemplatePath;
      }
    }

    public LoadOrderSorter()
    {
      LoadList();
    }

    /// <summary>
    ///   Loads the master list.
    /// </summary>
    public void LoadList()
    {
      m_dicMasterList = new Dictionary<string, RecordInfo>();
      if (!File.Exists(LoadOrderTemplatePath))
      {
        return;
      }
      var fileLines = File.ReadAllLines(LoadOrderTemplatePath);

      if (!int.TryParse(fileLines[0], out fileVersion))
      {
        fileVersion = 0;
      }
      var upto = 0;
      var requires = new List<string>();
      var conflicts = new List<string>();
      var comments = new List<string>();
      for (var i = 0; i < fileLines.Length; i++)
      {
        var comment = fileLines[i].IndexOf('\\');
        if (comment != -1)
        {
          fileLines[i] = fileLines[i].Remove(comment);
        }
        fileLines[i] = fileLines[i].Trim();
        if (fileLines[i] != string.Empty)
        {
          var ri = new RecordInfo(upto++);
          var skiplines = 0;
          for (var j = i + 1; j < fileLines.Length; j++)
          {
            fileLines[j] = fileLines[j].Trim();
            if (fileLines[j].Length > 0)
            {
              switch (fileLines[j][0])
              {
                case ':':
                  requires.Add(fileLines[j].Substring(1).ToLowerInvariant().Trim());
                  skiplines++;
                  continue;
                case '"':
                  conflicts.Add(fileLines[j].Substring(1).ToLowerInvariant().Trim());
                  skiplines++;
                  continue;
                case '*':
                case '?':
                  comments.Add(fileLines[j].Substring(1).Trim());
                  skiplines++;
                  continue;
              }
              break;
            }
            skiplines++;
          }
          if (requires.Count > 0)
          {
            ri.requires = requires.ToArray();
            requires.Clear();
          }
          if (conflicts.Count > 0)
          {
            ri.conflicts = conflicts.ToArray();
            conflicts.Clear();
          }
          if (comments.Count > 0)
          {
            ri.comments = comments.ToArray();
            comments.Clear();
          }
          fileLines[i] = fileLines[i].ToLowerInvariant();
          if (m_dicMasterList.ContainsKey(fileLines[i]))
          {
            duplicateCount++;
          }
          m_dicMasterList[fileLines[i]] = ri;
          i += skiplines;
        }
      }
    }

    // Returns an array of ModInfo records, in the 'correct'
    // order for the mod.

    private ModInfo[] BuildModInfo(string[] plugins)
    {
      var mi = new ModInfo[plugins.Length];
      var addcount = 1;
      var lastPos = -1;
      var maxPos = 0;
      for (var i = 0; i < mi.Length; i++)
      {
        var lplugin = plugins[i].ToLowerInvariant();
        if (m_dicMasterList.ContainsKey(lplugin))
        {
          lastPos = m_dicMasterList[lplugin].id;
          if (lastPos > maxPos)
          {
            maxPos = lastPos;
          }
          mi[i] = new ModInfo(plugins[i], lastPos, true);
          addcount = 1;
        }
        else
        {
          mi[i] = new ModInfo(plugins[i], lastPos + addcount*0.001, false);
          addcount++;
        }
      }
      addcount = 1;
      maxPos++;
      for (var i = mi.Length - 1; i >= 0; i--)
      {
        if (mi[i].hadEntry)
        {
          break;
        }
        mi[i].id = maxPos - addcount*0.001;
        addcount++;
      }
      return mi;
    }

    public string GenerateReport(string[] plugins, bool[] active, bool[] corrupt, string[][] masters)
    {
      var sb = new StringBuilder(plugins.Length*32);
      var lplugins = new string[plugins.Length];
      for (var i = 0; i < plugins.Length; i++)
      {
        lplugins[i] = plugins[i].ToLowerInvariant();
      }
      double latestPosition = 0;
      sb.AppendLine("Mod load order report");
      if (duplicateCount > 0)
      {
        sb.AppendLine("! Warning: current load order template contains " + duplicateCount +
                      " duplicate entries. This warning can be ignored.");
      }
      sb.AppendLine();
      var LoadOrderWrong = false;
      for (var i = 0; i < plugins.Length; i++)
      {
        sb.AppendLine(plugins[i] + (active[i] ? string.Empty : " (Inactive)"));
        plugins[i] = plugins[i].ToLowerInvariant();
        if (corrupt[i])
        {
          sb.AppendLine("! This plugin is unreadable, and probably corrupt");
        }
        if (active[i] && masters[i] != null)
        {
          for (var k = 0; k < masters[i].Length; k++)
          {
            var found = false;
            for (var j = 0; j < i; j++)
            {
              if (active[j] && lplugins[j] == masters[i][k])
              {
                found = true;
                break;
              }
            }
            if (!found)
            {
              for (var j = i + 1; j < plugins.Length; j++)
              {
                if (active[j] && lplugins[j] == masters[i][k])
                {
                  sb.AppendLine("! This plugin depends on master '" + masters[i][k] +
                                "', which is loading after it in the load order");
                  found = true;
                  break;
                }
              }
              if (!found)
              {
                sb.AppendLine("! This plugin depends on master '" + masters[i][k] + "', which is not loading");
              }
            }
          }
        }
        if (m_dicMasterList.ContainsKey(plugins[i]))
        {
          var ri = m_dicMasterList[plugins[i]];
          if (ri.id < latestPosition)
          {
            sb.AppendLine("* The current load order of this mod does not match the current template");
            LoadOrderWrong = true;
          }
          else
          {
            latestPosition = ri.id;
          }
          if (active[i] && ri.requires != null)
          {
            foreach (var r in ri.requires)
            {
              var found = false;
              for (var j = 0; j < lplugins.Length; j++)
              {
                if (lplugins[j] == r)
                {
                  if (active[j])
                  {
                    found = true;
                  }
                  break;
                }
              }
              if (!found)
              {
                sb.AppendLine("! This plugin requires '" + r + "', which was not found");
              }
            }
          }
          if (active[i] && ri.conflicts != null)
          {
            foreach (var conflict in ri.conflicts)
            {
              for (var j = 0; j < lplugins.Length; j++)
              {
                if (lplugins[j] == conflict)
                {
                  if (active[j])
                  {
                    sb.AppendLine("! This plugin conflicts with '" + conflict + "'");
                  }
                  break;
                }
              }
            }
          }
          if (ri.comments != null)
          {
            foreach (var comment in ri.comments)
            {
              sb.AppendLine("  " + comment);
            }
          }
        }
        else
        {
          sb.AppendLine(
            "* The auto-sorter doesn't recognize this mod. It is probably safe to put it anywhere, depending on how you want the various plugins to override one another.");
        }
        sb.AppendLine();
      }
      if (LoadOrderWrong)
      {
        var dup = (string[]) plugins.Clone();
        SortList(dup);
        sb.AppendLine("The order that the current template suggests is as follows:");
        foreach (var ds in dup)
        {
          sb.AppendLine(ds);
        }
      }
      return sb.ToString();
    }

    public void SortList(string[] plugins)
    {
      var mi = BuildModInfo(plugins);
      Array.Sort(mi, delegate(ModInfo a, ModInfo b)
      {
        return a.id.CompareTo(b.id);
      });
      for (var i = 0; i < mi.Length; i++)
      {
        plugins[i] = mi[i].name;
      }
    }

    /// <summary>
    ///   Determins if the given list of plugins has been auto-sorted.
    /// </summary>
    /// <param name="plugins">The plugins whose order is to be verified.</param>
    /// <returns>
    ///   <lang langref="true" /> if the plugins have been auto-sorted;
    ///   <lang langref="false" /> otherwise.
    /// </returns>
    public bool CheckList(string[] plugins)
    {
      if (!HasMasterList)
      {
        return false;
      }
      var mi = BuildModInfo(plugins);
      double upto = 0;
      for (var i = 0; i < mi.Length; i++)
      {
        if (mi[i].id < upto)
        {
          return false;
        }
        upto = mi[i].id;
      }
      return true;
    }

    public int GetInsertionPos(string[] plugins, string plugin)
    {
      plugin = plugin.ToLowerInvariant();
      if (!m_dicMasterList.ContainsKey(plugin))
      {
        return plugins.Length;
      }
      var mi = BuildModInfo(plugins);
      var target = m_dicMasterList[plugin].id;
      for (var i = 0; i < mi.Length; i++)
      {
        if (mi[i].id >= target)
        {
          return i;
        }
      }
      return plugins.Length;
    }

    public int GetFileVersion()
    {
      return fileVersion;
    }
  }
}