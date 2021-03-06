// FastZip.cs
//
// Copyright 2005 John Reilly
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// Linking this library statically or dynamically with other modules is
// making a combined work based on this library.  Thus, the terms and
// conditions of the GNU General Public License cover the whole
// combination.
// 
// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module.  An independent module is a module which is not derived from
// or based on this library.  If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so.  If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.IO;
using Fomm.SharpZipLib.Core;

namespace Fomm.SharpZipLib.Zip
{
  /// <summary>
  ///   Describes the arguments for the <see cref="FastZip.EntryExtracted" /> event.
  /// </summary>
  public class ZipEventArgs : EventArgs
  {
    #region Properties

    /// <summary>
    ///   Gets the name of the entry that was operated on.
    /// </summary>
    /// <value>The name of the entry that was operated on.</value>
    public string Entry { get; protected set; }

    /// <summary>
    ///   Gets or sets whether the zip operation should be cancelled.
    /// </summary>
    /// <value>Whether the zip operation should be cancelled.</value>
    public bool Cancel { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    ///   A simple constructor that initializes the object with the given values.
    /// </summary>
    /// <param name="p_strEntry">The name of the entry that was operated on.</param>
    public ZipEventArgs(string p_strEntry)
    {
      Entry = p_strEntry;
      Cancel = false;
    }

    #endregion
  }

  /// <summary>
  ///   FastZip provides facilities for creating and extracting zip files.
  /// </summary>
  internal class FastZip
  {
    #region Enumerations

    #endregion

    #region Events

    /// <summary>
    ///   Raised when a <see cref="ZipEntry" /> has been extracted.
    /// </summary>
    public event EventHandler<ZipEventArgs> EntryExtracted;

    //public event EventHandler<ZipEventArgs> EntryAdded;
    /// <summary>
    ///   Raised when a <see cref="ZipEntry" /> has been added to an archive.
    /// </summary>
    /// <summary>
    ///   Raises the <see cref="EntryExtracted" /> event.
    /// </summary>
    /// <param name="p_zeEntry">The <see cref="ZipEntry" /> that was extracted.</param>
    protected void OnEntryExtracted(ZipEntry p_zeEntry)
    {
      if (EntryExtracted != null)
      {
        var zeaArgs = new ZipEventArgs(p_zeEntry.Name);
        EntryExtracted(this, zeaArgs);
        continueRunning_ &= !zeaArgs.Cancel;
      }
    }

    /*protected void OnEntryAdded(ZipEntry p_zeEntry)
    {
      if (EntryAdded != null)
      {
        ZipEventArgs zeaArgs = new ZipEventArgs(p_zeEntry.Name);
        EntryAdded(this, zeaArgs);
        continueRunning_ &= !zeaArgs.Cancel;
      }
    }*/

    #endregion

    #region CreateZip

    /// <summary>
    ///   Raises the <see cref="EntryAdded" /> event.
    /// </summary>
    /// <param name="p_zeEntry">The <see cref="ZipEntry" /> that was added.</param>
    /// <summary>
    ///   Create a zip file/archive.
    /// </summary>
    /// <param name="zipFileName">The name of the zip file to create.</param>
    /// <param name="sourceDirectory">The directory to obtain files and directories from.</param>
    /// <param name="recurse">True to recurse directories, false for no recursion.</param>
    /// <param name="fileFilter">The file filter to apply.</param>
    public void CreateZip(string zipFileName, string sourceDirectory, bool recurse, string fileFilter)
    {
      CreateZip(File.Create(zipFileName), sourceDirectory, recurse, fileFilter, null);
    }

    /// <summary>
    ///   Create a zip archive sending output to the <paramref name="outputStream" /> passed.
    /// </summary>
    /// <param name="outputStream">The stream to write archive data to.</param>
    /// <param name="sourceDirectory">The directory to source files from.</param>
    /// <param name="recurse">True to recurse directories, false for no recursion.</param>
    /// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
    /// <param name="directoryFilter">The <see cref="PathFilter">directory filter</see> to apply.</param>
    public void CreateZip(Stream outputStream, string sourceDirectory, bool recurse, string fileFilter,
                          string directoryFilter)
    {
      entryFactory_.NameTransform = new ZipNameTransform(sourceDirectory);
      using (outputStream_ = new ZipOutputStream(outputStream))
      {
        var scanner = new FileSystemScanner(fileFilter, directoryFilter);
        scanner.ProcessFile += ProcessFile;

        scanner.Scan(sourceDirectory, recurse);
      }
    }

    #endregion

    #region ExtractZip

    /// <summary>
    ///   Extract the contents of a zip file.
    /// </summary>
    /// <param name="zipFileName">The zip file to extract from.</param>
    /// <param name="targetDirectory">The directory to save extracted information in.</param>
    /// <param name="fileFilter">A filter to apply to files.</param>
    public void ExtractZip(string zipFileName, string targetDirectory, string fileFilter)
    {
      ExtractZip(zipFileName, targetDirectory, fileFilter, null);
    }

    /// <summary>
    ///   Extract the contents of a zip file.
    /// </summary>
    /// <param name="zipFileName">The zip file to extract from.</param>
    /// <param name="targetDirectory">The directory to save extracted information in.</param>
    /// <param name="overwrite">The style of <see cref="Overwrite">overwriting</see> to apply.</param>
    /// <param name="confirmDelegate">A delegate to invoke when confirming overwriting.</param>
    /// <param name="fileFilter">A filter to apply to files.</param>
    /// <param name="directoryFilter">A filter to apply to directories.</param>
    /// <param name="restoreDateTime">Flag indicating wether to restore the date and time for extracted files.</param>
    public void ExtractZip(string zipFileName, string targetDirectory, string fileFilter, string directoryFilter)
    {
      continueRunning_ = true;
      extractNameTransform_ = new WindowsNameTransform(targetDirectory);

      fileFilter_ = new NameFilter(fileFilter);
      directoryFilter_ = new NameFilter(directoryFilter);

      using (zipFile_ = new ZipFile(zipFileName))
      {
        var enumerator = zipFile_.GetEnumerator();
        while (continueRunning_ && enumerator.MoveNext())
        {
          var entry = (ZipEntry) enumerator.Current;
          if (entry.IsFile)
          {
            // TODO Path.GetDirectory can fail here on invalid characters.
            if (directoryFilter_.IsMatch(Path.GetDirectoryName(entry.Name)) && fileFilter_.IsMatch(entry.Name))
            {
              ExtractEntry(entry);
            }
          }
        }
      }
    }

    #endregion

    #region Internal Processing

    private void ProcessFile(object sender, ScanEventArgs e)
    {
      if (e.ContinueRunning)
      {
        using (var stream = File.OpenRead(e.Name))
        {
          var entry = entryFactory_.MakeFileEntry(e.Name);
          outputStream_.PutNextEntry(entry);
          AddFileContents(stream);
        }
      }
    }

    private void AddFileContents(Stream stream)
    {
      if (stream == null)
      {
        throw new ArgumentNullException("stream");
      }

      if (buffer_ == null)
      {
        buffer_ = new byte[4096];
      }

      StreamUtils.Copy(stream, outputStream_, buffer_);
    }

    private void ExtractFileEntry(ZipEntry entry, string targetName)
    {
      if (continueRunning_)
      {
        try
        {
          using (var outputStream = File.Create(targetName))
          {
            if (buffer_ == null)
            {
              buffer_ = new byte[4096];
            }
            StreamUtils.Copy(zipFile_.GetInputStream(entry), outputStream, buffer_);
          }
          File.SetLastWriteTime(targetName, entry.DateTime);
        }
        catch (Exception)
        {
          continueRunning_ = false;
          throw;
        }
      }
    }

    private void ExtractEntry(ZipEntry entry)
    {
      var doExtraction = entry.IsCompressionMethodSupported();
      var targetName = entry.Name;

      if (doExtraction)
      {
        if (entry.IsFile)
        {
          targetName = extractNameTransform_.TransformFile(targetName);
        }
        else if (entry.IsDirectory)
        {
          targetName = extractNameTransform_.TransformDirectory(targetName);
        }

        doExtraction = !string.IsNullOrEmpty(targetName);
      }

      // TODO: Fire delegate/throw exception were compression method not supported, or name is invalid?

      string dirName = null;

      if (doExtraction)
      {
        dirName = entry.IsDirectory ? targetName : Path.GetDirectoryName(Path.GetFullPath(targetName));
      }

      if (doExtraction && !Directory.Exists(dirName))
      {
        if (!entry.IsDirectory)
        {
          try
          {
            Directory.CreateDirectory(dirName);
          }
          catch (Exception)
          {
            continueRunning_ = false;
            throw;
          }
        }
      }

      if (doExtraction && entry.IsFile)
      {
        ExtractFileEntry(entry, targetName);
      }
      OnEntryExtracted(entry);
    }

    #endregion

    #region Instance Fields

    private bool continueRunning_;
    private byte[] buffer_;
    private ZipOutputStream outputStream_;
    private ZipFile zipFile_;
    private NameFilter fileFilter_;
    private NameFilter directoryFilter_;

    private IEntryFactory entryFactory_ = new ZipEntryFactory();
    private INameTransform extractNameTransform_;

    #endregion
  }
}